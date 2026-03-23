using ECQ_Soft.Model;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using ECQ_Soft.Helper;
using System.Windows.Forms;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ComponentModel;
using Excel = Microsoft.Office.Interop.Excel;

namespace ECQ_Soft
{
    public partial class FrmConfig : UserControl
    {
        // ══════════════════════════════════════════════════════════════════
        // FIELDS – Biến trạng thái của form
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Cờ tránh vòng lặp khi cập nhật comboBox2 ↔ comboBox1 lẫn nhau.</summary>
        private bool isUpdatingComboBoxes = false;

        /// <summary>Kết nối tới Google Sheets API (khởi tạo một lần, dùng lại).</summary>
        private SheetsService _sheetsService;

        /// <summary>ID của Google Spreadsheet chứa toàn bộ dữ liệu.</summary>
        string spreadsheetId = "10gNCH_pG4LmkQ1g109H1WEM4nwBk4UBff_IDHar0Hd8";

        /// <summary>Tên sheet chứa danh sách sản phẩm (bảng master).</summary>
        string sheetName = "Products_Table";

        /// <summary>Tên sheet hiện tại đang làm việc (ví dụ: "Config_Tab1"). Null = chưa chọn.</summary>
        string configSheetName = null;

        /// <summary>Trả về đường dẫn file cache JSON cho một key dữ liệu cụ thể.</summary>
        private string GetCachePath(string key) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"cache_{key}_{configSheetName ?? "global"}.json");

        /// <summary>Trả về SheetsService để FrmMain/modal dùng chung.</summary>
        public SheetsService GetSheetsService()
        {
            if (_sheetsService == null) InitGoogleSheetsService();
            return _sheetsService;
        }

        /// <summary>Trả về Spreadsheet ID hiện tại.</summary>
        public string GetSpreadsheetId() => spreadsheetId;

        /// <summary>Trả về tên sheet cấu hình hiện tại.</summary>
        public string GetConfigSheetName() => configSheetName;

        /// <summary>
        /// Cập nhật tên sheet cấu hình và reload lại dữ liệu cấu hình.
        /// Được gọi sau khi người dùng chọn/tạo tab từ modal FrmSheetSelector.
        /// </summary>
        public async Task SetConfigSheet(string newConfigSheetName)
        {
            if (string.IsNullOrEmpty(newConfigSheetName)) return;
            configSheetName = newConfigSheetName;
            
            // Nạp từ cache ngay lập tức để người dùng thấy danh sách cấu hình TỨC THÌ
            var cachedNames = LoadFromCache<List<string>>("config_names");
            if (cachedNames != null)
            {
                lstSavedConfigs.ClearItems();
                foreach (var n in cachedNames) lstSavedConfigs.AddItem(n);
            }

            await LoadDataAsync();
        }

        /// <summary>Cây danh mục sản phẩm (phân cấp, dùng cho bộ lọc).</summary>
        private List<CategoryItem> categoryTree = new List<CategoryItem>();

        /// <summary>Toàn bộ sản phẩm nạp từ Google Sheets (sheet Products_Table).</summary>
        private List<Products> allProducts = new List<Products>();

        /// <summary>Danh sách sản phẩm đang được thêm vào cấu hình báo giá hiện tại.</summary>
        private List<ConfigProductItem> configProducts = new List<ConfigProductItem>();

        /// <summary>Danh sách quan hệ sản phẩm chính – sản phẩm con (relation PR).</summary>
        private List<RelationItem> productRelations = new List<RelationItem>();

        /// <summary>Danh sách sản phẩm con (bên phải) đang được chọn, binding với dataGridView1.</summary>
        private BindingList<Products> childProducts = new BindingList<Products>();

        /// <summary>Toàn bộ cấu hình đã lưu trên Google Sheets (dùng để merge khi nạp nhiều cấu hình).</summary>
        private List<ConfigProductItem> allSavedConfigs = new List<ConfigProductItem>();

        /// <summary>Tên cấu hình đang được chỉnh sửa (null = đang tạo mới).</summary>
        private string currentEditingConfigName = null;

        private CheckBox chkSelectAllAllProducts = new CheckBox();
        private CheckBox chkSelectAllChildProducts = new CheckBox();

        // ── Màu tuỳ chỉnh per-cell (được chọn qua color picker chuột phải) ──
        // Key = (rowIndex, colIndex) của DataGridView; Value = màu được chọn
        private Dictionary<(int r, int c), Color> _cellBgColors = new Dictionary<(int, int), Color>(); // màu nền
        private Dictionary<(int r, int c), Color> _cellFgColors = new Dictionary<(int, int), Color>(); // màu chữ

        /// <summary>Lưu vị trí ô được right-click (để hiển thị context menu đúng ô).</summary>
        private int _rightClickedRow = -1;
        private int _rightClickedCol = -1;

        /// <summary>Danh sách hiển thị trong dgvParentProducts (bao gồm cả 3 dòng TỔNG/VAT/THÀNH TIỀN).</summary>
        private List<ConfigProductItem> _displayList = new List<ConfigProductItem>();

