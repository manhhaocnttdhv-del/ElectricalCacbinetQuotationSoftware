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
using System.Text.RegularExpressions;
using ECQ_Soft.Helpers;

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

        // --- Layout Resizing ---
        private double _treeRatio = 0.55;
        private bool _isDraggingSplitter = false;
        private int _lastMouseY = 0;
        private string _currentActivePath = ""; // Lưu path để tự động điều hướng khi load nháp

        public string SelectedHeader { get; private set; }
        public List<AdvancedConfigResultItem> SelectedAdvancedItems { get; private set; } = new List<AdvancedConfigResultItem>();
        public bool IsCanceled { get; private set; } = false;

        // Key = TreePath (VD: "TỦ ĐIỆN - TỦ PHÂN PHỐI - ...")
        // Value = List các cấu hình nháp (Tên Sản Phẩm, Số Lượng, Thuộc tính)
        private string _currentDraftName = string.Empty;
        private Dictionary<string, List<IList<object>>> _allDraftGroups = new Dictionary<string, List<IList<object>>>();
        private Dictionary<string, string> _attributeAliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private class RowData
        {
            public string ItemName;
            public string Model;
            public string SKU;
            public string XuatXu;
            public string DonVi;
            public int Quantity;
            public decimal UnitPrice;
            public string TotalPrice;
            public string GiaNhap;
            public string DanhMuc;
            public string Type;
            public string Hang;
            public string Progress;
            public object Tag; // Products object
            public string FormId;
            public string Attributes; // Ghi chú / Thuộc tính mở rộng (Height, Width, etc.)
        }
        private Dictionary<string, List<RowData>> _formProductsCache = new Dictionary<string, List<RowData>>();

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
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFormId", HeaderText = "FormId", Visible = false });
            dgvSelectedItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAttributes", HeaderText = "Attributes", Visible = false });
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
            dgvSelectedItems.Columns["colFormId"].Visible = false;
            dgvSelectedItems.Columns["colAttributes"].Visible = false;
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
                    AddSelectedItemRow(defaultItemName, 1, 0, "0", "0", null, true, null, "GLOBAL");
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
            int? insertIndex = null,
            string formId = "",
            string attributes = "")
        {
            // --- GLOBAL DUPLICATE GUARD ---
            if (!string.IsNullOrEmpty(formId) && formId != "GLOBAL")
            {
                var existingRow = dgvSelectedItems.Rows.Cast<DataGridViewRow>()
                    .FirstOrDefault(gr => (gr.Cells["colFormId"].Value?.ToString() ?? "") == formId &&
                                       (gr.Cells["colTen"].Value?.ToString() ?? "") == itemName);
                if (existingRow != null) return existingRow.Index;
            }

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
            
            string model = product?.Model ?? "";
            string sku = product?.SKU ?? "";
            string xuatXu = brand;
            string donVi = isDefaultRow || IsDefaultSelectedItemName(itemName) ? "Tủ" : "Cái";

            if (itemName.StartsWith("Vỏ tủ điện") || itemName.StartsWith("Vỏ tủ trong nhà"))
            {
                model = ""; sku = ""; xuatXu = "VNECCO"; donVi = "";
            }
            else if (itemName == "Hệ thống đồng thanh cái")
            {
                model = ""; sku = ""; xuatXu = "Việt nam"; donVi = "Hệ";
            }
            else if (itemName.StartsWith("Phụ kiện, Vật tư phụ"))
            {
                model = ""; sku = ""; xuatXu = "VN / CN"; donVi = "Lô";
            }
            else if (itemName.StartsWith("Nhân công lắp đặt"))
            {
                model = ""; sku = ""; xuatXu = "VNECCO"; donVi = "Cái";
            }

            int rowIndex;
            if (insertIndex.HasValue)
            {
                dgvSelectedItems.Rows.Insert(
                    insertIndex.Value,
                    "",
                    itemName,
                    model,
                    sku,
                    xuatXu,
                    donVi,
                    qtyText,
                    unitPriceText,
                    totalText,
                    FormatCurrencyVnd(parsedCost),
                    product?.Category ?? "",
                    product?.Type ?? "",
                    brand,
                    progressText,
                    formId ?? "",
                    attributes ?? "");
                rowIndex = insertIndex.Value;
            }
            else
            {
                rowIndex = dgvSelectedItems.Rows.Add(
                    "",
                    itemName,
                    model,
                    sku,
                    xuatXu,
                    donVi,
                    qtyText,
                    unitPriceText,
                    totalText,
                    FormatCurrencyVnd(parsedCost),
                    product?.Category ?? "",
                    product?.Type ?? "",
                    brand,
                    progressText,
                    formId ?? "",
                    attributes ?? "");
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
            int stt = 1;
            for (int i = 0; i < dgvSelectedItems.Rows.Count; i++)
            {
                if (dgvSelectedItems.Rows[i].Visible)
                {
                    dgvSelectedItems.Rows[i].Cells["colSTT"].Value = (stt++).ToString();
                }
                else
                {
                    dgvSelectedItems.Rows[i].Cells["colSTT"].Value = "";
                }
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
                            Id = int.TryParse(ReadCol(row, "id", 0), out int pid) ? pid : i,
                            Name = ReadCol(row, "name", 1),
                            Model = ReadCol(row, "model", 2),
                            SKU = ReadCol(row, "sku", 3),
                            Price = ReadCol(row, "price", 4),
                            PriceCost = ReadCol(row, "pricecost", 5),
                            Weight = ReadCol(row, "weight", 6),
                            Length = ReadCol(row, "length", 7),
                            Width = ReadCol(row, "width", 8),
                            Height = ReadCol(row, "height", 9),
                            Category = ReadCol(row, "category", 10),
                            Type = ReadCol(row, "type", 11),
                            HÃNG = ReadCol(row, "hãng", 12),
                            PriceList = ReadCol(row, "pricelist", 13)
                        };
                        // Nạp ExtraAttributes cho các cột ngoài cột chuẩn
                        var standardKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            { "id","name","model","sku","price","pricecost","weight","length","width","height","category","type","hãng","pricelist" };
                        for (int ci = 0; ci < _productColumnHeaders.Count && ci < row.Count; ci++)
                        {
                            string colKey = _productColumnHeaders[ci];
                            if (string.IsNullOrEmpty(colKey)) continue;

                            // Nếu là cột gộp "Các thuộc tính", ta bóc tách từng key:value
                            if (colKey == "các thuộc tính" || colKey == "cacthuoctinh" || colKey == "attributes")
                            {
                                string raw = row[ci]?.ToString() ?? "";
                                var pairs = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var pStr in pairs)
                                {
                                    var kv = pStr.Split(new[] { ':' }, 2);
                                    if (kv.Length == 2)
                                    {
                                        string k = kv[0].Trim().ToLower();
                                        prod.ExtraAttributes[k] = kv[1].Trim();
                                    }
                                }
                            }
                            else if (!standardKeys.Contains(colKey))
                            {
                                prod.ExtraAttributes[colKey] = row[ci]?.ToString() ?? "";
                            }
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

                _allDraftGroups = draftGroups; // Lưu trữ toàn cục dữ liệu thô

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

                    var dgvDrafts = new DataGridView
                    {
                        Location = new Point(20, 70),
                        Size = new Size(440, 200),
                        BackgroundColor = Color.White,
                        BorderStyle = BorderStyle.FixedSingle,
                        RowHeadersVisible = false,
                        AllowUserToAddRows = false,
                        AllowUserToDeleteRows = false,
                        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                        MultiSelect = false,
                        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                        Font = new Font("Segoe UI", 9.5f),
                        GridColor = Color.FromArgb(224, 224, 224),
                        EnableHeadersVisualStyles = false,
                        ColumnHeadersHeight = 32,
                        RowTemplate = { Height = 28 }
                    };

                    dgvDrafts.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Color.FromArgb(230, 240, 250),
                        ForeColor = Color.Black,
                        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                        Padding = new System.Windows.Forms.Padding(5, 0, 0, 0)
                    };

                    var colInfo = new DataGridViewTextBoxColumn 
                    { 
                        Name = "colInfo", 
                        HeaderText = "Tên cấu hình", 
                        ReadOnly = true,
                        FillWeight = 85
                    };
                    var colDel = new DataGridViewButtonColumn 
                    { 
                        Name = "colDel", 
                        HeaderText = "", 
                        Text = "X", 
                        UseColumnTextForButtonValue = true, 
                        Width = 40,
                        FillWeight = 15,
                        FlatStyle = FlatStyle.Standard
                    };
                    
                    dgvDrafts.Columns.Add(colInfo);
                    dgvDrafts.Columns.Add(colDel);

                    foreach (var d in draftGroups.Keys)
                    {
                        dgvDrafts.Rows.Add($"{d} ({draftGroups[d].Count} cấu hình)", "X");
                    }
                    modal.Controls.Add(dgvDrafts);
                    if (dgvDrafts.Rows.Count > 0) dgvDrafts.Rows[0].Selected = true;

                    var btnLoad = new Button { Text = "Tải cấu hình đã chọn", Location = new Point(20, 300), Size = new Size(180, 40), BackColor = Color.FromArgb(0, 150, 70), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
                    var btnSkip = new Button { Text = "Tạo Cấu hình Mới", Location = new Point(280, 300), Size = new Size(180, 40), BackColor = Color.LightGray, Font = new Font("Segoe UI", 9.5f) };
                    modal.Controls.Add(btnLoad);
                    modal.Controls.Add(btnSkip);

                    // Xử lý Xóa bản nháp
                    dgvDrafts.CellContentClick += async (s, e) =>
                    {
                        if (e.ColumnIndex == dgvDrafts.Columns["colDel"].Index && e.RowIndex >= 0)
                        {
                            string selectedItem = dgvDrafts.Rows[e.RowIndex].Cells["colInfo"].Value.ToString();
                            string draftKey = selectedItem.Substring(0, selectedItem.LastIndexOf('(')).Trim();

                            var dr = MessageBox.Show($"Bạn có muốn xóa cấu hình nháp '{draftKey}' khỏi Google Sheets không?", 
                                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            
                            if (dr == DialogResult.Yes)
                            {
                                bool deleted = await DeleteDraftFromSheetsAsync(draftKey);
                                if (deleted)
                                {
                                    dgvDrafts.Rows.RemoveAt(e.RowIndex);
                                    draftGroups.Remove(draftKey);
                                    if (draftGroups.Count == 0) { modal.DialogResult = DialogResult.Ignore; modal.Close(); }
                                }
                            }
                        }
                    };

                    // Logic tải cấu hình – dùng chung cho nút bấm và double-click
                    Action loadSelectedDraft = () =>
                    {
                        if (dgvDrafts.CurrentRow != null)
                        {
                            string selectedItem = dgvDrafts.CurrentRow.Cells["colInfo"].Value.ToString();
                            string draftKey = selectedItem.Substring(0, selectedItem.LastIndexOf('(')).Trim();
                            _currentDraftName = draftKey;
                            EnsureDefaultRowsPresent();

                            var selectedRows = draftGroups[draftKey];
                            string targetPath = "";
                            foreach (var r in selectedRows)
                            {
                                string viTri = r.Count > 0 ? r[0]?.ToString()?.Trim() : "";
                                if (!string.IsNullOrEmpty(viTri) && !viTri.Equals("Sản phẩm đã chọn", StringComparison.OrdinalIgnoreCase))
                                {
                                    targetPath = viTri;
                                    break;
                                }
                            }
                            _formProductsCache.Clear();
                            foreach (var r in selectedRows)
                            {
                                string viTri = r.Count > 0 ? r[0]?.ToString()?.Trim() : "";
                                string tenSP = r.Count > 1 ? r[1]?.ToString()?.Trim() : "";
                                string soLuongStr = r.Count > 6 ? r[6]?.ToString()?.Trim() : (r.Count > 2 ? r[2]?.ToString()?.Trim() : "1");
                                string thuocTinh = r.Count > 14 ? r[14]?.ToString()?.Trim() : (r.Count > 3 ? r[3]?.ToString()?.Trim() : "");

                                int.TryParse(soLuongStr, out int qty);
                                if (qty <= 0) qty = 1;

                                // Fix: N?u dng Vỏ tủ b? thi?u colFormId (viTri) do là default row lúc lưu, thì fallback lấy targetPath
                                if (string.IsNullOrEmpty(viTri) && !string.IsNullOrEmpty(tenSP) && (tenSP.StartsWith("Vỏ tủ") || ConfigProductItem.IsPinned(tenSP)))
                                {
                                    viTri = string.IsNullOrEmpty(targetPath) ? "GLOBAL" : targetPath;
                                }

                                if (!string.IsNullOrEmpty(viTri) && !string.IsNullOrEmpty(tenSP))
                                {
                                    if (!_formProductsCache.ContainsKey(viTri)) _formProductsCache[viTri] = new List<RowData>();

                                    // Hiển thị tên sản phẩm sạch (không có tiền tố) trong Grid
                                    string finalName = tenSP;
                                    // Chỉ xóa prefix nếu không phải là chuỗi chứa Kích thước
                                    if (finalName.Contains(": ") && !finalName.Contains("Kích thước :"))
                                        finalName = finalName.Substring(finalName.IndexOf(": ") + 2);

                                    var matchedProduct = _allProducts.FirstOrDefault(p =>
                                            string.Equals((p.Name ?? "").Trim(), finalName.Trim(), StringComparison.OrdinalIgnoreCase));

                                    _formProductsCache[viTri].Add(new RowData
                                    {
                                        ItemName = finalName,
                                        Quantity = qty,
                                        Progress = "0",
                                        TotalPrice = "0",
                                        Tag = matchedProduct,
                                        FormId = viTri,
                                        Attributes = thuocTinh
                                    });

                                    // Nếu formId trùng với currentActiveTypeCMB (hoặc chưa có active), nạp luôn vào Grid
                                    string currentPath = (_currentActiveTypeCMB != null) ? _currentActiveTypeCMB.FullPath : "";
                                    if (viTri == currentPath || string.IsNullOrEmpty(currentPath))
                                    {
                                        DataGridViewRow targetRow = null;
                                        foreach (DataGridViewRow existing in dgvSelectedItems.Rows)
                                        {
                                            string existingName = existing.Cells["colTen"].Value?.ToString() ?? "";
                                            bool isMatch = (existingName == finalName);
                                            
                                            // Nếu là các item cố định (Vỏ tủ, H? th?ng d?ng thanh ci,...)
                                            if (!isMatch && ConfigProductItem.IsPinned(existingName))
                                            {
                                                if (finalName.StartsWith(existingName)) isMatch = true;
                                                // So kh?p d?c bi?t cho Vỏ tủ v ban d?u n l "Vỏ tủ trong nh" ho?c "Vỏ tủ ngoi tr?i", 
                                                // nhưng dữ liệu từ Sheet là "Vỏ tủ di?n trong nh..."
                                                else if (existingName.StartsWith("Vỏ tủ") && finalName.StartsWith("Vỏ tủ")) isMatch = true;
                                            }

                                            if (isMatch)
                                            {
                                                targetRow = existing;
                                                break;
                                            }
                                        }

                                        if (targetRow != null)
                                        {
                                            // Cập nhật dòng đã có (đặc biệt là Vỏ tủ)
                                            targetRow.Cells["colTen"].Value = finalName;
                                            targetRow.Cells["colSoLuong"].Value = qty.ToString();
                                            targetRow.Cells["colAttributes"].Value = thuocTinh;
                                            targetRow.Tag = matchedProduct;
                                            RecalculateSelectedItemRow(targetRow);
                                            AdjustCabinetRowHeight(targetRow); // auto-resize & repaint
                                        }
                                        else
                                        {
                                            // Thêm dòng mới nếu chưa có
                                            int insIdx = GetInsertIndex();
                                            AddSelectedItemRow(finalName, qty, 0, "0", "0", matchedProduct, false, insIdx, viTri, thuocTinh);
                                            btnApply.Enabled = true;
                                        }
                                    }
                                }
                            }
                            // Lưu lại Path đầu tiên tìm thấy để Navigate sau khi LoadInitialLevel
                            _currentActivePath = targetPath;

                            RenumberGridSTT();
                            ScanAndFixCabinetRowHeights(); // đảm bảo hiển thị đúng màu sau khi load
                            modal.DialogResult = DialogResult.OK;
                            modal.Close();
                        }
                    };

                    // Nhấn nút "Tải cấu hình đã chọn" hoặc double-click vào tên cấu hình => tải luôn
                    btnLoad.Click += (s, e) => loadSelectedDraft();
                    dgvDrafts.CellDoubleClick += (s, e) => {
                        if (e.RowIndex >= 0 && e.ColumnIndex != dgvDrafts.Columns["colDel"].Index)
                            loadSelectedDraft();
                    };

                    btnSkip.Click += (s, e) => { modal.DialogResult = DialogResult.Ignore; modal.Close(); };

                    var dlgRes = modal.ShowDialog();
                    if (dlgRes == DialogResult.Cancel)
                    {
                        this.IsCanceled = true;
                        this.DialogResult = DialogResult.Cancel;
                        this.Close();
                        return;
                    }
                }

                LoadInitialLevel();

                // Tự động điều hướng đến Path của cấu hình vừa load
                if (!string.IsNullOrEmpty(_currentActivePath))
                {
                    NavigateToPath(_currentActivePath);
                }
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
                    node.Id = id;

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

            // BƯỚC 4: Nạp bản đồ biến (Cột O và P - Index 14 và 15)
            _attributeAliasMap.Clear();
            foreach (var row in dataRows)
            {
                if (row.Count > 15)
                {
                    string nghia = row[14]?.ToString()?.Trim()?.ToLower() ?? "";
                    string bien = row[15]?.ToString()?.Trim()?.ToLower() ?? "";
                    if (!string.IsNullOrEmpty(nghia) && !string.IsNullOrEmpty(bien) && nghia != "nghĩa")
                    {
                        _attributeAliasMap[nghia] = bien;
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
            btnApply.Click += async (s, e) =>
            {
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

                        SelectedAdvancedItems.Add(new AdvancedConfigResultItem
                        {
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
            dgvSelectedItems.CellClick += (s, e) =>
            {
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
                    SyncGridToDraftGroups(); // Cập nhật cache khi sửa dữ liệu
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
                // ── Tô màu mô tả vỏ tủ ──
                else if (e.RowIndex >= 0 && e.ColumnIndex == dgvSelectedItems.Columns["colTen"].Index)
                {
                    string cellVal = e.Value?.ToString() ?? "";
                    if (cellVal.StartsWith("Vỏ tủ điện"))
                    {
                        bool isSelected = e.State.HasFlag(DataGridViewElementStates.Selected);
                        Color bgColor = isSelected ? e.CellStyle.SelectionBackColor : e.CellStyle.BackColor;
                        e.Graphics.FillRectangle(new SolidBrush(bgColor), e.CellBounds);
                        e.Paint(e.CellBounds, DataGridViewPaintParts.Border | DataGridViewPaintParts.Focus);
                        DrawRichCabinetCell(e.Graphics, e.CellBounds, cellVal, e.CellStyle.Font, isSelected);
                        e.Handled = true;
                    }
                }
            };

            // Menustrip để hiển thị Modal tính toán khi chuột phải vào dòng mặc định
            var ctxMenu = new ContextMenuStrip();
            ctxMenu.Items.Add("Tính toán...", null, (s, ev) =>
            {
                if (dgvSelectedItems.SelectedRows.Count > 0)
                {
                    var row = dgvSelectedItems.SelectedRows[0];
                    string tenHang = row.Cells["colTen"].Value?.ToString() ?? "";

                    // Lấy danh sách thô từ bản nháp hiện tại để tính toán
                    List<IList<object>> rawData = null;
                    if (!string.IsNullOrEmpty(_currentDraftName) && _allDraftGroups.ContainsKey(_currentDraftName))
                    {
                        var allRows = _allDraftGroups[_currentDraftName];
                        if (allRows.Count > 4)
                        {
                            // Bỏ 1 dòng đầu (Skip 1) và bỏ 3 dòng cuối
                            rawData = allRows.Skip(1).Take(allRows.Count - 4).ToList();
                        }
                        else
                        {
                            rawData = new List<IList<object>>();
                        }
                    }
                    // Xử lý logic hiển thị Modal tính toán: Lấy tên Form từ đường dẫn (ví dụ lấy "Form 1" từ "...\Form 1\...")
                    string formName = "";
                    if (rawData != null && rawData.Count > 0 && rawData[0].Count > 0)
                    {
                        string fullPath = rawData[0][0]?.ToString() == "GLOBAL" ? rawData[1][0]?.ToString() : rawData[0][0]?.ToString();
                        string[] parts = fullPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                        // Giả định Form luôn nằm ở cấp thứ 3 (index 2) trong đường dẫn
                        string rawFormName = (parts.Length > 2) ? parts[2].Trim() : "";
                        // XỬ LÝ XÓA "3: ": 
                        // Dùng Regex để xóa số và dấu hai chấm ở đầu chuỗi (ví dụ: "3: Form 1" -> "Form 1")
                        formName = System.Text.RegularExpressions.Regex.Replace(rawFormName, @"^\d+:\s*", "");
                    }

                    HierarchyNode workflowNode = null;
                    if (!string.IsNullOrEmpty(formName))
                    {
                        foreach (var root in _rootNodes)
                        {
                            workflowNode = FindNodeRecursive(root, formName);
                            if (workflowNode != null) break;
                        }
                    }

                    if (tenHang.StartsWith("Vỏ tủ", StringComparison.OrdinalIgnoreCase))
                    {
                        CalculateAndApplyCabinetDimensions(row, tenHang, rawData, workflowNode);
                    }
                    else if (tenHang == "Hệ thống đồng thanh cái")
                    {
                        int count = rawData?.Count ?? 0;
                        MessageBox.Show($"Đang mở bảng tính toán cho ĐỒNG THANH CÁI\n(Tìm thấy {count} linh kiện trong bản nháp)");
                    }
                }
            });

            dgvSelectedItems.CellMouseUp += (s, e) =>
            {
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

            // --- Splitter Resizing Events ---
            lblDivider.Cursor = Cursors.SizeNS;
            lblDivider.BorderStyle = BorderStyle.None;
            lblDivider.Paint += (s, e) =>
            {
                // Vẽ một đường kẻ mỏng ở giữa vùng grab 10px
                using (Pen p = new Pen(Color.FromArgb(200, 200, 200), 1f))
                {
                    e.Graphics.DrawLine(p, 0, lblDivider.Height / 2, lblDivider.Width, lblDivider.Height / 2);
                }
            };

            lblDivider.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _isDraggingSplitter = true;
                    _lastMouseY = Cursor.Position.Y;
                }
            };
            lblDivider.MouseMove += (s, e) =>
            {
                if (_isDraggingSplitter)
                {
                    int currentMouseY = Cursor.Position.Y;
                    int deltaY = currentMouseY - _lastMouseY;
                    if (Math.Abs(deltaY) > 0)
                    {
                        // Tính toán lại tỷ lệ dựa trên thay đổi pixel
                        // remainingH được tính trong RecalculateLayout, ở đây ta ước lượng hoặc ép scale
                        // Cách tốt nhất là thay đổi treeH trực tiếp nếu ta biết remainingH, 
                        // nhưng vì RecalculateLayout dùng tỷ lệ nên ta biến đổi tỷ lệ.
                        
                        int availableH = pnlControls.Top - pnlStepsContainer.Top - 15;
                        int expandToggleH = (_btnExpandToggle != null && _btnExpandToggle.Visible) ? 38 : 0;
                        int expandPanelH = (_expandPanelVisible && _expandPanel != null) ? 170 : 0;
                        int remainingH = availableH - (expandToggleH + expandPanelH) - 60;

                        if (remainingH > 100)
                        {
                            double deltaRatio = (double)deltaY / remainingH;
                            _treeRatio += deltaRatio;

                            // Giới hạn tỷ lệ từ 10% đến 85%
                            if (_treeRatio < 0.1) _treeRatio = 0.1;
                            if (_treeRatio > 0.85) _treeRatio = 0.85;

                            _lastMouseY = currentMouseY;
                            RecalculateLayout();
                        }
                    }
                }
            };
            lblDivider.MouseUp += (s, e) => { _isDraggingSplitter = false; };
            // Đảm bảo dừng drag nếu chuột ra khỏi form hoặc mất focus
            this.MouseUp += (s, e) => { _isDraggingSplitter = false; };
        }

        private Dictionary<string, double> GetCalculationVariables(List<IList<object>> rawData, HierarchyNode workflowNode)
        {
            var varMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var idCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (rawData == null || workflowNode == null) return varMap;

            foreach (var draftRow in rawData)
            {
                string fullPath = draftRow.Count > 0 ? draftRow[0]?.ToString() ?? "" : "";
                string[] parts = fullPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                string rowCabinetConfigName = (parts.Length > 3) ? parts[3].Trim() : "";

                // Lấy số lượng của dòng này (Cột Index 6)
                double currentQty = 1;
                if (draftRow.Count > 6 && double.TryParse(draftRow[6]?.ToString(), out double q))
                    currentQty = q;

                foreach (var item in workflowNode.Children)
                {
                    if (rowCabinetConfigName.Contains(item.Name))
                    {
                        string id = item.Id;
                        if (!idCounters.ContainsKey(id)) idCounters[id] = 0;
                        idCounters[id]++;
                        string suffix = "_" + idCounters[id];

                        // 1. Lưu số lượng: sl101_1 (theo instance) và sl101 (tổng của ID 101)
                        string slAlias = _attributeAliasMap.ContainsKey("số lượng") ? _attributeAliasMap["số lượng"] : "sl";
                        varMap[slAlias + id + suffix] = currentQty;

                        string slIdOnly = slAlias + id;
                        if (!varMap.ContainsKey(slIdOnly)) varMap[slIdOnly] = 0;
                        varMap[slIdOnly] += currentQty;

                        // sl chung (nếu công thức chỉ ghi 'sl' - dùng cho trường hợp đơn giản hoặc tổng quát)
                        if (!varMap.ContainsKey("sl")) varMap["sl"] = 0;
                        varMap["sl"] += currentQty;

                        // 2. Lưu thuộc tính: w101_1, h101_1... và w101, h101...
                        string attrStr = draftRow.Count > 14 ? draftRow[14]?.ToString() ?? "" : "";
                        if (!string.IsNullOrEmpty(attrStr))
                        {
                            var pairs = attrStr.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var pair in pairs)
                            {
                                var kv = pair.Split(new[] { ':' }, 2);
                                if (kv.Length == 2)
                                {
                                    string key = kv[0].Trim().ToLower();
                                    string valStr = kv[1].Trim();
                                    if (double.TryParse(valStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
                                    {
                                        string alias = _attributeAliasMap.ContainsKey(key) ? _attributeAliasMap[key] : key;

                                        // Lưu bản có suffix (ví dụ: w101_1)
                                        varMap[alias + id + suffix] = val;

                                        // Lưu bản không suffix (ví dụ: w101) - Cộng dồn nếu cùng ID (ví dụ 2 thiết bị đặt cạnh nhau)
                                        string attrIdOnly = alias + id;
                                        if (!varMap.ContainsKey(attrIdOnly)) varMap[attrIdOnly] = 0;
                                        varMap[attrIdOnly] += val;

                                        // YÊU CẦU ĐẶC BIỆT: 'a' sẽ là 'ir'
                                        if (alias == "ir")
                                        {
                                            varMap["a" + id + suffix] = val;
                                            string aIdOnly = "a" + id;
                                            if (!varMap.ContainsKey(aIdOnly)) varMap[aIdOnly] = 0;
                                            varMap[aIdOnly] += val;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return varMap;
        }

        private void CalculateAndApplyCabinetDimensions(DataGridViewRow row, string tenHang, List<IList<object>> rawData, HierarchyNode workflowNode)
        {
            var varMap = GetCalculationVariables(rawData, workflowNode);
            string formulaHCase1 = "";
            string formulaWCase1 = "";
            string formulaHCase2 = "";
            string formulaWCase2 = "";
            string formulaDCase1 = "";
            string formulaDCase2 = "";

            if (workflowNode != null && !string.IsNullOrEmpty(workflowNode.Formula))
            {
                var cases = Regex.Split(workflowNode.Formula, @"Case\d+:")
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                string ExtractFormula(string text, string varName)
                {
                    // Hỗ trợ cả H, W, D và dừng lại khi gặp biến tiếp theo hoặc cuối chuỗi
                    var match = Regex.Match(text, $@"\b{varName}\s*=\s*(.*?)(?=\s*\b[HWD]\s*=|$)");
                    return match.Success ? match.Groups[1].Value.Trim() : "";
                }

                if (cases.Count >= 1)
                {
                    formulaWCase1 = ExtractFormula(cases[0], "W");
                    formulaHCase1 = ExtractFormula(cases[0], "H");
                    formulaDCase1 = ExtractFormula(cases[0], "D");
                }
                if (cases.Count >= 2)
                {
                    formulaWCase2 = ExtractFormula(cases[1], "W");
                    formulaHCase2 = ExtractFormula(cases[1], "H");
                    formulaDCase2 = ExtractFormula(cases[1], "D");
                }

                // 3. Thực hiện tính toán thử nghiệm và hiển thị kết quả
                try
                {
                    double finalW = 0, finalH = 0, finalD = 0;

                    if (!string.IsNullOrEmpty(formulaWCase1))
                    {
                        double w1 = CalculationEngine.Evaluate(formulaWCase1, varMap);
                        if (w1 < 900)
                        {
                            finalW = w1;
                            finalH = string.IsNullOrEmpty(formulaHCase1) ? 0 : CalculationEngine.Evaluate(formulaHCase1, varMap);
                            finalD = string.IsNullOrEmpty(formulaDCase1) ? 0 : CalculationEngine.Evaluate(formulaDCase1, varMap);
                        }
                        else if (cases.Count >= 2)
                        {
                            // Case 2: W >= 900
                            finalW = string.IsNullOrEmpty(formulaWCase2) ? w1 : CalculationEngine.Evaluate(formulaWCase2, varMap);
                            finalH = string.IsNullOrEmpty(formulaHCase2) ? (string.IsNullOrEmpty(formulaHCase1) ? 0 : CalculationEngine.Evaluate(formulaHCase1, varMap)) : CalculationEngine.Evaluate(formulaHCase2, varMap);
                            finalD = string.IsNullOrEmpty(formulaDCase2) ? (string.IsNullOrEmpty(formulaDCase1) ? 0 : CalculationEngine.Evaluate(formulaDCase1, varMap)) : CalculationEngine.Evaluate(formulaDCase2, varMap);
                        }
                        else
                        {
                            // Chỉ có 1 Case
                            finalW = w1;
                            finalH = string.IsNullOrEmpty(formulaHCase1) ? 0 : CalculationEngine.Evaluate(formulaHCase1, varMap);
                            finalD = string.IsNullOrEmpty(formulaDCase1) ? 0 : CalculationEngine.Evaluate(formulaDCase1, varMap);
                        }
                    }

                    // Hàm tính để làm tròn lên theo bước 50 (1599 -> 1600, 327 -> 350...)
                    int RoundUpTo(double value, int step = 50) => value <= 0 ? 0 : (int)(Math.Ceiling(value / step) * step);

                    int finalHmm = RoundUpTo(finalH);
                    int finalWmm = RoundUpTo(finalW);
                    int finalDmm = RoundUpTo(finalD);
                    string kichThuoc = $"H{finalHmm}xW{finalWmm}xD{finalDmm}mm";

                    // ── Hiển thị Modal hỏi thêm thông số vỏ tủ ──
                    string viTri = "trong nhà";
                    string lopCanh = "2 lớp cánh";
                    string doDay = "2";
                    string loaiSon = "sơn sần";
                    string mauSon = "RAL 7035";

                    string moLung = "không mở lưng";
                    string vatLieu = "tấm Panel";

                    // Thử đọc giá trị cũ từ tên hiện tại (nếu đã có)
                    string existingName = tenHang;
                    if (existingName.Contains("ngoài trời")) viTri = "ngoài trời";
                    if (existingName.Contains("1 lớp cánh")) lopCanh = "1 lớp cánh";
                    if (existingName.Contains("sơn bóng")) loaiSon = "sơn bóng";
                    if (existingName.Contains("mở lưng")) moLung = "mở lưng";
                    if (existingName.Contains("thanh gá")) vatLieu = "thanh gá";

                    using (var frmCabSpec = new Form
                    {
                        Text = "Thông số vỏ tủ điện",
                        Size = new Size(480, 410),
                        StartPosition = FormStartPosition.CenterParent,
                        ShowIcon = false,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        MaximizeBox = false,
                        MinimizeBox = false,
                        BackColor = Color.White
                    })
                    {
                        int labelX = 20, controlX = 200, rowH = 44, startY = 20;
                        Font fntLabel = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                        Font fntCtrl  = new Font("Segoe UI", 9.5f);

                        // ── Header kết quả kích thước ──
                        var lblDimResult = new Label
                        {
                            Text = $"✅ Kích thước tính được: {kichThuoc}",
                            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                            ForeColor = Color.FromArgb(0, 120, 60),
                            Location = new Point(labelX, startY),
                            AutoSize = true
                        };
                        frmCabSpec.Controls.Add(lblDimResult);
                        int y = startY + 36;

                        // ── 1. Trong nhà / Ngoài trời ──
                        var lblViTri = new Label { Text = "Môi trường lắp đặt:", Font = fntLabel, Location = new Point(labelX, y + 4), AutoSize = true };
                        var cmbViTri = new ComboBox
                        {
                            Font = fntCtrl,
                            Location = new Point(controlX, y),
                            Size = new Size(230, 28),
                            DropDownStyle = ComboBoxStyle.DropDownList
                        };
                        cmbViTri.Items.AddRange(new[] { "trong nhà", "ngoài trời" });
                        cmbViTri.SelectedItem = viTri;
                        if (cmbViTri.SelectedIndex < 0) cmbViTri.SelectedIndex = 0;
                        frmCabSpec.Controls.Add(lblViTri);
                        frmCabSpec.Controls.Add(cmbViTri);
                        y += rowH;

                        // ── 2. Số lớp cánh ──
                        var lblLopCanh = new Label { Text = "Số lớp cánh:", Font = fntLabel, Location = new Point(labelX, y + 4), AutoSize = true };
                        var cmbLopCanh = new ComboBox
                        {
                            Font = fntCtrl,
                            Location = new Point(controlX, y),
                            Size = new Size(230, 28),
                            DropDownStyle = ComboBoxStyle.DropDownList
                        };
                        cmbLopCanh.Items.AddRange(new[] { "1 lớp cánh", "2 lớp cánh" });
                        cmbLopCanh.SelectedItem = lopCanh;
                        if (cmbLopCanh.SelectedIndex < 0) cmbLopCanh.SelectedIndex = 1;
                        frmCabSpec.Controls.Add(lblLopCanh);
                        frmCabSpec.Controls.Add(cmbLopCanh);
                        y += rowH;

                        // ── 3. Độ dày tôn (mm) ──
                        var lblDoDay = new Label { Text = "Độ dày tôn (mm):", Font = fntLabel, Location = new Point(labelX, y + 4), AutoSize = true };
                        var cmbDoDay = new ComboBox
                        {
                            Font = fntCtrl,
                            Location = new Point(controlX, y),
                            Size = new Size(120, 28),
                            DropDownStyle = ComboBoxStyle.DropDownList
                        };
                        cmbDoDay.Items.AddRange(new[] { "1", "1.2", "1.5", "2", "2.5", "3" });
                        cmbDoDay.SelectedItem = doDay;
                        if (cmbDoDay.SelectedIndex < 0) cmbDoDay.SelectedIndex = 3; // mặc định 2mm
                        frmCabSpec.Controls.Add(lblDoDay);
                        frmCabSpec.Controls.Add(cmbDoDay);
                        y += rowH;

                        // ── 4. Loại sơn ──
                        var lblLoaiSon = new Label { Text = "Loại sơn:", Font = fntLabel, Location = new Point(labelX, y + 4), AutoSize = true };
                        var cmbLoaiSon = new ComboBox
                        {
                            Font = fntCtrl,
                            Location = new Point(controlX, y),
                            Size = new Size(230, 28),
                            DropDownStyle = ComboBoxStyle.DropDownList
                        };
                        cmbLoaiSon.Items.AddRange(new[] { "sơn sần", "sơn bóng" });
                        cmbLoaiSon.SelectedItem = loaiSon;
                        if (cmbLoaiSon.SelectedIndex < 0) cmbLoaiSon.SelectedIndex = 0;
                        frmCabSpec.Controls.Add(lblLoaiSon);
                        frmCabSpec.Controls.Add(cmbLoaiSon);
                        y += rowH;

                        // ── 5. Màu sơn ──
                        var lblMauSon = new Label { Text = "Màu sơn:", Font = fntLabel, Location = new Point(labelX, y + 4), AutoSize = true };
                        var cmbMauSon = new ComboBox
                        {
                            Font = fntCtrl,
                            Location = new Point(controlX, y),
                            Size = new Size(230, 28),
                            DropDownStyle = ComboBoxStyle.DropDownList
                        };
                        cmbMauSon.Items.AddRange(new[]
                        {
                            "RAL 7035 (ghi sáng)", "RAL 7032 (ghi đậm)",
                            "Trắng", "Đen", "Xám", "Đỏ", "Vàng", "Xanh dương", "Xanh lá"
                        });
                        // Thử match màu cũ
                        bool matchedColor = false;
                        foreach (var item in cmbMauSon.Items) { if (item.ToString().StartsWith(mauSon, StringComparison.OrdinalIgnoreCase)) { cmbMauSon.SelectedItem = item; matchedColor = true; break; } }
                        if (!matchedColor) cmbMauSon.SelectedIndex = 0;
                        cmbMauSon.DropDownStyle = ComboBoxStyle.DropDown; // cho phép nhập tự do
                        frmCabSpec.Controls.Add(lblMauSon);
                        frmCabSpec.Controls.Add(cmbMauSon);
                        y += rowH;

                        // ── 6. Mở lưng / Không mở lưng ──
                        var lblMoLung = new Label { Text = "Mở lưng tủ:", Font = fntLabel, Location = new Point(labelX, y + 4), AutoSize = true };
                        var cmbMoLung = new ComboBox
                        {
                            Font = fntCtrl,
                            Location = new Point(controlX, y),
                            Size = new Size(230, 28),
                            DropDownStyle = ComboBoxStyle.DropDownList
                        };
                        cmbMoLung.Items.AddRange(new[] { "không mở lưng", "mở lưng" });
                        cmbMoLung.SelectedItem = moLung;
                        if (cmbMoLung.SelectedIndex < 0) cmbMoLung.SelectedIndex = 0;
                        frmCabSpec.Controls.Add(lblMoLung);
                        frmCabSpec.Controls.Add(cmbMoLung);
                        y += rowH;

                        // ── 7. Tấm Panel / Thanh gá (chỉ hiển thị khi mở lưng) ──
                        var lblVatLieu = new Label { Text = "Vật liệu lưng tủ:", Font = fntLabel, Location = new Point(labelX, y + 4), AutoSize = true };
                        var cmbVatLieu = new ComboBox
                        {
                            Font = fntCtrl,
                            Location = new Point(controlX, y),
                            Size = new Size(230, 28),
                            DropDownStyle = ComboBoxStyle.DropDownList
                        };
                        cmbVatLieu.Items.AddRange(new[] { "tấm Panel", "thanh gá" });
                        cmbVatLieu.SelectedItem = vatLieu;
                        if (cmbVatLieu.SelectedIndex < 0) cmbVatLieu.SelectedIndex = 0;
                        // Hiển thị/ẩn theo lựa chọn mở lưng
                        lblVatLieu.Enabled = cmbVatLieu.Enabled = (moLung == "mở lưng");
                        lblVatLieu.ForeColor = lblVatLieu.Enabled ? Color.Black : Color.Silver;
                        cmbMoLung.SelectedIndexChanged += (sv, ev) =>
                        {
                            bool isMoLung = cmbMoLung.SelectedItem?.ToString() == "mở lưng";
                            lblVatLieu.Enabled = cmbVatLieu.Enabled = isMoLung;
                            lblVatLieu.ForeColor = isMoLung ? Color.Black : Color.Silver;
                        };
                        frmCabSpec.Controls.Add(lblVatLieu);
                        frmCabSpec.Controls.Add(cmbVatLieu);
                        y += rowH + 8;

                        // ── Nút OK / Hủy ──
                        var btnOk = new Button
                        {
                            Text = "✔ Xác nhận",
                            Size = new Size(140, 36),
                            Location = new Point(controlX, y),
                            BackColor = Color.FromArgb(0, 150, 70),
                            ForeColor = Color.White,
                            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                            FlatStyle = FlatStyle.Flat,
                            DialogResult = DialogResult.OK
                        };
                        var btnCancel = new Button
                        {
                            Text = "✖ Hủy",
                            Size = new Size(80, 36),
                            Location = new Point(controlX + 150, y),
                            BackColor = Color.FromArgb(200, 60, 50),
                            ForeColor = Color.White,
                            Font = new Font("Segoe UI", 9f),
                            FlatStyle = FlatStyle.Flat,
                            DialogResult = DialogResult.Cancel
                        };
                        frmCabSpec.Controls.Add(btnOk);
                        frmCabSpec.Controls.Add(btnCancel);
                        frmCabSpec.AcceptButton = btnOk;
                        frmCabSpec.CancelButton = btnCancel;
                        frmCabSpec.ClientSize = new Size(450, y + 56);

                        if (frmCabSpec.ShowDialog(this) == DialogResult.OK)
                        {
                            string selViTri   = cmbViTri.SelectedItem?.ToString()   ?? viTri;
                            string selLopCanh = cmbLopCanh.SelectedItem?.ToString() ?? lopCanh;
                            string selDoDay   = cmbDoDay.SelectedItem?.ToString()   ?? doDay;
                            string selLoaiSon = cmbLoaiSon.SelectedItem?.ToString() ?? loaiSon;
                            string selMauSon  = cmbMauSon.Text.Trim();
                            if (string.IsNullOrEmpty(selMauSon)) selMauSon = "RAL 7035 (ghi sáng)";

                            string selMoLung  = cmbMoLung.SelectedItem?.ToString()  ?? moLung;
                            string selVatLieu = cmbVatLieu.SelectedItem?.ToString() ?? vatLieu;

                            // ── Build chuỗi mô tả hoàn chỉnh ──
                            var lines = new System.Text.StringBuilder();
                            lines.AppendLine($"Vỏ tủ điện {selViTri} loại {selLopCanh}:");
                            lines.AppendLine($"- Kích thước {kichThuoc}");
                            lines.AppendLine($"- Tôn dày {selDoDay}mm");
                            lines.AppendLine($"- Sơn tĩnh điện, {selLoaiSon}");
                            lines.Append($"- Sơn màu {selMauSon}");
                            if (selMoLung == "mở lưng")
                            {
                                lines.AppendLine();
                                lines.Append($"- Mở lưng, dùng {selVatLieu}");
                            }

                            row.Cells["colTen"].Value = lines.ToString().TrimEnd();
                            AdjustCabinetRowHeight(row); // auto-resize & repaint
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi tính toán công thức: " + ex.Message, "Lỗi tính toán", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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

        /// <summary>
        /// Tự động điều chỉnh chiều cao row theo số dòng text mô tả vỏ tủ và invalidate để vẽ lại màu.
        /// </summary>
        private void AdjustCabinetRowHeight(DataGridViewRow row)
        {
            if (row == null) return;
            string val = row.Cells["colTen"].Value?.ToString() ?? "";
            if (!val.StartsWith("Vỏ tủ điện")) return;

            int lineCount = val.Split('\n').Length;
            int baseFont  = dgvSelectedItems.Font?.Height ?? 15;
            int newHeight = lineCount * (baseFont + 3) + 10;
            if (newHeight < 28) newHeight = 28;
            if (row.Height != newHeight) row.Height = newHeight;
            dgvSelectedItems.InvalidateRow(row.Index);
        }

        /// <summary>
        /// Quét toàn bộ grid, điều chỉnh chiều cao mọi row chứa mô tả vỏ tủ.
        /// </summary>
        private void ScanAndFixCabinetRowHeights()
        {
            foreach (DataGridViewRow row in dgvSelectedItems.Rows)
            {
                if (!row.IsNewRow) AdjustCabinetRowHeight(row);
            }
            dgvSelectedItems.Invalidate();
        }

        private void DrawRichCabinetCell(Graphics g, Rectangle bounds, string text, Font baseFont, bool isSelected)
        {
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color colNormal    = isSelected ? Color.White : Color.FromArgb(30, 30, 30);
            Color colHighlight = Color.Red; // đỏ tươi

            Font fntBold   = new Font(baseFont ?? new Font("Segoe UI", 9f), FontStyle.Bold);
            Font fntNormal = baseFont ?? new Font("Segoe UI", 9f);

            // Các từ khóa cần tô đỏ
            var patterns = new[]
            {
                "trong nhà", "ngoài trời",
                @"\d+\s*lớp cánh",
                @"H\d+xW\d+xD\d+mm",
                @"\d+(?:\.\d+)?mm",
                "sơn sần", "sơn bóng",
                @"RAL\s*\d+[^\s,\n]*",
                @"có tô màu\s+\S+",
            };

            string[] lines = text.Split('\n');
            int lineH  = fntNormal.Height + 3;
            int topPad = Math.Max(3, (bounds.Height - lines.Length * lineH) / 2);
            int curY   = bounds.Top + topPad;

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd('\r');
                int curX = bounds.Left + 4;

                // Thu thập tất cả vị trí keyword
                var allMatches = new List<(int start, int len)>();
                foreach (var pat in patterns)
                {
                    var rx = new System.Text.RegularExpressions.Regex(
                        pat, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    foreach (System.Text.RegularExpressions.Match m in rx.Matches(line))
                        allMatches.Add((m.Index, m.Length));
                }
                allMatches.Sort((a, b) => a.start != b.start ? a.start.CompareTo(b.start) : b.len.CompareTo(a.len));

                // Loại overlap
                var clean = new List<(int start, int len)>();
                int covered = 0;
                foreach (var m in allMatches)
                    if (m.start >= covered) { clean.Add(m); covered = m.start + m.len; }

                // Vẽ từng đoạn
                int p = 0;
                foreach (var m in clean)
                {
                    if (m.start > p)
                    {
                        string normal = line.Substring(p, m.start - p);
                        using (var br = new SolidBrush(colNormal))
                            g.DrawString(normal, fntNormal, br, curX, curY);
                        curX += (int)g.MeasureString(normal, fntNormal).Width - 2;
                    }
                    string hi = line.Substring(m.start, m.len);
                    using (var br = new SolidBrush(colHighlight))
                        g.DrawString(hi, fntBold, br, curX, curY);
                    curX += (int)g.MeasureString(hi, fntBold).Width - 2;
                    p = m.start + m.len;
                }
                if (p < line.Length)
                {
                    string tail = line.Substring(p);
                    using (var br = new SolidBrush(colNormal))
                        g.DrawString(tail, fntNormal, br, curX, curY);
                }
                curY += lineH;
            }
            fntBold.Dispose();
        }

        private HierarchyNode FindNodeRecursive(HierarchyNode parent, string search)
        {
            if (string.IsNullOrEmpty(search)) return null;

            // Kiểm tra khớp ID hoặc khớp Tên
            if (string.Equals(parent.Id?.Trim(), search.Trim(), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(parent.Name?.Trim(), search.Trim(), StringComparison.OrdinalIgnoreCase))
                return parent;

            // Kiểm tra trường hợp search là "ID: Name"
            if (search.Contains(": "))
            {
                var parts = search.Split(new[] { ": " }, 2, StringSplitOptions.None);
                string sid = parts[0].Trim();
                string sname = parts[1].Trim();
                if (string.Equals(parent.Id?.Trim(), sid, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(parent.Name?.Trim(), sname, StringComparison.OrdinalIgnoreCase))
                    return parent;
            }

            foreach (var child in parent.Children)
            {
                var found = FindNodeRecursive(child, search);
                if (found != null) return found;
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
                        if (kvAttr.Key == "_internal_qty_") continue; // Không lưu số lượng nội bộ vào chuỗi thuộc tính
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

            foreach (var kv in _formProductsCache)
            {
                string path = kv.Key;
                foreach (var dItem in kv.Value)
                    draftItems.Add(new Tuple<string, string, string, string>(path, dItem.ItemName, dItem.Quantity.ToString(), dItem.Attributes ?? ""));
            }

            string fallbackPath = SelectedHeader;
            if (string.IsNullOrWhiteSpace(fallbackPath))
                fallbackPath = (_modernTreeView?.SelectedNode?.Text ?? "Sản phẩm đã chọn").Trim();

            foreach (DataGridViewRow row in dgvSelectedItems.Rows)
            {
                if (row.IsNewRow) continue;
                string tenHang = row.Cells["colTen"].Value?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(tenHang)) continue;
                string itemFormId = row.Cells["colFormId"].Value?.ToString() ?? "";
                if (itemFormId == "GLOBAL") continue;

                // Tránh trùng lặp nếu đã lấy từ _expandStateCache
                if (draftItems.Any(x => x.Item2 == tenHang)) continue;

                string itemPath = row.Cells["colFormId"].Value?.ToString() ?? fallbackPath;

                string ghiChu = row.Cells["colAttributes"].Value?.ToString() ?? "";

                draftItems.Add(new Tuple<string, string, string, string>(itemPath, tenHang, row.Cells["colSoLuong"].Value?.ToString() ?? "1", ghiChu));
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

                btnSave.Click += async (s1, e1) =>
                {
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
                if (tSheet == null)
                {
                    var addReq = new Google.Apis.Sheets.v4.Data.Request { AddSheet = new Google.Apis.Sheets.v4.Data.AddSheetRequest { Properties = new Google.Apis.Sheets.v4.Data.SheetProperties { Title = "Cấu hình nháp" } } };
                    var resp = await _service.Spreadsheets.BatchUpdate(new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest { Requests = new List<Google.Apis.Sheets.v4.Data.Request> { addReq } }, _spreadsheetId).ExecuteAsync();
                    shId = resp.Replies[0].AddSheet.Properties.SheetId ?? 0;
                }
                else shId = tSheet.Properties.SheetId ?? 0;

                var hCheck = await _service.Spreadsheets.Values.Get(_spreadsheetId, "Cấu hình nháp!A1:A1").ExecuteAsync();
                bool hasHeader = hCheck.Values != null && hCheck.Values.Count > 0 && hCheck.Values[0][0]?.ToString() == "Vị trí cấu hình";

                // Xóa cũ if exists
                await DeleteDraftFromSheetsAsync(draftName);

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

                foreach (DataGridViewRow row in dgvSelectedItems.Rows)
                {
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

                // Format nền + màu chữ keyword
                var match = System.Text.RegularExpressions.Regex.Match(appResp.Updates.UpdatedRange ?? "", @"[A-Za-z]+(\d+)");
                if (match.Success)
                {
                    int startI = int.Parse(match.Groups[1].Value) - 1;
                    int gI = hasHeader ? startI : startI + 1;
                    var cReqs = new List<Google.Apis.Sheets.v4.Data.Request>();

                    // ── Row header (nếu chưa có) ──
                    if (!hasHeader)
                        cReqs.Add(new Google.Apis.Sheets.v4.Data.Request
                        {
                            RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                            {
                                Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = shId, StartRowIndex = startI, EndRowIndex = startI + 1, StartColumnIndex = 0, EndColumnIndex = 15 },
                                Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 1f, Green = 0.9f, Blue = 0f }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true }, HorizontalAlignment = "CENTER" } },
                                Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment)"
                            }
                        });

                    // ── Row tên nháp (màu nền xanh lá) ──
                    cReqs.Add(new Google.Apis.Sheets.v4.Data.Request
                    {
                        RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                        {
                            Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = shId, StartRowIndex = gI, EndRowIndex = gI + 1, StartColumnIndex = 0, EndColumnIndex = 15 },
                            Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0.2f, Green = 0.8f, Blue = 0.2f }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true } } },
                            Fields = "userEnteredFormat(backgroundColor,textFormat)"
                        }
                    });

                    int dataRowStart = gI + 1;
                    int rowOffset = 0;
                    foreach (DataGridViewRow dgvRow in dgvSelectedItems.Rows)
                    {
                        if (dgvRow.IsNewRow) continue;
                        string cellText = dgvRow.Cells["colTen"].Value?.ToString() ?? "";
                        if (string.IsNullOrEmpty(cellText)) continue;

                        // Kiểm tra nếu row có chữ màu đỏ (các dòng mặc định như Phụ kiện, Nhân công...)
                        // NHƯNG bỏ qua dòng Vỏ tủ điện vì Vỏ tủ điện cần tô màu từng chữ (Rich Text)
                        if ((dgvRow.DefaultCellStyle.ForeColor == Color.Red || dgvRow.Cells["colTen"].Style.ForeColor == Color.Red) && !cellText.StartsWith("Vỏ tủ điện"))
                        {
                            cReqs.Add(new Google.Apis.Sheets.v4.Data.Request
                            {
                                RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                                {
                                    Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = shId, StartRowIndex = dataRowStart + rowOffset, EndRowIndex = dataRowStart + rowOffset + 1, StartColumnIndex = 0, EndColumnIndex = 15 },
                                    Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { ForegroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0.85f, Green = 0f, Blue = 0f } } } },
                                    Fields = "userEnteredFormat(textFormat.foregroundColor)"
                                }
                            });
                        }
                        else
                        {
                            // Nếu là dòng bình thường (chữ đen), áp dụng highlight đỏ cho các keyword ở cột B (như Vỏ tủ)
                            var richReqs = BuildRichTextUpdateRequests(shId, dataRowStart + rowOffset, 1, cellText);
                            cReqs.AddRange(richReqs);
                        }
                        
                        rowOffset++;
                    }

                    await _service.Spreadsheets.BatchUpdate(new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest { Requests = cReqs }, _spreadsheetId).ExecuteAsync();
                }

                _expandStateCache.Clear(); _formProductsCache.Clear(); _currentDraftName = draftName;
                SyncGridToDraftGroups(); // Cập nhật lại cache sau khi lưu thành công
                return true;
            }
            catch (Exception ex) { MessageBox.Show("Lỗi lưu nháp: " + ex.Message); return false; }
        }

        /// <summary>
        /// Tạo UpdateCells request để tô màu đỏ các keyword (trong nhà, ngoài trời, kích thước, độ dày, màu RAL, loại sơn)
        /// bên trong một ô text trên Google Sheets — tương tự hàm DrawRichCabinetCell trong WinForms.
        /// </summary>
        private List<Google.Apis.Sheets.v4.Data.Request> BuildRichTextUpdateRequests(
            int sheetId, int rowIndex, int colIndex, string text)
        {
            var result = new List<Google.Apis.Sheets.v4.Data.Request>();
            if (string.IsNullOrEmpty(text)) return result;

            // Google Sheets API tự động chuẩn hóa \r\n thành \n.
            // Phải xử lý trước khi tính StartIndex để tránh lỗi 400 Out Of Bounds!
            text = text.Replace("\r\n", "\n");

            // Các pattern keyword cần tô đỏ (giống DrawRichCabinetCell)
            var patterns = new[]
            {
                "trong nhà", "ngoài trời",
                @"\d+\s*lớp cánh",
                @"H\d+xW\d+xD\d+mm",
                @"\d+(?:\.\d+)?mm",
                "sơn sần", "sơn bóng",
                @"RAL\s*\d+[^\s,\n]*",
                @"có tô màu\s+\S+",
            };

            // Màu đỏ cho keyword
            var redColor  = new Google.Apis.Sheets.v4.Data.Color { Red = 0.85f, Green = 0f,    Blue = 0f    };
            var blackColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0.12f, Green = 0.12f, Blue = 0.12f };

            // Thu thập tất cả vị trí match trên toàn bộ text (tính theo char index)
            var allMatches = new List<(int start, int len)>();
            foreach (var pat in patterns)
            {
                var rx = new System.Text.RegularExpressions.Regex(pat,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                foreach (System.Text.RegularExpressions.Match m in rx.Matches(text))
                    allMatches.Add((m.Index, m.Length));
            }
            allMatches.Sort((a, b) => a.start != b.start ? a.start.CompareTo(b.start) : b.len.CompareTo(a.len));

            // Loại bỏ overlap
            var clean = new List<(int start, int len)>();
            int covered = 0;
            foreach (var m in allMatches)
                if (m.start >= covered) { clean.Add(m); covered = m.start + m.len; }

            // Nếu không có keyword nào thì không cần tạo request
            if (clean.Count == 0) return result;

            // Xây dựng danh sách TextFormatRun
            var runs = new List<Google.Apis.Sheets.v4.Data.TextFormatRun>();
            int p = 0;
            foreach (var m in clean)
            {
                if (m.start > p)
                    runs.Add(new Google.Apis.Sheets.v4.Data.TextFormatRun
                    {
                        StartIndex = p,
                        Format = new Google.Apis.Sheets.v4.Data.TextFormat { ForegroundColorStyle = new Google.Apis.Sheets.v4.Data.ColorStyle { RgbColor = blackColor }, Bold = false }
                    });

                runs.Add(new Google.Apis.Sheets.v4.Data.TextFormatRun
                {
                    StartIndex = m.start,
                    Format = new Google.Apis.Sheets.v4.Data.TextFormat { ForegroundColorStyle = new Google.Apis.Sheets.v4.Data.ColorStyle { RgbColor = redColor }, Bold = true }
                });
                p = m.start + m.len;
            }
            if (p < text.Length)
                runs.Add(new Google.Apis.Sheets.v4.Data.TextFormatRun
                {
                    StartIndex = p,
                    Format = new Google.Apis.Sheets.v4.Data.TextFormat { ForegroundColorStyle = new Google.Apis.Sheets.v4.Data.ColorStyle { RgbColor = blackColor }, Bold = false }
                });

            result.Add(new Google.Apis.Sheets.v4.Data.Request
            {
                UpdateCells = new Google.Apis.Sheets.v4.Data.UpdateCellsRequest
                {
                    Range = new Google.Apis.Sheets.v4.Data.GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = rowIndex, EndRowIndex = rowIndex + 1,
                        StartColumnIndex = colIndex, EndColumnIndex = colIndex + 1
                    },
                    Rows = new List<Google.Apis.Sheets.v4.Data.RowData>
                    {
                        new Google.Apis.Sheets.v4.Data.RowData
                        {
                            Values = new List<Google.Apis.Sheets.v4.Data.CellData>
                            {
                                new Google.Apis.Sheets.v4.Data.CellData
                                {
                                    UserEnteredValue = new Google.Apis.Sheets.v4.Data.ExtendedValue { StringValue = text },
                                    TextFormatRuns = runs,
                                    UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { WrapStrategy = "WRAP" }
                                }
                            }
                        }
                    },
                    Fields = "userEnteredValue,textFormatRuns,userEnteredFormat.wrapStrategy"
                }
            });

            return result;
        }

        private async Task<bool> DeleteDraftFromSheetsAsync(string draftName)
        {
            try
            {
                var spreadsheetInfo = await _service.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
                var tSheet = spreadsheetInfo.Sheets.FirstOrDefault(s => s.Properties.Title == "Cấu hình nháp");
                if (tSheet == null) return true;
                int shId = tSheet.Properties.SheetId ?? 0;

                var checkD = await _service.Spreadsheets.Values.Get(_spreadsheetId, "Cấu hình nháp!A:B").ExecuteAsync();
                if (checkD.Values != null)
                {
                    int sR = -1, eR = -1;
                    for (int i = 0; i < checkD.Values.Count; i++)
                    {
                        var row = checkD.Values[i];
                        string a = row.Count > 0 ? row[0]?.ToString()?.Trim() : "";
                        string b = row.Count > 1 ? row[1]?.ToString()?.Trim() : "";
                        if (sR == -1 && a == draftName && string.IsNullOrEmpty(b)) sR = i;
                        else if (sR != -1 && !string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b) && a != "Tên cấu hình nháp") { eR = i; break; }
                    }
                    if (sR != -1)
                    {
                        if (eR == -1) eR = checkD.Values.Count;
                        var delReq = new Google.Apis.Sheets.v4.Data.Request { DeleteDimension = new Google.Apis.Sheets.v4.Data.DeleteDimensionRequest { Range = new Google.Apis.Sheets.v4.Data.DimensionRange { SheetId = shId, Dimension = "ROWS", StartIndex = sR, EndIndex = eR } } };
                        await _service.Spreadsheets.BatchUpdate(new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest { Requests = new List<Google.Apis.Sheets.v4.Data.Request> { delReq } }, _spreadsheetId).ExecuteAsync();
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xóa bản nháp: " + ex.Message);
                return false;
            }
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

            _modernTreeView.AfterSelect += (s, e) =>
            {
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
            public TableLayoutPanel RowPanel;
            public Products SelectedProduct;
            public List<TextBox> AttrInputs = new List<TextBox>();
            public TextBox QtyInput;
            public ECQ_Soft.Helper.ProductSearchDropdown SearchDropdown; // Ô search chính
            public DataGridViewRow GridRowReference; // Dòng GridRow tương ứng (nếu đã lưu)
            public Dictionary<string, string> LastAttributes = new Dictionary<string, string>();
            public Dictionary<string, TextBox> Attrs = new Dictionary<string, TextBox>(StringComparer.OrdinalIgnoreCase);
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
            bool hasSearch = configParts.Contains("search");
            // Phần còn lại là tên thuộc tính cần hiển thị
            var attrKeys = configParts.Where(p => p != "search").ToList();

            // OnlyOne: Yes → bắt phải search; No → tự động load dòng sản phẩm
            string onlyOne = _expandedNode?.OnlyOne?.Trim()?.ToLower() ?? "";
            bool isOnlyOne = (onlyOne == "yes" || onlyOne == "có" || onlyOne == "true");
            bool mustSearch = hasSearch && isOnlyOne;
            // OnlyOne=No và có search → tự lấy dòng sản phẩm theo category + Type, hiển thị thuộc tính để edit
            bool autoLoad = hasSearch && !mustSearch; // No/blank → tự load 1 dòng nếu type onlyOne là Yes thì k hiện button thêm dòng cấu hình

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
            var bienList = ((_expandedNode?.Bien ?? "")).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
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

            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new System.Windows.Forms.Padding(10, 8, 10, 8),
                BackColor = Color.FromArgb(248, 252, 255),
                AutoScroll = true
            };

            // =============================================================
            // UNIFIED MULTI-ROW CONFIGURATION
            // =============================================================

            _configRows = new List<ConfigRow>();
            // --- TẠO CẤU TRÚC PANEL 2 LỚP ---
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 35, BackColor = Color.Transparent };
            var pnlMiddle = new Panel
            {
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
            var lblTitle = new Label
            {
                Text = $"📄 Cấu hình sản phẩm [{nodeType}]",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 50, 150),
                AutoSize = true,
                Location = new Point(10, 8)
            };
            pnlHeader.Controls.Add(lblTitle);

            var btnGlobalAddRow = new Button
            {
                Text = "➕ Thêm dòng cấu hình",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(160, 26),
                Location = new Point(lblTitle.Right + 30, 4),
                Cursor = Cursors.Hand,
                Visible = !isOnlyOne
            };
            btnGlobalAddRow.FlatAppearance.BorderSize = 0;
            btnGlobalAddRow.Click += (s, e) => AddConfigRowUI(attrKeys, attrLabels, formula, false, hasSearch ? filteredProducts : null);
            pnlHeader.Controls.Add(btnGlobalAddRow);

            // 2. Middle: Danh sách các dòng Config (Dùng lại _pnlRowsContainer)
            _pnlRowsContainer = new TableLayoutPanel
            {
                ColumnCount = 1,
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Margin = new System.Windows.Forms.Padding(0)
            };
            _pnlRowsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pnlMiddle.Controls.Add(_pnlRowsContainer);

            // --- KIỂM TRA DỮ LIỆU ĐỂ TỰ ĐỘNG NẠP LẠI (LAZY HYDRATION) ---
            string nodePath = GetFullPathForNode(_expandedNode, _modernTreeView.Nodes) ?? _expandedNode.Name;
            // Đảm bảo nodePath không kết thúc bằng dấu gạch chéo để khớp với colFormId
            if (nodePath.EndsWith("\\")) nodePath = nodePath.Substring(0, nodePath.Length - 1);

            // 1. ƯU TIÊN: Tìm các dòng trong Grid đã có sẵn cho node này (Vừa được restore bởi SwitchFormContext)
            var gridRowsForNode = dgvSelectedItems.Rows.Cast<DataGridViewRow>()
                .Where(gr => (gr.Cells["colFormId"].Value?.ToString() ?? "") == nodePath)
                .ToList();

            if (gridRowsForNode.Count > 0)
            {
                RestoreStateFromGridForNode(gridRowsForNode, attrKeys, attrLabels, formula, filteredProducts, hasSearch);
            }
            // 2. Nếu không có trong Grid, thử tìm trong Cache
            else if (_formProductsCache.ContainsKey(nodePath) && _formProductsCache[nodePath].Count > 0)
            {
                RestoreStateFromCacheForNode(nodePath, attrKeys, attrLabels, formula, filteredProducts, hasSearch);
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
                _expandStateCache[_expandedNode] = new ExpandPanelState
                {
                    ConfigRows = _configRows,
                    RowsContainer = _pnlRowsContainer,
                    ContentPanel = container
                };
            }
        }

        private void RestoreStateFromGridForNode(List<DataGridViewRow> gridRowsForNode, List<string> attrKeys, Dictionary<string, string> attrLabels, string formula, List<Products> filteredProducts, bool hasSearch)
        {
            foreach (var gr in gridRowsForNode)
            {
                AddConfigRowUI(attrKeys, attrLabels, formula, false, hasSearch ? filteredProducts : null);
                var rowUI = _configRows.Last();
                rowUI.GridRowReference = gr;

                // Nạp sản phẩm
                var p = gr.Tag as Products;
                if (p != null)
                {
                    if (rowUI.SearchDropdown != null) rowUI.SearchDropdown.Text = p.Name;
                    rowUI.SelectedProduct = p;
                }

                // Nạp số lượng
                if (rowUI.QtyInput != null) rowUI.QtyInput.Text = gr.Cells["colSoLuong"].Value?.ToString() ?? "1";

                // Nạp thuộc tính từ cột ẩn colAttributes
                string targetAttributes = gr.Cells["colAttributes"].Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(targetAttributes))
                {
                    var dictAttrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var splitAttrs = targetAttributes.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var pair in splitAttrs)
                    {
                        var kv = pair.Split(new[] { ':' }, 2);
                        if (kv.Length == 2) dictAttrs[kv[0].Trim()] = kv[1].Trim();
                    }
                    foreach (var kv in rowUI.Attrs)
                    {
                        if (dictAttrs.TryGetValue(kv.Key, out string val))
                        {
                            kv.Value.Text = val;
                        }
                    }
                }
            }
        }

        private void RestoreStateFromCacheForNode(string nodePath, List<string> attrKeys, Dictionary<string, string> attrLabels, string formula, List<Products> filteredProducts, bool hasSearch)
        {
            var draftsForNode = _formProductsCache[nodePath];
            foreach (var draft in draftsForNode)
            {
                string targetProductName = draft.ItemName;
                int targetQty = draft.Quantity;

                // Bơm UI
                AddConfigRowUI(attrKeys, attrLabels, formula, false, hasSearch ? filteredProducts : null);
                var rowUI = _configRows.Last();

                // Nạp sản phẩm
                Products matchedProduct = filteredProducts?.FirstOrDefault(p => p.Name == targetProductName);
                if (matchedProduct == null && _allProducts != null) matchedProduct = _allProducts.FirstOrDefault(p => p.Name == targetProductName);

                if (matchedProduct != null)
                {
                    if (rowUI.SearchDropdown != null) rowUI.SearchDropdown.Text = matchedProduct.Name;
                    rowUI.SelectedProduct = matchedProduct;
                }

                // Nạp số lượng
                if (rowUI.QtyInput != null) rowUI.QtyInput.Text = targetQty.ToString();

                // Nạp lại Ghi chú / Thuộc tính
                var dictAttrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string targetAttributes = draft.Attributes ?? "";
                var splitAttrs = targetAttributes.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var pair in splitAttrs)
                {
                    var kv = pair.Split(new[] { ':' }, 2);
                    if (kv.Length == 2) dictAttrs[kv[0].Trim()] = kv[1].Trim();
                }

                foreach (var kv in rowUI.Attrs)
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
            _formProductsCache.Remove(nodePath);
        }

        private void AddConfigRowUI(List<string> attrKeys, Dictionary<string, string> attrLabels, string formula, bool autoLoad, List<Products> products)
        {
            var row = new ConfigRow();
            _configRows.Add(row);

            bool showDelete = _configRows.Count > 1; // Không cho xóa dòng đầu tiên
            // colCount: Search(2, optional) + Qty(2) + Attrs(2*N) + Delete(1) + Filler(1)
            int colCount = (products != null ? 2 : 0) + 2 + (attrKeys.Count * 2) + 2;

            var rowPanel = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = colCount,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = (_configRows.Count % 2 != 0) ? Color.White : Color.FromArgb(250, 252, 255),
                Margin = new System.Windows.Forms.Padding(0, 0, 0, 1),
                Padding = new System.Windows.Forms.Padding(5, 5, 5, 5)
            };

            if (products != null)
            {
                rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));   // Nhãn Sản phẩm
                rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));  // Dropdown Sản phẩm
            }
            // Qty columns
            rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));       // Nhãn Số lượng
            rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45f));       // Ô nhập Số lượng
            foreach (var aKey in attrKeys)
            {
                rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75f));   // Nhãn thuộc tính
                rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60f));   // Ô nhập thuộc tính
            }
            rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30f));       // Cột cho nút xóa (Cố định 30px)
            rowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));       // Cột đệm (Filler) để đẩy mọi thứ sang trái

            row.RowPanel = rowPanel;
            int searchStartColumn = 0;
            int qtyStartColumn = products != null ? 2 : 0;

            // Khởi tạo txtQty trước để dùng trong sự kiện của cbo
            var txtQty = new TextBox
            {
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
            row.QtyInput = txtQty;

            // Thêm Search Controls vào TRƯỚC (để nằm bên Tái)
            if (products != null)
            {
                var lblSearch = new Label
                {
                    Text = "🔍 Sản phẩm:",
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 50, 150),
                    AutoSize = true,
                    Anchor = AnchorStyles.Right | AnchorStyles.Top, // Cho nhảy sang Phải để sát ô nhập
                    Margin = new System.Windows.Forms.Padding(0, 8, 2, 0) // Sát lề phải
                };
                rowPanel.Controls.Add(lblSearch);
                rowPanel.SetColumn(lblSearch, searchStartColumn);

                var cbo = new Helper.ProductSearchDropdown
                {
                    Font = new Font("Segoe UI", 9.5f),
                    Dock = DockStyle.Fill,
                    Margin = new System.Windows.Forms.Padding(0, 5, 8, 0) // Giảm margin từ 10 -> 8
                };
                cbo.LoadData(products);
                cbo.ProductSelected += (s, p) =>
                {
                    row.SelectedProduct = p;
                    PopulateAttrPanelRow(row, p, attrKeys);
                    AutoAddProductToGrid(row, p, txtQty.Text, attrKeys, formula); // Tính năng auto add
                };
                row.SearchDropdown = cbo;
                rowPanel.Controls.Add(cbo);
                rowPanel.SetColumn(cbo, searchStartColumn + 1);

                if (products.Count > 0)
                {
                    cbo.Text = "";
                }
            }

            // Thêm Qty Controls vào SAU (để nằm bên Phải của Sản phẩm)
            var lblQty = new Label
            {
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
                rowPanel.Controls.Add(new Label
                {
                    Text = label + ":",
                    Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(80, 80, 80),
                    AutoSize = true,
                    Anchor = AnchorStyles.Right | AnchorStyles.Top, // Căn phải
                    Margin = new System.Windows.Forms.Padding(5, 8, 2, 0) // Sát lề phải
                });

                var txt = new TextBox
                {
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
                var btnDel = new Button
                {
                    Text = "✕",
                    Size = new Size(24, 24),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.Red,
                    Margin = new System.Windows.Forms.Padding(5, 3, 0, 0),
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left // Căn trái trong cột 30px để sát ô nhập
                };
                btnDel.FlatAppearance.BorderSize = 0;
                btnDel.Click += (s, e) =>
                {
                    int delIdx = _configRows.IndexOf(row);
                    if (row.GridRowReference != null && dgvSelectedItems.Rows.Contains(row.GridRowReference))
                    {
                        dgvSelectedItems.Rows.Remove(row.GridRowReference);
                    }
                    _pnlRowsContainer.Controls.Remove(rowPanel);
                    _configRows.Remove(row);

                    // Sau khi xóa, đưa chuột (focus) về ô search của dòng gần nhất để user tiếp tục làm việc
                    if (_configRows.Count > 0)
                    {
                        int nextFocusIdx = Math.Min(delIdx, _configRows.Count - 1);
                        _configRows[nextFocusIdx].SearchDropdown?.Focus();
                    }
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
            SyncAttributesToGrid(row);
        }

        private void SyncAttributesToGrid(ConfigRow row)
        {
            if (row.GridRowReference != null && dgvSelectedItems.Rows.Contains(row.GridRowReference))
            {
                // Chỉ lấy các thuộc tính thực tế (bỏ qua Số lượng nội bộ nếu có)
                string attrsStr = string.Join(" | ", row.Attrs.Where(kv => kv.Key != "_internal_qty_").Select(kv => $"{kv.Key}: {kv.Value.Text}"));
                row.GridRowReference.Cells["colAttributes"].Value = attrsStr;
            }
        }

        /// <summary>
        /// Điểm đồng bộ tập trung: ghi thuộc tính vào dòng Grid, sau đó cập nhật cache toàn cục.
        /// Gọi mỗi khi sản phẩm được thêm mới hoặc cập nhật từ UI.
        /// </summary>
        private void SyncRowAndDraftGroups(ConfigRow row)
        {
            SyncAttributesToGrid(row);
            SyncGridToDraftGroups();
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
                AdjustCabinetRowHeight(row.GridRowReference); // auto-resize & repaint

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
                string currentNodePath = GetFullPathForNode(_expandedNode, _modernTreeView.Nodes) ?? _expandedNode.Name;
                string attrsStr = string.Join(" | ", row.Attrs.Where(kv => kv.Key != "_internal_qty_").Select(kv => $"{kv.Key}: {kv.Value.Text}"));

                int rowIndex = AddSelectedItemRow(finalName, sl, donGia, "0", "0", p, false, insIdx, currentNodePath, attrsStr);
                row.GridRowReference = dgvSelectedItems.Rows[rowIndex];

                // Cập nhật thêm các cột ẩn
                var gr = row.GridRowReference;
                gr.Cells["colModel"].Value = p.Model ?? "";
                gr.Cells["colSKU"].Value = p.SKU ?? "";
                gr.Cells["colXuatXu"].Value = GetProductBrand(p);
                gr.Cells["colDonVi"].Value = "Cái";
                gr.Cells["colGiaNhap"].Value = p.PriceCost;
                gr.Cells["colDanhMuc"].Value = p.Category;
                gr.Cells["colType"].Value = p.Type;
                gr.Cells["colHang"].Value = GetProductBrand(p);
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

            SyncRowAndDraftGroups(row); // Đồng bộ thuộc tính và cache ngay khi add mới hoặc cập nhật dòng
        }

        private void SyncGridToDraftGroups()
        {
            if (string.IsNullOrEmpty(_currentDraftName)) return;

            var rows = new List<IList<object>>();

            // 1. Dòng Header (Tên nháp) - Để khớp với logic Skip(1) khi tính toán
            rows.Add(new List<object> { _currentDraftName, "", "", "", "", "", "", "", "", "", "", "", "", "", "" });

            // 2. Các dòng sản phẩm từ Grid
            foreach (DataGridViewRow row in dgvSelectedItems.Rows)
            {
                if (row.IsNewRow) continue;

                string ten = row.Cells["colTen"].Value?.ToString() ?? "";
                // Nếu tên trùng với tên nháp hiện tại thì bỏ qua (dòng header nếu có trong grid)
                if (ten == _currentDraftName) continue;

                string path = row.Cells["colFormId"].Value?.ToString() ?? "";
                string attributes = row.Cells["colAttributes"].Value?.ToString() ?? "";

                var draftRow = BuildDraftSheetRowFromGrid(row, path, attributes);
                rows.Add(draftRow.Cast<object>().ToList());
            }

            // 3. Thêm 3 dòng trống ở cuối (Để khớp với logic Take(Count - 4) - bỏ 1 đầu 3 cuối)
            for (int i = 0; i < 3; i++) rows.Add(new List<object> { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" });

            // Cập nhật vào cache toàn cục
            _allDraftGroups[_currentDraftName] = rows;
        }
        /// <summary>
        /// Tạo panel Phase 2: hiển thị các thuộc tính + nút Thêm.
        /// </summary>
        private Panel BuildPhase2Panel(List<string> attrKeys, Dictionary<string, string> attrLabels,
                                       Dictionary<string, string> attrVarMap, string formula, bool startHidden,
                                       Control searchControl = null, bool showAddRowButton = false)
        {
            var pnl = new FlowLayoutPanel
            {
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
                    var btnAddSmall = new Button
                    {
                        Text = "➕",
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(0, 120, 215),
                        ForeColor = Color.White,
                        Size = new Size(32, 26),
                        Margin = new System.Windows.Forms.Padding(0, 2, 15, 0),
                        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                        Cursor = Cursors.Hand
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
            var pnlActions = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Width = 910,
                Height = 55,
                BackColor = Color.Transparent,
                Margin = new System.Windows.Forms.Padding(10, 10, 0, 10),
                Padding = new System.Windows.Forms.Padding(0)
            };

            if (!string.IsNullOrEmpty(formula))
            {
                var btnCalc = new Button
                {
                    Text = "🧠 Tính toán",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    BackColor = Color.FromArgb(0, 100, 200),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(110, 38),
                    Cursor = Cursors.Hand,
                    Margin = new System.Windows.Forms.Padding(15, 0, 0, 0)
                };
                btnCalc.FlatAppearance.BorderSize = 0;
                btnCalc.Click += (s, e) =>
                {
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
                    (p.Name != null && p.Name.ToLower().Contains(keyword)) ||
                    (p.SKU != null && p.SKU.ToLower().Contains(keyword)) ||
                    (p.Model != null && p.Model.ToLower().Contains(keyword)));

            var results = source.Take(200).ToList();
            string formula = _expandedNode?.Formula ?? "";

            _dgvSearchResults.Rows.Clear();
            foreach (var p in results)
            {
                bool hasL = decimal.TryParse(p.Length, out decimal L) && L > 0;
                bool hasW = decimal.TryParse(p.Width, out decimal W) && W > 0;
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
                _dgvSearchResults.Rows[idx].Cells["colId"].Value = p.Id;
                _dgvSearchResults.Rows[idx].Cells["colName"].Value = p.Name;
                _dgvSearchResults.Rows[idx].Cells["colModel"].Value = p.Model;
                _dgvSearchResults.Rows[idx].Cells["colSKU"].Value = p.SKU;
                _dgvSearchResults.Rows[idx].Cells["colSize"].Value = size;
                _dgvSearchResults.Rows[idx].Cells["colPrice"].Value = price > 0 ? (object)price : "";
                _dgvSearchResults.Rows[idx].Cells["colKQ"].Value = kqText;
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
                decimal.TryParse(p.Length, out pL);
                decimal.TryParse(p.Width, out pW);
                decimal.TryParse(p.Height, out pH);
                decimal.TryParse(p.Weight, out pWeight);
                decimal.TryParse(p.Price?.Replace(".", "").Replace(",", ""), out pPrice);
                decimal.TryParse(p.PriceCost?.Replace(".", "").Replace(",", ""), out pCost);
            }

            // Kích thước
            values["l"] = (double)pL; values["a"] = (double)pL;  // length / dài
            values["w"] = (double)pW; values["b"] = (double)pW;  // width  / rộng
            values["h"] = (double)pH; values["cao"] = (double)pH;  // height / cao
            values["d"] = (double)pL;                                // deep   (alias của length)
            // Giá
            values["p"] = (double)pPrice; values["gv"] = (double)pPrice;  // giá bán
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
            string currentFormId = (_currentActiveTypeCMB != null) ? _currentActiveTypeCMB.FullPath : "";
            AddSelectedItemRow(finalName, 1, 0, "0", "0", null, false, insIdx, currentFormId);
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
                SwitchFormContext(_currentActiveTypeCMB, activeTypeCMB);
                _currentActiveTypeCMB = activeTypeCMB;
            }

            if (treeNode?.Tag is HierarchyNode node && !string.IsNullOrEmpty(node.Config))
            {

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

        private void SwitchFormContext(TreeNode oldForm, TreeNode newForm)
        {
            string newPath = newForm?.FullPath ?? "";

            // 1. Lưu và xóa sản phẩm không thuộc về Form mới
            var rowsToRemove = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in dgvSelectedItems.Rows)
            {
                if (row.IsNewRow) continue;
                string rowFormId = row.Cells["colFormId"].Value?.ToString() ?? "";
                if (rowFormId == "GLOBAL") continue; // Không bao giờ xóa dòng mặc định

                // Một item được giữ lại nếu nó thuộc về Form mới đang được chọn
                // So sánh prefix để bao gồm cả các item của node con trong Form đó
                bool belongsToNewForm = !string.IsNullOrEmpty(newPath) &&
                    (rowFormId == newPath || rowFormId.StartsWith(newPath + "\\"));

                if (!belongsToNewForm)
                {
                    // Cất vào cache của node mà nó thuộc về (rowFormId lưu path chi tiết của node)
                    string targetCachePath = !string.IsNullOrEmpty(rowFormId) ? rowFormId : (oldForm?.FullPath ?? "");
                    if (!string.IsNullOrEmpty(targetCachePath))
                    {
                        if (!_formProductsCache.ContainsKey(targetCachePath))
                            _formProductsCache[targetCachePath] = new List<RowData>();

                        string rowName = row.Cells["colTen"].Value?.ToString() ?? "";

                        // Cố gắng lấy Attributes từ colAttributes hoặc UI hiện tại
                        string currentAttrs = row.Cells["colAttributes"].Value?.ToString() ?? "";
                        var configRow = _configRows?.FirstOrDefault(cr => cr.GridRowReference == row);
                        if (configRow != null)
                        {
                            currentAttrs = string.Join(" | ", configRow.Attrs.Select(kv => $"{kv.Key}: {kv.Value.Text}"));
                        }

                        _formProductsCache[targetCachePath].Add(new RowData
                        {
                            ItemName = rowName,
                            Model = row.Cells["colModel"].Value?.ToString() ?? "",
                            SKU = row.Cells["colSKU"].Value?.ToString() ?? "",
                            XuatXu = row.Cells["colXuatXu"].Value?.ToString() ?? "",
                            DonVi = row.Cells["colDonVi"].Value?.ToString() ?? "",
                            Quantity = int.TryParse(row.Cells["colSoLuong"].Value?.ToString(), out int q) ? q : 1,
                            UnitPrice = ParseCurrencyValue(row.Cells["colDonGia"].Value?.ToString()),
                            Progress = row.Cells["colTienDo"].Value?.ToString() ?? "0",
                            TotalPrice = row.Cells["colGiaTien"].Value?.ToString() ?? "0",
                            GiaNhap = row.Cells["colGiaNhap"].Value?.ToString() ?? "0",
                            DanhMuc = row.Cells["colDanhMuc"].Value?.ToString() ?? "",
                            Type = row.Cells["colType"].Value?.ToString() ?? "",
                            Hang = row.Cells["colHang"].Value?.ToString() ?? "",
                            Tag = row.Tag,
                            FormId = rowFormId,
                            Attributes = currentAttrs
                        });
                    }
                    rowsToRemove.Add(row);
                }
            }

            foreach (var row in rowsToRemove)
            {
                dgvSelectedItems.Rows.Remove(row);
            }

            // 2. Khôi phục sản phẩm của Form mới từ Cache
            if (!string.IsNullOrEmpty(newPath))
            {
                var keysToRestore = _formProductsCache.Keys.Where(k => k == newPath || k.StartsWith(newPath + "\\")).ToList();
                foreach (var key in keysToRestore)
                {
                    var cachedItems = _formProductsCache[key];
                    foreach (var item in cachedItems)
                    {
                        // Kiểm tra trùng lặp trong Grid trước khi nạp lại
                        bool alreadyInGrid = dgvSelectedItems.Rows.Cast<DataGridViewRow>()
                            .Any(gr => (gr.Cells["colFormId"].Value?.ToString() ?? "") == key &&
                                       (gr.Cells["colTen"].Value?.ToString() ?? "") == item.ItemName);

                        if (!alreadyInGrid)
                        {
                            int rowIndex = AddSelectedItemRow(item.ItemName, item.Quantity, item.UnitPrice, item.Progress, item.TotalPrice, item.Tag, false, GetInsertIndex(), key, item.Attributes);
                            var newRow = dgvSelectedItems.Rows[rowIndex];

                            newRow.Cells["colModel"].Value = item.Model;
                            newRow.Cells["colSKU"].Value = item.SKU;
                            newRow.Cells["colXuatXu"].Value = item.XuatXu;
                            newRow.Cells["colDonVi"].Value = item.DonVi;
                            newRow.Cells["colGiaNhap"].Value = item.GiaNhap;
                            newRow.Cells["colDanhMuc"].Value = item.DanhMuc;
                            newRow.Cells["colType"].Value = item.Type;
                            newRow.Cells["colHang"].Value = item.Hang;

                            UpdateExpandStateGridReference(item.ItemName, newRow);
                        }
                    }
                    _formProductsCache.Remove(key);
                }
            }

            RenumberGridSTT();
        }

        private void UpdateExpandStateGridReference(string itemName, DataGridViewRow newRow)
        {
            foreach (var state in _expandStateCache.Values)
            {
                foreach (var configRow in state.ConfigRows)
                {
                    if (configRow.SelectedProduct == null) continue;

                    string cleanName = configRow.SelectedProduct.Name;

                    // So sánh itemName từ Grid (có thể có prefix) với cleanName từ UI
                    bool isMatch = (itemName == cleanName) || itemName.EndsWith(": " + cleanName);

                    if (isMatch)
                    {
                        configRow.GridRowReference = newRow;
                    }
                }
            }
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
                    (p.Name != null && p.Name.ToLower().Contains(keyword)) ||
                    (p.SKU != null && p.SKU.ToLower().Contains(keyword)) ||
                    (p.Model != null && p.Model.ToLower().Contains(keyword))
                ).ToList();
            }

            _searchResults = results;
            _dgvSearchResults.Rows.Clear();
            foreach (var p in results)
            {
                string size = "";
                bool hasL = decimal.TryParse(p.Length, out decimal L) && L > 0;
                bool hasW = decimal.TryParse(p.Width, out decimal W) && W > 0;
                bool hasH = decimal.TryParse(p.Height, out decimal H) && H > 0;
                if (hasL || hasW || hasH)
                    size = $"{(hasL ? L.ToString("0.##") : "?")}×{(hasW ? W.ToString("0.##") : "?")}×{(hasH ? H.ToString("0.##") : "?")}";

                decimal price = 0;
                decimal.TryParse(p.Price?.Replace(".", "").Replace(",", ""), out price);

                int idx = _dgvSearchResults.Rows.Add();
                _dgvSearchResults.Rows[idx].Cells["colId"].Value = p.Id;
                _dgvSearchResults.Rows[idx].Cells["colName"].Value = p.Name;
                _dgvSearchResults.Rows[idx].Cells["colModel"].Value = p.Model;
                _dgvSearchResults.Rows[idx].Cells["colSKU"].Value = p.SKU;
                _dgvSearchResults.Rows[idx].Cells["colSize"].Value = size;
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

            int paddingH = 20;
            int paddingTop = pnlStepsContainer.Top;
            int formW = this.ClientSize.Width;
            int formH = this.ClientSize.Height;
            int controlW = formW - paddingH * 2;

            // 1. Cố định panel nút dưới cùng
            pnlControls.Height = 50;
            pnlControls.Width = formW;
            pnlControls.Location = new Point(0, formH - pnlControls.Height);

            // Căn lề nút ở góc dưới phải cho đồng nhất với paddingH mới
            btnReload.Location = new Point(formW - paddingH - btnReload.Width, btnReload.Location.Y);
            btnCancel.Location = new Point(btnReload.Left - btnCancel.Width - 10, btnCancel.Location.Y);
            btnApply.Location = new Point(btnCancel.Left - btnApply.Width - 10, btnApply.Location.Y);

            int availableH = pnlControls.Top - paddingTop - 15;

            // Di chuyển tiêu đề
            lblTitle.Location = new Point(paddingH, 15);

            // Tính toán components ở giữa
            int expandToggleH = (_btnExpandToggle != null && _btnExpandToggle.Visible) ? 38 : 0;
            int expandPanelH = (_expandPanelVisible && _expandPanel != null) ? 170 : 0;
            int extraH = expandToggleH + expandPanelH;

            // Phân bổ Tree / Grid theo tỷ lệ người dùng kéo (mặc định 55%)
            int remainingH = availableH - extraH - 60; // 60px cho header grid và khoàng cách
            int treeH = (int)(remainingH * _treeRatio);
            int gridH = remainingH - treeH;

            if (treeH < 100) { treeH = 100; gridH = remainingH - treeH; }
            if (gridH < 80) { gridH = 80; treeH = remainingH - gridH; }

            // Vẽ TreeView
            _modernTreeView.Location = new Point(paddingH, paddingTop);
            _modernTreeView.Size = new Size(controlW, treeH);

            int currentY = paddingTop + treeH + 10;

            // Nút expand
            if (_btnExpandToggle != null)
            {
                _btnExpandToggle.Location = new Point(paddingH, currentY);
                _btnExpandToggle.Size = new Size(controlW, 34);
                if (_btnExpandToggle.Visible) currentY += 38;
            }

            // Expand panel
            if (_expandPanel != null)
            {
                _expandPanel.Location = new Point(paddingH, currentY);
                _expandPanel.Size = new Size(controlW, 170); // Reduced height by half
                if (_expandPanelVisible) currentY += 170;
            }

            // Đường kẻ & Tiêu đề Grid (Sử dụng như splitter)
            currentY += 5;
            lblDivider.Cursor = Cursors.SizeNS;
            lblDivider.Location = new Point(paddingH, currentY);
            lblDivider.Width = controlW;
            lblDivider.Height = 10; // Tăng chiều cao vùng grab để dễ kéo hơn

            int headerY = currentY + 10;
            lblGridTitle.Location = new Point(paddingH, headerY + 5);
            btnAddToGrid.Location = new Point(formW - paddingH - btnAddToGrid.Width, headerY);

            // DataGridView (Chiếm phần còn lại)
            int dgvY = headerY + 40;
            dgvSelectedItems.Location = new Point(paddingH, dgvY);
            dgvSelectedItems.Size = new Size(controlW, Math.Max(60, pnlControls.Top - dgvY - 10));
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
            string nodeText = string.IsNullOrEmpty(dataNode.Id) ? dataNode.Name : $"{dataNode.Id}: {dataNode.Name}";
            var treeNode = new TreeNode(nodeText);
            treeNode.Tag = dataNode;

            foreach (var childNode in dataNode.Children)
            {
                treeNode.Nodes.Add(CreateTreeNode(childNode));
            }
            return treeNode;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);
        private const uint EM_SETCUEBANNER = 0x1501;

        private static void SetCueBanner(TextBox tb, string hint)
        {
            if (tb.IsHandleCreated)
                SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, hint);
        }

        private void NavigateToPath(string path)
        {
            if (string.IsNullOrEmpty(path) || _modernTreeView == null) return;

            // Hỗ trợ các loại phân cách phổ biến
            string[] parts = path.Split(new[] { '\\', '/', '-', '>' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(p => p.Trim())
                                 .ToArray();

            TreeNodeCollection currentNodes = _modernTreeView.Nodes;
            TreeNode lastFound = null;

            foreach (var part in parts)
            {
                TreeNode match = currentNodes.Cast<TreeNode>()
                    .FirstOrDefault(n => n.Text.Trim().Equals(part, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                {
                    // Thử tìm kiểu chứa (nếu text node có ID vd "1: TỦ ĐIỆN" mà path chỉ có "TỦ ĐIỆN")
                    match = currentNodes.Cast<TreeNode>()
                        .FirstOrDefault(n => n.Text.Contains(part));
                }

                if (match != null)
                {
                    match.Expand();
                    lastFound = match;
                    currentNodes = match.Nodes;
                }
                else break;
            }

            if (lastFound != null)
            {
                _modernTreeView.SelectedNode = lastFound;
                lastFound.EnsureVisible();
            }
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
                Rectangle bgRect = new Rectangle(offsetX + 4, e.Bounds.Y, controlWidth - 8, e.Bounds.Height);
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
