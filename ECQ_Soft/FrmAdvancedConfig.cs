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
        // Header cột của Products_Table (lowercase): để map ExtraAttributes
        private List<string> _productColumnHeaders = new List<string>();
        // Node đang được mở expand panel
        private HierarchyNode _expandedNode = null;
        
        public string SelectedHeader { get; private set; }
        public List<AdvancedConfigResultItem> SelectedAdvancedItems { get; private set; } = new List<AdvancedConfigResultItem>();

        // Key = TreePath (VD: "TỦ ĐIỆN - TỦ PHÂN PHỐI - ...")
        // Value = List các cấu hình nháp (Tên Sản Phẩm, Số Lượng, Thuộc tính)
        private Dictionary<string, List<Tuple<string, int, string>>> _pendingDraftItems = new Dictionary<string, List<Tuple<string, int, string>>>();
        private string _currentDraftName = string.Empty;

        public FrmAdvancedConfig()
        {
            InitializeComponent();
            ConfigureSelectedItemsGrid();
            SetupEvents();
            this.Resize += (s, e) => RecalculateLayout();
            this.Load += (s, e) =>
            {
                // Luôn mở form ở chế độ Full Màn Hình (Maximized)
                this.WindowState = FormWindowState.Maximized;

                InitDefaultRows();
            };
        }

        private void ConfigureSelectedItemsGrid()
        {
            dgvSelectedItems.Columns.Clear();
            dgvSelectedItems.AutoGenerateColumns = false;

            var headerStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                BackColor = Color.Yellow,
                ForeColor = Color.DarkBlue,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                SelectionBackColor = Color.Gold,
                SelectionForeColor = Color.DarkBlue,
                WrapMode = DataGridViewTriState.True
            };
            dgvSelectedItems.ColumnHeadersDefaultCellStyle = headerStyle;
            dgvSelectedItems.EnableHeadersVisualStyles = false;
            dgvSelectedItems.ColumnHeadersHeight = 36;

            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSTT", HeaderText = "STT", ReadOnly = false, FillWeight = 40 });
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTen", HeaderText = "Tên hàng", ReadOnly = false, FillWeight = 220 }); // Sản phẩm (Cho phép sửa)
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colModel", HeaderText = "Model", ReadOnly = false, FillWeight = 90 });
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSKU", HeaderText = "Mã hàng", ReadOnly = false, FillWeight = 90 });
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colXuatXu", HeaderText = "Xuất xứ", ReadOnly = false, FillWeight = 70 });
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDonVi", HeaderText = "Đơn vị", ReadOnly = false, FillWeight = 55 });
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSoLuong", HeaderText = "Số lượng", ReadOnly = false, FillWeight = 55 }); // Số lượng (Cho phép sửa)
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDonGia", HeaderText = "Đơn giá (VNĐ)", ReadOnly = false, FillWeight = 80 }); 
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colGiaTien", HeaderText = "Thành tiền (VNĐ)", ReadOnly = false, FillWeight = 90 });
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colGiaNhap", HeaderText = "Giá nhập", ReadOnly = false, FillWeight = 80 });
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDanhMuc", HeaderText = "Danh mục", ReadOnly = false, FillWeight = 90 });
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "Type", ReadOnly = false, FillWeight = 70 });
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colHang", HeaderText = "Hãng", ReadOnly = false, FillWeight = 70 });
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTienDo", HeaderText = "Tiến độ", ReadOnly = false, FillWeight = 70 });
            dgvSelectedItems.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colXoa",
                HeaderText = "",
                Text = "Xóa",
                UseColumnTextForButtonValue = true,
                FillWeight = 45,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(220, 50, 47),
                    ForeColor = Color.White
                }
            });

            dgvSelectedItems.Columns["colModel"].Visible = false;
            dgvSelectedItems.Columns["colGiaNhap"].Visible = false;
            dgvSelectedItems.Columns["colDanhMuc"].Visible = false;
            dgvSelectedItems.Columns["colType"].Visible = false;
            dgvSelectedItems.Columns["colHang"].Visible = false;
        }

        private void InitDefaultRows()
        {
            if (dgvSelectedItems.Rows.Count == 0)
            {
                EnsureDefaultRowsPresent();
                btnApply.Enabled = true;
            }
        }

        private void EnsureDefaultRowsPresent()
        {
            foreach (string defaultItemName in ConfigProductItem.PinnedItemNames)
            {
                bool alreadyExists = dgvSelectedItems.Rows
                    .Cast<DataGridViewRow>()
                    .Any(r => (r.Cells["colTen"].Value?.ToString() ?? string.Empty) == defaultItemName);

                if (!alreadyExists)
                {
                    AddSelectedItemRow(defaultItemName, 1, 0, "0", "0", null, true);
                }
                else
                {
                    var existingRow = dgvSelectedItems.Rows
                        .Cast<DataGridViewRow>()
                        .First(r => (r.Cells["colTen"].Value?.ToString() ?? string.Empty) == defaultItemName);

                    existingRow.Cells["colSoLuong"].Value = "1";
                    ApplyDefaultRowStyle(existingRow);
                    existingRow.Cells["colTen"].ToolTipText = "Chuột phải để hiển thị Tính Toán";
                }
            }
        }

        private bool IsDefaultSelectedItemName(string itemName)
        {
            return ConfigProductItem.IsPinned(itemName);
        }

        private void ApplyDefaultRowStyle(DataGridViewRow row)
        {
            row.DefaultCellStyle.ForeColor = Color.Red;
            row.DefaultCellStyle.SelectionForeColor = Color.Red;
            row.Cells["colTen"].Style.ForeColor = Color.Red;
            row.Cells["colTen"].Style.SelectionForeColor = Color.Red;
        }

        private void RecalculateSelectedItemRow(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow) return;

            int qty = 1;
            int.TryParse(row.Cells["colSoLuong"].Value?.ToString(), out qty);
            if (qty <= 0) qty = 1;
            row.Cells["colSoLuong"].Value = qty.ToString();

            decimal unitPrice = ParseCurrencyValue(row.Cells["colDonGia"].Value?.ToString());
            row.Cells["colDonGia"].Value = FormatCurrencyVnd(unitPrice);
            row.Cells["colGiaTien"].Value = FormatCurrencyVnd(qty * unitPrice);
        }

        private decimal ParseCurrencyValue(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            string normalized = text.Replace(".", "").Replace(",", "").Trim();
            decimal.TryParse(normalized, out decimal value);
            return value;
        }

        private string FormatCurrencyVnd(decimal value)
        {
            return value.ToString("N0");
        }

        private System.Data.DataTable BuildSelectedItemsPreviewTable()
        {
            var table = new System.Data.DataTable();
            var previewColumns = dgvSelectedItems.Columns
                .Cast<DataGridViewColumn>()
                .Where(c => c.Visible && c.Name != "colXoa")
                .Select(c => c.Name)
                .ToList();

            foreach (var colName in previewColumns)
            {
                if (dgvSelectedItems.Columns.Contains(colName))
                {
                    table.Columns.Add(colName, typeof(string));
                }
            }

            foreach (DataGridViewRow row in dgvSelectedItems.Rows)
            {
                if (row.IsNewRow) continue;

                var newRow = table.NewRow();
                foreach (System.Data.DataColumn col in table.Columns)
                {
                    newRow[col.ColumnName] = row.Cells[col.ColumnName].Value?.ToString() ?? string.Empty;
                }
                table.Rows.Add(newRow);
            }

            return table;
        }

        private string GetProductBrand(Products product)
        {
            if (product == null) return string.Empty;

            var prop = typeof(Products).GetProperty("HÃƒNG")
                       ?? typeof(Products).GetProperty("HÃNG")
                       ?? typeof(Products).GetProperty("Hang")
                       ?? typeof(Products).GetProperty("Brand");

            return prop?.GetValue(product)?.ToString() ?? string.Empty;
        }

        private List<object> BuildDraftSheetRowFromGrid(DataGridViewRow row, string path, string notes = "")
        {
            return new List<object>
            {
                path ?? "",
                row.Cells["colTen"].Value?.ToString() ?? "",
                row.Cells["colModel"].Value?.ToString() ?? "",
                row.Cells["colSKU"].Value?.ToString() ?? "",
                row.Cells["colXuatXu"].Value?.ToString() ?? "",
                row.Cells["colDonVi"].Value?.ToString() ?? "",
                row.Cells["colSoLuong"].Value?.ToString() ?? "1",
                row.Cells["colDonGia"].Value?.ToString() ?? "0",
                row.Cells["colGiaTien"].Value?.ToString() ?? "0",
                row.Cells["colGiaNhap"].Value?.ToString() ?? "0",
                row.Cells["colDanhMuc"].Value?.ToString() ?? "",
                row.Cells["colType"].Value?.ToString() ?? "",
                row.Cells["colHang"].Value?.ToString() ?? "",
                row.Cells["colTienDo"].Value?.ToString() ?? "0",
                notes ?? ""
            };
        }

        private int AddSelectedItemRow(
            string itemName,
            int quantity = 1,
            decimal unitPrice = 0,
            string progress = "0",
            string totalPrice = "0",
            object tag = null,
            bool isDefaultRow = false,
            int? insertIndex = null)
        {
            string qtyText = Math.Max(0, quantity).ToString();
            string progressText = progress ?? "0";
            var product = tag as Products;

            decimal parsedPrice = unitPrice;
            if (parsedPrice <= 0 && product != null)
                parsedPrice = ParseCurrencyValue(product.Price);

            decimal parsedCost = 0;
            if (product != null)
                parsedCost = ParseCurrencyValue(product.PriceCost);
            if (parsedCost <= 0) parsedCost = parsedPrice;

            decimal parsedTotal = parsedPrice * Math.Max(0, quantity);
            decimal explicitTotal = ParseCurrencyValue(totalPrice);
            if (explicitTotal > 0)
                parsedTotal = explicitTotal;

            string unitPriceText = FormatCurrencyVnd(parsedPrice);
            string totalText = FormatCurrencyVnd(parsedTotal);
            string brand = GetProductBrand(product);
            string xuatXu = brand;
            string donVi = isDefaultRow || IsDefaultSelectedItemName(itemName) ? "Tủ" : "Cái";

            int rowIndex;
            if (insertIndex.HasValue)
            {
                dgvSelectedItems.Rows.Insert(
                    insertIndex.Value,
                    "",
                    itemName,
                    product?.Model ?? "",
                    product?.SKU ?? "",
                    xuatXu,
                    donVi,
                    qtyText,
                    unitPriceText,
                    totalText,
                    FormatCurrencyVnd(parsedCost),
                    product?.Category ?? "",
                    product?.Type ?? "",
                    brand,
                    progressText);
                rowIndex = insertIndex.Value;
            }
            else
            {
                rowIndex = dgvSelectedItems.Rows.Add(
                    "",
                    itemName,
                    product?.Model ?? "",
                    product?.SKU ?? "",
                    xuatXu,
                    donVi,
                    qtyText,
                    unitPriceText,
                    totalText,
                    FormatCurrencyVnd(parsedCost),
                    product?.Category ?? "",
                    product?.Type ?? "",
                    brand,
                    progressText);
            }

            var row = dgvSelectedItems.Rows[rowIndex];
            row.Tag = tag;

            if (isDefaultRow || IsDefaultSelectedItemName(itemName))
            {
                ApplyDefaultRowStyle(row);
                row.Cells["colTen"].ToolTipText = "Chuột phải để hiển thị Tính Toán";
            }

            RenumberGridSTT();
            return rowIndex;
        }

        private void RenumberGridSTT()
        {
            for (int i = 0; i < dgvSelectedItems.Rows.Count; i++)
            {
                dgvSelectedItems.Rows[i].Cells["colSTT"].Value = (i + 1).ToString();
            }
        }

        private int GetInsertIndex()
        {
            for (int i = 0; i < dgvSelectedItems.Rows.Count; i++)
            {
                if (dgvSelectedItems.Rows[i].Cells["colTen"].Value?.ToString() == "Hệ thống đồng thanh cái")
                {
                    return i;
                }
            }
            return dgvSelectedItems.Rows.Count;
        }

        public async Task LoadDataAsync(SheetsService service, string spreadsheetId)
        {
            _service = service;
            _spreadsheetId = spreadsheetId;
            
            try
            {
                // Tải song song: Workflow + Products
                var workflowTask = _service.Spreadsheets.Values.Get(_spreadsheetId, "Workflow!A2:Z").ExecuteAsync();
                var productsTask = _service.Spreadsheets.Values.Get(_spreadsheetId, "Products_Table!A1:Z").ExecuteAsync();
                
                await Task.WhenAll(workflowTask, productsTask);
                
                var values = workflowTask.Result.Values;
                if (values == null || values.Count == 0) return;

                BuildTreeFromRows(values);
                
                // Nạp sản phẩm vào bộ nhớ để hỗ trợ search
                _allProducts.Clear();
                _productColumnHeaders.Clear();
                var pRows = productsTask.Result.Values;
                if (pRows != null && pRows.Count > 0)
                {
                    // Dòng đầu tiên của Products_Table!A2 thực ra là header
                    // Ta đã call A2:M nên dòng 0 là data, nhưng nếu dòng 0 không phải số thì đó là header
                    int dataStart = 0;
                    var firstRow = pRows[0];
                    bool firstRowIsHeader = firstRow.Count > 0 && !int.TryParse(firstRow[0]?.ToString(), out _);
                    if (firstRowIsHeader)
                    {
                        dataStart = 1;
                        for (int hi = 0; hi < firstRow.Count; hi++)
                            _productColumnHeaders.Add((firstRow[hi]?.ToString()?.Trim()?.ToLower()) ?? $"col{hi}");
                    }
                    else
                    {
                        // Fallback: gán tên cột mặc định
                        _productColumnHeaders = new List<string> { "id", "name", "model", "sku", "price", "pricecost", "weight", "length", "width", "height", "category", "hãng", "pricelist" };
                    }

                    // Thứ tự cột chuẩn (0-indexed)
                    // 0:id, 1:name, 2:model, 3:sku, 4:price, 5:pricecost, 6:weight,
                    // 7:length, 8:width, 9:height, 10:category, 11:type, 12:hãng, 13:pricelist
                    // Các cột thuộc tính mở rộng (định nghĩa bằng header): pole, ir, icu...
                    
                    // Xây dựng bản đồ vị trí cột dựa theo header đã đọc
                    var colIdx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int hi = 0; hi < _productColumnHeaders.Count; hi++)
                        colIdx[_productColumnHeaders[hi]] = hi;

                    // Hàm tiện lợi đọc cột theo header key, fallback về index cột cứng
                    string ReadCol(IList<object> row, string key, int fallbackIdx)
                    {
                        int i2 = colIdx.TryGetValue(key, out int ci) ? ci : fallbackIdx;
                        return (i2 >= 0 && i2 < row.Count) ? row[i2]?.ToString() ?? "" : "";
                    }

                    for (int i = dataStart; i < pRows.Count; i++)
                    {
                        var row = pRows[i];
                        if (row.Count < 2) continue;
                        var prod = new Products
                        {
                            Id        = int.TryParse(ReadCol(row, "id", 0), out int pid) ? pid : i,
                            Name      = ReadCol(row, "name", 1),
                            Model     = ReadCol(row, "model", 2),
                            SKU       = ReadCol(row, "sku", 3),
                            Price     = ReadCol(row, "price", 4),
                            PriceCost = ReadCol(row, "pricecost", 5),
                            Weight    = ReadCol(row, "weight", 6),
                            Length    = ReadCol(row, "length", 7),
                            Width     = ReadCol(row, "width", 8),
                            Height    = ReadCol(row, "height", 9),
                            Category  = ReadCol(row, "category", 10),
                            Type      = ReadCol(row, "type", 11),
                            HÃNG     = ReadCol(row, "hãng", 12),
                            PriceList = ReadCol(row, "pricelist", 13)
                        };
                        // Nạp ExtraAttributes cho các cột ngoài cột chuẩn
                        var standardKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            { "id","name","model","sku","price","pricecost","weight","length","width","height","category","type","hãng","pricelist" };
                        for (int ci = 0; ci < _productColumnHeaders.Count && ci < row.Count; ci++)
                        {
                            string colKey = _productColumnHeaders[ci];
                            if (!string.IsNullOrEmpty(colKey) && !standardKeys.Contains(colKey))
                                prod.ExtraAttributes[colKey] = row[ci]?.ToString() ?? "";
                        }
                        _allProducts.Add(prod);
                    }
                }

                await PromptLoadDraftAsync();
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
                        await PromptLoadDraftAsync();
                    }
                }
                catch (Exception ex2)
                {
                    MessageBox.Show("Lỗi tải dữ liệu Workflow: " + ex2.Message);
                }
            }
        }

        private async Task PromptLoadDraftAsync()
        {
            try
            {
                var draftsResponse = await _service.Spreadsheets.Values.Get(_spreadsheetId, "Cấu hình nháp!A:O").ExecuteAsync();
                var rows = draftsResponse.Values;
                if (rows == null || rows.Count == 0)
                {
                    LoadInitialLevel();
                    return;
                }

                // Parse drafts
                var draftGroups = new Dictionary<string, List<IList<object>>>();
                string currentDraft = null;

                foreach (var row in rows)
                {
                    if (row.Count == 0) continue;
                    
                    string colA = row[0]?.ToString()?.Trim() ?? "";
                    string colB = (row.Count > 1) ? row[1]?.ToString()?.Trim() ?? "" : "";
                    
                    // 1. Bỏ qua dòng tiêu đề bảng (Vị trí cấu hình, Tên hàng, ...)
                    if (colA == "Vị trí cấu hình") continue;

                    // 2. Nếu cột B trống và Cột A có chữ => Đây là Header Tên Nháp (VD: "Tủ ABC")
                    if (!string.IsNullOrEmpty(colA) && string.IsNullOrEmpty(colB))
                    {
                        currentDraft = colA;
                        if (!draftGroups.ContainsKey(currentDraft)) draftGroups[currentDraft] = new List<IList<object>>();
                        continue;
                    }
                    
                    // 3. Nếu cột B có giá trị => Đây là dữ liệu sản phẩm của bản nháp hiện tại
                    if (!string.IsNullOrEmpty(colB) && currentDraft != null)
                    {
                        draftGroups[currentDraft].Add(row);
                    }
                }

                if (draftGroups.Count == 0)
                {
                    LoadInitialLevel();
                    return;
                }

                // Hiển thị Modal
                using (var modal = new Form { Text = "Khôi phục dữ liệu nháp", Size = new Size(500, 400), StartPosition = FormStartPosition.CenterParent, ShowIcon = false })
                {
                    var lbl = new Label { Text = "Phần mềm tìm thấy các Cấu hình nháp đã lưu trên Google Sheets.\nBạn có muốn nạp lại để tiếp tục làm việc không?", AutoSize = true, Location = new Point(20, 20), Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 80, 200) };
                    modal.Controls.Add(lbl);

                    var lstDrafts = new ListBox { Location = new Point(20, 70), Size = new Size(440, 200), Font = new Font("Segoe UI", 10f) };
                    foreach (var d in draftGroups.Keys)
                    {
                        lstDrafts.Items.Add($"{d} ({draftGroups[d].Count} cấu hình)");
                    }
                    modal.Controls.Add(lstDrafts);
                    if (lstDrafts.Items.Count > 0) lstDrafts.SelectedIndex = 0;

                    var btnLoad = new Button { Text = "Tải cấu hình đã chọn", Location = new Point(20, 300), Size = new Size(180, 40), BackColor = Color.FromArgb(0, 150, 70), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
                    var btnSkip = new Button { Text = "Tạo Cấu hình Mới", Location = new Point(280, 300), Size = new Size(180, 40), BackColor = Color.LightGray, Font = new Font("Segoe UI", 9.5f) };
                    modal.Controls.Add(btnLoad);
                    modal.Controls.Add(btnSkip);

                    // Logic tải cấu hình – dùng chung cho nút bấm và double-click
                    Action loadSelectedDraft = () =>
                    {
                        if (lstDrafts.SelectedIndex >= 0)
                        {
                            string selectedItem = lstDrafts.SelectedItem.ToString();
                            string draftKey = selectedItem.Substring(0, selectedItem.LastIndexOf('(')).Trim();
                            _currentDraftName = draftKey;
                            EnsureDefaultRowsPresent();
 
                            var selectedRows = draftGroups[draftKey];
                            _pendingDraftItems.Clear();
                            foreach (var r in selectedRows)
                            {
                                string viTri = r.Count > 0 ? r[0]?.ToString()?.Trim() : "";
                                string tenSP = r.Count > 1 ? r[1]?.ToString()?.Trim() : "";
                                string soLuongStr = r.Count > 6 ? r[6]?.ToString()?.Trim() : (r.Count > 2 ? r[2]?.ToString()?.Trim() : "1");
                                string thuocTinh = r.Count > 14 ? r[14]?.ToString()?.Trim() : (r.Count > 3 ? r[3]?.ToString()?.Trim() : "");

                                int.TryParse(soLuongStr, out int qty);
                                if (qty <= 0) qty = 1;

                                if (!string.IsNullOrEmpty(viTri) && !string.IsNullOrEmpty(tenSP))
                                {
                                    if (!_pendingDraftItems.ContainsKey(viTri)) _pendingDraftItems[viTri] = new List<Tuple<string, int, string>>();
                                    _pendingDraftItems[viTri].Add(new Tuple<string, int, string>(tenSP, qty, thuocTinh));

                                    // Hiển thị tên sản phẩm sạch (không có tiền tố) trong Grid
                                    string finalName = tenSP;
                                    if (finalName.Contains(": ")) finalName = finalName.Substring(finalName.IndexOf(": ") + 2);

                                    bool isDuplicate = false;
                                    foreach (DataGridViewRow existing in dgvSelectedItems.Rows)
                                    {
                                        if (existing.Cells["colTen"].Value?.ToString() == finalName)
                                        { isDuplicate = true; break; }
                                    }
                                    if (!isDuplicate)
                                    {
                                        int insIdx = GetInsertIndex();
                                        string rawName = finalName;
                                        
                                        var matchedProduct = _allProducts.FirstOrDefault(p =>
                                            string.Equals((p.Name ?? "").Trim(), rawName.Trim(), StringComparison.OrdinalIgnoreCase));
                                        
                                        AddSelectedItemRow(finalName, qty, 0, "0", "0", matchedProduct, false, insIdx);
                                        btnApply.Enabled = true;
                                    }
                                }
                            }
                            RenumberGridSTT();
                            modal.Close();
                        }
                    };

                    // Nhấn nút "Tải cấu hình đã chọn" hoặc double-click vào tên cấu hình => tải luôn
                    btnLoad.Click += (s, e) => loadSelectedDraft();
                    lstDrafts.DoubleClick += (s, e) => loadSelectedDraft();

                    btnSkip.Click += (s, e) => { modal.Close(); };

                    modal.ShowDialog();
                }

                LoadInitialLevel();
            }
            catch (Exception ex)
            {
                // Nếu không tải được list nháp (hoặc sheet không tồn tại) => bỏ qua
                LoadInitialLevel();
            }
        }

        private void BuildTreeFromRows(IList<IList<object>> rows)
        {
            _rootNodes.Clear();
            var allNodes = new Dictionary<string, HierarchyNode>();
            var dataRows = rows.ToList();
            
            if (dataRows.Count < 2) return;

            // --- TÌM CHỈ SỐ CỘT ĐỘNG TỪ DÒNG HEADER (Dòng 0) ---
            int colId = -1, colName = -1, colIdMe = -1, colProcess = -1, colFormula = -1, colType = -1, colCategory = -1, colConfig = -1, colOnlyOne = -1, colNghia = -1, colBien = -1;
            var headerRow = dataRows[0];
            for (int i = 0; i < headerRow.Count; i++)
            {
                string header = headerRow[i]?.ToString()?.Trim()?.ToLower() ?? "";
                if (header == "id") colId = i;
                else if (header == "name") colName = i;
                else if (header == "id_mẹ" || header == "id_me") colIdMe = i;
                else if (header == "công thức" || header == "cong thuc") colFormula = i;
                else if (header == "process flow" || header.Contains("process flow")) colProcess = i;
                else if (header == "type") colType = i;
                else if (header == "category") colCategory = i;
                else if (header == "config") colConfig = i;
                else if (header == "onlyone" || header == "only one") colOnlyOne = i;
                else if (header == "nghĩa" || header == "nghia") colNghia = i;
                else if (header == "biến" || header == "bien") colBien = i;
            }
            
            // Fallback nếu không xác định được (đề phòng Data thiếu Header hoặc Header gõ khác)
            if (colId == -1) colId = 1;         // Mặc định là Cột B
            if (colName == -1) colName = 2;       // Mặc định là Cột C
            if (colIdMe == -1) colIdMe = 3;       // Cột D (0-indexed: 3)
            if (colFormula == -1) colFormula = 4; // Cột E
            if (colType == -1) colType = 5;       // Cột F
            if (colCategory == -1) colCategory = 6; // Cột G
            if (colConfig == -1) colConfig = 7;   // Cột H
            if (colOnlyOne == -1) colOnlyOne = 8; // Cột I
            if (colNghia == -1) colNghia = 13; 
            if (colBien == -1) colBien = 14; 
            // colConfig: nếu không tìm thấy header -> fallback là cột cuối cùng của header row
            if (colConfig == -1) colConfig = headerRow.Count - 1;

            // Nếu người dùng thực sự thiết kế Id_Mẹ ở Cột D thì vòng lặp for Header ở trên sẽ gán lại đúng colIdMe = 3.

            var pendingChildren = new List<Tuple<HierarchyNode, string[]>>();

            // BƯỚC 1: Khởi tạo tất cả các Node (Bỏ qua dòng Header)
            for (int r = 1; r < dataRows.Count; r++)
            {
                var row = dataRows[r];
                string id = (colId >= 0 && row.Count > colId) ? row[colId]?.ToString()?.Trim() : "";
                string name = (colName >= 0 && row.Count > colName) ? row[colName]?.ToString()?.Trim() : "";
                
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

                    // Đọc Type
                    string typeVal = (colType >= 0 && row.Count > colType)
                        ? row[colType]?.ToString()?.Trim() ?? ""
                        : "";
                    node.Type = typeVal;

                    // Đọc Category
                    string categoryVal = (colCategory >= 0 && row.Count > colCategory)
                        ? row[colCategory]?.ToString()?.Trim() ?? ""
                        : "";
                    node.Category = categoryVal;

                    // Đọc OnlyOne
                    string onlyOneVal = (colOnlyOne >= 0 && row.Count > colOnlyOne)
                        ? row[colOnlyOne]?.ToString()?.Trim() ?? ""
                        : "";
                    node.OnlyOne = onlyOneVal;
                    // Đọc Nghĩa
                    string nghiaVal = (colNghia >= 0 && row.Count > colNghia)
                        ? row[colNghia]?.ToString()?.Trim() ?? ""
                        : "";
                    node.Nghia = nghiaVal;
                    // Đọc Biến
                    string bienVal = (colBien >= 0 && row.Count > colBien)
                        ? row[colBien]?.ToString()?.Trim() ?? ""
                        : "";
                    node.Bien = bienVal;

                    
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
                string id = (colId >= 0 && row.Count > colId) ? row[colId]?.ToString()?.Trim() : "";
                string name = (colName >= 0 && row.Count > colName) ? row[colName]?.ToString()?.Trim() : "";
                string idMeRaw = (colIdMe >= 0 && row.Count > colIdMe) ? row[colIdMe]?.ToString()?.Trim() : "";
                string processFlow = (colProcess >= 0 && row.Count > colProcess) ? row[colProcess]?.ToString()?.Trim() : "";
                string congThuc = (colFormula >= 0 && row.Count > colFormula) ? row[colFormula]?.ToString()?.Trim() : "";
                
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
            // Cấu hình btnAddToGrid thành nút "Lưu nháp"
            btnAddToGrid.Text = "📝 Lưu nháp";
            btnAddToGrid.Font = new Font("Times New Roman", 9f, FontStyle.Bold);
            btnAddToGrid.BackColor = Color.Orange;
            btnAddToGrid.ForeColor = Color.White;
            btnAddToGrid.FlatStyle = FlatStyle.Flat;
            btnAddToGrid.FlatAppearance.BorderSize = 0;
            btnAddToGrid.Visible = true;
            btnAddToGrid.Enabled = true;
            btnAddToGrid.Click += BtnLuuNhap_Click;

            // Nút XÁC NHẬN -> trả danh sách sản phẩm đã chọn
            btnApply.Click += async (s, e) => {
                // Thu thập tất cả các dòng trong grid kèm chi tiết
                SelectedAdvancedItems = new List<AdvancedConfigResultItem>();

                // Lấy SelectedHeader từ node đang chọn trên cây; nếu chưa chọn thì để rỗng
                SelectedHeader = (_modernTreeView?.SelectedNode?.Tag is HierarchyNode selNode)
                    ? selNode.Name
                    : "";

                foreach (DataGridViewRow row in dgvSelectedItems.Rows)
                {
                    var tenCfg = row.Cells["colTen"].Value?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(tenCfg))
                    {
                        int sl = 1;
                        if (row.Cells["colSoLuong"].Value != null)
                            int.TryParse(row.Cells["colSoLuong"].Value.ToString(), out sl);
                        if (sl <= 0) sl = 1;

                        decimal dGia = 0;
                        if (row.Cells["colDonGia"].Value != null)
                            decimal.TryParse(row.Cells["colDonGia"].Value.ToString(), out dGia);

                        string tTinh = ""; // Cột Thuộc tính đã bị xóa

                        SelectedAdvancedItems.Add(new AdvancedConfigResultItem {
                            TenCauHinh = tenCfg,
                            ThuocTinh = tTinh,
                            SoLuong = sl,
                            DonGia = dGia,
                            ReferenceProduct = row.Tag as ECQ_Soft.Model.Products
                        });
                    }
                }

                if (SelectedAdvancedItems.Count > 0)
                {
                    // TỰ ĐỘNG LƯU NHÁP KHI XÁC NHẬN
                    if (!string.IsNullOrEmpty(_currentDraftName))
                    {
                        // Đã có tên nháp -> Tự động cập nhật
                        await HandleSaveDraftFlowAsync(_currentDraftName);
                    }
                    else
                    {
                        // Cấu hình mới -> Hỏi có muốn lưu nháp không
                        var drSaveDraft = MessageBox.Show("Bạn có muốn lưu nội dung này thành Cấu hình nháp không?", 
                            "Lưu cấu hình nháp", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (drSaveDraft == DialogResult.Yes)
                        {
                            // Mở modal nhập tên (truyền null để hiện modal)
                            await HandleSaveDraftFlowAsync(null);
                        }
                    }

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Danh sách đang trống, vui lòng thêm ít nhất 1 cấu hình!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            // Xóa logic cũ của btnAddToGrid và vô hiệu hóa các tác động khác lên nó
            // btnAddToGrid.Click += (s, e) => { ... } // (Đã được chuyển sang BtnLuuNhap_Click ở trên)

            // Nút XÓA trong DataGridView
            dgvSelectedItems.CellClick += (s, e) => {
                if (e.ColumnIndex == dgvSelectedItems.Columns["colXoa"].Index && e.RowIndex >= 0)
                {
                    dgvSelectedItems.Rows.RemoveAt(e.RowIndex);
                    btnApply.Enabled = dgvSelectedItems.Rows.Count > 0;
                    RenumberGridSTT();
                }
            };

            dgvSelectedItems.CellEndEdit += (s, e) =>
            {
                if (e.RowIndex >= 0 &&
                    (e.ColumnIndex == dgvSelectedItems.Columns["colSoLuong"].Index ||
                     e.ColumnIndex == dgvSelectedItems.Columns["colDonGia"].Index))
                {
                    RecalculateSelectedItemRow(dgvSelectedItems.Rows[e.RowIndex]);
                }
            };

            // Vẽ Icon Thùng Rác (SVG-like) cho cột Xóa
            dgvSelectedItems.CellPainting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == dgvSelectedItems.Columns["colXoa"].Index)
                {
                    // Tự vẽ nền cell để xóa màu nền đỏ của ButtonColumn cũ
                    using (Brush cellBg = new SolidBrush(e.State.HasFlag(DataGridViewElementStates.Selected) ? e.CellStyle.SelectionBackColor : Color.White))
                    {
                        e.Graphics.FillRectangle(cellBg, e.CellBounds);
                    }
                    e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
                    
                    Graphics g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    // Khung button đen nhỏ bó sát icon
                    int btnWidth = 24;
                    int btnHeight = 26;
                    int btnX = e.CellBounds.Left + (e.CellBounds.Width - btnWidth) / 2;
                    int btnY = e.CellBounds.Top + (e.CellBounds.Height - btnHeight) / 2;
                    
                    Rectangle btnRect = new Rectangle(btnX, btnY, btnWidth, btnHeight);
                    using (Brush bgBrush = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(bgBrush, btnRect);
                    }

                    int iconWidth = 12;
                    int iconHeight = 14;
                    int x = btnX + (btnWidth - iconWidth) / 2;
                    int y = btnY + (btnHeight - iconHeight) / 2;

                    using (Pen pen = new Pen(Color.Black, 1.5f))
                    {
                        // Nắp
                        g.DrawLine(pen, x, y + 2, x + iconWidth, y + 2);
                        g.DrawLine(pen, x + 3, y + 2, x + 3, y);
                        g.DrawLine(pen, x + iconWidth - 3, y + 2, x + iconWidth - 3, y);
                        g.DrawLine(pen, x + 3, y, x + iconWidth - 3, y);

                        // Thân
                        g.DrawLine(pen, x + 1, y + 2, x + 2, y + iconHeight);
                        g.DrawLine(pen, x + iconWidth - 1, y + 2, x + iconWidth - 2, y + iconHeight);
                        g.DrawLine(pen, x + 2, y + iconHeight, x + iconWidth - 2, y + iconHeight);

                        // Sọc
                        g.DrawLine(pen, x + 4, y + 5, x + 4, y + iconHeight - 2);
                        g.DrawLine(pen, x + iconWidth - 4, y + 5, x + iconWidth - 4, y + iconHeight - 2);
                    }
                    e.Handled = true;
                }
            };

            // Menustrip để hiển thị Modal tính toán khi chuột phải vào dòng mặc định
            var ctxMenu = new ContextMenuStrip();
            ctxMenu.Items.Add("Tính toán...", null, (s, ev) => {
                MessageBox.Show("Chức năng Load Modal Tính Toán đang đợi File Thiết kế", "Tính Toán", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            dgvSelectedItems.CellMouseUp += (s, e) => {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    var rowName = dgvSelectedItems.Rows[e.RowIndex].Cells["colTen"].Value?.ToString() ?? "";
                    if (IsDefaultSelectedItemName(rowName))
                    {
                        dgvSelectedItems.ClearSelection();
                        dgvSelectedItems.Rows[e.RowIndex].Selected = true;
                        ctxMenu.Show(Cursor.Position);
                    }
                }
            };
        }

        private string GetFullPathForNode(HierarchyNode targetNode, TreeNodeCollection nodes)
        {
            if (nodes == null) return null;
            foreach (TreeNode tn in nodes)
            {
                if (tn.Tag == targetNode) return tn.FullPath;
                string foundPath = GetFullPathForNode(targetNode, tn.Nodes);
                if (foundPath != null) return foundPath;
            }
            return null;
        }

        private async void BtnLuuNhap_Click(object sender, EventArgs e)
        {
            await HandleSaveDraftFlowAsync(null);
        }

        private async Task<bool> HandleSaveDraftFlowAsync(string forcedDraftName)
        {
            // 1. Thu thập dữ liệu từ cache và grid
            var draftItems = new List<Tuple<string, string, string, string>>(); 
            
            foreach (var kvp in _expandStateCache)
            {
                var node = kvp.Key;
                var state = kvp.Value;
                string formula = node.Formula ?? "";
                string fullPath = GetFullPathForNode(node, _modernTreeView.Nodes) ?? node.Name;

                foreach (var row in state.ConfigRows)
                {
                    Products p = row.SelectedProduct;
                    if (p == null) continue;

                    var noteItems = new List<string>();
                    var dictValues = new Dictionary<string, string>();
                    foreach (var kvAttr in row.Attrs)
                    {
                        string val = kvAttr.Value.Text.Trim();
                        noteItems.Add($"{kvAttr.Key}: {val}");
                        dictValues[kvAttr.Key] = val;
                    }

                    if (!string.IsNullOrEmpty(formula))
                    {
                        decimal? kq = EvaluateAdvancedFormula(formula, p, dictValues);
                        if (kq.HasValue) noteItems.Add($"={formula} → {kq.Value:N2}");
                    }
                    
                    // Chỉ lưu Tên sản phẩm sạch vào draftItems. Item1 (fullPath) sẽ vào Cột A, Item2 (finalName) sẽ vào Cột B.
                    string finalName = p.Name;
                    draftItems.Add(new Tuple<string, string, string, string>(fullPath, finalName, "1", string.Join(" | ", noteItems)));
                }
            }

            foreach (var kv in _pendingDraftItems)
            {
                string path = kv.Key;
                foreach(var dItem in kv.Value)
                    draftItems.Add(new Tuple<string, string, string, string>(path, dItem.Item1, dItem.Item2.ToString(), dItem.Item3));
            }

            if (draftItems.Count == 0)
            {
                string fallbackPath = SelectedHeader;
                if (string.IsNullOrWhiteSpace(fallbackPath))
                    fallbackPath = (_modernTreeView?.SelectedNode?.Text ?? "Sản phẩm đã chọn").Trim();

                foreach (DataGridViewRow row in dgvSelectedItems.Rows)
                {
                    if (row.IsNewRow) continue;
                    string tenHang = row.Cells["colTen"].Value?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(tenHang)) continue;

                    string ghiChu = string.Join(" | ", new[] {
                        $"Model: {row.Cells["colModel"].Value?.ToString() ?? ""}",
                        $"SKU: {row.Cells["colSKU"].Value?.ToString() ?? ""}",
                        $"Xuất xứ: {row.Cells["colXuatXu"].Value?.ToString() ?? ""}",
                        $"Đơn vị: {row.Cells["colDonVi"].Value?.ToString() ?? ""}",
                        $"Đơn giá: {row.Cells["colDonGia"].Value?.ToString() ?? "0"}",
                        $"Thành tiền: {row.Cells["colGiaTien"].Value?.ToString() ?? "0"}",
                        $"Giá nhập: {row.Cells["colGiaNhap"].Value?.ToString() ?? "0"}",
                        $"Danh mục: {row.Cells["colDanhMuc"].Value?.ToString() ?? ""}",
                        $"Type: {row.Cells["colType"].Value?.ToString() ?? ""}",
                        $"Hãng: {row.Cells["colHang"].Value?.ToString() ?? ""}",
                        $"Tiến độ: {row.Cells["colTienDo"].Value?.ToString() ?? "0"}"
                    }.Where(x => !x.EndsWith(": ")));

                    draftItems.Add(new Tuple<string, string, string, string>(fallbackPath, tenHang, "1", ghiChu));
                }
            }

            if (draftItems.Count == 0)
            {
                if (string.IsNullOrEmpty(forcedDraftName))
                    MessageBox.Show("Không có cấu hình nào trong nháp để lưu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // 2. Chế độ Lưu (Nếu có forcedDraftName thì không hiện Modal nhập tên)
            if (!string.IsNullOrEmpty(forcedDraftName))
            {
                return await CommitSaveDraftToSheetsAsync(forcedDraftName, draftItems);
            }

            // 3. Hiện Modal xác nhận và nhập tên (Trường hợp lưu mới hoặc nhấn nút Lưu Nháp)
            bool result = false;
            using (var draftForm = new Form { Text = "Xác nhận Lưu Cấu Hình Nháp", Size = new Size(1000, 500), StartPosition = FormStartPosition.CenterParent, ShowIcon = false })
            {
                var pnlTop = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(245, 250, 255) };
                var lblDraftName = new Label { Text = "Tên cấu hình nháp:", AutoSize = true, Location = new Point(20, 18), Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 80, 200) };
                var txtDraftName = new TextBox { Location = new Point(160, 15), Size = new Size(400, 27), Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(180, 50, 0) };
                if (!string.IsNullOrEmpty(_currentDraftName)) txtDraftName.Text = _currentDraftName;

                pnlTop.Controls.Add(lblDraftName);
                pnlTop.Controls.Add(txtDraftName);

                var grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, ReadOnly = true, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
                grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.Yellow, ForeColor = Color.DarkBlue, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), SelectionBackColor = Color.Gold, SelectionForeColor = Color.DarkBlue };
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersHeight = 36;

                var previewTable = BuildSelectedItemsPreviewTable();
                foreach (System.Data.DataColumn col in previewTable.Columns)
                    grid.Columns.Add(new DataGridViewTextBoxColumn { Name = col.ColumnName, HeaderText = col.ColumnName, DataPropertyName = col.ColumnName });
                grid.DataSource = previewTable;

                var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.FromArgb(245, 245, 245) };
                var btnSave = new Button { Text = "✔ XÁC NHẬN LƯU", Size = new Size(200, 40), BackColor = Color.FromArgb(0, 150, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Cursor = Cursors.Hand };
                pnlBottom.Resize += (s2, e2) => btnSave.Location = new Point((pnlBottom.Width - btnSave.Width) / 2, (pnlBottom.Height - btnSave.Height) / 2);
                pnlBottom.Controls.Add(btnSave);

                draftForm.Controls.Add(grid); draftForm.Controls.Add(pnlTop); draftForm.Controls.Add(pnlBottom);
                pnlTop.SendToBack(); grid.BringToFront(); pnlBottom.BringToFront();

                btnSave.Click += async (s1, e1) => {
                    string dName = txtDraftName.Text.Trim();
                    if (string.IsNullOrEmpty(dName)) { MessageBox.Show("Vui lòng nhập Tên!", "Thông báo"); return; }
                    btnSave.Enabled = false;
                    if (await CommitSaveDraftToSheetsAsync(dName, draftItems)) { result = true; draftForm.DialogResult = DialogResult.OK; }
                    btnSave.Enabled = true;
                };
                draftForm.ShowDialog();
            }
            return result;
        }

        private async Task<bool> CommitSaveDraftToSheetsAsync(string draftName, List<Tuple<string, string, string, string>> draftItems)
        {
            try
            {
                var spreadsheetInfo = await _service.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
                var tSheet = spreadsheetInfo.Sheets.FirstOrDefault(s => s.Properties.Title == "Cấu hình nháp");
                int shId = 0;
                if (tSheet == null) {
                    var addReq = new Google.Apis.Sheets.v4.Data.Request { AddSheet = new Google.Apis.Sheets.v4.Data.AddSheetRequest { Properties = new Google.Apis.Sheets.v4.Data.SheetProperties { Title = "Cấu hình nháp" } } };
                    var resp = await _service.Spreadsheets.BatchUpdate(new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest { Requests = new List<Google.Apis.Sheets.v4.Data.Request> { addReq } }, _spreadsheetId).ExecuteAsync();
                    shId = resp.Replies[0].AddSheet.Properties.SheetId ?? 0;
                } else shId = tSheet.Properties.SheetId ?? 0;

                var hCheck = await _service.Spreadsheets.Values.Get(_spreadsheetId, "Cấu hình nháp!A1:A1").ExecuteAsync();
                bool hasHeader = hCheck.Values != null && hCheck.Values.Count > 0 && hCheck.Values[0][0]?.ToString() == "Vị trí cấu hình";

                // Xóa cũ if exists
                var checkD = await _service.Spreadsheets.Values.Get(_spreadsheetId, "Cấu hình nháp!A:B").ExecuteAsync();
                if (checkD.Values != null) {
                    int sR = -1, eR = -1;
                    for (int i = 1; i < checkD.Values.Count; i++) {
                        var row = checkD.Values[i];
                        string a = row.Count > 0 ? row[0]?.ToString()?.Trim() : "";
                        string b = row.Count > 1 ? row[1]?.ToString()?.Trim() : "";
                        if (sR == -1 && a == draftName && string.IsNullOrEmpty(b)) sR = i;
                        else if (sR != -1 && !string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b) && a != "Tên cấu hình nháp") { eR = i; break; }
                    }
                    if (sR != -1) {
                        if (eR == -1) eR = checkD.Values.Count;
                        var delReq = new Google.Apis.Sheets.v4.Data.Request { DeleteDimension = new Google.Apis.Sheets.v4.Data.DeleteDimensionRequest { Range = new Google.Apis.Sheets.v4.Data.DimensionRange { SheetId = shId, Dimension = "ROWS", StartIndex = sR, EndIndex = eR } } };
                        await _service.Spreadsheets.BatchUpdate(new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest { Requests = new List<Google.Apis.Sheets.v4.Data.Request> { delReq } }, _spreadsheetId).ExecuteAsync();
                    }
                }

                var values = new List<IList<object>>();
                if (!hasHeader) values.Add(new List<object> { "Vị trí cấu hình", "Tên hàng", "Model", "Mã SKU", "Xuất xứ", "Đơn vị", "Số lượng", "Đơn giá", "Thành tiền", "Giá nhập", "Danh mục", "Type", "Hãng", "Tiến độ", "Các thuộc tính" });
                values.Add(new List<object> { draftName, "", "", "", "", "", "", "", "", "", "", "", "", "", "" });

                // Lấy Full Path từ TreeView cho Cột A (Vị trí cấu hình)
                string path = "";
                if (_modernTreeView?.SelectedNode != null)
                {
                    path = _modernTreeView.SelectedNode.FullPath.Replace(_modernTreeView.PathSeparator, " - ");
                }
                if (string.IsNullOrWhiteSpace(path)) path = SelectedHeader;
                if (string.IsNullOrWhiteSpace(path)) path = (_modernTreeView?.SelectedNode?.Text ?? "Sản phẩm đã chọn").Trim();

                foreach (DataGridViewRow row in dgvSelectedItems.Rows) {
                    if (row.IsNewRow) continue;
                    string name = row.Cells["colTen"].Value?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(name)) continue;

                    var entry = draftItems.FirstOrDefault(x => string.Equals(x.Item2?.Trim(), name, StringComparison.OrdinalIgnoreCase));
                    string nts = entry?.Item4 ?? "";
                    string itemPath = entry?.Item1 ?? path; 

                    values.Add(BuildDraftSheetRowFromGrid(row, itemPath, nts));
                }

                var vRange = new Google.Apis.Sheets.v4.Data.ValueRange { Values = values };
                var appReq = _service.Spreadsheets.Values.Append(vRange, _spreadsheetId, "Cấu hình nháp!A:O");
                appReq.ValueInputOption = Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                var appResp = await appReq.ExecuteAsync();

                // Format
                var match = System.Text.RegularExpressions.Regex.Match(appResp.Updates.UpdatedRange ?? "", @"[A-Za-z]+(\d+)");
                if (match.Success) {
                    int startI = int.Parse(match.Groups[1].Value) - 1;
                    int gI = hasHeader ? startI : startI + 1;
                    var cReqs = new List<Google.Apis.Sheets.v4.Data.Request>();
                    if (!hasHeader) cReqs.Add(new Google.Apis.Sheets.v4.Data.Request { RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest { Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = shId, StartRowIndex = startI, EndRowIndex = startI+1, StartColumnIndex = 0, EndColumnIndex = 15 }, Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 1f, Green = 0.9f, Blue = 0f }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true }, HorizontalAlignment = "CENTER" } }, Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment)" } });
                    cReqs.Add(new Google.Apis.Sheets.v4.Data.Request { RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest { Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = shId, StartRowIndex = gI, EndRowIndex = gI+1, StartColumnIndex = 0, EndColumnIndex = 15 }, Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0.2f, Green = 0.8f, Blue = 0.2f }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true } } }, Fields = "userEnteredFormat(backgroundColor,textFormat)" } });
                    await _service.Spreadsheets.BatchUpdate(new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest { Requests = cReqs }, _spreadsheetId).ExecuteAsync();
                }

                _expandStateCache.Clear(); _pendingDraftItems.Clear(); _currentDraftName = draftName;
                return true;
            }
            catch (Exception ex) { MessageBox.Show("Lỗi lưu nháp: " + ex.Message); return false; }
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
            _expandStateCache.Clear();
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
                // btnAddToGrid.Enabled = _modernTreeView.SelectedNode != null;
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

        private class ConfigRow
        {
            public Helper.ProductSearchDropdown SearchControl;
            public Dictionary<string, TextBox> Attrs = new Dictionary<string, TextBox>();
            public Products SelectedProduct;
            public TableLayoutPanel RowPanel;
            public DataGridViewRow GridRowReference; // Link to the row in dgvSelectedItems
        }

        private List<ConfigRow> _configRows = new List<ConfigRow>();
        private TableLayoutPanel _pnlRowsContainer = null;
        private Label _lblSelectedProductPhase2 = null;
        private Products _selectedProduct = null;
        private Dictionary<string, TextBox> _dynamicTextBoxes = new Dictionary<string, TextBox>();
        private Panel _pnlPhase2 = null;

        private class ExpandPanelState
        {
            public List<ConfigRow> ConfigRows;
            public TableLayoutPanel RowsContainer;
            public Control ContentPanel;
        }
        private Dictionary<HierarchyNode, ExpandPanelState> _expandStateCache = new Dictionary<HierarchyNode, ExpandPanelState>();
        private TreeNode _currentActiveTypeCMB = null;

        /// <summary>
        /// Xây dựng nội dung bên trong expand panel theo loại Config.
        /// Config syntax: "search, height, width, color, icu, ir, pole"
        /// - "search" → hiện ô tìm kiếm sản phẩm
        /// - các từ sau → thuộc tính hiển thị sau khi chọn SP
        /// OnlyOne=No → hiển thị SP đầu tiên (lấy default) + ô thuộc tính + nút Add
        /// OnlyOne=Yes → hiễn thị search + thuộc tính
        /// </summary>
        private void BuildExpandContent(string configRaw)
        {
            _expandPanel.Controls.Clear();

            if (_expandedNode != null && _expandStateCache.TryGetValue(_expandedNode, out var cachedState))
            {
                _configRows = cachedState.ConfigRows;
                _pnlRowsContainer = cachedState.RowsContainer;
                _expandPanel.Controls.Add(cachedState.ContentPanel);
                cachedState.ContentPanel.Dock = DockStyle.Fill;
                return;
            }

            _txtSearch = null; _btnSearch = null; _lblExpandTitle = null; _lblProductInfo = null; _dgvSearchResults = null;
            _selectedProduct = null;
            _dynamicTextBoxes.Clear();
            _pnlPhase2 = null;
            _lblSelectedProductPhase2 = null;

            if (string.IsNullOrWhiteSpace(configRaw)) configRaw = "";

            // --- Phân tích Config ---
            // VD: "search, height, width, color, icu, ir, pole"
            var configParts = configRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(p => p.Trim().ToLower()).ToList();
            bool hasSearch  = configParts.Contains("search");
            // Phần còn lại là tên thuộc tính cần hiển thị
            var attrKeys = configParts.Where(p => p != "search").ToList();

            // OnlyOne: Yes → bắt phải search; No → tự động load dòng sản phẩm
            string onlyOne = _expandedNode?.OnlyOne?.Trim()?.ToLower() ?? "";
            bool isOnlyOne = (onlyOne == "yes" || onlyOne == "có" || onlyOne == "true");
            bool mustSearch = hasSearch && isOnlyOne;
            // OnlyOne=No và có search → tự lấy dòng sản phẩm theo category + Type, hiển thị thuộc tính để edit
            bool autoLoad  = hasSearch && !mustSearch; // No/blank → tự load 1 dòng nếu type onlyOne là Yes thì k hiện button thêm dòng cấu hình

            // --- Lấy danh sách sản phẩm phù hợp theo Type Workflow vs Type Products_Table ---
            string nodeType = (_expandedNode?.Type ?? "").Trim();
            List<Products> filteredProducts;
            if (!string.IsNullOrEmpty(nodeType))
            {
                // So sánh Type chính xác trước (OrdinalIgnoreCase + Trim)
                filteredProducts = _allProducts.Where(p =>
                    string.Equals((p.Type ?? "").Trim(), nodeType, StringComparison.OrdinalIgnoreCase)).ToList();
                // Nếu không tìm thấy theo Type → thử tìm theo Category chứa nodeType
                if (filteredProducts.Count == 0)
                    filteredProducts = _allProducts.Where(p =>
                        (p.Category ?? "").IndexOf(nodeType, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }
            else
                filteredProducts = _allProducts.ToList();

            // --- Làm nhãn: chỉ dùng attrKey trực tiếp làm label và cột tra cứu ---
            // Nghĩa/Biến chỉ dùng để map biến vào công thức, không dùng cho nhãn UI
            var nghiaList = ((_expandedNode?.Nghia ?? "")).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            var bienList  = ((_expandedNode?.Bien  ?? "")).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            var attrLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var attrVarMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int ai = 0; ai < attrKeys.Count; ai++)
            {
                string aKey = attrKeys[ai];
                // Label hiển thị: dùng chính attrKey.ToUpper()
                attrLabels[aKey] = aKey.ToUpper();
                // Biến cho công thức: dùng Biến nếu có, ngược lại dùng attrKey
                attrVarMap[aKey] = (ai < bienList.Count && !string.IsNullOrEmpty(bienList[ai])) ? bienList[ai] : aKey;
            }

            string formula = _expandedNode?.Formula ?? "";

            var pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new System.Windows.Forms.Padding(10, 8, 10, 8),
                BackColor = Color.FromArgb(248, 252, 255), AutoScroll = true };

            // =============================================================
            // UNIFIED MULTI-ROW CONFIGURATION
            // =============================================================
            
            _configRows = new List<ConfigRow>();
            // --- TẠO CẤU TRÚC PANEL 2 LỚP ---
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 35, BackColor = Color.Transparent };
            var pnlMiddle = new Panel { 
                Dock = DockStyle.Fill, 
                AutoScroll = true, 
                BackColor = Color.Transparent,
                Padding = new System.Windows.Forms.Padding(10, 5, 10, 5)
            };
            // Đảm bảo thanh cuộn xuất hiện khi nội dung vượt quá
            pnlMiddle.VerticalScroll.Enabled = true;
            pnlMiddle.HorizontalScroll.Enabled = true;
            pnlMiddle.HorizontalScroll.Visible = true;

            // 1. Header: Tiêu đề + Nút Thêm dòng
            var lblTitle = new Label { 
                Text = $"📄 Cấu hình sản phẩm [{nodeType}]", 
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), 
                ForeColor = Color.FromArgb(0, 50, 150), 
                AutoSize = true, Location = new Point(10, 8) 
            };
            pnlHeader.Controls.Add(lblTitle);

            var btnGlobalAddRow = new Button {
                Text = "➕ Thêm dòng cấu hình",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Size = new Size(160, 26), 
                Location = new Point(lblTitle.Right + 30, 4),
                Cursor = Cursors.Hand,
                Visible = !isOnlyOne
            };
            btnGlobalAddRow.FlatAppearance.BorderSize = 0;
            btnGlobalAddRow.Click += (s, e) => AddConfigRowUI(attrKeys, attrLabels, formula, false, hasSearch ? filteredProducts : null);
            pnlHeader.Controls.Add(btnGlobalAddRow);

            // 2. Middle: Danh sách các dòng Config (Dùng lại _pnlRowsContainer)
            _pnlRowsContainer = new TableLayoutPanel {
                ColumnCount = 1,
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new System.Windows.Forms.Padding(0)
            };
            _pnlRowsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pnlMiddle.Controls.Add(_pnlRowsContainer);

            // --- KIỂM TRA PENDING DRAFTS ĐỂ TỰ ĐỘNG NẠP LẠI (LAZY HYDRATION) ---
            string nodePath = GetFullPathForNode(_expandedNode, _modernTreeView.Nodes) ?? _expandedNode.Name;
            if (_pendingDraftItems.ContainsKey(nodePath) && _pendingDraftItems[nodePath].Count > 0)
            {
                var draftsForNode = _pendingDraftItems[nodePath];
                foreach (var draft in draftsForNode)
                {
                    string targetProductName = draft.Item1;
                    int targetQty = draft.Item2; 
                    string targetAttrsStr = draft.Item3; 

                    // Bơm UI
                    AddConfigRowUI(attrKeys, attrLabels, formula, false, hasSearch ? filteredProducts : null);
                    var rowUI = _configRows.Last();

                    // Nạp sản phẩm
                    Products matchedProduct = filteredProducts?.FirstOrDefault(p => p.Name == targetProductName);
                    if (matchedProduct == null && _allProducts != null) matchedProduct = _allProducts.FirstOrDefault(p => p.Name == targetProductName);
                    
                    if (matchedProduct != null)
                    {
                        if (rowUI.SearchControl != null) rowUI.SearchControl.Text = matchedProduct.Name;
                        rowUI.SelectedProduct = matchedProduct;
                    }

                    // Nạp lại Ghi chú / Thuộc tính
                    var dictAttrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var splitAttrs = targetAttrsStr.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach(var pair in splitAttrs)
                    {
                        var kv = pair.Split(new[] { ':' }, 2);
                        if(kv.Length == 2) dictAttrs[kv[0].Trim()] = kv[1].Trim();
                    }

                    foreach(var kv in rowUI.Attrs)
                    {
                        if (dictAttrs.ContainsKey(kv.Key))
                        {
                            kv.Value.Text = dictAttrs[kv.Key];
                        }
                    }

                    // Link tới dòng DataGridView hiện tại
                    string prefix = GetNodePathPrefix();
                    string expectedNameInGrid = string.IsNullOrEmpty(prefix) ? targetProductName : $"{prefix}: {targetProductName}";

                    var existingGridRow = dgvSelectedItems.Rows.Cast<DataGridViewRow>()
                        .FirstOrDefault(gr => (gr.Cells["colTen"].Value?.ToString() ?? "") == expectedNameInGrid);
                    if (existingGridRow != null)
                    {
                        rowUI.GridRowReference = existingGridRow;
                    }
                }
                // Xóa để tránh nạp lại lần 2
                _pendingDraftItems.Remove(nodePath);
            }
            else
            {
                // CASE BÌNH THƯỜNG: Thêm dòng đầu tiên trống
                AddConfigRowUI(attrKeys, attrLabels, formula, hasSearch && autoLoad, hasSearch ? filteredProducts : null);
            }

            var container = new Panel { Dock = DockStyle.Fill };
            container.Controls.Add(pnlMiddle);
            container.Controls.Add(pnlHeader);
            
            pnlHeader.SendToBack();

            _expandPanel.Controls.Add(container);

            if (_expandedNode != null)
            {
                _expandStateCache[_expandedNode] = new ExpandPanelState {
                    ConfigRows = _configRows,
                    RowsContainer = _pnlRowsContainer,
                    ContentPanel = container
                };
            }
        }

        private void AddConfigRowUI(List<string> attrKeys, Dictionary<string, string> attrLabels, string formula, bool autoLoad, List<Products> products)
        {
            var row = new ConfigRow();
            _configRows.Add(row);

            bool showDelete = _configRows.Count > 1; // Không cho xóa dòng đầu tiên
            // 2 for Search, 2 for Qty, 2*attrs, 1 for Delete
            int colCount = (products != null ? 2 : 0) + 2 + (attrKeys.Count * 2) + 1;
            
            var rowPanel = new TableLayoutPanel {
                RowCount = 1,
                ColumnCount = colCount,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = (_configRows.Count % 2 != 0) ? Color.White : Color.FromArgb(250, 252, 255),
                Margin = new System.Windows.Forms.Padding(0, 0, 0, 1),
                Padding = new System.Windows.Forms.Padding(5, 5, 5, 5)
            };
            
            if (products != null) {
                rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));   // Nhãn Sản phẩm (Tăng nhẹ để không xuống dòng)
                rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));  // Dropdown Sản phẩm
            }
            // Qty columns
            rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));       // Nhãn Số lượng
            rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45f));       // Ô nhập Số lượng
            foreach (var aKey in attrKeys) {
                rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75f));   // Nhãn thuộc tính
                rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60f));   // Ô nhập thuộc tính
            }
            rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30f));       // Luôn dành chỗ cho nút xóa

            row.RowPanel = rowPanel;
            int searchStartColumn = 0;
            int qtyStartColumn = products != null ? 2 : 0;

            // Khởi tạo txtQty trước để dùng trong sự kiện của cbo
            var txtQty = new TextBox { 
                Name = "txt_qty", 
                Text = "1", 
                Font = new Font("Segoe UI", 9.5f), 
                Width = 35,
                Margin = new System.Windows.Forms.Padding(0, 5, 8, 0) // Giảm margin right từ 15 -> 8
            };
            txtQty.Leave += (s, e) =>
            {
                if (!int.TryParse(txtQty.Text.Trim(), out int qty) || qty <= 0)
                {
                    txtQty.Text = "1";
                }
            };
            txtQty.TextChanged += (s, ev) =>
            {
                if (row.GridRowReference != null && dgvSelectedItems.Rows.Contains(row.GridRowReference))
                {
                    row.GridRowReference.Cells["colSoLuong"].Value = txtQty.Text;
                    RecalculateSelectedItemRow(row.GridRowReference);
                }
            };
            row.Attrs["_internal_qty_"] = txtQty;

            // Thêm Search Controls vào TRƯỚC (để nằm bên Tái)
            if (products != null)
            {
                var lblSearch = new Label { 
                    Text = "🔍 Sản phẩm:", 
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold), 
                    ForeColor = Color.FromArgb(0, 50, 150), 
                    AutoSize = true, 
                    Anchor = AnchorStyles.Right | AnchorStyles.Top, // Cho nhảy sang Phải để sát ô nhập
                    Margin = new System.Windows.Forms.Padding(0, 8, 2, 0) // Sát lề phải
                };
                rowPanel.Controls.Add(lblSearch);
                rowPanel.SetColumn(lblSearch, searchStartColumn);

                var cbo = new Helper.ProductSearchDropdown { 
                    Font = new Font("Segoe UI", 9.5f), 
                    Dock = DockStyle.Fill, 
                    Margin = new System.Windows.Forms.Padding(0, 5, 8, 0) // Giảm margin từ 10 -> 8
                };
                cbo.LoadData(products);
                cbo.ProductSelected += (s, p) => {
                    row.SelectedProduct = p;
                    PopulateAttrPanelRow(row, p, attrKeys);
                    AutoAddProductToGrid(row, p, txtQty.Text, attrKeys, formula); // Tính năng auto add
                };
                row.SearchControl = cbo;
                rowPanel.Controls.Add(cbo);
                rowPanel.SetColumn(cbo, searchStartColumn + 1);

                if (products.Count > 0)
                {
                    cbo.Text = "";
                }
            }

            // Thêm Qty Controls vào SAU (để nằm bên Phải của Sản phẩm)
            var lblQty = new Label { 
                Text = "Số lượng:", 
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), 
                ForeColor = Color.FromArgb(0, 50, 150), 
                AutoSize = true, 
                Anchor = AnchorStyles.Right | AnchorStyles.Top, // Căn phải
                Margin = new System.Windows.Forms.Padding(0, 8, 2, 0) // Sát lề phải
            };
            rowPanel.Controls.Add(lblQty);
            rowPanel.SetColumn(lblQty, qtyStartColumn);

            rowPanel.Controls.Add(txtQty);
            rowPanel.SetColumn(txtQty, qtyStartColumn + 1);

            // Attrs
            foreach (var aKey in attrKeys)
            {
                string label = attrLabels.TryGetValue(aKey, out string lbl) ? lbl : aKey.ToUpper();
                rowPanel.Controls.Add(new Label { 
                    Text = label + ":", 
                    Font = new Font("Segoe UI", 8.2f, FontStyle.Bold), 
                    ForeColor = Color.FromArgb(80, 80, 80),
                    AutoSize = true, 
                    Anchor = AnchorStyles.Right | AnchorStyles.Top, // Căn phải
                    Margin = new System.Windows.Forms.Padding(5, 8, 2, 0) // Sát lề phải
                });
                
                var txt = new TextBox { 
                    Name = "txt_" + aKey, 
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 9f), 
                    BorderStyle = BorderStyle.FixedSingle, 
                    Margin = new System.Windows.Forms.Padding(0, 6, 8, 0), // Giảm margin từ 10 -> 8
                    ReadOnly = true, // Khóa mặc định
                    BackColor = Color.FromArgb(240, 240, 240), // Màu xám mặc định
                    TabStop = false, // Không cho Tab tới ô này
                    Enabled = false, // Vô hiệu hóa hoàn toàn để chặn nhập liệu
                    Text = "0" // Giá trị mặc định
                };
                txt.KeyPress += (s, ev) => { ev.Handled = true; }; // Chặn đứng mọi thao tác bàn phím
                row.Attrs[aKey] = txt;
                rowPanel.Controls.Add(txt);
            }

            // Nút xóa dòng này
            if (showDelete)
            {
                var btnDel = new Button { 
                    Text = "✕", Size = new Size(24, 24), 
                    FlatStyle = FlatStyle.Flat, ForeColor = Color.Red, 
                    Margin = new System.Windows.Forms.Padding(5, 3, 0, 0), 
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                btnDel.FlatAppearance.BorderSize = 0;
                btnDel.Click += (s, e) => { 
                    if (row.GridRowReference != null && dgvSelectedItems.Rows.Contains(row.GridRowReference))
                    {
                        dgvSelectedItems.Rows.Remove(row.GridRowReference);
                    }
                    _pnlRowsContainer.Controls.Remove(rowPanel); 
                    _configRows.Remove(row); 
                };
                rowPanel.Controls.Add(btnDel);
            }

            _pnlRowsContainer.Controls.Add(rowPanel);
            
            if (row.SelectedProduct != null) PopulateAttrPanelRow(row, row.SelectedProduct, attrKeys);
        }

        private void PopulateAttrPanelRow(ConfigRow row, Products p, List<string> attrKeys)
        {
            if (p == null) return;
            foreach (var aKey in attrKeys)
            {
                if (row.Attrs.TryGetValue(aKey, out TextBox txt))
                {
                    string val = p.GetAttribute(aKey);
                    txt.Text = string.IsNullOrEmpty(val) ? "0" : val;
                    txt.ReadOnly = true;
                    txt.TabStop = false; // Chặn Tab
                    txt.Enabled = false; // Khóa cứng
                    txt.BackColor = Color.FromArgb(240, 240, 240); // Luôn khóa và làm xám ô
                }
            }
        }

        private void AutoAddProductToGrid(ConfigRow row, Products p, string slText, List<string> attrKeys, string formula)
        {
            if (p == null) return;
            string prefix = GetNodePathPrefix();
            string finalName = string.IsNullOrEmpty(prefix) ? p.Name : $"{prefix}: {p.Name}";

            int sl = 1;
            int.TryParse(slText, out sl);
            if (sl <= 0) sl = 1;

            decimal donGia = 0;
            if (!string.IsNullOrEmpty(p.Price))
            {
                decimal.TryParse(p.Price.Replace(".", "").Replace(",", ""), out donGia);
            }

            if (row.GridRowReference != null && dgvSelectedItems.Rows.Contains(row.GridRowReference))
            {
                // Cập nhật dòng hiện tại thay vì thêm mới
                row.GridRowReference.Cells["colTen"].Value = finalName;
                row.GridRowReference.Cells["colSoLuong"].Value = sl.ToString();
                row.GridRowReference.Cells["colDonGia"].Value = FormatCurrencyVnd(donGia);
                row.GridRowReference.Tag = p;

                // Cập nhật các cột thông tin kèm theo
                row.GridRowReference.Cells["colModel"].Value = p.Model ?? "";
                row.GridRowReference.Cells["colSKU"].Value = p.SKU ?? "";
                row.GridRowReference.Cells["colXuatXu"].Value = GetProductBrand(p);
                row.GridRowReference.Cells["colDonVi"].Value = IsDefaultSelectedItemName(finalName) ? "Tủ" : "Cái";

                RecalculateSelectedItemRow(row.GridRowReference);
            }
            else
            {
                // Thêm dòng mới và lưu reference
                int insIdx = GetInsertIndex();
                int rowIndex = AddSelectedItemRow(finalName, sl, donGia, "0", "0", p, false, insIdx);
                row.GridRowReference = dgvSelectedItems.Rows[rowIndex];
            }

            btnApply.Enabled = true;

            if (_lblProductInfo != null)
            {
                _lblProductInfo.Text = $"✔ Đã tự động cập nhật [{finalName}] vào danh sách dưới.";
                _lblProductInfo.ForeColor = Color.FromArgb(0, 160, 60);
            }

            string onlyOne = _expandedNode?.OnlyOne?.Trim()?.ToLower() ?? "";
            if (onlyOne == "yes" || onlyOne == "có" || onlyOne == "true")
                HideExpandPanel();
        }
        /// <summary>
        /// Tạo panel Phase 2: hiển thị các thuộc tính + nút Thêm.
        /// </summary>
        private Panel BuildPhase2Panel(List<string> attrKeys, Dictionary<string, string> attrLabels,
                                       Dictionary<string, string> attrVarMap, string formula, bool startHidden,
                                       Control searchControl = null, bool showAddRowButton = false)
        {
            var pnl = new FlowLayoutPanel { 
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Width = 920, 
                AutoSize = true, 
                BackColor = Color.Transparent,
                Padding = new System.Windows.Forms.Padding(0)
            };

            // Layout NGANG: Search + Attributes gom chung một luồng
            var flowMain = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                Width = 910,
                BackColor = Color.Transparent,
                Padding = new System.Windows.Forms.Padding(10, 5, 0, 5),
                Margin = new System.Windows.Forms.Padding(0)
            };

            // --- SEARCH CONTROL (Gộp vào hàng ngang) ---
            if (searchControl != null)
            {
                var lblSearch = new Label { Text = "🔍 Sản phẩm:", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(60, 60, 60), AutoSize = true, Margin = new System.Windows.Forms.Padding(0, 7, 5, 0) };
                flowMain.Controls.Add(lblSearch);

                searchControl.Width = showAddRowButton ? 340 : 380;
                searchControl.Margin = new System.Windows.Forms.Padding(0, 2, 5, 0);
                flowMain.Controls.Add(searchControl);

                if (showAddRowButton)
                {
                    var btnAddSmall = new Button {
                        Text = "➕", FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White,
                        Size = new Size(32, 26), Margin = new System.Windows.Forms.Padding(0, 2, 15, 0),
                        Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
                    };
                    btnAddSmall.FlatAppearance.BorderSize = 0;
                    btnAddSmall.Click += (s, e) => BtnThem_Phase2_Click(null, null);
                    flowMain.Controls.Add(btnAddSmall);
                }
            }

            // --- ATTRIBUTES (Tiếp tục trên hàng ngang đó) ---
            foreach (var aKey in attrKeys)
            {
                string label = attrLabels.TryGetValue(aKey, out string lbl) ? lbl : aKey.ToUpper();

                var lblAttr = new Label
                {
                    Text = label + ":",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(60, 60, 60),
                    AutoSize = true,
                    Margin = new System.Windows.Forms.Padding(10, 7, 4, 0)
                };

                var txtAttr = new TextBox
                {
                    Name = "txt_" + aKey,
                    Font = new Font("Segoe UI", 9.5f),
                    Width = 90, // Cực kỳ gọn gàng
                    Margin = new System.Windows.Forms.Padding(0, 2, 10, 5),
                    BorderStyle = BorderStyle.FixedSingle,
                    ReadOnly = true,
                    BackColor = Color.FromArgb(245, 248, 255),
                    TabStop = false,
                    Enabled = false
                };
                txtAttr.KeyPress += (s, ev) => { ev.Handled = true; }; // Chặn đứng thao tác bàn phím
                txtAttr.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { BtnThem_Phase2_Click(null, null); e.Handled = true; } };

                _dynamicTextBoxes[aKey] = txtAttr;
                flowMain.Controls.Add(lblAttr);
                flowMain.Controls.Add(txtAttr);
            }

            pnl.Controls.Add(flowMain);

            // Nút Thêm / Tính toán (Đã ẩn nút Thêm dòng thủ công)
            var pnlActions = new FlowLayoutPanel {
                FlowDirection = FlowDirection.LeftToRight, 
                Width = 910, 
                Height = 55,
                BackColor = Color.Transparent,
                Margin = new System.Windows.Forms.Padding(10, 10, 0, 10),
                Padding = new System.Windows.Forms.Padding(0)
            };

            if (!string.IsNullOrEmpty(formula))
            {
                var btnCalc = new Button {
                    Text = "🧠 Tính toán",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    BackColor = Color.FromArgb(0, 100, 200), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, Size = new Size(110, 38), Cursor = Cursors.Hand,
                    Margin = new System.Windows.Forms.Padding(15, 0, 0, 0) };
                btnCalc.FlatAppearance.BorderSize = 0;
                btnCalc.Click += (s, e) => {
                    var dict = _dynamicTextBoxes.ToDictionary(k => k.Key, v => v.Value.Text);
                    var result = EvaluateAdvancedFormula(formula, _selectedProduct, dict);
                    var lRes = pnlActions.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "lblCalcResult");
                    if (lRes != null) { lRes.Text = result.HasValue ? $"Kết quả: {result.Value:N3}" : "Lỗi CT"; lRes.ForeColor = result.HasValue ? Color.DarkBlue : Color.Red; }
                };
                pnlActions.Controls.Add(btnCalc);
                pnlActions.Controls.Add(new Label { Name = "lblCalcResult", Text = "Kết quả: 0.000", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 80, 150), AutoSize = true, Margin = new System.Windows.Forms.Padding(20, 10, 0, 0) });
            }

            pnl.Controls.Add(pnlActions);
            return pnl;
        }

        /// <summary>
        /// Nạp giá trị thuộc tính của sản phẩm vào các TextBox đã tạo trong Phase 2.
        /// </summary>
        private void PopulateAttrPanel(Products p, List<string> attrKeys, Dictionary<string, string> attrLabels)
        {
            if (p == null) return;
            foreach (var aKey in attrKeys)
            {
                if (_dynamicTextBoxes.TryGetValue(aKey, out TextBox txt))
                {
                    string val = p.GetAttribute(aKey);
                    txt.Text = val;
                    txt.ReadOnly = true; // Luôn khóa thông số sản phẩm
                    txt.TabStop = false; // Chặn Tab
                    txt.Enabled = false; // Khóa cứng
                    txt.BackColor = Color.FromArgb(240, 240, 240); // Màu xám khóa
                }
            }
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
            // Trả về chuỗi rỗng để không hiện tiền tố lặp lại trong cột Tên hàng của Grid (theo yêu cầu clean UI)
            return "";
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

            int insIdx = GetInsertIndex();
            AddSelectedItemRow(finalName, 1, 0, "0", "0", null, false, insIdx);
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
            TreeNode activeTypeCMB = treeNode;
            while (activeTypeCMB != null)
            {
                if (activeTypeCMB.Tag is HierarchyNode t && string.Equals(t.Type, "TypeCMB", StringComparison.OrdinalIgnoreCase))
                    break;
                activeTypeCMB = activeTypeCMB.Parent;
            }

            if (activeTypeCMB != _currentActiveTypeCMB)
            {
                // Chỉ reset khi chuyển đổi giữa các nhóm tủ chính (TypeCMB) khác nhau.
                // Nếu _currentActiveTypeCMB đang null (mới mở form hoặc mới load nháp), không được xóa dữ liệu.
                if (_currentActiveTypeCMB != null)
                {
                    foreach (var kvp in _expandStateCache)
                    {
                        kvp.Value.ContentPanel?.Dispose();
                    }
                    _expandStateCache.Clear();

                    // Clear both the form cache AND the selected items grid to fully reset the previous form's state
                    dgvSelectedItems.Rows.Clear();
                    InitDefaultRows();
                }

                _currentActiveTypeCMB = activeTypeCMB;
            }

            if (treeNode?.Tag is HierarchyNode node && !string.IsNullOrEmpty(node.Config))
            {
                // Force clear cache for this node to ensure the new "Enabled = false" logic is applied
                if (_expandStateCache.ContainsKey(node))
                {
                    _expandStateCache[node].ContentPanel?.Dispose();
                    _expandStateCache.Remove(node);
                }

                // Node có cột Config có giá trị → hiện nút expand
                _expandedNode = node;
                _btnExpandToggle.Visible = true;
                
                if (_expandPanelVisible)
                {
                    ShowExpandPanel();
                }
                else
                {
                    _btnExpandToggle.Text = $"▶  Tìm & chọn sản phẩm  —  Config: {node.Config}";
                }
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
                
                // Không có textbox phụ nào, tự gọi thêm luôn
                BtnThem_Phase2_Click(null, null);
            }
            else
            {
                BtnThem_Phase2_Click(null, null);
            }
        }

        private void BtnThem_Phase2_Click(object sender, EventArgs e)
        {
            string prefix = GetNodePathPrefix();
            string configRaw = _expandedNode?.Config ?? "";
            string formula = _expandedNode?.Formula ?? "";
            int itemsAdded = 0;

            foreach (var row in _configRows)
            {
                Products p = row.SelectedProduct;
                if (p == null) continue;

                var noteItems = new List<string>();
                string finalName = string.IsNullOrEmpty(prefix) ? p.Name : $"{prefix}: {p.Name}";

                // Attr values
                var dictValues = new Dictionary<string, string>();
                foreach (var kvp in row.Attrs)
                {
                    string val = kvp.Value.Text.Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        noteItems.Add($"{kvp.Key}: {val}");
                        dictValues[kvp.Key] = val;
                    }
                }

                // Formula
                if (!string.IsNullOrEmpty(formula))
                {
                    decimal? kq = EvaluateAdvancedFormula(formula, p, dictValues);
                    if (kq.HasValue) noteItems.Add($"={formula} → {kq.Value:N2}");
                }

                int insIdx = GetInsertIndex();
                AddSelectedItemRow(finalName, 1, 0, "0", "0", p, false, insIdx);
                itemsAdded++;
            }

            if (itemsAdded > 0)
            {
                btnApply.Enabled = true;
                if (_lblProductInfo != null) { _lblProductInfo.Text = $"✔ Đã thêm {itemsAdded} dòng mới."; _lblProductInfo.ForeColor = Color.Green; }
                
                string onlyOne = _expandedNode?.OnlyOne?.Trim()?.ToLower() ?? "";
                if (onlyOne == "yes" || onlyOne == "có" || onlyOne == "true")
                    HideExpandPanel();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn sản phẩm ít nhất 1 dòng trước khi thêm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        /// <summary>
        /// Tính lại kích thước và vị trí các control theo tỷ lệ 70% TreeView / 30% DataGridView
        /// </summary>
        private void RecalculateLayout()
        {
            if (_modernTreeView == null) return;

            int paddingH  = 20;
            int paddingTop = pnlStepsContainer.Top;
            int formW      = this.ClientSize.Width;
            int formH      = this.ClientSize.Height;
            int controlW   = formW - paddingH * 2;

            // 1. Cố định panel nút dưới cùng
            pnlControls.Height = 50; 
            pnlControls.Width = formW;
            pnlControls.Location = new Point(0, formH - pnlControls.Height);

            // Căn lề nút ở góc dưới phải cho đồng nhất với paddingH mới
            btnReload.Location = new Point(formW - paddingH - btnReload.Width, btnReload.Location.Y);
            btnCancel.Location = new Point(btnReload.Left - btnCancel.Width - 10, btnCancel.Location.Y);
            btnApply.Location  = new Point(btnCancel.Left - btnApply.Width - 10, btnApply.Location.Y);
            
            int availableH = pnlControls.Top - paddingTop - 15;

            // Di chuyển tiêu đề
            lblTitle.Location = new Point(paddingH, 15);

            // Tính toán components ở giữa
            int expandToggleH  = (_btnExpandToggle != null && _btnExpandToggle.Visible) ? 38 : 0;
            int expandPanelH   = (_expandPanelVisible && _expandPanel != null) ? 170 : 0;
            int extraH         = expandToggleH + expandPanelH;

            // Phân bổ 55% Tree / 45% Grid (trên phần còn lại sau khi trừ Config Panel)
            int remainingH = availableH - extraH - 60; // 60px cho header grid và khoàng cách
            int treeH = (int)(remainingH * 0.55);
            int gridH = remainingH - treeH;

            if (treeH < 100) { treeH = 100; gridH = remainingH - treeH; }
            if (gridH < 80) gridH = 80;

            // Vẽ TreeView
            _modernTreeView.Location = new Point(paddingH, paddingTop);
            _modernTreeView.Size     = new Size(controlW, treeH);

            int currentY = paddingTop + treeH + 10;

            // Nút expand
            if (_btnExpandToggle != null)
            {
                _btnExpandToggle.Location = new Point(paddingH, currentY);
                _btnExpandToggle.Size     = new Size(controlW, 34);
                if (_btnExpandToggle.Visible) currentY += 38;
            }

            // Expand panel
            if (_expandPanel != null)
            {
                _expandPanel.Location = new Point(paddingH, currentY);
                _expandPanel.Size     = new Size(controlW, 170); // Reduced height by half
                if (_expandPanelVisible) currentY += 170;
            }

            // Đường kẻ & Tiêu đề Grid
            currentY += 10;
            lblDivider.Location = new Point(paddingH, currentY);
            lblDivider.Width    = controlW;

            int headerY = currentY + 10;
            lblGridTitle.Location  = new Point(paddingH, headerY + 5);
            btnAddToGrid.Location  = new Point(formW - paddingH - btnAddToGrid.Width, headerY);

            // DataGridView (Chiếm phần còn lại)
            int dgvY = headerY + 40;
            dgvSelectedItems.Location = new Point(paddingH, dgvY);
            dgvSelectedItems.Size     = new Size(controlW, Math.Max(60, pnlControls.Top - dgvY - 10));
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
            this.ItemHeight = 35; // Reduced height
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
                Rectangle expandRect = new Rectangle(levelIndent, hitTest.Node.Bounds.Y, 24, this.ItemHeight);
                // Cập nhật: Khi click vào CHÍNH NÓ hoặc CON CỦA NÓ, đều tìm ra node TypeCMB gốc để thu gọn các TypeCMB khác
                TreeNode activeTypeCMB = hitTest.Node;
                while (activeTypeCMB != null)
                {
                    if (activeTypeCMB.Tag is ECQ_Soft.Model.HierarchyNode tagNode && string.Equals(tagNode.Type, "TypeCMB", StringComparison.OrdinalIgnoreCase))
                        break;
                    activeTypeCMB = activeTypeCMB.Parent;
                }

                if (expandRect.Contains(e.Location) && hitTest.Node.Nodes.Count > 0)
                {
                    if (hitTest.Node.IsExpanded) hitTest.Node.Collapse();
                    else 
                    {
                        // Nếu mở rộng node này và node phụ thuộc một TypeCMB, thu gọn các TypeCMB khác
                        if (activeTypeCMB != null)
                        {
                            Action<TreeNodeCollection> collapseOtherTypeCMBs = null;
                            collapseOtherTypeCMBs = (nodes) =>
                            {
                                foreach (TreeNode n in nodes)
                                {
                                    if (n != activeTypeCMB && n.Tag is ECQ_Soft.Model.HierarchyNode tagNode && string.Equals(tagNode.Type, "TypeCMB", StringComparison.OrdinalIgnoreCase))
                                    {
                                        n.Collapse();
                                    }
                                    collapseOtherTypeCMBs(n.Nodes);
                                }
                            };
                            collapseOtherTypeCMBs(this.Nodes);
                        }
                        hitTest.Node.Expand();
                    }
                    return; // Đừng gọi base
                }
                else
                {
                    // Bỏ chọn nếu click lại chính node đang chọn
                    if (this.SelectedNode == hitTest.Node)
                    {
                        this.SelectedNode = null;
                        return; // Ngắt để không gọi base
                    }

                    this.SelectedNode = hitTest.Node;

                    // Nếu select một node thuộc nhánh TypeCMB, tự động thu gọn các TypeCMB khác
                    if (activeTypeCMB != null)
                    {
                        Action<TreeNodeCollection> collapseOtherTypeCMBs = null;
                        collapseOtherTypeCMBs = (nodes) =>
                        {
                            foreach (TreeNode n in nodes)
                            {
                                if (n != activeTypeCMB && n.Tag is ECQ_Soft.Model.HierarchyNode tagNode && string.Equals(tagNode.Type, "TypeCMB", StringComparison.OrdinalIgnoreCase))
                                {
                                    n.Collapse();
                                }
                                collapseOtherTypeCMBs(n.Nodes);
                            }
                        };
                        collapseOtherTypeCMBs(this.Nodes);
                        if (!activeTypeCMB.IsExpanded)
                        {
                            activeTypeCMB.Expand();
                        }
                    }
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

            bool isTypeCMB = e.Node.Tag is ECQ_Soft.Model.HierarchyNode nodeTag && string.Equals(nodeTag.Type, "TypeCMB", StringComparison.OrdinalIgnoreCase);

            // Xác định xem có phải là TypeCMB thuộc nhánh khác nhánh đang active hay không
            bool isInactiveTypeCMB = false;
            if (isTypeCMB)
            {
                TreeNode active = this.SelectedNode;
                while (active != null)
                {
                    if (active.Tag is ECQ_Soft.Model.HierarchyNode t && string.Equals(t.Type, "TypeCMB", StringComparison.OrdinalIgnoreCase))
                        break;
                    active = active.Parent;
                }
                if (active != null && e.Node != active)
                {
                    isInactiveTypeCMB = true;
                }
            }

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
            if (isInactiveTypeCMB && !isSelected) textColor = Color.FromArgb(180, 180, 180); // Màu mờ cho các form bị 'ẩn' / disable
            Font textFont = isSelected ? new Font(this.Font, FontStyle.Bold) : this.Font;
            
            using (var brush = new SolidBrush(textColor))
            {
                g.DrawString(e.Node.Text, textFont, brush, textX, e.Bounds.Y + (e.Bounds.Height - textFont.Height) / 2);
            }
        }
    }

    public class AdvancedConfigResultItem
    {
        public string TenCauHinh { get; set; }
        public string ThuocTinh { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public ECQ_Soft.Model.Products ReferenceProduct { get; set; }
    }
}