        // ══════════════════════════════════════════════════════════════════
        // KHỞI TẠO FORM
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Constructor: đăng ký tất cả event handlers và thiết lập context menu màu ô.
        /// </summary>
        public FrmConfig()
        {
            InitializeComponent();
            dgvParentProducts.CellValueChanged += DgvParentProducts_CellValueChanged;
            dgvParentProducts.CurrentCellDirtyStateChanged += DgvParentProducts_CurrentCellDirtyStateChanged;

            dgvAllProducts.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
            dataGridView1.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;

            dgvAllProducts.DataBindingComplete += Grid_DataBindingComplete;
            dataGridView1.DataBindingComplete += Grid_DataBindingComplete;
            dgvParentProducts.DataBindingComplete += DgvParentProducts_DataBindingComplete;
            dgvParentProducts.CellFormatting += DgvParentProducts_CellFormatting;
            dgvParentProducts.CellPainting += DgvParentProducts_CellPainting;

            // Handle DataError to suppress technical dialogs
            dgvAllProducts.DataError += Grid_DataError;
            dataGridView1.DataError += Grid_DataError;
            dgvParentProducts.DataError += Grid_DataError;

            // Multi-row toggle logic
            dgvAllProducts.CellContentClick += Grid_CellContentClick;
            dataGridView1.CellContentClick += Grid_CellContentClick;
            dgvAllProducts.KeyDown += Grid_KeyDown;
            dataGridView1.KeyDown += Grid_KeyDown;

            SetupHeaderCheckBox(dataGridView1, chkSelectAllChildProducts, "IsSelected");

            dataGridView1.DataSource = childProducts;

            // Click ra ngoài DataGridView → xóa bôi đen (selection)
            this.Click += (s, e) => dgvParentProducts.ClearSelection();
            this.MouseDown += (s, e) => dgvParentProducts.ClearSelection();
            // Khi focus rời khỏi DataGridView (ví dụ click vào button, textbox khác) → xóa selection
            dgvParentProducts.Leave += (s, e) => dgvParentProducts.ClearSelection();

            // ── Context menu chuột phải cho ô trong danh sách cấu hình ──
            var ctxCell = new ContextMenuStrip();

            var miSetBg = new System.Windows.Forms.ToolStripMenuItem("🎨  Màu nền ô");
            miSetBg.Click += (s, e) =>
            {
                using (var picker = new ECQ_Soft.Helper.ColorPickerPopup())
                {
                    if (picker.ShowDialog() == DialogResult.OK && picker.SelectedColor.HasValue)
                    {
                        foreach (DataGridViewCell cell in dgvParentProducts.SelectedCells)
                        {
                            if (cell.RowIndex >= 0 && cell.ColumnIndex >= 0)
                                _cellBgColors[(cell.RowIndex, cell.ColumnIndex)] = picker.SelectedColor.Value;
                        }
                        dgvParentProducts.Refresh();
                    }
                }
            };

            var miSetFg = new System.Windows.Forms.ToolStripMenuItem("✏️  Màu chữ ô");
            miSetFg.Click += (s, e) =>
            {
                using (var picker = new ECQ_Soft.Helper.ColorPickerPopup())
                {
                    if (picker.ShowDialog() == DialogResult.OK && picker.SelectedColor.HasValue)
                    {
                        foreach (DataGridViewCell cell in dgvParentProducts.SelectedCells)
                        {
                            if (cell.RowIndex >= 0 && cell.ColumnIndex >= 0)
                                _cellFgColors[(cell.RowIndex, cell.ColumnIndex)] = picker.SelectedColor.Value;
                        }
                        dgvParentProducts.Refresh();
                    }
                }
            };

            var miClearColor = new System.Windows.Forms.ToolStripMenuItem("✖  Xoá màu ô (tất cả ô đang chọn)");
            miClearColor.Click += (s, e) =>
            {
                foreach (DataGridViewCell cell in dgvParentProducts.SelectedCells)
                {
                    var key = (cell.RowIndex, cell.ColumnIndex);
                    _cellBgColors.Remove(key);
                    _cellFgColors.Remove(key);
                }
                dgvParentProducts.Refresh();
            };

            ctxCell.Items.Add(miSetBg);
            ctxCell.Items.Add(miSetFg);
            ctxCell.Items.Add(new ToolStripSeparator());
            ctxCell.Items.Add(miClearColor);

            dgvParentProducts.ContextMenuStrip = ctxCell;
            dgvParentProducts.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    // Chỉ ghi nhận ô right-click để làm màu khởi tạo cho dialog
                    // KHÔNG thay đổi CurrentCell để giữ nguyên selection nhiều ô
                    _rightClickedRow = e.RowIndex;
                    _rightClickedCol = e.ColumnIndex;
                }
            };
        }

        // ══════════════════════════════════════════════════════════════════
        // EVENT HANDLERS – DataGridView
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Sau khi binding xong, áp dụng style cho dgvParentProducts (danh sách cấu hình).</summary>
        private void DgvParentProducts_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            FormatConfigGrid(dgvParentProducts);
        }

        /// <summary>Sau khi binding xong ở grid sản phẩm (trái/phải), áp dụng style chung.</summary>
        private void Grid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv != null)
                FormatDataGridView(dgv);
        }

        /// <summary>
        /// Commit ngay khi ô đang sửa thay đổi giá trị (tránh phải nhấn Enter thủ công).
        /// Cần thiết để checkbox IsSelected hoạt động mượt mà.
        /// </summary>
        private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv != null && dgv.IsCurrentCellDirty)
                dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        // ══════════════════════════════════════════════════════════════════
        // KẾT NỐI GOOGLE SHEETS
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Khởi tạo _sheetsService từ file config.json (Service Account credentials).
        /// Chỉ gọi một lần; các lần sau dùng lại instance đã có.
        /// </summary>
        private void InitGoogleSheetsService()
        {
            try
            {
                GoogleCredential credential;

                using (var stream = new FileStream("config.json", FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(SheetsService.Scope.Spreadsheets);
                }


                _sheetsService = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "GSheetConfig",
                });
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show("Không tìm thấy file 'credentials.json'.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show("Lỗi khi đọc file credentials.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show("Lỗi xác thực với Google API.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi không xác định khi kết nối Google Sheets.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FrmConfig_Load(object sender, EventArgs e)
        {
            // Không gọi LoadDataAsync() ở đây vì FrmMain đã gọi trước đó.
            // Việc gọi lại sẽ gây race condition: khi tab lần đầu hiển thị,
            // sự kiện Load này kích hoạt và load lại từ configSheetName mặc định
            // ("Products_Config"), ghi đè tab mà người dùng đã chọn từ modal.
            
            button2.Click += Button2_Click;
            button1.Click += BtnAddParent_Click;
            btnSearch.Click += BtnAddFromRelation_Click;
            button4.Click += BtnRemoveParent_Click;
            button3.Click += Button3_Click;
            button5.Click += Button5_Click;
            button6.Click += Button6_Click;
            button7.Click += Button7_Click;
            button10.Click += BtnExportExcel_Click;

            // button9 = "Cập nhật": reload toàn bộ dữ liệu từ Google Sheets
            button9.Click += async (s, ev) =>
            {
                button9.Enabled = false;
                button9.Text = "Đang tải...";
                try { await LoadDataAsync(); }
                finally { button9.Enabled = true; button9.Text = "Cập nhật"; }
            };

            // Nhấn Enter trong ô tìm kiếm = kích hoạt tìm kiếm
            textBox2.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter) { Button2_Click(s, ev); ev.Handled = true; }
            };
            
            comboBox2.SelectedValueChanged -= ComboBox2_SelectedValueChanged;
            comboBox2.SelectedValueChanged += ComboBox2_SelectedValueChanged;
            
            comboBox1.SelectedValueChanged -= ComboBox1_SelectedValueChanged;
            comboBox1.SelectedValueChanged += ComboBox1_SelectedValueChanged;

            lstSavedConfigs.Confirmed -= Button6_Click;
            lstSavedConfigs.Confirmed += Button6_Click;
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            // Tìm vị trí của các Header được chọn
            var checkedItems = lstSavedConfigs.CheckedItems.Cast<string>().ToList();
            if (checkedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng tích chọn ít nhất một cấu hình để tải!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // configProducts.Clear(); // Bỏ Clear để thực hiện cộng dồn (Merge) theo yêu cầu
            int totalAdded = 0;

            foreach (string selectedHeaderName in checkedItems)
            {
                // Tránh nạp trùng cấu hình đã có trong bảng hiện tại
                if (configProducts.Any(p => p.IsHeader && string.Equals(p.TenHang?.Trim(), selectedHeaderName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                int headerIndex = allSavedConfigs.FindIndex(c =>
                    c.IsHeader &&
                    string.Equals(c.TenHang?.Trim(), selectedHeaderName, StringComparison.OrdinalIgnoreCase));

                if (headerIndex >= 0)
                {
                    // Thêm dòng Header của cấu hình này (Clone để độc lập STT)
                    configProducts.Add(allSavedConfigs[headerIndex].Clone());

                    // Thêm các sản phẩm thuộc cấu hình này
                    for (int i = headerIndex + 1; i < allSavedConfigs.Count; i++)
                    {
                        if (allSavedConfigs[i].IsHeader) break;
                        configProducts.Add(allSavedConfigs[i].Clone());
                        totalAdded++;
                    }
                }
            }


            // Cập nhật lại STT cho toàn bộ danh sách mới
            for (int i = 0; i < configProducts.Count; i++)
            {
                configProducts[i].STT = i + 1;
            }

            UpdateHeaderSum();
            UpdateConfigGrid();

            // Reset editing state since we may have multiple configs now
            currentEditingConfigName = checkedItems.Count == 1 ? checkedItems[0] : null;
            button5.Text = checkedItems.Count == 1 ? "Cập nhật" : "Lưu";

            MessageBox.Show($"Đã nạp {totalAdded} sản phẩm từ {checkedItems.Count} cấu hình được chọn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            string searchText = textBox2.Text.Trim().ToLower();
            string selectedCat = cboCategory?.SelectedFullPath ?? "";

            var filteredProducts = allProducts.AsEnumerable();

            // 1. Filter by Name / SKU / Model (Partial match)
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredProducts = filteredProducts.Where(p =>
                    (p.Name  != null && p.Name.ToLower().Contains(searchText)) ||
                    (p.SKU   != null && p.SKU.ToLower().Contains(searchText))  ||
                    (p.Model != null && p.Model.ToLower().Contains(searchText))
                );
            }

            // 2. Filter by Category (từ CategoryTreeDropdown)
            if (!string.IsNullOrEmpty(selectedCat))
            {
                // Match nếu category của sản phẩm bắt đầu bằng selectedCat (bao gồm node cha lẫn con)
                filteredProducts = filteredProducts.Where(p =>
                    p.Category != null &&
                    p.Category.TrimEnd(';').Trim().StartsWith(selectedCat, StringComparison.OrdinalIgnoreCase)
                );
            }

            dgvAllProducts.DataSource = filteredProducts.ToList();
            // FormatDataGridView will be called by DataBindingComplete
        }

        // ══════════════════════════════════════════════════════════════════
        // NẠP DỮ LIỆU (LOAD DATA)
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Nạp toàn bộ dữ liệu: trước nạp từ cache (hiện ngay), sau đó nạp song song từ mạng.
        /// Bao gồm: danh sách cấu hình, sản phẩm, quan hệ PR, cấu hình đã lưu.
        /// </summary>
        public async Task LoadDataAsync()
        {
            if (_sheetsService == null) InitGoogleSheetsService();

            // 1. NẠP TOÀN BỘ TỪ CACHE (HIỆN LÊN TỨC THÌ)
            // Nạp Tên Cấu Hình
            var cachedConfigNames = LoadFromCache<List<string>>("config_names");
            if (cachedConfigNames != null)
            {
                lstSavedConfigs.ClearItems();
                foreach (var n in cachedConfigNames) lstSavedConfigs.AddItem(n);
            }
            // Nạp Sản Phẩm
            var cachedProducts = LoadFromCache<List<Products>>("all_products");
            if (cachedProducts != null)
            {
                allProducts.Clear();
                allProducts.AddRange(cachedProducts);
                UpdateFiltersFromProducts(allProducts); // Cập nhật Hãng & Danh mục từ cache
                dgvAllProducts.DataSource = allProducts.ToList();
            }
            // Nạp Quan hệ
            var cachedRelations = LoadFromCache<List<RelationItem>>("product_relations");
            if (cachedRelations != null)
            {
                productRelations.Clear();
                productRelations.AddRange(cachedRelations);
                UpdateProductRelationCombo();
            }

            try
            {
                // 2. NẠP SONG SONG TỪ GOOGLE SHEETS (GIẢM DELAY)
                var configNamesTask = FetchConfigNamesAsync();
                var productsTask = FetchAllProductsAsync();
                var relationsTask = FetchProductRelationsAsync();
                var savedConfigsFullTask = FetchSavedConfigsFullDataAsync();

                await Task.WhenAll(configNamesTask, productsTask, relationsTask, savedConfigsFullTask);

                // Sau khi nạp xong mạng, dữ liệu sẽ được cập nhật và lưu vào cache trong từng hàm con
                UpdateHeaderSum();
                UpdateConfigGrid();
                UpdateGridSelector(dgvAllProducts, allProducts);
                // dataGridView1.DataSource is already bound to childProducts in constructor
            }
            catch (Exception ex)
            {
                // Lỗi mạng không quan trọng vì đã có Cache hiển thị
                Console.WriteLine($"LoadDataAsync network error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy danh sách tên cấu hình đã lưu (header dòng IsHeader=1) từ Google Sheets.
        /// Cập nhật lstSavedConfigs và lưu vào cache.
        /// </summary>
        private async Task FetchConfigNamesAsync()
        {
            if (string.IsNullOrEmpty(configSheetName)) return;
            try
            {
                // Lấy cột A→E để có DonVi (cột E, index 4) xác định đây là header (DonVi="TỦ")
                var response = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, $"{configSheetName}!A2:E").ExecuteAsync();
                if (response.Values != null)
                {
                    var freshNames = response.Values
                        .Where(r => r.Count >= 5
                            && r[4]?.ToString()?.Trim() == "TỦ"
                            && !string.IsNullOrEmpty(r[1]?.ToString())
                            && !r[1].ToString().StartsWith("--"))
                        .Select(r => r[1].ToString())
                        .Distinct().ToList();

                    if (freshNames.Count > 0)
                    {
                        var currentNames = lstSavedConfigs.Items.Cast<object>().Select(x => x.ToString()).ToList();
                        if (!freshNames.SequenceEqual(currentNames))
                        {
                            this.Invoke((MethodInvoker)delegate {
                                lstSavedConfigs.ClearItems();
                                foreach (var name in freshNames) lstSavedConfigs.AddItem(name);
                            });
                            SaveToCache("config_names", freshNames);
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Lấy toàn bộ sản phẩm từ sheet Products_Table (cột A→K).
        /// Sau khi nạp: cập nhật bộ lọc hãng/danh mục và hiển thị lên dgvAllProducts.
        /// </summary>
        private async Task FetchAllProductsAsync()
        {
            try
            {
                var response = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, $"{sheetName}!A2:K").ExecuteAsync();
                if (response.Values != null && response.Values.Count > 0)
                {
                    var newProducts = new List<Products>();
                    for (int i = 0; i < response.Values.Count; i++)
                    {
                        var row = response.Values[i];
                        if (row.Count < 2) continue;
                        newProducts.Add(new Products {
                            Id = (row.Count > 0 && int.TryParse(row[0]?.ToString(), out int id)) ? id : i + 1,
                            Name = row.Count > 1 ? row[1]?.ToString() : "",
                            Model = row.Count > 2 ? row[2]?.ToString() : "",
                            SKU = row.Count > 3 ? row[3]?.ToString() : "",
                            Price = row.Count > 4 ? row[4]?.ToString() : "0",
                            Weight = row.Count > 5 ? row[5]?.ToString() : "0",
                            Length = row.Count > 6 ? row[6]?.ToString() : "0",
                            Width = row.Count > 7 ? row[7]?.ToString() : "0",
                            Height = row.Count > 8 ? row[8]?.ToString() : "0",
                            Category = row.Count > 9 ? row[9]?.ToString() : "",
                            HÃNG = row.Count > 10 ? row[10]?.ToString() : ""
                        });
                    }
                    allProducts.Clear();
                    allProducts.AddRange(newProducts);
                    this.Invoke((MethodInvoker)delegate {
                        UpdateFiltersFromProducts(allProducts);
                        dgvAllProducts.DataSource = allProducts.ToList();
                    });
                    SaveToCache("all_products", allProducts);
                }
            }
            catch { }
        }

        /// <summary>
        /// Xây dựng cây danh mục và danh sách hãng từ dữ liệu sản phẩm.
        /// Được gọi sau khi nạp sản phẩm để cập nhật các bộ lọc.
        /// </summary>
        private void UpdateFiltersFromProducts(List<Products> products)
        {
            var rawCategories = products.Select(p => p.Category).Where(c => !string.IsNullOrEmpty(c)).ToList();

            // ── CategoryTreeDropdown mới (đa cấp đệ quy) ──
            var treeNodes = CategoryParser.ParseToTreeNodes(rawCategories);
            if (cboCategory != null)
                cboCategory.LoadTree(treeNodes);

            // Giữ lại categoryTree cũ để tương thích các chỗ khác vẫn còn dùng
            categoryTree = CategoryParser.ParseToTree(rawCategories);
            categoryTree.Insert(0, new CategoryItem { DisplayText = "-- Tất cả danh mục --", FullPath = "" });
        }

        /// <summary>
        /// Lấy danh sách quan hệ sản phẩm chính – con từ sheet Products_Relatation.
        /// Cập nhật productRelations và comboBox2/comboBox1.
        /// </summary>
        private async Task FetchProductRelationsAsync()
        {
            try
            {
                var response = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, "Products_Relatation!A2:E").ExecuteAsync();
                if (response.Values != null)
                {
                    var newRelations = new List<RelationItem>();
                    foreach (var row in response.Values)
                    {
                        if (row.Count < 3) continue;
                        int.TryParse(row[1]?.ToString(), out int mainId);
                        int.TryParse(row[2]?.ToString(), out int childId);
                        newRelations.Add(new RelationItem { ID_Product_Main = mainId, ID_Product_Child = childId, Category_PR = row.Count > 3 ? row[3]?.ToString() : "" });
                    }
                    productRelations.Clear();
                    productRelations.AddRange(newRelations);
                    this.Invoke((MethodInvoker)delegate { UpdateProductRelationCombo(); });
                    SaveToCache("product_relations", productRelations);
                }
            }
            catch { }
        }

        /// <summary>
        /// Cập nhật comboBox2 (Sản phẩm chính) và comboBox1 (Danh mục PR)
        /// dựa trên dữ liệu quan hệ hiện tại.
        /// </summary>
        private void UpdateProductRelationCombo()
        {
            // CHỈ lấy ID_Product_Main (Sản phẩm chính) để tránh hiển thị linh kiện con
            var mainProductIds = productRelations.Select(r => r.ID_Product_Main).Distinct().ToList();
            var mainProductsDisplay = allProducts
                .Where(p => mainProductIds.Contains(p.Id))
                .Select(p => new { Id = p.Id, Name = p.Name })
                .OrderBy(p => p.Name).ToList();

            mainProductsDisplay.Insert(0, new { Id = 0, Name = "-- Chọn sản phẩm --" });
            comboBox2.DataSource = mainProductsDisplay;
            comboBox2.DisplayMember = "Name";
            comboBox2.ValueMember = "Id";

            var catPRs = productRelations.Select(r => r.Category_PR).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
            catPRs.Insert(0, "-- Tất cả danh mục --");
            comboBox1.DataSource = catPRs;
        }

        // ── Màu đọc từ Google Sheet (dùng để khôi phục màu khi load cấu hình) ──
        // Key = (sheetRowIndex 0-based, sheetColIndex 0-based); Value = màu tương ứng
        private Dictionary<(int r, int c), Color> _sheetBgColors = new Dictionary<(int, int), Color>(); // màu nền
        private Dictionary<(int r, int c), Color> _sheetFgColors = new Dictionary<(int, int), Color>(); // màu chữ

        /// <summary>
        /// Lấy toàn bộ dữ liệu cấu hình đã lưu (bao gồm allSavedConfigs)
        /// và sau đó đọc cả thông tin màu sắc từng ô của sheet.
        /// </summary>
        private async Task FetchSavedConfigsFullDataAsync()
        {
            if (string.IsNullOrEmpty(configSheetName)) return;
            try
            {
                var response = await _sheetsService.Spreadsheets.Values.Get(spreadsheetId, $"{configSheetName}!A2:L").ExecuteAsync();
                if (response.Values != null)
                {
                    var newSavedItems = new List<ConfigProductItem>();
                    for (int i = 0; i < response.Values.Count; i++)
                    {
                        var row = response.Values[i];
                        if (row.Count < 2) continue;
                        string tenHang = row[1]?.ToString()?.Trim() ?? "";
                        if (tenHang.StartsWith("TỔNG CỘNG") || tenHang.StartsWith("THUẾ VAT") || tenHang.StartsWith("THÀNH TIỀN")) continue;

                        Func<string, decimal> parseCurrency = (s) => {
                            if (string.IsNullOrEmpty(s)) return 0;
                            string clean = s.Replace(".", "").Replace(",", "").Replace("₫", "").Trim();
                            decimal.TryParse(clean, out decimal res);
                            return res;
                        };

                        newSavedItems.Add(new ConfigProductItem {
                            STT = (row.Count > 0 && int.TryParse(row[0]?.ToString(), out int stt)) ? stt : i + 1,
                            TenHang = tenHang,
                            MaHang = row.Count > 2 ? row[2]?.ToString() : "",
                            XuatXu = row.Count > 3 ? row[3]?.ToString() : "",
                            DonVi = row.Count > 4 ? row[4]?.ToString() : "",
                            SoLuong = (row.Count > 5 && int.TryParse(row[5]?.ToString(), out int sl)) ? sl : 0,
                            DonGiaVND = parseCurrency(row.Count > 6 ? row[6]?.ToString() : "0"),
                            ThanhTienVND = parseCurrency(row.Count > 7 ? row[7]?.ToString() : "0"),
                            GhiChu = row.Count > 8 ? row[8]?.ToString() : "",
                            GiaNhap = parseCurrency(row.Count > 9 ? row[9]?.ToString() : "0"),
                            ThanhTien = parseCurrency(row.Count > 10 ? row[10]?.ToString() : "0"),
                            BangGia = parseCurrency(row.Count > 11 ? row[11]?.ToString() : "0"),
                            IsHeader = row.Count > 4 && row[4]?.ToString()?.Trim() == "TỦ",
                            SheetRowIndex = i // Lưu vị trí dòng trên sheet (0-based, tương ứng row 2+)
                        });
                    }
                    allSavedConfigs.Clear();
                    allSavedConfigs.AddRange(newSavedItems);
                }

                // Đọc formatting (màu nền, màu chữ) từ Google Sheet
                await FetchSheetFormattingAsync();
            }
            catch { }
        }

        /// <summary>
        /// Đọc màu nền và màu chữ của từng ô trong config sheet.
        /// Kết quả lưu vào _sheetBgColors và _sheetFgColors để áp dụng lên DGV.
        /// </summary>
        private async Task FetchSheetFormattingAsync()
        {
            try
            {
                var getRequest = _sheetsService.Spreadsheets.Get(spreadsheetId);
                getRequest.Ranges = new[] { $"{configSheetName}!A2:L1000" };
                getRequest.IncludeGridData = true;
                var spreadsheet = await getRequest.ExecuteAsync();

                _sheetBgColors.Clear();
                _sheetFgColors.Clear();

                var sheet = spreadsheet.Sheets?.FirstOrDefault();
                if (sheet?.Data == null || sheet.Data.Count == 0) return;

                var gridData = sheet.Data[0];
                if (gridData.RowData == null) return;

                for (int r = 0; r < gridData.RowData.Count; r++)
                {
                    var rowData = gridData.RowData[r];
                    if (rowData.Values == null) continue;

                    for (int c = 0; c < rowData.Values.Count && c < 12; c++)
                    {
                        var cell = rowData.Values[c];
                        if (cell?.UserEnteredFormat == null) continue;

                        // Màu nền
                        var bg = cell.UserEnteredFormat.BackgroundColor;
                        if (bg != null)
                        {
                            int red = (int)((bg.Red ?? 1f) * 255);
                            int green = (int)((bg.Green ?? 1f) * 255);
                            int blue = (int)((bg.Blue ?? 1f) * 255);
                            // Bỏ qua trắng (mặc định)
                            if (!(red >= 250 && green >= 250 && blue >= 250))
                                _sheetBgColors[(r, c)] = Color.FromArgb(red, green, blue);
                        }

                        // Màu chữ
                        var fg = cell.UserEnteredFormat.TextFormat?.ForegroundColor;
                        if (fg != null)
                        {
                            int red = (int)((fg.Red ?? 0f) * 255);
                            int green = (int)((fg.Green ?? 0f) * 255);
                            int blue = (int)((fg.Blue ?? 0f) * 255);
                            // Bỏ qua đen (mặc định)
                            if (!(red <= 5 && green <= 5 && blue <= 5))
                                _sheetFgColors[(r, c)] = Color.FromArgb(red, green, blue);
                        }
                    }
                }
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════════════════
        // ĐỊNH DẠNG GIAO DIỆN (FORMAT / STYLE)
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Định dạng hai grid sản phẩm (dgvAllProducts & dataGridView1):
        /// ẩn cột không cần thiết, đặt header text, chỉ cho phép sửa checkbox IsSelected.
        /// </summary>
        private void FormatDataGridView(DataGridView dgv)
        {
            if (dgv == null || dgv.IsDisposed || dgv.Columns == null || dgv.Columns.Count == 0) return;

            try
            {
                dgv.ColumnHeadersVisible = true;
                // Sử dụng mảng cố định để duyệt để tránh lỗi đồng bộ
                var cols = dgv.Columns.Cast<DataGridViewColumn>().ToList();

                foreach (var col in cols)
                {
                    if (col == null || col.DataGridView == null) continue;
                    
                    string colName = col.Name;

                    // 1. Hide unwanted columns
                    if (colName == "Id" || colName == "Weight" || colName == "Length" || colName == "Width" || colName == "Height" || colName == "SheetRowIndex")
                    {
                        col.Visible = false;
                        continue;
                    }

                    // 2. Set headers and format
                    if (colName == "Name") col.HeaderText = "Tên sản phẩm";
                    else if (colName == "Model") col.HeaderText = "Model";
                    else if (colName == "SKU") col.HeaderText = "Mã SKU";
                    else if (colName == "Price")
                    {
                        col.HeaderText = "Giá bán";
                        col.DefaultCellStyle.Format = "N0";
                    }
                    else if (colName == "HÃNG") col.HeaderText = "Hãng";
                    else if (colName == "Category") col.HeaderText = "Danh mục";
                    else if (colName == "IsSelected")
                    {
                        // Chỉ hiện checkbox ở dataGridView1 (grid phải - Sản phẩm con/relation)
                        if (dgv == dataGridView1)
                        {
                            col.HeaderText = "Chọn";
                            try { col.ReadOnly = false; } catch { }
                            try { col.DisplayIndex = dgv.Columns.Count - 1; } catch { }
                        }
                        else
                        {
                            col.Visible = false;
                            continue;
                        }
                    }

                    // 3. Global ReadOnly
                    if (colName != "IsSelected")
                    {
                        try { col.ReadOnly = true; } catch { }
                    }
                }

                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgv.MultiSelect = true;
            }
            catch (Exception)
            {
                // Silently ignore layout-related exceptions during binding
            }
        }
        // ══════════════════════════════════════════════════════════════════
        // BỘ LỌC QUAN HỆ (COMBO SẢN PHẨM CHÍNH – DANH MỤC PR)
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Khi đổi sản phẩm chính (comboBox2), lọc lại danh sách Danh mục PR (comboBox1)
        /// và cập nhật grid sản phẩm con (dataGridView1) tương ứng.
        /// </summary>
        private void ComboBox2_SelectedValueChanged(object sender, EventArgs e)
        {
            if (isUpdatingComboBoxes) return;

            int selectedId = 0;
            if (comboBox2.SelectedValue != null) int.TryParse(comboBox2.SelectedValue.ToString(), out selectedId);

            isUpdatingComboBoxes = true;
            try
            {
                if (selectedId > 0)
                {
                    var catPRs = productRelations
                        .Where(r => r.ID_Product_Main == selectedId)
                        .Select(r => r.Category_PR)
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Distinct()
                        .ToList();
                    
                    catPRs.Insert(0, "-- Tất cả danh mục --");
                    string currentCat = comboBox1.SelectedItem?.ToString();

                    comboBox1.DataSource = catPRs;

                    if (catPRs.Contains(currentCat))
                        comboBox1.SelectedItem = currentCat;
                }
                else
                {
                    var catPRs = productRelations.Select(r => r.Category_PR).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                    catPRs.Insert(0, "-- Tất cả danh mục --");
                    string currentCat = comboBox1.SelectedItem?.ToString();

                    comboBox1.DataSource = catPRs;

                    if (catPRs.Contains(currentCat))
                        comboBox1.SelectedItem = currentCat;
                }
            }
            finally
            {
                isUpdatingComboBoxes = false;
            }
        }

        /// <summary>
        /// Khi đổi Danh mục PR (comboBox1), lọc lại danh sách sản phẩm chính (comboBox2)
        /// cho phù hợp và cập nhật grid sản phẩm con.
        /// </summary>
        private void ComboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (isUpdatingComboBoxes) return;

            isUpdatingComboBoxes = true;
            try
            {
                string selectedCatPR = comboBox1.SelectedItem?.ToString();
                
                var relationProductIds = productRelations.Select(r => r.ID_Product_Main)
                    .Distinct()
                    .ToList();

                if (!string.IsNullOrEmpty(selectedCatPR) && selectedCatPR != "-- Tất cả danh mục --")
                {
                    relationProductIds = productRelations
                        .Where(r => string.Equals(r.Category_PR?.Trim(), selectedCatPR.Trim(), StringComparison.OrdinalIgnoreCase))
                        .Select(r => r.ID_Product_Main)
                        .Distinct()
                        .ToList();
                }

                var relationProductsDisplay = allProducts
                    .Where(p => relationProductIds.Contains(p.Id))
                    .Select(p => new { Id = p.Id, Name = p.Name })
                    .OrderBy(p => p.Name)
                    .ToList();
                relationProductsDisplay.Insert(0, new { Id = 0, Name = "-- Chọn sản phẩm --" });

                object currentProdId = comboBox2.SelectedValue;

                comboBox2.DataSource = relationProductsDisplay;
                comboBox2.DisplayMember = "Name";
                comboBox2.ValueMember = "Id";
                if (currentProdId != null && relationProductsDisplay.Any(p => p.Id.ToString() == currentProdId.ToString()))
                {
                    comboBox2.SelectedValue = currentProdId;
                }
            }
            finally
            {
                isUpdatingComboBoxes = false;
            }
        }

        private void BtnAddFromRelation_Click(object sender, EventArgs e)
        {
            string selectedCatPR = comboBox1.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedCatPR) || selectedCatPR == "-- Tất cả danh mục --") selectedCatPR = null;

            int selectedId = 0;
            if (comboBox2.SelectedValue != null) int.TryParse(comboBox2.SelectedValue.ToString(), out selectedId);

            if (selectedId <= 0 && string.IsNullOrEmpty(selectedCatPR))
            {
                childProducts.Clear();
                return;
            }

            // Tìm các sản phẩm con (Child) dựa trên Sản phẩm chính (Main) đã chọn
            var relatedIds = productRelations
                .Where(r => (selectedId <= 0 || r.ID_Product_Main == selectedId) && 
                            (string.IsNullOrEmpty(selectedCatPR) || 
                             string.Equals(r.Category_PR?.Trim(), selectedCatPR?.Trim(), StringComparison.OrdinalIgnoreCase)))
                .Select(r => r.ID_Product_Child)
                .Distinct()
                .ToList();

            childProducts.Clear();
            if (relatedIds.Count > 0)
            {
                var foundProducts = allProducts.Where(p => relatedIds.Contains(p.Id)).ToList();
                foreach (var p in foundProducts)
                {
                    p.IsSelected = false;
                    childProducts.Add(p);
                }
            }
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            var selectedItems = childProducts.Where(p => p.IsSelected).ToList();
            if (selectedItems.Count == 0) return;

            // Xác định tên nhóm (header) mặc định từ Danh mục PR hoặc fallback tên sản phẩm chính
            string catPR = comboBox1.SelectedItem?.ToString();
            bool hasCatPR = !string.IsNullOrEmpty(catPR) && catPR != "-- Tất cả danh mục --";
            string defaultHeaderName = hasCatPR ? catPR : comboBox2.Text;

            // Lấy danh sách các nhóm (header) hiện có
            var existingHeaders = configProducts
                .Where(p => p.IsHeader && !string.IsNullOrWhiteSpace(p.TenHang))
                .ToList();

            string headerName;
            if (existingHeaders.Count == 0)
            {
                // Chưa có nhóm nào → tạo mới với tên mặc định
                headerName = defaultHeaderName;
            }
            else if (existingHeaders.Count == 1)
            {
                // Chỉ có 1 nhóm → thêm thẳng vào nhóm đó
                headerName = existingHeaders[0].TenHang;
            }
            else
            {
                // Nhiều nhóm → hiện dialog hỏi chọn
                headerName = ChooseHeaderDialog(existingHeaders, defaultHeaderName);
                if (headerName == null) return; // Người dùng bấm Huỷ
            }

            // Tìm vị trí header khớp tên trong danh sách cấu hình hiện tại
            int headerIdx = configProducts.FindIndex(p =>
                p.IsHeader && string.Equals(p.TenHang?.Trim(), headerName?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (headerIdx < 0)
            {
                // Chưa có header này → tạo mới và thêm vào cuối
                button5.Text = "Lưu";
                currentEditingConfigName = null;
                configProducts.Add(new ConfigProductItem
                {
                    STT = configProducts.Count + 1,
                    TenHang = headerName,
                    MaHang = "",
                    XuatXu = "VNECCO",
                    DonVi = "TỦ",
                    SoLuong = 1,
                    DonGiaVND = 0,
                    ThanhTienVND = 0,
                    GhiChu = "",
                    GiaNhap = 0,
                    ThanhTien = 0,
                    BangGia = 0,
                    IsHeader = true
                });
                headerIdx = configProducts.Count - 1;
            }

            // Tìm vị trí cuối của nhóm này (trước header kế tiếp)
            int insertAt = headerIdx + 1;
            while (insertAt < configProducts.Count && !configProducts[insertAt].IsHeader)
                    insertAt++;

            // Thêm các sản phẩm được chọn vào đúng vị trí trong nhóm
            foreach (var product in selectedItems)
            {
                int groupStart = headerIdx + 1;
                int groupEnd   = insertAt;
                bool alreadyInGroup = configProducts
                    .Skip(groupStart).Take(groupEnd - groupStart)
                    .Any(p => !p.IsHeader && p.MaHang == product.SKU);
                if (alreadyInGroup) continue;

                decimal price = 0;
                decimal.TryParse(product.Price?.Replace(".", "").Replace(",", ""), out price);

                configProducts.Insert(insertAt, new ConfigProductItem
                {
                    TenHang = product.Name,
                    MaHang = product.SKU,
                    XuatXu = product.HÃNG,
                    DonVi = "Cái",
                    SoLuong = 1,
                    DonGiaVND = price,
                    ThanhTienVND = price,
                    GhiChu = "",
                    GiaNhap = price,
                    ThanhTien = price,
                    BangGia = price,
                    IsHeader = false
                });
                insertAt++;
            }

            // Reset IsSelected
            foreach (var p in selectedItems) p.IsSelected = false;

            // Cập nhật lại STT toàn bộ
            for (int i = 0; i < configProducts.Count; i++)
                configProducts[i].STT = i + 1;

            UpdateHeaderSum();
            UpdateConfigGrid();
            dataGridView1.Refresh();
        }

        /// <summary>
        /// Hiện dialog cho người dùng chọn nhóm (I, II, III...) để thêm sản phẩm vào.
        /// Trả về tên header được chọn, hoặc null nếu người dùng Cancel.
        /// </summary>
        private string ChooseHeaderDialog(List<ConfigProductItem> headers, string defaultHeaderName)
        {
            using (var frm = new Form())
            {
                frm.Text = "Chọn mục để thêm vào";
                frm.StartPosition = FormStartPosition.CenterParent;
                frm.FormBorderStyle = FormBorderStyle.FixedDialog;
                frm.MaximizeBox = false;
                frm.MinimizeBox = false;
                frm.Width = 380;
                frm.Height = 120 + headers.Count * 38 + 48;
                frm.Font = new Font("Times New Roman", 9.5f, FontStyle.Regular);

                var lbl = new Label
                {
                    Text = "Danh sách cấu hình có nhiều mục.\nBạn muốn thêm sản phẩm vào mục nào?",
                    AutoSize = false,
                    Size = new Size(340, 40),
                    Location = new Point(16, 12),
                    Font = new Font("Times New Roman", 9.5f, FontStyle.Bold)
                };
                frm.Controls.Add(lbl);

                int btnY = 60;
                string chosen = null;

                for (int i = 0; i < headers.Count; i++)
                {
                    var h = headers[i];
                    string roman = ToRomanNumeral(i + 1);
                    var btn = new Button
                    {
                        Text = $"{roman}.  {h.TenHang}",
                        Location = new Point(16, btnY),
                        Size = new Size(340, 32),
                        Tag = h.TenHang,
                        Font = new Font("Times New Roman", 9.5f, FontStyle.Bold),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.LightGreen,
                        ForeColor = Color.Black,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Padding = new Padding(8, 0, 0, 0)
                    };
                    btn.FlatAppearance.BorderColor = Color.SeaGreen;
                    btn.Click += (s, ev) =>
                    {
                        chosen = (string)((Button)s).Tag;
                        frm.DialogResult = DialogResult.OK;
                        frm.Close();
                    };
                    frm.Controls.Add(btn);
                    btnY += 38;
                }

                // Nút Huỷ
                var btnCancel = new Button
                {
                    Text = "Huỷ",
                    Location = new Point(16, btnY + 6),
                    Size = new Size(80, 28),
                    DialogResult = DialogResult.Cancel,
                    Font = new Font("Times New Roman", 9.5f, FontStyle.Regular)
                };
                frm.CancelButton = btnCancel;
                frm.Controls.Add(btnCancel);

                return frm.ShowDialog(this) == DialogResult.OK ? chosen : null;
            }
        }

        private async void Button5_Click(object sender, EventArgs e)
        {
            if (configProducts.Count == 0)
            {
                MessageBox.Show("Danh sách cấu hình đang trống, không thể lưu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                this.Cursor = Cursors.WaitCursor;
                string action = button5.Text;
                await SaveConfigToSheetsAsync();
                
                this.Cursor = Cursors.Default;
                MessageBox.Show($"{action} cấu hình lên Google Sheets thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Lỗi khi lưu dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SaveConfigToSheetsAsync()
        {
            if (_sheetsService == null) InitGoogleSheetsService();

            // ── Tách configProducts thành từng nhóm theo header ──
            // Mỗi nhóm gồm: 1 dòng header + các sản phẩm con ngay sau
            var groups = new List<List<ConfigProductItem>>();
            List<ConfigProductItem> currentGroup = null;
            foreach (var item in configProducts)
            {
                if (item.IsHeader)
                {
                    currentGroup = new List<ConfigProductItem> { item };
                    groups.Add(currentGroup);
                }
                else if (currentGroup != null)
                {
                    currentGroup.Add(item);
                }
                else
                {
                    // Sản phẩm không thuộc nhóm nào (không có header trước) → tạo nhóm ảo
                    currentGroup = new List<ConfigProductItem> { item };
                    groups.Add(currentGroup);
                }
            }

            // ── Bắt đầu từ bản sao của allSavedConfigs ──
            // Với mỗi nhóm trong configProducts:
            //   - Nếu TenHang của header TRÙNG với header đã lưu → thay thế nhóm cũ
            //   - Nếu KHÔNG trùng → thêm mới vào cuối
            var workingList = allSavedConfigs.ToList();

            foreach (var group in groups)
            {
                var groupHeader = group.FirstOrDefault(x => x.IsHeader);
                string groupHeaderName = groupHeader?.TenHang?.Trim();

                if (!string.IsNullOrEmpty(groupHeaderName))
                {
                    // Tìm header trùng tên trong danh sách hiện tại
                    int existingHeaderIdx = workingList.FindIndex(c =>
                        c.IsHeader &&
                        string.Equals(c.TenHang?.Trim(), groupHeaderName, StringComparison.OrdinalIgnoreCase));

                    if (existingHeaderIdx >= 0)
                    {
                        // Tìm vị trí kết thúc của nhóm cũ (header tiếp theo hoặc hết list)
                        int existingGroupEnd = workingList.FindIndex(existingHeaderIdx + 1, c => c.IsHeader);
                        if (existingGroupEnd < 0) existingGroupEnd = workingList.Count;

                        // Xóa nhóm cũ và chèn nhóm mới vào đúng vị trí
                        workingList.RemoveRange(existingHeaderIdx, existingGroupEnd - existingHeaderIdx);
                        workingList.InsertRange(existingHeaderIdx, group);
                    }
                    else
                    {
                        // Chưa có → thêm mới vào cuối
                        workingList.AddRange(group);
                    }
                }
                else
                {
                    // Không có tên header → luôn thêm mới
                    workingList.AddRange(group);
                }
            }

            List<ConfigProductItem> finalDataToSave = workingList;


            // Gán lại tham chiếu
            allSavedConfigs = finalDataToSave.ToList();

            // 1. Chuẩn bị dữ liệu để ghi
            var valueRange = new Google.Apis.Sheets.v4.Data.ValueRange();
            var objectList = new List<IList<object>>();

            // Tính STT hiển thị đúng cho từng dòng
            int headerOrder = 0;
            int productOrder = 0;

            for (int i = 0; i < finalDataToSave.Count; i++)
            {
                var item = finalDataToSave[i];

                string displaySTT;
                if (item.IsSummary)
                {
                    displaySTT = ""; // Dòng tổng: để trống
                }
                else if (item.IsHeader)
                {
                    headerOrder++;
                    productOrder = 0; // Reset số thứ tự sản phẩm cho nhóm mới
                    displaySTT = ToRomanNumeral(headerOrder); // I, II, III...
                }
                else
                {
                    productOrder++;
                    displaySTT = productOrder.ToString(); // 1, 2, 3...
                }

                var row = new List<object>
                {
                    displaySTT,
                    item.TenHang,
                    item.MaHang,
                    item.XuatXu,
                    item.DonVi,
                    item.SoLuong,
                    item.DonGiaVND,
                    item.ThanhTienVND,
                    item.GhiChu,
                    item.GiaNhap,
                    item.ThanhTien,
                    item.BangGia
                };
                objectList.Add(row);
            }
            valueRange.Values = objectList;

            // 2. Trước khi ghi, ta xóa dữ liệu cũ (từ dòng 2 trở xuống)
            string clearRange = $"{configSheetName}!A2:L1000"; 
            var clearRequest = _sheetsService.Spreadsheets.Values.Clear(new Google.Apis.Sheets.v4.Data.ClearValuesRequest(), spreadsheetId, clearRange);
            await clearRequest.ExecuteAsync();

            // 3. Ghi dữ liệu mới vào
            string updateRange = $"{configSheetName}!A2";
            var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, updateRange);
            updateRequest.ValueInputOption = Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await updateRequest.ExecuteAsync();

            // 3b. Tính tổng và ghi 3 dòng tóm tắt ở dưới cùng
            // Giống DGV: chỉ hiện GiaNhap(J=col9), ThanhTien(K=col10) — ẩn DonGiaVND(G) & ThanhTienVND(H)
            decimal tongCongThanhTienVND = finalDataToSave.Where(x => !x.IsHeader).Sum(x => x.ThanhTienVND);
            decimal tongCongGiaNhap     = finalDataToSave.Where(x => !x.IsHeader).Sum(x => x.GiaNhap);
            decimal tongCongThanhTien   = finalDataToSave.Where(x => !x.IsHeader).Sum(x => x.ThanhTien);

            decimal thueMul = 0.08m;
            decimal vatGiaNhap      = Math.Round(tongCongGiaNhap    * thueMul, 0);
            decimal vatThanhTien    = Math.Round(tongCongThanhTien  * thueMul, 0);

            decimal totalGiaNhap    = tongCongGiaNhap    + vatGiaNhap;
            decimal totalThanhTien  = tongCongThanhTien  + vatThanhTien;

            int summaryStartRow = finalDataToSave.Count + 2; // +2 vì row 1 là header cột

            // Cột: A(0) B(1) C(2) D(3) E(4) F(5) G(6) H(7) I(8) J(9)       K(10)           L(11)
            var summaryValues = new Google.Apis.Sheets.v4.Data.ValueRange
            {
                Values = new List<IList<object>>
                {
                    // TỔNG CỘNG: chỉ J=GiaNhap, K=ThanhTien (giống DGV ẩn G/H)
                    new List<object> { "", "TỔNG CỘNG (Giá chưa bao gồm VAT)", "", "", "", "",
                        "", "", "", tongCongGiaNhap, tongCongThanhTien, "" },
                    // THUẾ VAT 8%: J=vatGiaNhap, K=vatThanhTien
                    new List<object> { "", "THUẾ VAT 8%", "", "", "", "",
                        "", "", "", vatGiaNhap, vatThanhTien, "" },
                    // THÀNH TIỀN: J=totalGiaNhap, K=totalThanhTien
                    new List<object> { "", "THÀNH TIỀN", "", "", "", "",
                        "", "", "", totalGiaNhap, totalThanhTien, "" }
                }
            };
            string summaryRange = $"{configSheetName}!A{summaryStartRow}";
            var summaryRequest = _sheetsService.Spreadsheets.Values.Update(summaryValues, spreadsheetId, summaryRange);
            summaryRequest.ValueInputOption = Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await summaryRequest.ExecuteAsync();


            // Không cập nhật lại currentEditingConfigName ở đây nữa vì sẽ được reset ở sự kiện Click
            // Không cập nhật lại currentEditingConfigName ở đây nữa vì sẽ được reset ở sự kiện Click
            // Đổ lại danh sách Tên Header vào lstSavedConfigs
            var headerNames = allSavedConfigs
                .Where(c => c.IsHeader && !string.IsNullOrEmpty(c.TenHang) && !c.TenHang.Trim().StartsWith("--"))
                .Select(c => c.TenHang)
                .Distinct()
                .ToList();
            
            lstSavedConfigs.ClearItems();
            foreach (var name in headerNames)
            {
                lstSavedConfigs.AddItem(name);
            }

            // 4. Tô màu dòng Header
            try
            {
                var spreadsheet = await _sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
                var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == configSheetName);
                if (sheet != null)
                {
                    int sheetId = sheet.Properties.SheetId.Value;
                    var requests = new List<Google.Apis.Sheets.v4.Data.Request>();

                    // Reset TOÀN BỘ định dạng cũ vùng dữ liệu (A2:L1000)
                    // Dùng "userEnteredFormat" (không có sub-field) để xóa sạch cả màu nền lẫn màu chữ
                    requests.Add(new Google.Apis.Sheets.v4.Data.Request
                    {
                        UpdateCells = new Google.Apis.Sheets.v4.Data.UpdateCellsRequest
                        {
                            Range = new Google.Apis.Sheets.v4.Data.GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = 1, // Dòng 2
                                EndRowIndex = 1000,
                                StartColumnIndex = 0,
                                EndColumnIndex = 12 // Cột L
                            },
                            Fields = "userEnteredFormat"
                        }
                    });

                    // ── Tô màu dòng HEADER CỘT (row index 0 = dòng 1 trên sheet) giống DGV ──
                    // Cột STT→GhiChu (0-8): nền vàng nhạt #FFEB9C, chữ xanh đậm #1E497D, bold
                    requests.Add(new Google.Apis.Sheets.v4.Data.Request
                    {
                        RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                        {
                            Range = new Google.Apis.Sheets.v4.Data.GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = 0, EndRowIndex = 1,
                                StartColumnIndex = 0, EndColumnIndex = 9
                            },
                            Cell = new Google.Apis.Sheets.v4.Data.CellData
                            {
                                UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                                {
                                    BackgroundColor = new Google.Apis.Sheets.v4.Data.Color
                                        { Red = 1.0f, Green = 0.922f, Blue = 0.612f }, // #FFEB9C vàng nhạt
                                    TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat
                                    {
                                        Bold = true,
                                        ForegroundColor = new Google.Apis.Sheets.v4.Data.Color
                                            { Red = 0.122f, Green = 0.286f, Blue = 0.490f } // #1F497D xanh đậm
                                    },
                                    HorizontalAlignment = "CENTER",
                                    VerticalAlignment = "MIDDLE",
                                    WrapStrategy = "WRAP"
                                }
                            },
                            Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"
                        }
                    });
                    // Cột GiaNhap/ThanhTien/BangGia (9-11): nền xanh dương #0070C0, chữ trắng, bold
                    requests.Add(new Google.Apis.Sheets.v4.Data.Request
                    {
                        RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                        {
                            Range = new Google.Apis.Sheets.v4.Data.GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = 0, EndRowIndex = 1,
                                StartColumnIndex = 9, EndColumnIndex = 12
                            },
                            Cell = new Google.Apis.Sheets.v4.Data.CellData
                            {
                                UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                                {
                                    BackgroundColor = new Google.Apis.Sheets.v4.Data.Color
                                        { Red = 0.0f, Green = 0.439f, Blue = 0.753f }, // #0070C0 xanh dương
                                    TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat
                                    {
                                        Bold = true,
                                        ForegroundColor = new Google.Apis.Sheets.v4.Data.Color
                                            { Red = 1.0f, Green = 1.0f, Blue = 1.0f } // Trắng
                                    },
                                    HorizontalAlignment = "CENTER",
                                    VerticalAlignment = "MIDDLE",
                                    WrapStrategy = "WRAP"
                                }
                            },
                            Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"
                        }
                    });

                    // ── Tô màu các dòng là Header nhóm (xanh lá - giống DataGridView) ──
                    for (int i = 0; i < finalDataToSave.Count; i++)
                    {
                        if (finalDataToSave[i].IsHeader)
                        {
                            requests.Add(new Google.Apis.Sheets.v4.Data.Request
                            {
                                RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                                {
                                    Range = new Google.Apis.Sheets.v4.Data.GridRange
                                    {
                                        SheetId = sheetId,
                                        StartRowIndex = i + 1, // i + 1 vì dòng 2 là index 1
                                        EndRowIndex = i + 2,
                                        StartColumnIndex = 0,
                                        EndColumnIndex = 12
                                    },
                                    Cell = new Google.Apis.Sheets.v4.Data.CellData
                                    {
                                        UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                                        {
                                            BackgroundColor = new Google.Apis.Sheets.v4.Data.Color
                                            { Red = 0.565f, Green = 0.933f, Blue = 0.565f }, // LightGreen
                                            TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true },
                                            NumberFormat = new Google.Apis.Sheets.v4.Data.NumberFormat { Type = "NUMBER", Pattern = "#,##0" }
                                        }
                                    },
                                    Fields = "userEnteredFormat(backgroundColor,textFormat,numberFormat)"
                                }
                            });
                        }
                    }

                    // ── Định dạng tiền cho cột G(6=DonGiaVND), H(7=ThanhTienVND), J(9=GiaNhap), K(10=ThanhTien), L(11=BangGia) ──
                    int[] moneyCols = { 6, 7, 9, 10, 11 };
                    foreach (int colIdx in moneyCols)
                    {
                        requests.Add(new Google.Apis.Sheets.v4.Data.Request
                        {
                            RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                            {
                                Range = new Google.Apis.Sheets.v4.Data.GridRange
                                {
                                    SheetId = sheetId,
                                    StartRowIndex = 1,
                                    EndRowIndex = 1000,
                                    StartColumnIndex = colIdx,
                                    EndColumnIndex = colIdx + 1
                                },
                                Cell = new Google.Apis.Sheets.v4.Data.CellData
                                {
                                    UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                                    {
                                        NumberFormat = new Google.Apis.Sheets.v4.Data.NumberFormat { Type = "NUMBER", Pattern = "#,##0" }
                                    }
                                },
                                Fields = "userEnteredFormat.numberFormat"
                            }
                        });
                    }

                    // Tô màu 3 dòng tóm tắt cuối (vàng + bold + chữ đỏ cho cột số - giống DataGridView)
                    int baseSummaryRow = finalDataToSave.Count + 1;
                    var summaryBgColor = new Google.Apis.Sheets.v4.Data.Color
                    { Red = 1.0f, Green = 1.0f, Blue = 0.0f }; // Vàng #FFFF00

                    for (int s = 0; s < 3; s++)
                    {
                        // Nền vàng + bold cho toàn dòng
                        requests.Add(new Google.Apis.Sheets.v4.Data.Request
                        {
                            RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                            {
                                Range = new Google.Apis.Sheets.v4.Data.GridRange
                                {
                                    SheetId = sheetId,
                                    StartRowIndex = baseSummaryRow + s,
                                    EndRowIndex = baseSummaryRow + s + 1,
                                    StartColumnIndex = 0, EndColumnIndex = 12
                                },
                                Cell = new Google.Apis.Sheets.v4.Data.CellData
                                {
                                    UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                                    {
                                        BackgroundColor = summaryBgColor,
                                        TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true },
                                        NumberFormat = new Google.Apis.Sheets.v4.Data.NumberFormat { Type = "NUMBER", Pattern = "#,##0" }
                                    }
                                },
                                Fields = "userEnteredFormat(backgroundColor,textFormat,numberFormat)"
                            }
                        });

                        // Chữ đỏ cho cột số tiền chỉ J=9, K=10 (giống DGV - không tô G/H)
                        foreach (int colIdx in moneyCols)
                        {
                            requests.Add(new Google.Apis.Sheets.v4.Data.Request
                            {
                                RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                                {
                                    Range = new Google.Apis.Sheets.v4.Data.GridRange
                                    {
                                        SheetId = sheetId,
                                        StartRowIndex = baseSummaryRow + s,
                                        EndRowIndex = baseSummaryRow + s + 1,
                                        StartColumnIndex = colIdx,
                                        EndColumnIndex = colIdx + 1
                                    },
                                    Cell = new Google.Apis.Sheets.v4.Data.CellData
                                    {
                                        UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                                        {
                                            TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat
                                            {
                                                Bold = true,
                                                ForegroundColor = new Google.Apis.Sheets.v4.Data.Color
                                                { Red = 1.0f, Green = 0.0f, Blue = 0.0f } // Đỏ
                                            }
                                        }
                                    },
                                    Fields = "userEnteredFormat.textFormat"
                                }
                            });
                        }
                    }

                    // Tô màu per-cell từ color picker (nếu user đã set màu riêng)
                    // Tạo mapping: DGV column index → Sheet column index (0-11)
                    string[] sheetColOrder = { "STT", "TenHang", "MaHang", "XuatXu", "DonVi", "SoLuong",
                        "DonGiaVND", "ThanhTienVND", "GhiChu", "GiaNhap", "ThanhTien", "BangGia" };
                    var dgvToSheetCol = new Dictionary<int, int>();
                    for (int sc = 0; sc < sheetColOrder.Length; sc++)
                    {
                        if (dgvParentProducts.Columns.Contains(sheetColOrder[sc]))
                            dgvToSheetCol[dgvParentProducts.Columns[sheetColOrder[sc]].Index] = sc;
                    }

                    // ── Build map: configProduct index → sheet row index (0-based) ──
                    // Duyệt finalDataToSave để tìm vị trí thực của từng item trong configProducts
                    // (vì sau merge, configProducts có thể nằm rải rác trong finalDataToSave)
                    var configProductToSheetRow = new Dictionary<int, int>(); // key: configProducts index, value: finalDataToSave index
                    {
                        // Dùng reference equality để match
                        var configSet = new HashSet<ConfigProductItem>(configProducts);
                        for (int fi = 0; fi < finalDataToSave.Count; fi++)
                        {
                            if (configSet.Contains(finalDataToSave[fi]))
                            {
                                int cpIdx = configProducts.IndexOf(finalDataToSave[fi]);
                                if (cpIdx >= 0)
                                    configProductToSheetRow[cpIdx] = fi;
                            }
                        }
                    }

                    int configRowCount = configProducts.Where(p => !p.IsSummary).Count();

                    // Tô màu nền per-cell từ color picker (chuột phải → chọn màu)
                    foreach (var kvp in _cellBgColors)
                    {
                        int dgvRow = kvp.Key.r;
                        int dgvCol = kvp.Key.c;
                        if (dgvRow >= configRowCount) continue; // Bỏ qua summary rows
                        if (!dgvToSheetCol.ContainsKey(dgvCol)) continue;
                        if (!configProductToSheetRow.ContainsKey(dgvRow)) continue;

                        int sheetRow = configProductToSheetRow[dgvRow] + 1; // +1 vì row 1 là header cột
                        int sheetCol = dgvToSheetCol[dgvCol];
                        System.Drawing.Color c = kvp.Value;

                        requests.Add(new Google.Apis.Sheets.v4.Data.Request
                        {
                            RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                            {
                                Range = new Google.Apis.Sheets.v4.Data.GridRange
                                {
                                    SheetId = sheetId,
                                    StartRowIndex = sheetRow,
                                    EndRowIndex = sheetRow + 1,
                                    StartColumnIndex = sheetCol,
                                    EndColumnIndex = sheetCol + 1
                                },
                                Cell = new Google.Apis.Sheets.v4.Data.CellData
                                {
                                    UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                                    {
                                        BackgroundColor = new Google.Apis.Sheets.v4.Data.Color
                                        {
                                            Red = c.R / 255f, Green = c.G / 255f, Blue = c.B / 255f
                                        }
                                    }
                                },
                                Fields = "userEnteredFormat.backgroundColor"
                            }
                        });
                    }

                    // Tô màu chữ per-cell từ color picker
                    foreach (var kvp in _cellFgColors)
                    {
                        int dgvRow = kvp.Key.r;
                        int dgvCol = kvp.Key.c;
                        if (dgvRow >= configRowCount) continue;
                        if (!dgvToSheetCol.ContainsKey(dgvCol)) continue;
                        if (!configProductToSheetRow.ContainsKey(dgvRow)) continue;

                        int sheetRow = configProductToSheetRow[dgvRow] + 1;
                        int sheetCol = dgvToSheetCol[dgvCol];
                        System.Drawing.Color c = kvp.Value;

                        requests.Add(new Google.Apis.Sheets.v4.Data.Request
                        {
                            RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                            {
                                Range = new Google.Apis.Sheets.v4.Data.GridRange
                                {
                                    SheetId = sheetId,
                                    StartRowIndex = sheetRow,
                                    EndRowIndex = sheetRow + 1,
                                    StartColumnIndex = sheetCol,
                                    EndColumnIndex = sheetCol + 1
                                },
                                Cell = new Google.Apis.Sheets.v4.Data.CellData
                                {
                                    UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                                    {
                                        TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat
                                        {
                                            ForegroundColor = new Google.Apis.Sheets.v4.Data.Color
                                            {
                                                Red = c.R / 255f, Green = c.G / 255f, Blue = c.B / 255f
                                            }
                                        }
                                    }
                                },
                                Fields = "userEnteredFormat.textFormat.foregroundColor"
                            }
                        });
                    }

                    // Luôn thực thi (ít nhất có clear + 3 summary rows)
                    var batchUpdate = new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest { Requests = requests };
                    await _sheetsService.Spreadsheets.BatchUpdate(batchUpdate, spreadsheetId).ExecuteAsync();
                }
            }
            catch { /* Bỏ qua lỗi định dạng nếu có */ }
        }


        private void BtnAddParent_Click(object sender, EventArgs e)
        {
            // Lấy sản phẩm từ các dòng đang được chọn trên grid (highlight xanh)
            var selectedItems = dgvAllProducts.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as Products)
                .Where(p => p != null)
                .ToList();

            if (selectedItems.Count == 0) return;

            // Lấy danh sách các nhóm (header) hiện có
            var existingHeaders = configProducts
                .Where(p => p.IsHeader && !string.IsNullOrWhiteSpace(p.TenHang))
                .ToList();

            string targetHeaderName;

            if (existingHeaders.Count == 0)
            {
                // Chưa có nhóm → tạo mới, dùng tên sản phẩm đầu tiên làm header
                targetHeaderName = selectedItems[0].Name;
                button5.Text = "Lưu";
                currentEditingConfigName = null;
                configProducts.Add(new ConfigProductItem
                {
                    STT = 1,
                    TenHang = targetHeaderName,
                    MaHang = "",
                    XuatXu = "VNECCO",
                    DonVi = "TỦ",
                    SoLuong = 1,
                    DonGiaVND = 0,
                    ThanhTienVND = 0,
                    GhiChu = "",
                    GiaNhap = 0,
                    ThanhTien = 0,
                    BangGia = 0,
                    IsHeader = true
                });
            }
            else if (existingHeaders.Count == 1)
            {
                // Chỉ có 1 nhóm → thêm thẳng
                targetHeaderName = existingHeaders[0].TenHang;
            }
            else
            {
                // Nhiều nhóm → hiện dialog hỏi chọn
                targetHeaderName = ChooseHeaderDialog(existingHeaders, selectedItems[0].Name);
                if (targetHeaderName == null) return; // Người dùng bấm Huỷ
            }

            // Tìm vị trí header đã chọn
            int headerIdx = configProducts.FindIndex(p =>
                p.IsHeader && string.Equals(p.TenHang?.Trim(), targetHeaderName?.Trim(), StringComparison.OrdinalIgnoreCase));

            // Tìm vị trí cuối nhóm (trước header kế)
            int insertAt = headerIdx + 1;
            while (insertAt < configProducts.Count && !configProducts[insertAt].IsHeader)
                insertAt++;

            // Thêm sản phẩm vào nhóm đã chọn
            foreach (var product in selectedItems)
            {
                int groupStart = headerIdx + 1;
                bool alreadyInGroup = configProducts
                    .Skip(groupStart).Take(insertAt - groupStart)
                    .Any(p => !p.IsHeader && p.MaHang == product.SKU);
                if (alreadyInGroup) continue;

                decimal price = 0;
                decimal.TryParse(product.Price?.Replace(".", "").Replace(",", ""), out price);

                configProducts.Insert(insertAt, new ConfigProductItem
                {
                    STT = insertAt + 1,
                    TenHang = product.Name,
                    MaHang = product.SKU,
                    XuatXu = product.HÃNG,
                    DonVi = "Cái",
                    SoLuong = 1,
                    DonGiaVND = price,
                    ThanhTienVND = price,
                    GhiChu = "",
                    GiaNhap = price,
                    ThanhTien = price,
                    BangGia = price,
                    IsHeader = false
                });
                insertAt++;
            }

            // Cập nhật lại STT toàn bộ
            for (int i = 0; i < configProducts.Count; i++)
                configProducts[i].STT = i + 1;

            UpdateHeaderSum();
            UpdateConfigGrid();
            dgvAllProducts.ClearSelection();
        }

        private void UpdateHeaderSum()
        {
            // Cập nhật tổng cho TỪ́NG header - mỗi nhóm tính riêng
            for (int i = 0; i < configProducts.Count; i++)
            {
                if (!configProducts[i].IsHeader) continue;

                // Phạm vi nhóm: từ i+1 đến header kế tiếp (hoặc cuối list)
                int groupEnd = i + 1;
                while (groupEnd < configProducts.Count && !configProducts[groupEnd].IsHeader)
                    groupEnd++;

                var groupItems = configProducts
                    .Skip(i + 1).Take(groupEnd - i - 1)
                    .Where(p => !p.IsHeader && !p.IsSummary)
                    .ToList();

                configProducts[i].DonGiaVND    = groupItems.Sum(p => p.DonGiaVND  * p.SoLuong);
                configProducts[i].ThanhTienVND = groupItems.Sum(p => p.ThanhTienVND);
                configProducts[i].GiaNhap      = groupItems.Sum(p => p.GiaNhap    * p.SoLuong);
                configProducts[i].ThanhTien    = groupItems.Sum(p => p.ThanhTien);
                configProducts[i].BangGia      = groupItems.Sum(p => p.BangGia);
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            BtnAddParent_Click(sender, e);
        }

        private void DgvParentProducts_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvParentProducts.IsCurrentCellDirty)
            {
                dgvParentProducts.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void DgvParentProducts_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var columnName = dgvParentProducts.Columns[e.ColumnIndex].Name;
                if (columnName == "SoLuong")
                {
                    var item = dgvParentProducts.Rows[e.RowIndex].DataBoundItem as ConfigProductItem;
                    if (item != null && !item.IsHeader)
                    {
                        item.ThanhTienVND = item.SoLuong * item.DonGiaVND;
                        item.ThanhTien = item.SoLuong * item.DonGiaVND; 
                        
                        UpdateHeaderSum();
                        dgvParentProducts.Refresh(); // Gọi refresh thay vì InvalidateRow để có thể update row Header
                    }
                }
            }
        }

        private void BtnRemoveParent_Click(object sender, EventArgs e)
        {
            if (dgvParentProducts.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvParentProducts.SelectedRows)
                {
                    var product = row.DataBoundItem as ConfigProductItem;
                    if (product != null)
                    {
                        // Không cho phép XÓA dòng Header bằng nút Delete này 
                        if (!product.IsHeader) 
                        {
                            configProducts.RemoveAll(p => p.MaHang == product.MaHang && !p.IsHeader);
                        }
                    }
                }
                
                // Cập nhật lại STT sau khi xóa
                for (int i = 0; i < configProducts.Count; i++)
                {
                    configProducts[i].STT = i + 1;
                }

                UpdateHeaderSum();
                UpdateConfigGrid();
            }
        }

        private void UpdateConfigGrid()
        {
            // Tạo bản sao cho DataSource để không ảnh hưởng configProducts gốc
            _displayList = configProducts.Where(p => !p.IsSummary).ToList();

            if (_displayList.Count > 0)
            {
                // Tính tổng (chỉ tính các dòng không phải header)
                decimal tongCongGiaNhap = _displayList.Where(p => !p.IsHeader).Sum(p => p.ThanhTien);
                decimal tongCongThanhTien = _displayList.Where(p => !p.IsHeader).Sum(p => p.ThanhTienVND);
                decimal tongCongBangGia = _displayList.Where(p => !p.IsHeader).Sum(p => p.BangGia);
                decimal vatRate = 0.08m;
                decimal vatGiaNhap = tongCongGiaNhap * vatRate;
                decimal vatThanhTien = tongCongThanhTien * vatRate;

                _displayList.Add(new ConfigProductItem
                {
                    TenHang = "TỔNG CỘNG (Giá chưa bao gồm VAT)",
                    DonGiaVND = tongCongThanhTien,
                    ThanhTienVND = tongCongThanhTien,
                    GiaNhap = tongCongGiaNhap,
                    ThanhTien = tongCongThanhTien,
                    BangGia = tongCongBangGia,
                    IsSummary = true
                });
                _displayList.Add(new ConfigProductItem
                {
                    TenHang = "THUẾ VAT 8%",
                    DonGiaVND = vatThanhTien,
                    ThanhTienVND = vatThanhTien,
                    GiaNhap = vatGiaNhap,
                    ThanhTien = vatThanhTien,
                    IsSummary = true
                });
                _displayList.Add(new ConfigProductItem
                {
                    TenHang = "THÀNH TIỀN",
                    DonGiaVND = tongCongThanhTien + vatThanhTien,
                    ThanhTienVND = tongCongThanhTien + vatThanhTien,
                    GiaNhap = tongCongGiaNhap + vatGiaNhap,
                    ThanhTien = tongCongThanhTien + vatThanhTien,
                    BangGia = tongCongBangGia,
                    IsSummary = true
                });
            }

            dgvParentProducts.DataSource = _displayList;

            // ═══ Set style trực tiếp cho từng dòng ═══
            for (int i = 0; i < _displayList.Count; i++)
            {
                var row = dgvParentProducts.Rows[i];
                var item = _displayList[i];

                if (item.IsSummary)
                {
                    // Nền VÀNG, Bold (giống bảng báo giá)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 0);
                    row.DefaultCellStyle.ForeColor = Color.Black;
                    row.DefaultCellStyle.Font = new Font("Times New Roman", 9f, FontStyle.Bold);

                    // Số tiền → chữ đỏ (tất cả cột tiền kể cả DonGiaVND, ThanhTienVND)
                    foreach (var colName in new[] { "DonGiaVND", "ThanhTienVND", "GiaNhap", "ThanhTien", "BangGia" })
                    {
                        if (dgvParentProducts.Columns.Contains(colName))
                        {
                            row.Cells[colName].Style.ForeColor = Color.Red;
                            row.Cells[colName].Style.Font = new Font("Times New Roman", 9f, FontStyle.Bold);
                        }
                    }
                }
                else if (item.IsHeader)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                    row.DefaultCellStyle.Font = new Font("Times New Roman", 8.5f, FontStyle.Bold);
                }
            }

        }
        private void FormatConfigGrid(DataGridView dgv)
        {
            if (dgv == null || dgv.IsDisposed || dgv.Columns == null || dgv.Columns.Count == 0) return;

            try
            {
                if (dgv.Columns.Contains("STT")) dgv.Columns["STT"].HeaderText = "STT";
                if (dgv.Columns.Contains("TenHang")) dgv.Columns["TenHang"].HeaderText = "Tên hàng";
                if (dgv.Columns.Contains("MaHang")) dgv.Columns["MaHang"].HeaderText = "Mã hàng";
                if (dgv.Columns.Contains("XuatXu")) dgv.Columns["XuatXu"].HeaderText = "Xuất xứ";
                if (dgv.Columns.Contains("DonVi")) dgv.Columns["DonVi"].HeaderText = "Đơn vị";
                if (dgv.Columns.Contains("SoLuong")) dgv.Columns["SoLuong"].HeaderText = "Số lượng";
                
                var viVN = new System.Globalization.CultureInfo("vi-VN");

                if (dgv.Columns.Contains("DonGiaVND"))
                {
                    dgv.Columns["DonGiaVND"].HeaderText = "Đơn giá (VNĐ)";
                    dgv.Columns["DonGiaVND"].DefaultCellStyle.Format = "N0";
                    dgv.Columns["DonGiaVND"].DefaultCellStyle.FormatProvider = viVN;
                }
                if (dgv.Columns.Contains("ThanhTienVND"))
                {
                    dgv.Columns["ThanhTienVND"].HeaderText = "Thành tiền (VNĐ)";
                    dgv.Columns["ThanhTienVND"].DefaultCellStyle.Format = "N0";
                    dgv.Columns["ThanhTienVND"].DefaultCellStyle.FormatProvider = viVN;
                }
                if (dgv.Columns.Contains("GhiChu")) dgv.Columns["GhiChu"].HeaderText = "Ghi chú";
                if (dgv.Columns.Contains("GiaNhap"))
                {
                    dgv.Columns["GiaNhap"].HeaderText = "Giá Nhập";
                    dgv.Columns["GiaNhap"].DefaultCellStyle.Format = "N0";
                    dgv.Columns["GiaNhap"].DefaultCellStyle.FormatProvider = viVN;
                }
                if (dgv.Columns.Contains("ThanhTien"))
                {
                    dgv.Columns["ThanhTien"].HeaderText = "Thành Tiền";
                    dgv.Columns["ThanhTien"].DefaultCellStyle.Format = "N0";
                    dgv.Columns["ThanhTien"].DefaultCellStyle.FormatProvider = viVN;
                }
                if (dgv.Columns.Contains("BangGia"))
                {
                    dgv.Columns["BangGia"].HeaderText = "Bảng Giá";
                    dgv.Columns["BangGia"].DefaultCellStyle.Format = "N0";
                    dgv.Columns["BangGia"].DefaultCellStyle.FormatProvider = viVN;
                }
                
                if (dgv.Columns.Contains("IsHeader")) dgv.Columns["IsHeader"].Visible = false;
                if (dgv.Columns.Contains("IsSummary")) dgv.Columns["IsSummary"].Visible = false;
                if (dgv.Columns.Contains("SheetRowIndex")) dgv.Columns["SheetRowIndex"].Visible = false;

                // ── Kiểu dáng tổng thể ───────────────────────────────────
                dgv.BackgroundColor           = Color.White;
                dgv.GridColor                 = Color.FromArgb(189, 215, 238);
                dgv.BorderStyle               = BorderStyle.FixedSingle;
                dgv.CellBorderStyle           = DataGridViewCellBorderStyle.Single; // Viền đầy đủ 4 cạnh
                dgv.RowHeadersVisible         = false;
                dgv.EnableHeadersVisualStyles = false;
                dgv.AllowUserToAddRows        = false; // Không tạo dòng trống cuối
                dgv.ColumnHeadersHeight       = 36;
                dgv.RowTemplate.Height        = 22;

                // Dòng dữ liệu: nền trắng, chữ đen
                dgv.DefaultCellStyle.BackColor          = Color.White;
                dgv.DefaultCellStyle.ForeColor          = Color.Black;
                dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 112, 192);
                dgv.DefaultCellStyle.SelectionForeColor = Color.White;
                dgv.DefaultCellStyle.Font               = new Font("Times New Roman", 8.5f);
                dgv.DefaultCellStyle.Padding            = new Padding(2, 1, 2, 1);

                // Header cột chính: nền vàng, chữ xanh đậm, bold, căn giữa
                var yellowHeader = new DataGridViewCellStyle
                {
                    BackColor  = Color.Yellow,
                    ForeColor  = Color.FromArgb(31, 73, 125),
                    Font       = new Font("Times New Roman", 8.5f, FontStyle.Bold),
                    Alignment  = DataGridViewContentAlignment.MiddleCenter,
                    WrapMode   = DataGridViewTriState.True
                };
                dgv.ColumnHeadersDefaultCellStyle = yellowHeader;

                // Cột giá: header màu xanh dương, phân biệt với cột thông tin
                var blueHeader = new DataGridViewCellStyle(yellowHeader)
                {
                    BackColor = Color.FromArgb(0, 112, 192),
                    ForeColor = Color.Black
                };
                foreach (var colName in new[] { "GiaNhap", "ThanhTien", "BangGia" })
                {
                    if (dgv.Columns.Contains(colName))
                        dgv.Columns[colName].HeaderCell.Style = blueHeader;
                }

                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv.SelectionMode       = DataGridViewSelectionMode.CellSelect;
                dgv.MultiSelect         = true;

                // FillWeight: tỉ lệ chiều rộng cột
                if (dgv.Columns.Contains("STT"))          dgv.Columns["STT"].FillWeight          = 25;
                if (dgv.Columns.Contains("TenHang"))      dgv.Columns["TenHang"].FillWeight      = 200;
                if (dgv.Columns.Contains("MaHang"))       dgv.Columns["MaHang"].FillWeight       = 80;
                if (dgv.Columns.Contains("XuatXu"))       dgv.Columns["XuatXu"].FillWeight       = 50;
                if (dgv.Columns.Contains("DonVi"))        dgv.Columns["DonVi"].FillWeight        = 40;
                if (dgv.Columns.Contains("SoLuong"))      dgv.Columns["SoLuong"].FillWeight      = 40;
                if (dgv.Columns.Contains("DonGiaVND"))    dgv.Columns["DonGiaVND"].FillWeight    = 90;
                if (dgv.Columns.Contains("ThanhTienVND")) dgv.Columns["ThanhTienVND"].FillWeight = 90;
                if (dgv.Columns.Contains("GhiChu"))       dgv.Columns["GhiChu"].FillWeight       = 70;
                if (dgv.Columns.Contains("GiaNhap"))      dgv.Columns["GiaNhap"].FillWeight      = 90;
                if (dgv.Columns.Contains("ThanhTien"))    dgv.Columns["ThanhTien"].FillWeight    = 90;
                if (dgv.Columns.Contains("BangGia"))      dgv.Columns["BangGia"].FillWeight      = 90;

                // Căn giữa cột STT, Xuất xứ, Đơn vị, Số lượng
                foreach (var colName in new[] { "STT", "XuatXu", "DonVi", "SoLuong" })
                {
                    if (dgv.Columns.Contains(colName))
                        dgv.Columns[colName].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // Căn phải cột số tiền
                foreach (var colName in new[] { "DonGiaVND", "ThanhTienVND", "GiaNhap", "ThanhTien", "BangGia" })
                {
                    if (dgv.Columns.Contains(colName))
                        dgv.Columns[colName].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.Name != "SoLuong" && col.Name != "GhiChu" && col.Name != "TenHang")
                        col.ReadOnly = true;
                }
            }
            catch (Exception) { /* Ignore lifecycle exceptions */ }
        }

        // ────────────────────────────────────────────────────────────────────────
        // CellPainting: vẽ cột STT riêng (La Mã cho header, số thứ tự cho data)
        // ────────────────────────────────────────────────────────────────────────
        private void DgvParentProducts_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvParentProducts.Columns[e.ColumnIndex].Name != "STT") return;
            if (_displayList == null || e.RowIndex >= _displayList.Count) return;

            var item = _displayList[e.RowIndex];
            string displayValue = "";

            if (item.IsHeader)
            {
                // Đếm số header từ đầu → dùng La Mã (I, II, III...)
                int headerOrder = _displayList.Take(e.RowIndex + 1).Count(x => x.IsHeader);
                displayValue = ToRomanNumeral(headerOrder);
            }
            else if (!item.IsSummary)
            {
                // Đếm số thứ tự trong nhóm (1, 2, 3...)
                int pos = 1;
                for (int ri = e.RowIndex - 1; ri >= 0; ri--)
                {
                    if (_displayList[ri].IsHeader) break;
                    if (!_displayList[ri].IsSummary) pos++;
                }
                displayValue = pos.ToString();
            }
            // IsSummary → displayValue = "" (không hiển thị STT)

            // Vẽ toàn bộ nội dung cell (bao gồm nền, border, selection)
            e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground & ~DataGridViewPaintParts.ContentBackground);

            // Màu chữ tùy theo trạng thái
            Color fgColor = e.State.HasFlag(DataGridViewElementStates.Selected)
                ? e.CellStyle.SelectionForeColor
                : e.CellStyle.ForeColor;

            using (var brush = new SolidBrush(fgColor))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                // Padding nhỏ để không bị sát viền
                var drawRect = new RectangleF(e.CellBounds.X + 1, e.CellBounds.Y + 1,
                                              e.CellBounds.Width - 2, e.CellBounds.Height - 2);
                e.Graphics.DrawString(displayValue, new Font(e.CellStyle.Font ?? dgvParentProducts.Font, FontStyle.Bold), brush, drawRect, sf);
            }

            e.Handled = true;
        }

        private void DgvParentProducts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_displayList == null || e.RowIndex < 0 || e.RowIndex >= _displayList.Count) return;

            var item = _displayList[e.RowIndex];
            string currentColName = dgvParentProducts.Columns[e.ColumnIndex].Name;

            if (item.IsSummary)
            {
                e.CellStyle.Font = new Font("Times New Roman", 8.5f, FontStyle.Bold);

                // Ẩn giá trị 0 ở các cột không liên quan (giống gộp ô trong Excel)
                var hiddenCols = new[] { "STT", "MaHang", "XuatXu", "DonVi", "SoLuong", "GhiChu" };
                if (Array.IndexOf(hiddenCols, currentColName) >= 0)
                {
                    e.Value = "";
                    e.FormattingApplied = true;
                }

                // Số tiền → chữ đỏ
                var blackCols = new[] { "DonGiaVND", "ThanhTienVND" };
                var numberCols = new[] { "DonGiaVND", "ThanhTienVND", "GiaNhap", "ThanhTien", "BangGia" };

                if (Array.IndexOf(numberCols, currentColName) >= 0)
                {
                    e.CellStyle.ForeColor =
                        Array.IndexOf(blackCols, currentColName) >= 0
                        ? Color.Black
                        : Color.Red;
                }
            }
            else if (item.IsHeader)
            {
                // Dòng header nhóm: nền xanh lá (STT được vẽ riêng bằng CellPainting)
                e.CellStyle.BackColor = Color.LightGreen;
                e.CellStyle.ForeColor = Color.Black;
                e.CellStyle.SelectionBackColor = Color.LimeGreen;
                e.CellStyle.SelectionForeColor = Color.Black;
                e.CellStyle.Font = new Font(dgvParentProducts.Font, FontStyle.Bold);
            }

            // Áp dụng màu tuỳ chỉnh per-cell từ Google Sheet (nếu có lưu SheetRowIndex)
            if (item.SheetRowIndex >= 0)
            {
                // Mapping: Tên cột DGV -> Index cột trên Sheet (0-11)
                string[] sheetColOrder = { "STT", "TenHang", "MaHang", "XuatXu", "DonVi", "SoLuong",
                                         "DonGiaVND", "ThanhTienVND", "GhiChu", "GiaNhap", "ThanhTien", "BangGia" };
                string colName = dgvParentProducts.Columns[e.ColumnIndex].Name;
                int sheetColIdx = Array.IndexOf(sheetColOrder, colName);

                if (sheetColIdx >= 0)
                {
                    var sheetKey = (item.SheetRowIndex, sheetColIdx);
                    if (_sheetBgColors.TryGetValue(sheetKey, out Color sBg))
                        e.CellStyle.BackColor = sBg;
                    if (_sheetFgColors.TryGetValue(sheetKey, out Color sFg))
                        e.CellStyle.ForeColor = sFg;
                }
            }

            // Áp dụng màu tuỳ chỉnh per-cell được chọn trực tiếp trong session này (Ghi đè màu gốc từ Sheet)
            var key = (e.RowIndex, e.ColumnIndex);
            if (_cellBgColors.TryGetValue(key, out Color bg))
                e.CellStyle.BackColor = bg;
            if (_cellFgColors.TryGetValue(key, out Color fg))
                e.CellStyle.ForeColor = fg;
        }

        private void UpdateGridSelector(DataGridView dgv, List<Products> source)
        {
            dgv.DataSource = source.ToList();
            // FormatDataGridView will be called by DataBindingComplete
        }

        private async void button9_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                await LoadDataAsync();
                this.Cursor = Cursors.Default;
                MessageBox.Show("Đã cập nhật dữ liệu từ Google Sheets!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Lỗi khi cập nhật dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveToCache<T>(string key, T data)
        {
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                File.WriteAllText(GetCachePath(key), json);
            }
            catch { }
        }

        private T LoadFromCache<T>(string key)
        {
            try
            {
                string path = GetCachePath(key);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
                }
            }
            catch { }
            return default;
        }

        private void Grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // Suppress the default technical error dialog
            e.ThrowException = false;

            // Bỏ qua lỗi format ở cột STT: cột này được hiển thị dạng La Mã (string)
            // nên DataGridView sẽ báo FormatException khi cố parse ngược lại thành int.
            var dgv = sender as DataGridView;
            if (dgv != null && e.ColumnIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "STT")
                return;

            // Optionally show a user friendly message if it's a formatting error
            if (e.Exception is FormatException)
            {
                MessageBox.Show("Dữ liệu nhập vào không đúng định dạng (ví dụ: cần nhập số).", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv != null && e.KeyCode == Keys.Space)
            {
                ToggleSelectedRows(dgv);
                e.Handled = true;
            }
        }

        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv != null && dgv.Columns[e.ColumnIndex].Name == "IsSelected")
            {
                dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
                
                // If multiple rows are selected, sync their checkbox with the clicked one
                if (dgv.SelectedRows.Count > 1)
                {
                    bool newValue = (bool)dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                    foreach (DataGridViewRow row in dgv.SelectedRows)
                    {
                        var item = row.DataBoundItem as Products;
                        if (item != null) item.IsSelected = newValue;
                    }
                    dgv.Refresh();
                }
            }
        }

        private void ToggleSelectedRows(DataGridView dgv)
        {
            if (dgv.SelectedRows.Count > 0)
            {
                // Find current state from first selected row
                var firstItem = dgv.SelectedRows[0].DataBoundItem as Products;
                if (firstItem != null)
                {
                    bool newValue = !firstItem.IsSelected;
                    foreach (DataGridViewRow row in dgv.SelectedRows)
                    {
                        var item = row.DataBoundItem as Products;
                        if (item != null) item.IsSelected = newValue;
                    }
                    dgv.Refresh();
                }
            }
        }

        private void Button7_Click(object sender, EventArgs e)
        {
            childProducts.Clear();
            chkSelectAllChildProducts.Checked = false;
        }

        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
            if (configProducts.Count == 0)
            {
                MessageBox.Show("Danh s\u00e1ch c\u1ea5u h\u00ecnh \u0111ang tr\u1ed1ng, kh\u00f4ng c\u00f3 d\u1eef li\u1ec7u \u0111\u1ec3 xu\u1ea5t!",
                    "Th\u00f4ng b\u00e1o", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ExportConfigToExcel(dgvParentProducts, configProducts);
        }

        private void ExportConfigToExcel(DataGridView dgv, List<ConfigProductItem> items)
        {
            try
            {
                Excel.Application excelApp = new Excel.Application();
                excelApp.Visible       = true;
                excelApp.DisplayAlerts = false;

                Excel.Workbook  workbook  = excelApp.Workbooks.Add(Type.Missing);
                Excel.Worksheet ws = workbook.ActiveSheet;
                ws.Name = "Danh Sach Cau Hinh";

                // Thu thập các cột hiện thị
                var visibleCols = new List<DataGridViewColumn>();
                foreach (DataGridViewColumn col in dgv.Columns)
                    if (col.Visible) visibleCols.Add(col);

                // ── 1. Header cột ──────────────────────────────────────────────
                // Lấy màu header từ DGV (ColumnHeadersDefaultCellStyle.BackColor)
                Color dgvHeaderBg = dgv.ColumnHeadersDefaultCellStyle.BackColor;
                Color dgvHeaderFg = dgv.ColumnHeadersDefaultCellStyle.ForeColor;

                for (int c = 0; c < visibleCols.Count; c++)
                {
                    Excel.Range hCell = (Excel.Range)ws.Cells[1, c + 1];
                    hCell.Value2 = visibleCols[c].HeaderText;

                    // Lấy màu header riêng của từng cột (ví dụ cột giá màu xanh)
                    Color colHdrBg = visibleCols[c].HeaderCell.Style.BackColor != Color.Empty
                                     ? visibleCols[c].HeaderCell.Style.BackColor : dgvHeaderBg;
                    Color colHdrFg = visibleCols[c].HeaderCell.Style.ForeColor != Color.Empty
                                     ? visibleCols[c].HeaderCell.Style.ForeColor : dgvHeaderFg;

                    hCell.Interior.Color    = ColorTranslator.ToOle(colHdrBg);
                    hCell.Font.Color        = ColorTranslator.ToOle(colHdrFg);
                    hCell.Font.Bold         = true;
                    hCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    hCell.VerticalAlignment   = Excel.XlVAlign.xlVAlignCenter;
                }

                // ── 2. Dữ liệu + màu nền/chữ theo logic DGV ──────────────────
                // ** Không đọc từ dgvRow.DefaultCellStyle vì CellFormatting là dynamic,
                //    màu không được lưu vào Style. Phải áp dụng cùng quy tắc với UpdateConfigGrid. **
                var moneyCols       = new[] { "DonGiaVND", "ThanhTienVND", "GiaNhap", "ThanhTien", "BangGia" };
                var hiddenSumCols   = new[] { "STT", "MaHang", "XuatXu", "DonVi", "SoLuong", "GhiChu" };

                for (int r = 0; r < _displayList.Count; r++)
                {
                    var item = _displayList[r];

                    // --- Quy tắc màu dòng (giống UpdateConfigGrid + CellFormatting) ---
                    Color rowBg;
                    Color rowFg;
                    bool  rowBold;

                    if (item.IsSummary)
                    {
                        rowBg   = Color.Yellow;         // Dòng tổng: nền vàng
                        rowFg   = Color.Black;
                        rowBold = true;
                    }
                    else if (item.IsHeader)
                    {
                        rowBg   = Color.LightGreen;     // Dòng header nhóm: xanh lá
                        rowFg   = Color.Black;
                        rowBold = true;
                    }
                    else
                    {
                        rowBg   = Color.White;          // Dòng thường: trắng
                        rowFg   = Color.Black;
                        rowBold = false;
                    }

                    // --- Ghi từng ô: đọc giá trị trực tiếp từ item, không qua DGV cell
                    for (int c = 0; c < visibleCols.Count; c++)
                    {
                        Excel.Range xCell = (Excel.Range)ws.Cells[r + 2, c + 1];
                        string colNm      = visibleCols[c].Name;
                        int    dgvColIdx  = visibleCols[c].Index;

                        // ── Giá trị: đọc thẳng từ item ──
                        if (item.IsSummary && Array.IndexOf(hiddenSumCols, colNm) >= 0)
                        {
                            xCell.Value2 = ""; // ẩn cột không liên quan ở dòng tổng
                        }
                        else
                        {
                            object val = null;
                            switch (colNm)
                            {
                                case "STT":          val = item.STT;          break;
                                case "TenHang":      val = item.TenHang;      break;
                                case "MaHang":       val = item.MaHang;       break;
                                case "XuatXu":       val = item.XuatXu;       break;
                                case "DonVi":        val = item.DonVi;        break;
                                case "SoLuong":      val = item.SoLuong;      break;
                                case "DonGiaVND":    val = item.DonGiaVND;    break;
                                case "ThanhTienVND": val = item.ThanhTienVND; break;
                                case "GhiChu":       val = item.GhiChu;       break;
                                case "GiaNhap":      val = item.GiaNhap;      break;
                                case "ThanhTien":    val = item.ThanhTien;    break;
                                case "BangGia":      val = item.BangGia;      break;
                            }
                            if (val is decimal decVal)
                                xCell.Value2 = (double)decVal;
                            else if (val != null)
                                xCell.Value2 = val.ToString();
                        }

                        // ── Màu nền: per-cell picker > sheet color > màu dòng mặc định ──
                        // _sheetBgColors: màu load từ Google Sheet (nguồn chính của per-cell color)
                        // _cellBgColors:  màu set qua color picker trong session hiện tại (ghi đè)
                        string[] sheetColOrd = { "STT","TenHang","MaHang","XuatXu","DonVi","SoLuong",
                                                  "DonGiaVND","ThanhTienVND","GhiChu","GiaNhap","ThanhTien","BangGia" };
                        int sheetC = Array.IndexOf(sheetColOrd, colNm);
                        var sheetKeyBg = (item.SheetRowIndex, sheetC);

                        Color cellBg = rowBg;
                        if (sheetC >= 0 && item.SheetRowIndex >= 0 && _sheetBgColors.TryGetValue(sheetKeyBg, out Color sheetBg))
                            cellBg = sheetBg;                                   // màu từ Google Sheet
                        if (_cellBgColors.TryGetValue((r, dgvColIdx), out Color customBg))
                            cellBg = customBg;                                  // picker ghi đè
                        xCell.Interior.Color = ColorTranslator.ToOle(cellBg);

                        // ── Màu chữ: summary+tiền → đỏ; sheet color > picker ghi đè ──
                        Color cellFg = (item.IsSummary && Array.IndexOf(moneyCols, colNm) >= 0)
                                       ? Color.Red : rowFg;
                        var sheetKeyFg = (item.SheetRowIndex, sheetC);
                        if (sheetC >= 0 && item.SheetRowIndex >= 0 && _sheetFgColors.TryGetValue(sheetKeyFg, out Color sheetFg))
                            cellFg = sheetFg;
                        if (_cellFgColors.TryGetValue((r, dgvColIdx), out Color customFg))
                            cellFg = customFg;
                        xCell.Font.Color = ColorTranslator.ToOle(cellFg);
                        xCell.Font.Bold  = rowBold;

                        // ── Căn chỉnh ──
                        if (Array.IndexOf(new[] { "STT", "XuatXu", "DonVi", "SoLuong" }, colNm) >= 0)
                            xCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                        else if (Array.IndexOf(moneyCols, colNm) >= 0)
                            xCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
                        else
                            xCell.HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;

                        // ── Định dạng số tiền ──
                        if (Array.IndexOf(moneyCols, colNm) >= 0)
                            xCell.NumberFormat = "#,##0";
                    }
                }

                // ── 3. Viền bảng + chiều cao hàng ──
                Excel.Range used = ws.Range[ws.Cells[1, 1], ws.Cells[_displayList.Count + 1, visibleCols.Count]];
                used.Borders.LineStyle     = Excel.XlLineStyle.xlContinuous;
                used.Borders.Weight        = Excel.XlBorderWeight.xlThin;
                used.WrapText              = false;       // Không xuống dòng bên trong ô
                used.VerticalAlignment     = Excel.XlVAlign.xlVAlignCenter;

                // Header cột cao 30pt, dữ liệu 15pt (giống DGV)
                ws.Rows[1].RowHeight = 30;
                for (int r2 = 2; r2 <= _displayList.Count + 1; r2++)
                    ((Excel.Range)ws.Rows[r2]).RowHeight = 15;

                // ── 4. Độ rộng cột tuỳ chỉnh ──────────────────────────────
                for (int c = 0; c < visibleCols.Count; c++)
                {
                    Excel.Range excelCol = (Excel.Range)ws.Columns[c + 1];
                    switch (visibleCols[c].Name)
                    {
                        case "STT":      excelCol.ColumnWidth = 5;  break;
                        case "TenHang":  excelCol.ColumnWidth = 40; break;
                        case "MaHang":   excelCol.ColumnWidth = 14; break;
                        case "XuatXu":   excelCol.ColumnWidth = 10; break;
                        case "DonVi":    excelCol.ColumnWidth = 8;  break;
                        case "SoLuong":  excelCol.ColumnWidth = 8;  break;
                        case "GhiChu":   excelCol.ColumnWidth = 20; break;
                        case "DonGiaVND":
                        case "ThanhTienVND":
                        case "GiaNhap":
                        case "ThanhTien":
                        case "BangGia":  excelCol.ColumnWidth = 16; break;
                        default:         excelCol.ColumnWidth = 12; break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xuất Excel: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupHeaderCheckBox(DataGridView dgv, CheckBox chk, string columnName)
        {
            chk.Size = new Size(15, 15);
            chk.BackColor = Color.White;
            dgv.Controls.Add(chk);

            chk.CheckedChanged += (s, e) =>
            {
                dgv.EndEdit();
                if (dgv.DataSource is IEnumerable<Products> source)
                {
                    foreach (var item in source) item.IsSelected = chk.Checked;
                    dgv.Refresh();
                }
            };

            dgv.DataBindingComplete += (s, e) => PositionHeaderCheckBox(dgv, chk, columnName);
            dgv.ColumnWidthChanged += (s, e) => PositionHeaderCheckBox(dgv, chk, columnName);
            dgv.Scroll += (s, e) => PositionHeaderCheckBox(dgv, chk, columnName);
            dgv.Resize += (s, e) => PositionHeaderCheckBox(dgv, chk, columnName);
        }

        private void PositionHeaderCheckBox(DataGridView dgv, CheckBox chk, string columnName)
        {
            if (!dgv.Columns.Contains(columnName)) return;
            Rectangle rect = dgv.GetCellDisplayRectangle(dgv.Columns[columnName].Index, -1, true);
            chk.Location = new Point(rect.X + (rect.Width - chk.Width) / 2, rect.Y + (rect.Height - chk.Height) / 2);
        }

        /// <summary>Chuyển số nguyên dương sang ký hiệu số La Mã (I, II, III, IV, V...)</summary>
        private static string ToRomanNumeral(int number)
        {
            if (number <= 0) return number.ToString();
            var romanNumerals = new[]
            {
                (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
                (100,  "C"), (90,  "XC"), (50,  "L"), (40,  "XL"),
                (10,   "X"), (9,   "IX"), (5,   "V"), (4,   "IV"), (1, "I")
            };
            var result = new System.Text.StringBuilder();
            foreach (var (value, numeral) in romanNumerals)
            {
                while (number >= value) { result.Append(numeral); number -= value; }
            }
            return result.ToString();
        }
    }
}
