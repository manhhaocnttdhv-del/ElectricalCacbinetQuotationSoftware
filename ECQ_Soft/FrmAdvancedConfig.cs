using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using ECQ_Soft.Model;
using Color = System.Drawing.Color;
using System.ComponentModel;

namespace ECQ_Soft
{
    public partial class FrmAdvancedConfig : Form
    {
        private List<HierarchyNode> _rootNodes = new List<HierarchyNode>();
        private SheetsService _service;
        private string _spreadsheetId;
        
        // Danh sách sản phẩm để hỗ trợ tính năng search trong expand panel
        private List<Products> _allProducts = new List<Products>();
        // Danh sách sản phẩm hiện đang được lọc khi search
        private List<Products> _searchResults = new List<Products>();
        // Node đang được mở expand panel
        private HierarchyNode _expandedNode = null;
        
        public string SelectedHeader { get; private set; }
        public List<string> SelectedComponents { get; private set; } = new List<string>();

        public FrmAdvancedConfig()
        {
            InitializeComponent();
            SetupEvents();
            this.Resize += (s, e) => RecalculateLayout();
            this.Load += (s, e) =>
            {
                // Luôn mở form ở 80% kích thước màn hình, căn giữa
                var screen = Screen.FromControl(this).WorkingArea;
                int w = (int)(screen.Width  * 0.80);
                int h = (int)(screen.Height * 0.80);
                this.Size = new Size(w, h);
                this.Location = new Point(
                    screen.Left + (screen.Width  - w) / 2,
                    screen.Top  + (screen.Height - h) / 2
                );
            };
        }

        public async Task LoadDataAsync(SheetsService service, string spreadsheetId)
        {
            _service = service;
            _spreadsheetId = spreadsheetId;
            
            try
            {
                // Tải song song: Workflow + Products
                var workflowTask = _service.Spreadsheets.Values.Get(_spreadsheetId, "Workflow!A2:Z").ExecuteAsync();
                var productsTask = _service.Spreadsheets.Values.Get(_spreadsheetId, "Products_Table!A2:M").ExecuteAsync();
                
                await Task.WhenAll(workflowTask, productsTask);
                
                var values = workflowTask.Result.Values;
                if (values == null || values.Count == 0) return;

                BuildTreeFromRows(values);
                
                // Nạp sản phẩm vào bộ nhớ để hỗ trợ search
                _allProducts.Clear();
                var pRows = productsTask.Result.Values;
                if (pRows != null)
                {
                    for (int i = 0; i < pRows.Count; i++)
                    {
                        var row = pRows[i];
                        if (row.Count < 2) continue;
                        _allProducts.Add(new Products
                        {
                            Id = (row.Count > 0 && int.TryParse(row[0]?.ToString(), out int id)) ? id : i + 1,
                            Name = row.Count > 1 ? row[1]?.ToString() : "",
                            Model = row.Count > 2 ? row[2]?.ToString() : "",
                            SKU = row.Count > 3 ? row[3]?.ToString() : "",
                            Price = row.Count > 4 ? row[4]?.ToString() : "0",
                            PriceCost = row.Count > 5 ? row[5]?.ToString() : "0",
                            Weight = row.Count > 6 ? row[6]?.ToString() : "0",
                            Length = row.Count > 7 ? row[7]?.ToString() : "0",
                            Width = row.Count > 8 ? row[8]?.ToString() : "0",
                            Height = row.Count > 9 ? row[9]?.ToString() : "0",
                            Category = row.Count > 10 ? row[10]?.ToString() : "",
                            HÃNG = row.Count > 11 ? row[11]?.ToString() : "",
                            PriceList = row.Count > 12 ? row[12]?.ToString() : ""
                        });
                    }
                }

                LoadInitialLevel();
            }
            catch (Exception ex)
            {
                // Nếu Products_Table fail thì vẫn load Workflow
                try
                {
                    var response = await _service.Spreadsheets.Values.Get(_spreadsheetId, "Workflow!A2:Z").ExecuteAsync();
                    var values = response.Values;
                    if (values != null && values.Count > 0)
                    {
                        BuildTreeFromRows(values);
                        LoadInitialLevel();
                    }
                }
                catch (Exception ex2)
                {
                    MessageBox.Show("Lỗi tải dữ liệu Workflow: " + ex2.Message);
                }
            }
        }

        private void BuildTreeFromRows(IList<IList<object>> rows)
        {
            _rootNodes.Clear();
            var allNodes = new Dictionary<string, HierarchyNode>();
            var dataRows = rows.ToList();
            
            if (dataRows.Count < 2) return;

            // --- TÌM CHỈ SỐ CỘT ĐỘNG TỪ DÒNG HEADER (Dòng 0) ---
            int colId = -1, colName = -1, colIdMe = -1, colProcess = -1, colFormula = -1, colConfig = -1;
            var headerRow = dataRows[0];
            for (int i = 0; i < headerRow.Count; i++)
            {
                string header = headerRow[i]?.ToString()?.Trim()?.ToLower() ?? "";
                if (header == "id") colId = i;
                else if (header == "name") colName = i;
                else if (header == "id_mẹ" || header == "id_me") colIdMe = i;
                else if (header == "công thức" || header == "cong thuc") colFormula = i;
                else if (header == "process flow" || header.Contains("process flow")) colProcess = i;
                else if (header == "config") colConfig = i;  // ← cột mới
            }
            
            // Fallback nếu không xác định được (đề phòng Data thiếu Header hoặc Header gõ khác)
            if (colId == -1) colId = 1;         // Mặc định là Cột B
            if (colName == -1) colName = 2;       // Mặc định là Cột C
            if (colIdMe == -1) colIdMe = 4;       // === ĐÂY LÀ ĐIỂM QUAN TRỌNG: CỘT E THAY VÌ CỘT D ===
            if (colProcess == -1) colProcess = 5; // Cột F
            if (colFormula == -1) colFormula = 6; // Cột G
            // colConfig: nếu không tìm thấy header -> fallback là cột cuối cùng của header row
            if (colConfig == -1) colConfig = headerRow.Count - 1;

            // Nếu người dùng thực sự thiết kế Id_Mẹ ở Cột D thì vòng lặp for Header ở trên sẽ gán lại đúng colIdMe = 3.

            var pendingChildren = new List<Tuple<HierarchyNode, string[]>>();

            // BƯỚC 1: Khởi tạo tất cả các Node (Bỏ qua dòng Header)
            for (int r = 1; r < dataRows.Count; r++)
            {
                var row = dataRows[r];
                string id = row.Count > colId ? row[colId]?.ToString()?.Trim() : "";
                string name = row.Count > colName ? row[colName]?.ToString()?.Trim() : "";
                
                if (!string.IsNullOrEmpty(name))
                {
                    var node = new HierarchyNode(name);
                    
                    // Đọc giá trị cột Config
                    string configVal = (colConfig >= 0 && row.Count > colConfig)
                        ? row[colConfig]?.ToString()?.Trim() ?? ""
                        : "";
                    node.Config = configVal;

                    // Đọc công thức (Công thức column)
                    string formulaVal = (colFormula >= 0 && row.Count > colFormula)
                        ? row[colFormula]?.ToString()?.Trim() ?? ""
                        : "";
                    node.Formula = formulaVal;

                    
                    if (!string.IsNullOrEmpty(id) && !allNodes.ContainsKey(id))
                    {
                        allNodes[id] = node;
                    }
                    
                    string idMeRaw = row.Count > colIdMe ? row[colIdMe]?.ToString()?.Trim() : "0";
                    if (string.IsNullOrEmpty(idMeRaw)) idMeRaw = "0";
                    
                    string[] idMes = idMeRaw.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (idMes.Length == 0) idMes = new[] { "0" };

                    pendingChildren.Add(new Tuple<HierarchyNode, string[]>(node, idMes));
                }
            }

            // BƯỚC 2: Ráp thành cây
            foreach (var item in pendingChildren)
            {
                var node = item.Item1;
                var idMes = item.Item2;

                foreach (var pid in idMes)
                {
                    string parentId = pid.Trim();
                    
                    // Tuyệt đối chỉ bắt "0", không bắt khoảng trắng nữa để tránh nhầm
                    if (parentId == "0")
                    {
                        if (!_rootNodes.Contains(node)) _rootNodes.Add(node);
                    }
                    else if (allNodes.ContainsKey(parentId))
                    {
                        if (!allNodes[parentId].Children.Contains(node))
                        {
                            allNodes[parentId].Children.Add(node);
                        }
                    }
                }
            }

            // BƯỚC 3: Cập nhật Components
            for (int r = 1; r < dataRows.Count; r++)
            {
                var row = dataRows[r];
                string id = row.Count > colId ? row[colId]?.ToString()?.Trim() : "";
                string name = row.Count > colName ? row[colName]?.ToString()?.Trim() : "";
                string idMeRaw = row.Count > colIdMe ? row[colIdMe]?.ToString()?.Trim() : "";
                string processFlow = row.Count > colProcess ? row[colProcess]?.ToString()?.Trim() : "";
                string congThuc = row.Count > colFormula ? row[colFormula]?.ToString()?.Trim() : "";
                
                string[] idMes = idMeRaw.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (string.IsNullOrEmpty(name) && idMes.Length > 0)
                {
                    foreach (var pid in idMes)
                    {
                        string parentId = pid.Trim();
                        if (allNodes.ContainsKey(parentId))
                        {
                            if (!string.IsNullOrEmpty(processFlow) && !allNodes[parentId].Components.Contains(processFlow))
                                allNodes[parentId].Components.Add(processFlow);

                            if (!string.IsNullOrEmpty(congThuc) && !congThuc.StartsWith("=") && !allNodes[parentId].Components.Contains(congThuc))
                                allNodes[parentId].Components.Add(congThuc);
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(name))
                {
                    HierarchyNode targetNode = null;
                    if (!string.IsNullOrEmpty(id) && allNodes.ContainsKey(id)) 
                    {
                        targetNode = allNodes[id];
                    } 
                    else 
                    {
                        var match = pendingChildren.FirstOrDefault(x => x.Item1.Name == name && string.Join(";", x.Item2) == string.Join(";", idMes));
                        if (match != null) targetNode = match.Item1;
                    }

                    if (targetNode != null)
                    {
                        if (!string.IsNullOrEmpty(processFlow) && !targetNode.Components.Contains(processFlow))
                            targetNode.Components.Add(processFlow);
                            
                        if (!string.IsNullOrEmpty(congThuc) && !congThuc.StartsWith("=") && !targetNode.Components.Contains(congThuc))
                            targetNode.Components.Add(congThuc);
                    }
                }
            }
            
        }



        private ModernTreeView _modernTreeView;
        // ── Expand Panel (Search sản phẩm) ──
        private Panel _expandPanel;
        private TextBox _txtSearch;
        private Button _btnSearch;
        private Button _btnExpandToggle;  // nút mở rộng trên header panel
        private DataGridView _dgvSearchResults;
        private Label _lblExpandTitle;
        private Label _lblProductInfo;     // hiển thị thông tin sản phẩm đã chọn
        private bool _expandPanelVisible = false;

        private void SetupEvents()
        {
            // Nút XáC NHẬN -> trả danh sách sản phẩm đã chọn
            btnApply.Click += (s, e) => {
                // Thu thập tất cả các dòng trong grid
                SelectedComponents = new List<string>();
                SelectedHeader = "";
                foreach (DataGridViewRow row in dgvSelectedItems.Rows)
                {
                    var tenCfg = row.Cells["colTen"].Value?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(tenCfg))
                    {
                        if (string.IsNullOrEmpty(SelectedHeader)) SelectedHeader = tenCfg;
                        SelectedComponents.Add(tenCfg);
                    }
                }
                if (SelectedComponents.Count > 0)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Vui làng thêm ít nhất 1 cấu hình vào danh sách!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            // Nút THÊM VÀO DANH SÁCH
            btnAddToGrid.Click += (s, e) => {
                if (_modernTreeView?.SelectedNode?.Tag is HierarchyNode node)
                {
                    // Kiểm tra đã có trong danh sách chưa
                    foreach (DataGridViewRow existing in dgvSelectedItems.Rows)
                    {
                        if (existing.Cells["colTen"].Value?.ToString() == node.Name)
                        {
                            MessageBox.Show($"Đã có [{node.Name}] trong danh sách rồi!", "Trùng", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                    int idx = dgvSelectedItems.Rows.Add();
                    dgvSelectedItems.Rows[idx].Cells["colTen"].Value = node.Name;
                    dgvSelectedItems.Rows[idx].Cells["colSoLuong"].Value = "1";
                    dgvSelectedItems.Rows[idx].Cells["colGhiChu"].Value = "";
                    // Lưu HierarchyNode vào Tag của Row để dùng sau
                    dgvSelectedItems.Rows[idx].Tag = node;
                    btnApply.Enabled = true;
                }
            };

            // Nút XÓA trong DataGridView
            dgvSelectedItems.CellClick += (s, e) => {
                if (e.ColumnIndex == dgvSelectedItems.Columns["colXoa"].Index && e.RowIndex >= 0)
                {
                    dgvSelectedItems.Rows.RemoveAt(e.RowIndex);
                    btnApply.Enabled = dgvSelectedItems.Rows.Count > 0;
                }
            };

            btnCancel.Click += (s, e) => this.Close();
        }

        /// <summary>
        /// Reload: tải lại toàn bộ dữ liệu từ Google Sheets.
        /// </summary>
        private async void BtnReload_Click(object sender, EventArgs e)
        {
            btnReload.Enabled = false;
            btnReload.Text = "⟳ Đang tải...";

            // Reset trạng thái
            _allProducts.Clear();
            _rootNodes.Clear();
            _expandedNode = null;
            HideExpandPanel();

            try
            {
                await LoadDataAsync(_service, _spreadsheetId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải lại dữ liệu: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnReload.Text = "⟳ Tải lại";
                btnReload.Enabled = true;
            }
        }


        private void LoadInitialLevel()
        {
            // Tắt FlowLayoutPanel cũ vì nó bóp méo Size
            pnlStepsContainer.Visible = false;

            if (_modernTreeView != null)
            {
                this.Controls.Remove(_modernTreeView);
                _modernTreeView.Dispose();
            }

            // Khởi tạo TreeView được căn chỉnh theo pnlStepsContainer
            _modernTreeView = new ModernTreeView();
            _modernTreeView.Location = new Point(pnlStepsContainer.Left, pnlStepsContainer.Top);
            _modernTreeView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _modernTreeView.AfterSelect += (s, e) => {
                btnAddToGrid.Enabled = _modernTreeView.SelectedNode != null;
                // Kiểm tra node có Components không → hiện/ẩn nút expand
                OnTreeNodeSelected(e.Node);
            };

            this.Controls.Add(_modernTreeView);
            _modernTreeView.BringToFront();

            // Khởi tạo Expand Panel
            InitExpandPanel();
            
            PopulateTree();
            RecalculateLayout();
        }

        /// <summary>
        /// Khởi tạo panel mở rộng để search sản phẩm.
        /// </summary>
        private void InitExpandPanel()
        {
            if (_expandPanel != null) { this.Controls.Remove(_expandPanel); _expandPanel.Dispose(); }

            // ── Nút toggle (hiện/ẩn panel) ──
            _btnExpandToggle = new Button
            {
                Text = "▶  Nhập giá trị",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Height = 34,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new System.Windows.Forms.Padding(8, 0, 0, 0),
                BackColor = Color.FromArgb(230, 240, 255),
                ForeColor = Color.FromArgb(30, 80, 180),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnExpandToggle.FlatAppearance.BorderColor = Color.FromArgb(180, 210, 255);
            _btnExpandToggle.FlatAppearance.BorderSize = 1;
            _btnExpandToggle.Click += (s, e) => ToggleExpandPanel();
            this.Controls.Add(_btnExpandToggle);
            _btnExpandToggle.BringToFront();

            // ── Shell panel (nội dung sẽ được tạo động trong ShowExpandPanel) ──
            _expandPanel = new Panel
            {
                BackColor = Color.FromArgb(248, 252, 255),
                BorderStyle = BorderStyle.None,
                Visible = false,
                Padding = new System.Windows.Forms.Padding(0)
            };
            this.Controls.Add(_expandPanel);
            _expandPanel.BringToFront();
        }

        /// <summary>
        /// Xử lý các loại Config: SEARCH-PRODUCT, SEARCH-CATEGORY, TEXT.
        /// </summary>
        private Products _selectedProduct = null;
        private Dictionary<string, TextBox> _dynamicTextBoxes = new Dictionary<string, TextBox>();
        private Panel _pnlPhase2 = null;
        private Label _lblSelectedProductPhase2 = null;

        /// <summary>
        /// Xây dựng nội dung bên trong expand panel theo loại Config.
        /// </summary>
        private void BuildExpandContent(string configRaw)
        {
            // Xóa nội dung cũ
            _expandPanel.Controls.Clear();
            _txtSearch = null; _btnSearch = null; _lblExpandTitle = null; _lblProductInfo = null; _dgvSearchResults = null;
            _selectedProduct = null;
            _dynamicTextBoxes.Clear();
            _pnlPhase2 = null;
            _lblSelectedProductPhase2 = null;

            if (string.IsNullOrWhiteSpace(configRaw)) configRaw = "TEXT";
            string configVal = _expandedNode?.Config ?? "";
            string formula   = _expandedNode?.Formula ?? "";
            
            // Phân tích config theo first/last hoặc dấu phẩy
            string cLow = configRaw.ToLowerInvariant();
            bool hasSearchProduct = cLow.Contains("search_product") || cLow.Contains("search-product");
            bool hasSearchCategory = cLow.Contains("search_category") || cLow.Contains("search-category");
            bool requireSearch = hasSearchProduct || hasSearchCategory;

            // Tìm các trường text (chứa "-text" hoặc bằng "text")
            var rawParts = configRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
            var textFields = new List<string>();
            var _fieldNameCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in rawParts)
            {
                string pLow = part.ToLower();
                if (pLow.StartsWith("first:")) pLow = pLow.Substring(6).Trim();
                if (pLow.StartsWith("last:")) pLow = pLow.Substring(5).Trim();
                
                string fieldBase = null;
                if (pLow.EndsWith("-text")) fieldBase = pLow.Replace("-text", "");
                else if (pLow == "text") fieldBase = "Giá trị";
                
                if (fieldBase != null)
                {
                    // Đánh số tự động nếu tên trùng: height, height2, height3...
                    if (!_fieldNameCount.ContainsKey(fieldBase))
                    {
                        _fieldNameCount[fieldBase] = 1;
                        textFields.Add(fieldBase);
                    }
                    else
                    {
                        _fieldNameCount[fieldBase]++;
                        textFields.Add(fieldBase + _fieldNameCount[fieldBase].ToString());
                    }
                }
            }
            if (!requireSearch && textFields.Count == 0) textFields.Add("Giá trị");

            int yOffset = 6;
            var pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new System.Windows.Forms.Padding(10, 8, 10, 8), BackColor = Color.FromArgb(248, 252, 255), AutoScroll = true };

            // ================== PHASE 1: SEARCH ==================
            if (requireSearch)
            {
                var pnlPhase1 = new Panel { Width = 920, AutoSize = true, Location = new Point(0, 0) };
                
                string titleIcon = hasSearchCategory ? "📂" : "🔍";
                string titleText = hasSearchCategory ? $"Lọc danh mục và chọn sản phẩm  [{configVal}]" : $"Tìm kiếm sản phẩm  [{configVal}]";
                if (!string.IsNullOrEmpty(formula)) titleText += $"   (CT: {formula})";
                _lblExpandTitle = new Label { Text = $"{titleIcon}  {titleText}", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 100, 210), AutoSize = true, Location = new Point(10, yOffset) };
                pnlPhase1.Controls.Add(_lblExpandTitle);
                yOffset += 28;

                // Khởi tạo ProductSearch
                var cboProductSearch = new Helper.ProductSearchDropdown { Location = new Point(10, yOffset), Width = 450, Font = new Font("Segoe UI", 10.5f) };
                
                cboProductSearch.Text = "Gõ tên, SKU hoặc model sản phẩm...";
                cboProductSearch.Enter += (s, e) => { if (cboProductSearch.Text == "Gõ tên, SKU hoặc model sản phẩm...") cboProductSearch.Text = ""; };
                cboProductSearch.LoadData(_allProducts);
                
                cboProductSearch.ProductSelected += (sender, p) => {
                    _selectedProduct = p;
                    if (_pnlPhase2 != null)
                    {
                        _pnlPhase2.Visible = true;
                        if (_lblSelectedProductPhase2 != null)
                        {
                            _lblSelectedProductPhase2.Text = $"✅ Đã chọn SP: {p.Name} (Giá: {p.Price})";
                            _lblSelectedProductPhase2.ForeColor = Color.FromArgb(0, 120, 60);
                        }
                        
                        // Focus vào ô nhập liệu đầu tiên nếu có
                        var firstTxt = _dynamicTextBoxes.Values.FirstOrDefault();
                        if (firstTxt != null && firstTxt.Visible) firstTxt.Focus();
                    }
                };

                // Lắp ráp Category Dropdown nếu cần Search Category
                if (hasSearchCategory)
                {
                    var cboCategory = new Helper.CategorySearchDropdown { Location = new Point(10, yOffset), Width = 300, Font = new Font("Segoe UI", 10.5f) };
                    cboCategory.Text = "Gõ tìm danh mục...";
                    cboCategory.Enter += (s, e) => { if (cboCategory.Text == "Gõ tìm danh mục...") cboCategory.Text = ""; };
                    
                    var categoryStrings = _allProducts.Select(p => p.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                    cboCategory.LoadData(categoryStrings);
                    
                    cboProductSearch.Location = new Point(325, yOffset);
                    cboProductSearch.Width = 500;

                    cboCategory.SelectionChanged += (sender, catStr) => {
                        if (string.IsNullOrEmpty(catStr) || catStr.StartsWith("--"))
                            cboProductSearch.LoadData(_allProducts);
                        else
                        {
                            var filtered = _allProducts.Where(p => string.Equals((p.Category ?? "").Trim(), catStr.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                            cboProductSearch.LoadData(filtered);
                        }
                        cboProductSearch.Text = "";    
                        cboProductSearch.Focus();
                    };
                    pnlPhase1.Controls.Add(cboCategory);
                }

                pnlPhase1.Controls.Add(cboProductSearch);
                yOffset += 40;
                
                _lblProductInfo = new Label { Text = "📝 Gõ chữ để tìm và nhấn Enter / Nhấp đúp vào sản phẩm hiện ra ở danh sách thả xuống.", Font = new Font("Segoe UI", 9f, FontStyle.Italic), ForeColor = Color.Gray, AutoSize = true, Location = new Point(hasSearchCategory ? 325 : 10, yOffset) };
                pnlPhase1.Controls.Add(_lblProductInfo);
                yOffset += 24;

                pnlPhase1.Height = yOffset + 10;
                pnlMain.Controls.Add(pnlPhase1);
                
                yOffset += 10; // Đệm trước Phase 2
            }

            // ================== PHASE 2: INPUTS ==================
            _pnlPhase2 = new Panel { Width = 920, AutoSize = true, Location = new Point(0, requireSearch ? yOffset + 10 : 10) };
            _pnlPhase2.Visible = !requireSearch; // Nếu không cần search thì hiện luôn Phase 2
            
            int py = 0;
            if (requireSearch)
            {
                _lblSelectedProductPhase2 = new Label { Text = "Sản phẩm được chọn: (Chưa chọn)", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(200, 60, 0), AutoSize = true, Location = new Point(10, py) };
                _pnlPhase2.Controls.Add(_lblSelectedProductPhase2);
                py += 26;
            }

            if (textFields.Count > 0 && !(textFields.Count == 1 && textFields[0] == "Giá trị" && requireSearch))
            {
                var lblInputTitle = new Label { Text = "✏️ Nhập thêm các thông số:", Font = new Font("Segoe UI", 9f, FontStyle.Bold | FontStyle.Italic), ForeColor = Color.FromArgb(30, 80, 150), AutoSize = true, Location = new Point(10, py) };
                _pnlPhase2.Controls.Add(lblInputTitle);
                py += 24;

                foreach (var tf in textFields)
                {
                    string labelText = tf;
                    if (labelText == "Giá trị" && !requireSearch) labelText = $"Nhập {configVal}";

                    var lbl = new Label { Text = labelText.ToUpper() + ":", Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(20, py + 5), AutoSize = true };
                    var txt = new TextBox { Font = new Font("Segoe UI", 10f), Location = new Point(120, py), Width = 300, BorderStyle = BorderStyle.FixedSingle };
                    txt.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { BtnThem_Phase2_Click(null, null); e.Handled = true; } };

                    _dynamicTextBoxes[tf] = txt;
                    _pnlPhase2.Controls.Add(lbl);
                    _pnlPhase2.Controls.Add(txt);
                    py += 34;
                }
            }

            // LUÔN LUÔN tạo cụm nút điều khiển nếu là chế độ Cần Tìm Kiếm hoặc có TextFields
            if (requireSearch || textFields.Count > 0)
            {
                var pnlBottomActions = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Width = 900, Height = 50, Location = new Point(10, py), BackColor = Color.Transparent };
                
                var btnThem = new Button { Text = "✔ Thêm vào danh sách", Font = new Font("Segoe UI", 10f, FontStyle.Bold), BackColor = Color.FromArgb(0, 150, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(200, 40), Cursor = Cursors.Hand };
                btnThem.FlatAppearance.BorderSize = 0;
                btnThem.Click += BtnThem_Phase2_Click;

                var btnCalc = new Button { Text = "🧮 Tính toán", Font = new Font("Segoe UI", 10f, FontStyle.Bold), BackColor = Color.FromArgb(0, 100, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(140, 40), Cursor = Cursors.Hand, Margin = new System.Windows.Forms.Padding(10, 0, 0, 0) };
                btnCalc.FlatAppearance.BorderSize = 0;
                btnCalc.Click += (s, e) => {
                    var dict = _dynamicTextBoxes.ToDictionary(k => k.Key, v => v.Value.Text);
                    var resultVal = EvaluateAdvancedFormula(formula, _selectedProduct, dict);
                    var lblResult = pnlBottomActions.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "lblCalcResult");
                    if (lblResult != null)
                    {
                        lblResult.Text = resultVal.HasValue ? $"Kết quả: {resultVal.Value.ToString("N3")}" : "Lỗi CT";
                        lblResult.ForeColor = resultVal.HasValue ? Color.DarkBlue : Color.Red;
                    }
                };

                var lblCalcResult = new Label { Name = "lblCalcResult", Text = "Kết quả: 0.000", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.Gray, AutoSize = true, Margin = new System.Windows.Forms.Padding(20, 10, 0, 0) };

                pnlBottomActions.Controls.Add(btnThem);
                pnlBottomActions.Controls.Add(btnCalc);
                pnlBottomActions.Controls.Add(lblCalcResult);

                _pnlPhase2.Controls.Add(pnlBottomActions);
                py += 55;
            }

            _pnlPhase2.Height = py + 10;
            pnlMain.Controls.Add(_pnlPhase2);

            _expandPanel.Controls.Add(pnlMain);
        }

        /// <summary>
        /// Lọc sản phẩm theo danh mục + từ khóa và hiển thị vào grid, kèm tính sẵn công thức.
        /// </summary>
        private void FilterAndSearch(string category, string keyword)
        {
            if (_dgvSearchResults == null) return;
            keyword = (keyword ?? "").Trim().ToLower();
            string catFilter = (category ?? "").Trim();

            IEnumerable<Products> source = _allProducts;

            // Lọc theo danh mục
            if (!string.IsNullOrEmpty(catFilter) && !catFilter.StartsWith("--") && catFilter != "-- Tất cả danh mục --")
                source = source.Where(p => string.Equals(p.Category?.Trim(), catFilter, StringComparison.OrdinalIgnoreCase));

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(keyword))
                source = source.Where(p =>
                    (p.Name  != null && p.Name.ToLower().Contains(keyword)) ||
                    (p.SKU   != null && p.SKU.ToLower().Contains(keyword))  ||
                    (p.Model != null && p.Model.ToLower().Contains(keyword)));

            var results = source.Take(200).ToList();
            string formula = _expandedNode?.Formula ?? "";

            _dgvSearchResults.Rows.Clear();
            foreach (var p in results)
            {
                bool hasL = decimal.TryParse(p.Length, out decimal L) && L > 0;
                bool hasW = decimal.TryParse(p.Width,  out decimal W) && W > 0;
                bool hasH = decimal.TryParse(p.Height, out decimal H) && H > 0;
                string size = (hasL || hasW || hasH)
                    ? $"{(hasL ? L.ToString("0.##") : "?")}×{(hasW ? W.ToString("0.##") : "?")}×{(hasH ? H.ToString("0.##") : "?")}"
                    : "";

                decimal.TryParse(p.Price?.Replace(".", "").Replace(",", ""), out decimal price);

                // Tính công thức (sử dụng giá trị mặc định của SP) nếu có
                string kqText = "";
                if (!string.IsNullOrEmpty(formula))
                {
                    decimal? kq = EvaluateAdvancedFormula(formula, p, new Dictionary<string, string>());
                    if (kq.HasValue) kqText = kq.Value.ToString("N2");
                }

                int idx = _dgvSearchResults.Rows.Add();
                _dgvSearchResults.Rows[idx].Cells["colId"].Value    = p.Id;
                _dgvSearchResults.Rows[idx].Cells["colName"].Value  = p.Name;
                _dgvSearchResults.Rows[idx].Cells["colModel"].Value = p.Model;
                _dgvSearchResults.Rows[idx].Cells["colSKU"].Value   = p.SKU;
                _dgvSearchResults.Rows[idx].Cells["colSize"].Value  = size;
                _dgvSearchResults.Rows[idx].Cells["colPrice"].Value = price > 0 ? (object)price : "";
                _dgvSearchResults.Rows[idx].Cells["colKQ"].Value    = kqText;
                _dgvSearchResults.Rows[idx].Tag = p;
            }

            if (_lblProductInfo != null)
                _lblProductInfo.Text = results.Count > 0
                    ? $"Tìm thấy {results.Count} sản phẩm. Nhấp đúp để chọn."
                    : "Không tìm thấy sản phẩm phù hợp.";
        }

        /// <summary>
        /// Xây dựng từ điển biến cho công thức:
        /// - Giá trị mặc định từ thuộc tính sản phẩm (p).
        /// - Ghi đè bằng giá trị người dùng nhập (dictValues).
        /// - Hỗ trợ trùng tên: height2, height3 → h2, h3 (không ghi đè h).
        /// </summary>
        private static Dictionary<string, double> BuildVariableMap(Products p, Dictionary<string, string> dictValues)
        {
            var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            // ── Bước 1: Giá trị mặc định từ thuộc tính sản phẩm ──
            decimal pL = 0, pW = 0, pH = 0, pPrice = 0, pCost = 0, pWeight = 0;
            if (p != null)
            {
                decimal.TryParse(p.Length,  out pL);
                decimal.TryParse(p.Width,   out pW);
                decimal.TryParse(p.Height,  out pH);
                decimal.TryParse(p.Weight,  out pWeight);
                decimal.TryParse(p.Price?.Replace(".", "").Replace(",", ""), out pPrice);
                decimal.TryParse(p.PriceCost?.Replace(".", "").Replace(",", ""), out pCost);
            }

            // Kích thước
            values["l"] = (double)pL;  values["a"]   = (double)pL;  // length / dài
            values["w"] = (double)pW;  values["b"]   = (double)pW;  // width  / rộng
            values["h"] = (double)pH;  values["cao"] = (double)pH;  // height / cao
            values["d"] = (double)pL;                                // deep   (alias của length)
            // Giá
            values["p"]  = (double)pPrice;  values["gv"]  = (double)pPrice;  // giá bán
            values["goc"] = (double)pCost; values["cost"] = (double)pCost;  // giá vốn
            // Khối lượng
            values["kl"] = (double)pWeight; values["kg"] = (double)pWeight;
            // Diện tích / Thể tích mặc định
            values["m2"] = 0;
            values["m3"] = 0;

            // ── Bước 2: Ghi đè từ ô nhập liệu người dùng ──
            var occur = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in dictValues)
            {
                string rawKey = kvp.Key.ToLower()
                                        .Replace("~", "_")
                                        .Replace("/", "_")
                                        .Trim();

                if (!double.TryParse(kvp.Value
                        .Replace(",", "."),    // hỗ trợ dấu phẩy thập phân VN
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double userVal)) continue;

                // Lưu biến gốc (ví dụ: "height2" = 5)
                values[rawKey] = userVal;

                // Ánh xạ thông minh: tăng biến đếm theo nhóm
                ApplyAliasMapping(rawKey, userVal, occur, values);
            }

            return values;
        }

        /// <summary>
        /// Ánh xạ một key vào các biến ngắn trong bản đồ biến, có hỗ trợ đánh số khi trùng tên.
        /// Ví dụ: height lần 1 → h / a, lần 2 → h2 / a2
        /// </summary>
        private static void ApplyAliasMapping(
            string rawKey, double val,
            Dictionary<string, int> occur,
            Dictionary<string, double> values)
        {
            // Helper: lấy số đếm và increment, trả về suffix ("" hoặc "2", "3"...)
            string Suffix(string group)
            {
                if (!occur.ContainsKey(group)) occur[group] = 0;
                occur[group]++;
                return occur[group] == 1 ? "" : occur[group].ToString();
            }

            // ── Chiều cao (height / cao / h) ──
            if (rawKey.Contains("height") || rawKey.Contains("chieu_cao") || rawKey == "h")
            {
                string s = Suffix("height");
                values["h" + s] = val; values["a" + s] = val;
            }
            // ── Chiều rộng (width / rong / rộng / w) ──
            else if (rawKey.Contains("width") || rawKey.Contains("rong") || rawKey.Contains("rộng") || rawKey == "w")
            {
                string s = Suffix("width");
                values["w" + s] = val; values["b" + s] = val;
            }
            // ── Chiều dài / sâu (length / deep / dai / sau) ──
            else if (rawKey.Contains("length") || rawKey.Contains("deep") ||
                     rawKey.Contains("dai") || rawKey.Contains("dài") ||
                     rawKey.Contains("sau") || rawKey.Contains("sâu") || rawKey == "l" || rawKey == "d")
            {
                string s = Suffix("length");
                values["l" + s] = val; values["d" + s] = val;
            }
            // ── Màu / Color ──
            else if (rawKey.Contains("color") || rawKey.Contains("mau") || rawKey.Contains("màu") || rawKey == "c")
            {
                string s = Suffix("color");
                values["c" + s] = val;
            }
            // ── Độ dày (thickness / day) ──
            else if (rawKey.Contains("thick") || rawKey.Contains("thickness") ||
                     rawKey.Contains("day") || rawKey.Contains("độ_dày") || rawKey == "t")
            {
                string s = Suffix("thick");
                values["t" + s] = val; values["day" + s] = val;
            }
            // ── Đường kính (diameter / duong_kinh) ──
            else if (rawKey.Contains("diam") || rawKey.Contains("duong_kinh") ||
                     rawKey.Contains("đường_kính") || rawKey == "dk")
            {
                string s = Suffix("diam");
                values["dk" + s] = val; values["r" + s] = val;
            }
            // ── Số lượng (quantity / so_luong / sl) ──
            else if (rawKey.Contains("quant") || rawKey.Contains("so_luong") ||
                     rawKey.Contains("quantity") || rawKey == "sl" || rawKey == "qty")
            {
                string s = Suffix("qty");
                values["sl" + s] = val; values["qty" + s] = val; values["n" + s] = val;
            }
            // ── Khối lượng (weight / kl / kg) ──
            else if (rawKey.Contains("weight") || rawKey.Contains("khoi_luong") ||
                     rawKey.Contains("khối_lượng") || rawKey == "kl" || rawKey == "kg")
            {
                string s = Suffix("weight");
                values["kl" + s] = val; values["kg" + s] = val;
            }
            // ── Giá bán (price / gia_ban / p / gv) ──
            else if (rawKey.Contains("price") || rawKey.Contains("gia_ban") ||
                     rawKey.Contains("giá_bán") || rawKey == "p" || rawKey == "gv")
            {
                string s = Suffix("price");
                values["p" + s] = val; values["gv" + s] = val;
            }
            // ── Giá vốn (cost / gia_von / goc) ──
            else if (rawKey.Contains("cost") || rawKey.Contains("gia_von") ||
                     rawKey.Contains("giá_vốn") || rawKey.Contains("goc") || rawKey.Contains("gốc"))
            {
                string s = Suffix("cost");
                values["goc" + s] = val; values["cost" + s] = val;
            }
            // ── Diện tích (area / m2 / dien_tich) ──
            else if (rawKey.Contains("area") || rawKey.Contains("m2") || rawKey.Contains("dien_tich"))
            {
                string s = Suffix("area");
                values["m2" + s] = val; values["area" + s] = val;
            }
            // ── Thể tích (volume / m3 / the_tich) ──
            else if (rawKey.Contains("volume") || rawKey.Contains("m3") || rawKey.Contains("the_tich"))
            {
                string s = Suffix("vol");
                values["m3" + s] = val; values["vol" + s] = val;
            }
            // ── Số đếm / count ──
            else if (rawKey.Contains("count") || rawKey.Contains("so_dem") || rawKey == "so")
            {
                string s = Suffix("count");
                values["so" + s] = val; values["count" + s] = val;
            }
            // ── Tỷ lệ / ratio / percent ──
            else if (rawKey.Contains("ratio") || rawKey.Contains("ty_le") || rawKey.Contains("tỷ_lệ"))
            {
                string s = Suffix("ratio");
                values["ty" + s] = val; values["ratio" + s] = val;
            }
            else if (rawKey.Contains("percent") || rawKey.Contains("phan_tram") || rawKey.Contains("phần_trăm") || rawKey == "pct")
            {
                string s = Suffix("pct");
                values["pct" + s] = val; values["phan_tram" + s] = val;
            }
            // ── Không khớp nhóm nào: đã lưu rawKey ở bước trước, không cần làm thêm ──
        }

        /// <summary>
        /// Tính giá trị công thức với ánh xạ biến tùy chỉnh.
        /// </summary>
        private static decimal? EvaluateAdvancedFormula(string formula, Products p, Dictionary<string, string> dictValues)
        {
            if (string.IsNullOrWhiteSpace(formula)) return null;
            try
            {
                // 1. Chuẩn hóa công thức
                string expr = formula.TrimStart('=').Trim().ToLower()
                                     .Replace("~", "_")
                                     .Replace("/", "_");

                // 2. Xây dựng bản đồ biến
                var values = BuildVariableMap(p, dictValues);

                // 3. Thay thế biến trong biểu thức (dài trước để tránh thay nhầm chuỗi con)
                var sortedKeys = values.Keys.OrderByDescending(k => k.Length).ToList();
                foreach (var k in sortedKeys)
                {
                    string pattern = @"\b" + System.Text.RegularExpressions.Regex.Escape(k) + @"\b";
                    expr = System.Text.RegularExpressions.Regex.Replace(
                        expr,
                        pattern,
                        values[k].ToString(System.Globalization.CultureInfo.InvariantCulture),
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }

                // 4. Tính toán bằng DataTable.Compute
                var result = new System.Data.DataTable().Compute(expr, null);
                return Convert.ToDecimal(result);
            }
            catch { return null; }
        }


        private string GetNodePathPrefix()
        {
            if (_modernTreeView?.SelectedNode == null) return "";
            string path = _modernTreeView.SelectedNode.FullPath.Replace("\\", " - ");
            return path;
        }

        private void AddTextValueToList()
        {
            string val = _txtSearch?.Text?.Trim();
            if (string.IsNullOrEmpty(val)) return;
            string configVal = _expandedNode?.Config ?? "";
            
            // Format name like "TỦ ĐIỆN - TỦ PHÂN PHỐI - Màu sơn: màu đỏ"
            string prefix = GetNodePathPrefix();
            string finalName = string.IsNullOrEmpty(prefix) ? val : $"{prefix}: {val}";

            foreach (DataGridViewRow existing in dgvSelectedItems.Rows)
            {
                if (existing.Cells["colTen"].Value?.ToString() == finalName) 
                { 
                    MessageBox.Show($"Đã có [{finalName}] trong danh sách!", "Trùng", MessageBoxButtons.OK, MessageBoxIcon.Information); 
                    return; 
                }
            }

            int idx = dgvSelectedItems.Rows.Add();
            dgvSelectedItems.Rows[idx].Cells["colTen"].Value     = finalName;
            dgvSelectedItems.Rows[idx].Cells["colSoLuong"].Value = "1";
            dgvSelectedItems.Rows[idx].Cells["colGhiChu"].Value  = $"Config: {configVal}";
            btnApply.Enabled = true;

            if (_lblProductInfo != null) 
            { 
                _lblProductInfo.Text = $"✔ Đã thêm [{finalName}]!"; 
                _lblProductInfo.ForeColor = Color.FromArgb(0, 160, 60); 
            }
            _txtSearch.Clear();
        }

        /// <summary>
        /// Khi chọn node trên TreeView, cập nhật nút expand.
        /// </summary>
        private void OnTreeNodeSelected(TreeNode treeNode)
        {
            if (treeNode?.Tag is HierarchyNode node && !string.IsNullOrEmpty(node.Config))
            {
                // Node có cột Config có giá trị → hiện nút expand
                _expandedNode = node;
                _btnExpandToggle.Visible = true;
                string arrow = _expandPanelVisible ? "▼" : "▶";
                _btnExpandToggle.Text = $"{arrow}  Tìm & chọn sản phẩm  —  Config: {node.Config}";
            }
            else
            {
                // Node không có Config → ẩn toàn bộ expand
                _expandedNode = null;
                _btnExpandToggle.Visible = false;
                HideExpandPanel();
            }
            RecalculateLayout();
        }

        private void ToggleExpandPanel()
        {
            if (_expandPanelVisible) HideExpandPanel();
            else ShowExpandPanel();
        }

        private void ShowExpandPanel()
        {
            _expandPanelVisible = true;
            string configLabel = _expandedNode?.Config ?? "";

            // Xây dựng lại nội dung theo loại Config
            BuildExpandContent(configLabel);

            _expandPanel.Visible = true;
            _btnExpandToggle.BackColor = Color.FromArgb(200, 225, 255);

            // Đặt tiêu đề nút toggle theo loại
            string configLower = configLabel.ToLowerInvariant();
            string icon = "✏️";
            if (configLower.Contains("search_category") || configLower.Contains("search-category")) icon = "📂";
            else if (configLower.Contains("search_product") || configLower.Contains("search-product")) icon = "🔍";

            _btnExpandToggle.Text = $"▼  {icon} {configLabel}";

            // Focus vào ô nhập liệu nếu có
            _txtSearch?.Focus();
            RecalculateLayout();
        }


        private void HideExpandPanel()
        {
            _expandPanelVisible = false;
            _expandPanel.Visible = false;
            if (_btnExpandToggle.Visible)
            {
                string configLabel = !string.IsNullOrEmpty(_expandedNode?.Config) ? _expandedNode.Config : "";
                _btnExpandToggle.Text = $"▶  Tìm & chọn sản phẩm  —  Config: {configLabel}";
                _btnExpandToggle.BackColor = Color.FromArgb(230, 240, 255);
            }
            RecalculateLayout();
        }

        /// <summary>
        /// Thực hiện tìm kiếm sản phẩm theo từ khóa.
        /// </summary>
        private void DoSearch()
        {
            string keyword = _txtSearch.Text.Trim().ToLower();
            List<Products> results;

            if (string.IsNullOrEmpty(keyword))
            {
                // Không có từ khóa → lấy tất cả (tối đa 100 dps)
                results = _allProducts.Take(100).ToList();
            }
            else
            {
                results = _allProducts.Where(p =>
                    (p.Name  != null && p.Name.ToLower().Contains(keyword)) ||
                    (p.SKU   != null && p.SKU.ToLower().Contains(keyword))  ||
                    (p.Model != null && p.Model.ToLower().Contains(keyword))
                ).ToList();
            }

            _searchResults = results;
            _dgvSearchResults.Rows.Clear();
            foreach (var p in results)
            {
                string size = "";
                bool hasL = decimal.TryParse(p.Length, out decimal L) && L > 0;
                bool hasW = decimal.TryParse(p.Width,  out decimal W) && W > 0;
                bool hasH = decimal.TryParse(p.Height, out decimal H) && H > 0;
                if (hasL || hasW || hasH)
                    size = $"{(hasL?L.ToString("0.##"):"?") }×{(hasW?W.ToString("0.##"):"?") }×{(hasH?H.ToString("0.##"):"?")}";

                decimal price = 0;
                decimal.TryParse(p.Price?.Replace(".","").Replace(",",""), out price);

                int idx = _dgvSearchResults.Rows.Add();
                _dgvSearchResults.Rows[idx].Cells["colId"].Value    = p.Id;
                _dgvSearchResults.Rows[idx].Cells["colName"].Value  = p.Name;
                _dgvSearchResults.Rows[idx].Cells["colModel"].Value = p.Model;
                _dgvSearchResults.Rows[idx].Cells["colSKU"].Value   = p.SKU;
                _dgvSearchResults.Rows[idx].Cells["colSize"].Value  = size;
                _dgvSearchResults.Rows[idx].Cells["colPrice"].Value = price;
                _dgvSearchResults.Rows[idx].Tag = p;  // lưu object gốc
            }

            _lblProductInfo.Text = results.Count > 0
                ? $"Tìm thấy {results.Count} sản phẩm. Nhấp đúp vào dòng để chọn và tải cấu hình."
                : "Không tìm thấy sản phẩm phù hợp.";
        }

        private void DgvSearchResults_SelectionChanged(object sender, EventArgs e) { } // Ẩn thông báo khi click đơn

        /// <summary>
        /// Nhấp đúp vào dòng kết quả → Chuyển sang Phase 2 (nhập thông số) hoặc tự động thêm nếu không có Phase 2.
        /// </summary>
        private void DgvSearchResults_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _dgvSearchResults.Rows[e.RowIndex];
            if (!(row.Tag is Products p)) return;

            // Đánh dấu sản phẩm được chọn
            _selectedProduct = p;
            
            // Hiện thông báo thành công ở lưới
            if (_lblProductInfo != null) 
            {
                _lblProductInfo.Text = $"✅ Đã chọn: {p.Name}. Vui lòng nhập thông số bên dưới và bấm Thêm.";
                _lblProductInfo.ForeColor = Color.FromArgb(0, 130, 80);
            }

            // Hiện Panel Phase 2 và set nhãn
            if (_pnlPhase2 != null)
            {
                _pnlPhase2.Visible = true;
                if (_lblSelectedProductPhase2 != null)
                {
                    _lblSelectedProductPhase2.Text = $"Sản phẩm chọn: {p.Name} (Giá: {p.Price})";
                    _lblSelectedProductPhase2.ForeColor = Color.FromArgb(0, 100, 0);
                }
                
                // Tự động focus ô nhập liệu đầu tiên
                var firstTxt = _dynamicTextBoxes.Values.FirstOrDefault();
                if (firstTxt != null && firstTxt.Visible) 
                {
                    firstTxt.Focus();
                }
                else
                {
                    // Nếu không có textbox nào (chỉ có nút thêm), tự gọi thêm
                    if (_dynamicTextBoxes.Count == 0) BtnThem_Phase2_Click(null, null);
                }
            }
            else
            {
                BtnThem_Phase2_Click(null, null);
            }
        }

        private void BtnThem_Phase2_Click(object sender, EventArgs e)
        {
            Products p = _selectedProduct;
            string prefix = GetNodePathPrefix();
            string finalName = "";

            var noteItems = new List<string>();

            // 1. Kiểm tra cấu hình có yêu cầu SP ko
            string configRaw = _expandedNode?.Config ?? "";
            bool hasSearchProduct = configRaw.ToLowerInvariant().Contains("search_product") || configRaw.ToLowerInvariant().Contains("search-product");
            bool hasSearchCategory = configRaw.ToLowerInvariant().Contains("search_category") || configRaw.ToLowerInvariant().Contains("search-category");
            bool requireSearch = hasSearchProduct || hasSearchCategory;

            if (requireSearch && p == null)
            {
                MessageBox.Show("Bạn chưa chọn sản phẩm nào từ lưới! Vui lòng nhấp đúp vào sản phẩm để chọn.", "Thiếu sản phẩm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Định danh sản phẩm
            if (p != null)
            {
                finalName = string.IsNullOrEmpty(prefix) ? p.Name : $"{prefix}: {p.Name}";

                decimal.TryParse(p.Length, out decimal cL);
                decimal.TryParse(p.Width, out decimal cW);
                decimal.TryParse(p.Height, out decimal cH);

                if (cL > 0) noteItems.Add($"D:{cL:0.##}mm");
                if (cW > 0) noteItems.Add($"R:{cW:0.##}mm");
                if (cH > 0) noteItems.Add($"C:{cH:0.##}mm");
            }
            else
            {
                // Chỉ nhập Text
                string firstVal = _dynamicTextBoxes.Values.FirstOrDefault()?.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(firstVal)) return;
                finalName = string.IsNullOrEmpty(prefix) ? firstVal : $"{prefix}: {firstVal}";
            }

            // Kiểm tra trùng
            foreach (DataGridViewRow existing in dgvSelectedItems.Rows)
            {
                if (existing.Cells["colTen"].Value?.ToString() == finalName)
                {
                    MessageBox.Show($"Đã có [{finalName}] trong danh sách!", "Trùng lập", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            // 3. Xử lý các thông số nhập thêm (Phase 2)
            var dictValues = new Dictionary<string, string>(); 
            foreach (var kvp in _dynamicTextBoxes)
            {
                string val = kvp.Value.Text.Trim();
                if (p == null && _dynamicTextBoxes.Count == 1) continue; 
                
                if (!string.IsNullOrEmpty(val))
                {
                    noteItems.Add($"{kvp.Key}: {val}");
                    dictValues[kvp.Key] = val;
                }
            }

            // 4. Tính toán công thức
            string formula = _expandedNode?.Formula ?? "";
            if (!string.IsNullOrEmpty(formula))
            {
                decimal? kq = EvaluateAdvancedFormula(formula, p, dictValues);
                if (kq.HasValue) noteItems.Add($"={formula} → {kq.Value:N2}");
            }

            if (!string.IsNullOrEmpty(configRaw)) noteItems.Add($"[{configRaw}]");

            // 5. Thêm vào bảng
            int idx = dgvSelectedItems.Rows.Add();
            dgvSelectedItems.Rows[idx].Cells["colTen"].Value = finalName;
            dgvSelectedItems.Rows[idx].Cells["colSoLuong"].Value = "1";
            dgvSelectedItems.Rows[idx].Cells["colGhiChu"].Value = string.Join(" | ", noteItems);
            dgvSelectedItems.Rows[idx].Tag = p;
            
            btnApply.Enabled = true;

            // 6. Thông báo thành công & Reset
            foreach (var txt in _dynamicTextBoxes.Values) txt.Clear();
            if (_lblProductInfo != null)
            {
                _lblProductInfo.Text = $"✔ Đã thêm [{finalName}]";
                _lblProductInfo.ForeColor = Color.FromArgb(0, 160, 60);
            }
            if (_lblSelectedProductPhase2 != null) _lblSelectedProductPhase2.Text = "Sản phẩm được chọn: (Chưa chọn)";
            _selectedProduct = null;
            if (requireSearch && _pnlPhase2 != null) _pnlPhase2.Visible = false; // Ẩn phase 2 đi bắt chọn lại
        }


        /// <summary>
        /// Tính lại kích thước và vị trí các control theo tỷ lệ 70% TreeView / 30% DataGridView
        /// </summary>
        private void RecalculateLayout()
        {
            if (_modernTreeView == null) return;

            // Padding xung quanh content khi form Maximize - giống modal
            int paddingH  = 80;
            int paddingTop = pnlStepsContainer.Top;
            int formW      = this.ClientSize.Width;
            int controlW   = formW - paddingH * 2;
            int availableH = pnlControls.Top - paddingTop - 10;

            // Di chuyển tiêu đề
            lblTitle.Location = new Point(paddingH, 15);

            // Tính toán: nếu expand panel đang hiện → giảm TreeView height để nhường chỗ
            int expandToggleH  = (_btnExpandToggle != null && _btnExpandToggle.Visible) ? 38 : 0;
            int expandPanelH   = (_expandPanelVisible && _expandPanel != null) ? 260 : 0;
            int extraH         = expandToggleH + expandPanelH;

            int headerH = 44;
            int gridH   = Math.Max(80, (int)(availableH * 0.28));  // ~28% cho danh sách đã chọn
            int treeH   = availableH - extraH - headerH - gridH - 8;
            if (treeH < 80) treeH = 80;

            // TreeView
            _modernTreeView.Location = new Point(paddingH, paddingTop);
            _modernTreeView.Size     = new Size(controlW, treeH);

            int currentY = paddingTop + treeH + 4;

            // Nút expand (nếu node có Components)
            if (_btnExpandToggle != null)
            {
                _btnExpandToggle.Location = new Point(paddingH, currentY);
                _btnExpandToggle.Size     = new Size(controlW, 34);
                if (_btnExpandToggle.Visible) currentY += 38;
            }

            // Expand panel bên dưới nút toggle
            if (_expandPanel != null)
            {
                _expandPanel.Location = new Point(paddingH, currentY);
                _expandPanel.Size     = new Size(controlW, 260);
                if (_expandPanelVisible)
                {
                    // Resize các control bên trong
                    var pnlInner = _expandPanel.Controls.Count > 0 ? _expandPanel.Controls[0] as Panel : null;
                    if (pnlInner != null && _dgvSearchResults != null)
                    {
                        int innerW = controlW - 20;
                        _txtSearch.Width = Math.Max(100, innerW - 130);
                        _btnSearch.Location = new Point(_txtSearch.Right + 10, _txtSearch.Top - 2);
                        _lblProductInfo.Width = innerW;
                        _dgvSearchResults.Size = new Size(innerW, 260 - 100);
                    }
                    currentY += 264;
                }
            }

            // Đường kẻ phân cách
            currentY += 4;
            lblDivider.Location = new Point(paddingH, currentY);
            lblDivider.Width    = controlW;

            // Nhãn + nút Thêm vào
            int headerY = currentY + 6;
            lblGridTitle.Location  = new Point(paddingH, headerY + 7);
            btnAddToGrid.Location  = new Point(formW - paddingH - btnAddToGrid.Width, headerY + 2);

            // DataGridView
            int dgvY = headerY + headerH;
            dgvSelectedItems.Location = new Point(paddingH, dgvY);
            dgvSelectedItems.Size     = new Size(controlW, gridH);
        }


        private void PopulateTree()
        {
            _modernTreeView.Nodes.Clear();
            foreach (var rootNode in _rootNodes)
            {
                var treeNode = CreateTreeNode(rootNode);
                _modernTreeView.Nodes.Add(treeNode);
            }
            if (_modernTreeView.Nodes.Count > 0)
            {
                _modernTreeView.Nodes[0].Expand(); // Mở sẵn root node đầu tiên
            }
        }


        private TreeNode CreateTreeNode(HierarchyNode dataNode)
        {
            var treeNode = new TreeNode(dataNode.Name);
            treeNode.Tag = dataNode;

            foreach (var childNode in dataNode.Children)
            {
                treeNode.Nodes.Add(CreateTreeNode(childNode));
            }
            return treeNode;
        }

        // ── Win32 helper: đặt cue (placeholder) text cho TextBox trên .NET Framework ──
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);
        private const uint EM_SETCUEBANNER = 0x1501;

        private static void SetCueBanner(TextBox tb, string hint)
        {
            if (tb.IsHandleCreated)
                SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, hint);
        }
    }

    /// <summary>
    /// TreeView được vẽ lại tùy chỉnh để trông giống thiết kế hiện đại trên Web/Figma
    /// </summary>
    public class ModernTreeView : TreeView
    {
        private TreeNode _hoverNode;

        public ModernTreeView()
        {
            this.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            this.ShowLines = false;
            this.ShowPlusMinus = false;
            this.FullRowSelect = true;
            this.ItemHeight = 40; // Cao hơn cho đẹp
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.BorderStyle = BorderStyle.None;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var node = this.GetNodeAt(e.Location);
            if (_hoverNode != node)
            {
                if (_hoverNode != null) this.Invalidate(_hoverNode.Bounds);
                _hoverNode = node;
                if (_hoverNode != null) this.Invalidate(_hoverNode.Bounds);
            }
            
            // Đổi trỏ chuột khi đang hover vào Icon Expand
            if (node != null && node.Nodes.Count > 0)
            {
                int levelIndent = 16 + node.Level * 24;
                Rectangle expandRect = new Rectangle(levelIndent, node.Bounds.Y, 24, this.ItemHeight);
                if (expandRect.Contains(e.Location))
                    this.Cursor = Cursors.Hand;
                else
                    this.Cursor = Cursors.Default;
            }
            else
            {
                this.Cursor = Cursors.Default;
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverNode != null)
            {
                this.Invalidate(_hoverNode.Bounds);
                _hoverNode = null;
            }
            this.Cursor = Cursors.Default;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            var hitTest = this.HitTest(e.Location);
            if (hitTest.Node != null)
            {
                int levelIndent = 16 + hitTest.Node.Level * 24;
                // Vùng bắt thao tác Click để thu/mở là vùng chứa Chevron
                Rectangle expandRect = new Rectangle(levelIndent, hitTest.Node.Bounds.Y, 24, this.ItemHeight);
                if (expandRect.Contains(e.Location) && hitTest.Node.Nodes.Count > 0)
                {
                    if (hitTest.Node.IsExpanded) hitTest.Node.Collapse();
                    else hitTest.Node.Expand();
                }
                else
                {
                    this.SelectedNode = hitTest.Node;
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnDrawNode(DrawTreeNodeEventArgs e)
        {
            if (e.Node == null || e.Bounds.Height <= 0) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            bool isSelected = (e.State & TreeNodeStates.Selected) != 0 || this.SelectedNode == e.Node;
            bool isHovered = _hoverNode == e.Node;

            // Vô hiệu hóa translation mặc định của TreeView bằng cách trừ e.Bounds.X
            int offsetX = -e.Bounds.X;
            int controlWidth = this.ClientRectangle.Width;

            // 1. Nền trắng toàn bộ hàng
            Rectangle rowBounds = new Rectangle(offsetX, e.Bounds.Y, controlWidth, e.Bounds.Height);
            g.FillRectangle(Brushes.White, rowBounds);

            // 2. Màu nền cho Hover/Selection
            Color bgColor = Color.Transparent;
            if (isSelected) 
                bgColor = Color.FromArgb(230, 247, 255); // Blue nhạt Ant Design
            else if (isHovered) 
                bgColor = Color.FromArgb(250, 250, 250); // Xám cực nhạt

            if (isSelected || isHovered)
            {
                Rectangle bgRect = new Rectangle(offsetX + 4, e.Bounds.Y + 1, controlWidth - 8, e.Bounds.Height - 2);
                using (var brush = new SolidBrush(bgColor))
                {
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        int radius = 4;
                        path.AddArc(bgRect.X, bgRect.Y, radius, radius, 180, 90);
                        path.AddArc(bgRect.Right - radius, bgRect.Y, radius, radius, 270, 90);
                        path.AddArc(bgRect.Right - radius, bgRect.Bottom - radius, radius, radius, 0, 90);
                        path.AddArc(bgRect.X, bgRect.Bottom - radius, radius, radius, 90, 90);
                        path.CloseAllFigures();
                        g.FillPath(brush, path);
                    }
                }
            }

            // 3. Thanh chỉ thị (Indicator) màu xanh bên phải khi chọn
            if (isSelected)
            {
                using (var blueBrush = new SolidBrush(Color.FromArgb(24, 144, 255)))
                {
                    g.FillRectangle(blueBrush, offsetX + controlWidth - 3, e.Bounds.Y + 4, 3, e.Bounds.Height - 8);
                }
            }

            // 4. Chevron (Mũi tên)
            int currentIndent = 20 + (e.Node.Level * 24);
            if (e.Node.Nodes.Count > 0)
            {
                int cy = e.Bounds.Y + (e.Bounds.Height) / 2;
                int cx = currentIndent + 6;
                Color chevronColor = Color.FromArgb(140, 140, 140);
                
                using (var pen = new Pen(chevronColor, 1.5f))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    if (e.Node.IsExpanded)
                    {
                        g.DrawLine(pen, cx - 3, cy - 1, cx, cy + 2);
                        g.DrawLine(pen, cx, cy + 2, cx + 3, cy - 1);
                    }
                    else
                    {
                        g.DrawLine(pen, cx - 1, cy - 3, cx + 2, cy);
                        g.DrawLine(pen, cx + 2, cy, cx - 1, cy + 3);
                    }
                }
            }

            // 5. Chữ danh mục
            int textX = currentIndent + 26;
            Color textColor = isSelected ? Color.FromArgb(24, 144, 255) : Color.FromArgb(40, 40, 40);
            Font textFont = isSelected ? new Font(this.Font, FontStyle.Bold) : this.Font;
            
            using (var brush = new SolidBrush(textColor))
            {
                g.DrawString(e.Node.Text, textFont, brush, textX, e.Bounds.Y + (e.Bounds.Height - textFont.Height) / 2);
            }
        }
    }
}
