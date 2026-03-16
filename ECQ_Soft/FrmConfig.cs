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

namespace ECQ_Soft
{
    public partial class FrmConfig : UserControl
    {
        private bool isUpdatingComboBoxes = false;
        private SheetsService _sheetsService;
        string spreadsheetId = "10gNCH_pG4LmkQ1g109H1WEM4nwBk4UBff_IDHar0Hd8";
        string sheetName = "Products_Table";
        string configSheetName = null;

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
            await LoadDataAsync();
        }

        private List<CategoryItem> categoryTree = new List<CategoryItem>();
        private List<Products> allProducts = new List<Products>(); 
        private List<ConfigProductItem> configProducts = new List<ConfigProductItem>();
        private List<RelationItem> productRelations = new List<RelationItem>();
        private List<Products> childProducts = new List<Products>();
        private List<ConfigProductItem> allSavedConfigs = new List<ConfigProductItem>();
        private string currentEditingConfigName = null;
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
        }

        private void DgvParentProducts_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            FormatConfigGrid(dgvParentProducts);
        }

        private void Grid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv != null)
            {
                FormatDataGridView(dgv);
            }
        }

        private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv != null && dgv.IsCurrentCellDirty)
            {
                dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

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
            
            comboBox2.SelectedValueChanged -= ComboBox2_SelectedValueChanged;
            comboBox2.SelectedValueChanged += ComboBox2_SelectedValueChanged;
            
            comboBox1.SelectedValueChanged -= ComboBox1_SelectedValueChanged;
            comboBox1.SelectedValueChanged += ComboBox1_SelectedValueChanged;
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            // Dùng .Text thay vì .SelectedItem để hỗ trợ cả gõ tay lẫn chọn từ dropdown
            string selectedHeaderName = comboBox3.Text?.Trim();
            if (string.IsNullOrEmpty(selectedHeaderName) || selectedHeaderName == "-- Chọn cấu hình đã lưu --")
            {
                MessageBox.Show("Vui lòng chọn một cấu hình để tải!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Tìm vị trí Header - dùng Trim() và OrdinalIgnoreCase để tránh lỗi khoảng trắng/encoding
            int headerIndex = allSavedConfigs.FindIndex(c =>
                c.IsHeader &&
                string.Equals(c.TenHang?.Trim(), selectedHeaderName, StringComparison.OrdinalIgnoreCase));

            if (headerIndex >= 0)
            {
                configProducts.Clear();
                
                // Thêm dòng Header đầu tiên
                configProducts.Add(allSavedConfigs[headerIndex]);

                // Thêm các dòng tiếp theo cho đến khi gặp Header mới hoặc hết danh sách
                for (int i = headerIndex + 1; i < allSavedConfigs.Count; i++)
                {
                    if (allSavedConfigs[i].IsHeader) break;
                    configProducts.Add(allSavedConfigs[i]);
                }

                UpdateHeaderSum();
                UpdateConfigGrid();
                
                currentEditingConfigName = selectedHeaderName;
                button5.Text = "Cập nhật";
                
                MessageBox.Show($"Đã tải cấu hình '{selectedHeaderName}' thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Không tìm thấy cấu hình '{selectedHeaderName}' trong danh sách!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            string searchText = textBox2.Text.Trim().ToLower();
            string selectedBrand = comboBox4.Text;
            string selectedCategoryPath = comboBox5.SelectedValue?.ToString();

            var filteredProducts = allProducts.AsEnumerable();

            // 1. Filter by Name / SKU (Partial match)
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredProducts = filteredProducts.Where(p => 
                    (p.Name != null && p.Name.ToLower().Contains(searchText)) || 
                    (p.SKU != null && p.SKU.ToLower().Contains(searchText)) ||
                    (p.Model != null && p.Model.ToLower().Contains(searchText))
                );
            }

            // 2. Filter by Manufacturer (Exact match)
            if (!string.IsNullOrEmpty(selectedBrand) && selectedBrand != "-- Tất cả hãng --")
            {
                filteredProducts = filteredProducts.Where(p => p.HÃNG == selectedBrand);
            }

            // 3. Filter by Category (Prefix match)
            if (!string.IsNullOrEmpty(selectedCategoryPath) && selectedCategoryPath != "-- Tất cả danh mục --")
            {
                filteredProducts = filteredProducts.Where(p => !string.IsNullOrEmpty(p.Category) && p.Category.StartsWith(selectedCategoryPath));
            }

            dgvAllProducts.DataSource = null;
            dgvAllProducts.DataSource = filteredProducts.ToList();
            // FormatDataGridView will be called by DataBindingComplete
        }

        public async Task LoadDataAsync()
        {
            if (_sheetsService == null) InitGoogleSheetsService();

            try
            {
                // Đọc dữ liệu từ Google Sheet (A2:K - 11 cột)
                // A0:ID, B1:Tên, C2:Model, D3:SKU, E4:Giá, F5:Khối lượng, G6:Dài, H7:Rộng, I8:Cao, J9:Danh mục, K10:Hãng
                string range = $"{sheetName}!A2:K";
                var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
                var response = await request.ExecuteAsync();
                IList<IList<object>> rows = response.Values;

                if (rows != null && rows.Count > 0)
                {
                    allProducts.Clear();
                    List<string> rawCategories = new List<string>();
                    HashSet<string> rawBrands = new HashSet<string>();

                    for (int i = 0; i < rows.Count; i++)
                    {
                        var row = rows[i];
                        if (row.Count < 2) continue;

                        var p = new Products
                        {
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
                        };

                        allProducts.Add(p);

                        if (!string.IsNullOrEmpty(p.Category)) rawCategories.Add(p.Category);
                        if (!string.IsNullOrEmpty(p.HÃNG)) rawBrands.Add(p.HÃNG);
                    }

                    // 1. Load Cây danh mục vào comboBox1 (Bên phải - Danh mục)
                    categoryTree = CategoryParser.ParseToTree(rawCategories);
                    categoryTree.Insert(0, new CategoryItem { DisplayText = "-- Tất cả danh mục --", FullPath = "" });
                    comboBox1.DataSource = null;
                    comboBox1.DataSource = categoryTree;
                    comboBox1.DisplayMember = "DisplayText";
                    comboBox1.ValueMember = "FullPath";

                    // Load Cây danh mục vào comboBox5 (Bên trái - Danh mục)
                    var categoryTree5 = CategoryParser.ParseToTree(rawCategories);
                    categoryTree5.Insert(0, new CategoryItem { DisplayText = "-- Tất cả danh mục --", FullPath = "" });
                    comboBox5.DataSource = null;
                    comboBox5.DataSource = categoryTree5;
                    comboBox5.DisplayMember = "DisplayText";
                    comboBox5.ValueMember = "FullPath";

                    // 2. Load Hãng vào comboBox4 (Bên trái - Hãng sản xuất)
                    var brandList = rawBrands.OrderBy(b => b).ToList();
                    brandList.Insert(0, "-- Tất cả hãng --");
                    comboBox4.DataSource = null;
                    comboBox4.DataSource = brandList;

                    // 3. Hiển thị lên DataGridView
                    dgvAllProducts.DataSource = null;
                    dgvAllProducts.DataSource = allProducts;
                    // FormatDataGridView will be called by DataBindingComplete
                }

                // --------- LOAD PRODUCTS RELATION ---------
                string relRange = "Products_Relatation!A2:E";
                var relRequest = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, relRange);
                var relResponse = await relRequest.ExecuteAsync();
                IList<IList<object>> relRows = relResponse.Values;

                productRelations.Clear();
                if (relRows != null && relRows.Count > 0)
                {
                    foreach (var row in relRows)
                    {
                        if (row.Count < 3) continue;
                        int mainId = 0, childId = 0;
                        int.TryParse(row.Count > 1 ? row[1]?.ToString() : "0", out mainId);
                        int.TryParse(row.Count > 2 ? row[2]?.ToString() : "0", out childId);
                        string catPr = row.Count > 3 ? row[3]?.ToString() : "";

                        productRelations.Add(new RelationItem
                        {
                            ID_Product_Main = mainId,
                            ID_Product_Child = childId,
                            Category_PR = catPr
                        });
                    }
                }

                // 4. Load Tên sản phẩm có trong bảng Relation vào comboBox2 (cả Main và Child)
                var relationProductIds = productRelations.Select(r => r.ID_Product_Main)
                    .Concat(productRelations.Select(r => r.ID_Product_Child))
                    .Distinct()
                    .ToList();
                var relationProductsDisplay = allProducts
                    .Where(p => relationProductIds.Contains(p.Id))
                    .Select(p => new { Id = p.Id, Name = p.Name })
                    .OrderBy(p => p.Name)
                    .ToList();
                relationProductsDisplay.Insert(0, new { Id = 0, Name = "-- Chọn sản phẩm --" });
                comboBox2.DataSource = null;
                comboBox2.DataSource = relationProductsDisplay;
                comboBox2.DisplayMember = "Name";
                comboBox2.ValueMember = "Id";

                // 5. Load Danh mục PR vào comboBox1
                var catPRs = productRelations.Select(r => r.Category_PR).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                catPRs.Insert(0, "-- Tất cả danh mục --");
                comboBox1.DataSource = null;
                comboBox1.DataSource = catPRs;

                // --------- LOAD SAVED CONFIGS ---------
                string savedConfigRange = $"{configSheetName}!A2:L";
                var savedConfigRequest = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, savedConfigRange);
                var savedConfigResponse = await savedConfigRequest.ExecuteAsync();
                IList<IList<object>> savedRows = savedConfigResponse.Values;

                allSavedConfigs.Clear();
                if (savedRows != null && savedRows.Count > 0)
                {
                    for (int i = 0; i < savedRows.Count; i++)
                    {
                        var row = savedRows[i];
                        if (row.Count < 2) continue;

                        string tenHang = row[1]?.ToString()?.Trim() ?? "";
                        // Bỏ qua các dòng tổng cộng/tóm tắt
                        if (tenHang.StartsWith("TỔNG CỘNG") || tenHang.StartsWith("THUẾ VAT") || tenHang.StartsWith("THÀNH TIỀN"))
                            continue;

                        Func<string, decimal> parseCurrency = (s) => {
                            if (string.IsNullOrEmpty(s)) return 0;
                            // Loại bỏ dấu chấm và dấu phẩy để parse (giả sử dấu chấm là phân cách nghìn)
                            // Nếu có cả chấm và phẩy, cần cẩn thận hơn, nhưng ở đây thường là N0
                            string clean = s.Replace(".", "").Replace(",", "").Replace("₫", "").Trim();
                            decimal.TryParse(clean, out decimal res);
                            return res;
                        };

                        decimal dg = parseCurrency(row.Count > 6 ? row[6]?.ToString() : "0");
                        decimal ttVnd = parseCurrency(row.Count > 7 ? row[7]?.ToString() : "0");
                        decimal gn = parseCurrency(row.Count > 9 ? row[9]?.ToString() : "0");
                        decimal tt = parseCurrency(row.Count > 10 ? row[10]?.ToString() : "0");
                        decimal bg = parseCurrency(row.Count > 11 ? row[11]?.ToString() : "0");

                        var item = new ConfigProductItem
                        {
                            STT = (row.Count > 0 && int.TryParse(row[0]?.ToString(), out int stt)) ? stt : i + 1,
                            TenHang = row.Count > 1 ? row[1]?.ToString() : "",
                            MaHang = row.Count > 2 ? row[2]?.ToString() : "",
                            XuatXu = row.Count > 3 ? row[3]?.ToString() : "",
                            DonVi = row.Count > 4 ? row[4]?.ToString() : "",
                            SoLuong = (row.Count > 5 && int.TryParse(row[5]?.ToString(), out int sl)) ? sl : 0,
                            DonGiaVND = dg,
                            ThanhTienVND = ttVnd,
                            GhiChu = row.Count > 8 ? row[8]?.ToString() : "",
                            GiaNhap = gn,
                            ThanhTien = tt,
                            BangGia = bg,
                            IsHeader = (row.Count > 4 && row[4]?.ToString() == "TỦ") || (row.Count > 0 && row[0]?.ToString() == "1")
                        };
                        allSavedConfigs.Add(item);
                    }
                }

                // Luôn cập nhật comboBox3 dù sheet có dữ liệu hay rỗng
                var headerNames = allSavedConfigs
                    .Where(c => c.IsHeader && !string.IsNullOrEmpty(c.TenHang) && !c.TenHang.StartsWith("--"))
                    .Select(c => c.TenHang)
                    .Distinct()
                    .ToList();
                headerNames.Insert(0, "-- Chọn cấu hình đã lưu --");
                comboBox3.DataSource = null;
                comboBox3.DataSource = headerNames;
                // ------------------------------------------

                UpdateHeaderSum();
                UpdateConfigGrid();
                UpdateGridSelector(dgvAllProducts, allProducts);
                UpdateGridSelector(dataGridView1, childProducts);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu cấu hình: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatDataGridView(DataGridView dgv)
        {
            if (dgv == null || dgv.IsDisposed || dgv.Columns == null || dgv.Columns.Count == 0) return;

            try
            {
                // Sử dụng mảng cố định để duyệt để tránh lỗi đồng bộ
                var cols = dgv.Columns.Cast<DataGridViewColumn>().ToList();

                foreach (var col in cols)
                {
                    if (col == null || col.DataGridView == null) continue;
                    
                    string colName = col.Name;

                    // 1. Hide unwanted columns
                    if (colName == "Id" || colName == "Weight" || colName == "Length" || colName == "Width" || colName == "Height")
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
                        col.HeaderText = "Chọn";
                        try { col.ReadOnly = false; } catch { }
                        try { col.DisplayIndex = dgv.Columns.Count - 1; } catch { }
                    }

                    // 3. Global ReadOnly
                    if (colName != "IsSelected")
                    {
                        try { col.ReadOnly = true; } catch { }
                    }
                }

                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dgv.MultiSelect = true;
            }
            catch (Exception)
            {
                // Silently ignore layout-related exceptions during binding
            }
        }
        private void ComboBox2_SelectedValueChanged(object sender, EventArgs e)
        {
            if (isUpdatingComboBoxes) return;

            isUpdatingComboBoxes = true;
            try
            {
                if (comboBox2.SelectedValue != null && int.TryParse(comboBox2.SelectedValue.ToString(), out int selectedId) && selectedId > 0)
                {
                    var catPRs = productRelations
                        .Where(r => r.ID_Product_Main == selectedId || r.ID_Product_Child == selectedId)
                        .Select(r => r.Category_PR)
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Distinct()
                        .ToList();
                    
                    catPRs.Insert(0, "-- Tất cả danh mục --");
                    string currentCat = comboBox1.SelectedItem?.ToString();

                    comboBox1.DataSource = null;
                    comboBox1.DataSource = catPRs;

                    if (catPRs.Contains(currentCat))
                        comboBox1.SelectedItem = currentCat;
                }
                else
                {
                    var catPRs = productRelations.Select(r => r.Category_PR).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                    catPRs.Insert(0, "-- Tất cả danh mục --");
                    string currentCat = comboBox1.SelectedItem?.ToString();

                    comboBox1.DataSource = null;
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

        private void ComboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (isUpdatingComboBoxes) return;

            isUpdatingComboBoxes = true;
            try
            {
                string selectedCatPR = comboBox1.SelectedItem?.ToString();
                
                var relationProductIds = productRelations.Select(r => r.ID_Product_Main)
                    .Concat(productRelations.Select(r => r.ID_Product_Child))
                    .Distinct()
                    .ToList();

                if (!string.IsNullOrEmpty(selectedCatPR) && selectedCatPR != "-- Tất cả danh mục --")
                {
                    relationProductIds = productRelations
                        .Where(r => r.Category_PR == selectedCatPR)
                        .Select(r => r.ID_Product_Main)
                        .Concat(productRelations
                            .Where(r => r.Category_PR == selectedCatPR)
                            .Select(r => r.ID_Product_Child))
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

                comboBox2.DataSource = null;
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
            if (selectedCatPR == "-- Tất cả danh mục --") selectedCatPR = null;

            int selectedId = 0;
            if (comboBox2.SelectedValue != null) int.TryParse(comboBox2.SelectedValue.ToString(), out selectedId);

            List<int> relatedIds = new List<int>();

            if (selectedId > 0)
            {
                // Nếu chọn một sản phẩm cụ thể: Lấy các sản phẩm liên quan (Main <-> Child) và lọc theo danh mục nếu có
                var childIds = productRelations
                    .Where(r => r.ID_Product_Main == selectedId && (string.IsNullOrEmpty(selectedCatPR) || r.Category_PR == selectedCatPR))
                    .Select(r => r.ID_Product_Child);

                var mainIds = productRelations
                    .Where(r => r.ID_Product_Child == selectedId && (string.IsNullOrEmpty(selectedCatPR) || r.Category_PR == selectedCatPR))
                    .Select(r => r.ID_Product_Main);

                relatedIds = childIds.Concat(mainIds).Distinct().ToList();
            }
            else if (!string.IsNullOrEmpty(selectedCatPR))
            {
                // Nếu KHÔNG chọn sản phẩm nhưng có chọn Danh mục PR: Lấy tất cả sản phẩm tham gia vào các quan hệ thuộc danh mục này
                relatedIds = productRelations
                    .Where(r => r.Category_PR == selectedCatPR)
                    .SelectMany(r => new[] { r.ID_Product_Main, r.ID_Product_Child })
                    .Distinct()
                    .ToList();
            }

            if (relatedIds.Count > 0)
            {
                // Hiển thị danh sách sản phẩm tìm được
                childProducts = allProducts.Where(p => relatedIds.Contains(p.Id)).ToList();
                foreach (var p in childProducts) p.IsSelected = false; // Reset selection state
                dataGridView1.DataSource = null;
                dataGridView1.DataSource = childProducts;
                // FormatDataGridView will be called by DataBindingComplete
            }
            else
            {
                // Nếu không tìm thấy kết quả hoặc không có filter, xóa trắng bảng
                dataGridView1.DataSource = null;
            }
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            var selectedItems = childProducts.Where(p => p.IsSelected).ToList();
            if (selectedItems.Count > 0)
            {
                // Tự động thêm dòng Header nếu danh sách đang rỗng (Không còn chkAddHeader nữa)
                if (configProducts.Count == 0 || !configProducts.Any(p => p.IsHeader))
                {
                    string headerName = comboBox2.Text; // Tên của sản phẩm Main làm Header
                    
                    // if (string.IsNullOrEmpty(headerName) || headerName.StartsWith("--"))
                    // {
                    //     MessageBox.Show("Vui lòng chọn sản phẩm chính (Main Product) trước khi thêm các sản phẩm liên quan vào cấu hình!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //     return;
                    // }

                    button5.Text = "Lưu";
                    currentEditingConfigName = null;
                    configProducts.Add(new ConfigProductItem
                    {
                        STT = 1,
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
                }

                foreach (var product in selectedItems)
                {
                    if (!configProducts.Any(p => p.MaHang == product.SKU))
                    {
                        decimal price = 0;
                        decimal.TryParse(product.Price.Replace(".", "").Replace(",", ""), out price);

                        configProducts.Add(new ConfigProductItem
                        {
                            STT = configProducts.Count + 1,
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
                    }
                    product.IsSelected = false; // Reset sau khi thêm
                }
                
                for (int i = 0; i < configProducts.Count; i++)
                {
                    configProducts[i].STT = i + 1;
                }
                
                UpdateHeaderSum();
                UpdateConfigGrid();
                dataGridView1.Refresh();
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
                
                // Clear grid và reset button sau khi thao tác thành công
                configProducts.Clear();
                UpdateConfigGrid();
                button5.Text = "Lưu";
                currentEditingConfigName = null;
                
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

            List<ConfigProductItem> finalDataToSave = new List<ConfigProductItem>();
            string newHeaderName = configProducts.FirstOrDefault(p => p.IsHeader)?.TenHang;

            if (button5.Text == "Cập nhật" && !string.IsNullOrEmpty(currentEditingConfigName))
            {
                int headerIndex = allSavedConfigs.FindIndex(c => c.IsHeader && c.TenHang == currentEditingConfigName);
                if (headerIndex >= 0)
                {
                    finalDataToSave.AddRange(allSavedConfigs.Take(headerIndex));
                    
                    int nextHeaderIndex = allSavedConfigs.FindIndex(headerIndex + 1, c => c.IsHeader);
                    
                    finalDataToSave.AddRange(configProducts);
                    
                    if (nextHeaderIndex > 0)
                    {
                        finalDataToSave.AddRange(allSavedConfigs.Skip(nextHeaderIndex));
                    }
                }
                else
                {
                    finalDataToSave.AddRange(allSavedConfigs);
                    finalDataToSave.AddRange(configProducts);
                }
            }
            else
            {
                finalDataToSave.AddRange(allSavedConfigs);
                finalDataToSave.AddRange(configProducts);
            }

            // Gán lại tham chiếu
            allSavedConfigs = finalDataToSave.ToList();

            // 1. Chuẩn bị dữ liệu để ghi
            var valueRange = new Google.Apis.Sheets.v4.Data.ValueRange();
            var objectList = new List<IList<object>>();

            for (int i = 0; i < finalDataToSave.Count; i++)
            {
                var item = finalDataToSave[i];
                var row = new List<object>
                {
                    item.STT,
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
            decimal tongCongThanhTienVND = finalDataToSave.Where(x => !x.IsHeader).Sum(x => x.ThanhTienVND);
            decimal tongCongGiaNhap     = finalDataToSave.Where(x => !x.IsHeader).Sum(x => x.GiaNhap);
            decimal tongCongThanhTien   = finalDataToSave.Where(x => !x.IsHeader).Sum(x => x.ThanhTien);

            decimal thueMul = 0.08m;
            decimal vatThanhTienVND = Math.Round(tongCongThanhTienVND * thueMul, 0);
            decimal vatThanhTien    = Math.Round(tongCongThanhTien    * thueMul, 0);

            decimal totalThanhTienVND = tongCongThanhTienVND + vatThanhTienVND;
            decimal totalThanhTien    = tongCongThanhTien    + vatThanhTien;

            int summaryStartRow = finalDataToSave.Count + 2; // +2 vì row 1 là header cột

            var summaryValues = new Google.Apis.Sheets.v4.Data.ValueRange
            {
                Values = new List<IList<object>>
                {
                    // TỔNG CỘNG — col A trống, B=nhãn, G=ThanhTienVND, J=GiaNhap, K=ThanhTien
                    new List<object> { "", "TỔNG CỘNG (Giá chưa bao gồm VAT)", "", "", "", "",
                        tongCongThanhTienVND, "", "", tongCongGiaNhap, tongCongThanhTien, "" },
                    // THUẾ VAT 8%
                    new List<object> { "", "THUẾ VAT 8%", "", "", "", "",
                        vatThanhTienVND, "", "", "", vatThanhTien, "" },
                    // THÀNH TIỀN
                    new List<object> { "", "THÀNH TIỀN", "", "", "", "",
                        totalThanhTienVND, "", "", "", totalThanhTien, "" }
                }
            };
            string summaryRange = $"{configSheetName}!A{summaryStartRow}";
            var summaryRequest = _sheetsService.Spreadsheets.Values.Update(summaryValues, spreadsheetId, summaryRange);
            summaryRequest.ValueInputOption = Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await summaryRequest.ExecuteAsync();


            // Không cập nhật lại currentEditingConfigName ở đây nữa vì sẽ được reset ở sự kiện Click
            // Đổ lại danh sách Tên Header vào comboBox3
            var headerNames = allSavedConfigs
                .Where(c => c.IsHeader)
                .Select(c => c.TenHang)
                .Distinct()
                .ToList();
            headerNames.Insert(0, "-- Chọn cấu hình đã lưu --");
            
            // Invoke in case updates from background task issue cross-thread mapping but await usually returns to UI context
            comboBox3.DataSource = null;
            comboBox3.DataSource = headerNames;
            if (headerNames.Contains(currentEditingConfigName))
            {
                comboBox3.SelectedItem = currentEditingConfigName;
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

                    // Xóa định dạng cũ vùng dữ liệu (A2:L1000)
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
                            Fields = "userEnteredFormat.backgroundColor"
                        }
                    });

                    // Tô màu các dòng là Header
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
                                            BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0.2f, Green = 0.8f, Blue = 0.2f }, // Màu xanh lá
                                            NumberFormat = new Google.Apis.Sheets.v4.Data.NumberFormat { Type = "NUMBER", Pattern = "#,##0" }
                                        }
                                    },
                                    Fields = "userEnteredFormat(backgroundColor,numberFormat)"
                                }
                            });
                        }
                    }

                    // Định dạng tiền cho toàn bộ cột G, H, J, K, L (index 6, 7, 9, 10, 11)
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

                    // Tô màu 3 dòng tóm tắt cuối (màu cyan/xanh ngọc như ảnh)
                    int baseSummaryRow = finalDataToSave.Count + 1; // index 0-based (row 1 = header cột)
                    var summaryColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0.0f, Green = 0.9f, Blue = 0.9f }; // cyan
                    for (int s = 0; s < 3; s++)
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
                                    StartColumnIndex = 0, EndColumnIndex = 12
                                },
                                Cell = new Google.Apis.Sheets.v4.Data.CellData
                                {
                                    UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                                    {
                                        BackgroundColor = summaryColor,
                                        TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true },
                                        NumberFormat = new Google.Apis.Sheets.v4.Data.NumberFormat { Type = "NUMBER", Pattern = "#,##0" }
                                    }
                                },
                                Fields = "userEnteredFormat(backgroundColor,textFormat,numberFormat)"
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
            var selectedItems = allProducts.Where(p => p.IsSelected).ToList();
            if (selectedItems.Count > 0)
            {
                // Tự động thêm dòng Header nếu danh sách đang rỗng 
                if (configProducts.Count == 0 || !configProducts.Any(p => p.IsHeader))
                {
                    button5.Text = "Lưu";
                    currentEditingConfigName = null;
                    configProducts.Add(new ConfigProductItem
                    {
                        STT = 1,
                        TenHang = "TỦ ĐIỆN VÍ DỤ", // Có thể cho người dùng tự sửa
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

                foreach (var product in selectedItems)
                {
                    if (!configProducts.Any(p => p.MaHang == product.SKU))
                    {
                        decimal price = 0;
                        decimal.TryParse(product.Price.Replace(".", "").Replace(",", ""), out price);

                        configProducts.Add(new ConfigProductItem
                        {
                            STT = configProducts.Count + 1,
                            TenHang = product.Name,
                            MaHang = product.SKU,
                            XuatXu = product.HÃNG,
                            DonVi = "Cái", // Default logic
                            SoLuong = 1,
                            DonGiaVND = price,
                            ThanhTienVND = price,
                            GhiChu = "",
                            GiaNhap = price, // Default logic?
                            ThanhTien = price,
                            BangGia = price,
                            IsHeader = false
                        });
                    }
                    product.IsSelected = false; // Reset sau khi thêm
                }
                
                UpdateHeaderSum();
                UpdateConfigGrid();
                dgvAllProducts.Refresh();
            }
        }

        private void UpdateHeaderSum()
        {
            var headerRow = configProducts.FirstOrDefault(p => p.IsHeader);
            if (headerRow != null)
            {
                // Tính tổng trừ dòng Header ra
                decimal totalDonGia = configProducts.Where(p => !p.IsHeader).Sum(p => p.DonGiaVND * p.SoLuong);
                decimal totalThanhTien = configProducts.Where(p => !p.IsHeader).Sum(p => p.ThanhTienVND);
                decimal totalGiaNhap = configProducts.Where(p => !p.IsHeader).Sum(p => p.GiaNhap * p.SoLuong);

                headerRow.DonGiaVND = totalDonGia;
                headerRow.ThanhTienVND = totalThanhTien;
                headerRow.GiaNhap = totalGiaNhap;
                headerRow.ThanhTien = totalThanhTien;
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
            dgvParentProducts.DataSource = null;
            dgvParentProducts.DataSource = configProducts.ToList();
            // Format will be handled by DgvParentProducts_DataBindingComplete
            
            // Re-bind CellFormatting if needed (usually stays bound, but for safety)
            dgvParentProducts.CellFormatting -= DgvParentProducts_CellFormatting;
            dgvParentProducts.CellFormatting += DgvParentProducts_CellFormatting;
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
                
                if (dgv.Columns.Contains("DonGiaVND"))
                {
                    dgv.Columns["DonGiaVND"].HeaderText = "Đơn giá (VNĐ)";
                    dgv.Columns["DonGiaVND"].DefaultCellStyle.Format = "N0";
                }
                if (dgv.Columns.Contains("ThanhTienVND"))
                {
                    dgv.Columns["ThanhTienVND"].HeaderText = "Thành tiền (VNĐ)";
                    dgv.Columns["ThanhTienVND"].DefaultCellStyle.Format = "N0";
                }
                if (dgv.Columns.Contains("GhiChu")) dgv.Columns["GhiChu"].HeaderText = "Ghi chú";
                if (dgv.Columns.Contains("GiaNhap"))
                {
                    dgv.Columns["GiaNhap"].HeaderText = "Giá Nhập";
                    dgv.Columns["GiaNhap"].DefaultCellStyle.Format = "N0";
                }
                if (dgv.Columns.Contains("ThanhTien"))
                {
                    dgv.Columns["ThanhTien"].HeaderText = "Thành Tiền";
                    dgv.Columns["ThanhTien"].DefaultCellStyle.Format = "N0";
                }
                if (dgv.Columns.Contains("BangGia"))
                {
                    dgv.Columns["BangGia"].HeaderText = "Bảng Giá";
                    dgv.Columns["BangGia"].DefaultCellStyle.Format = "N0";
                }
                
                if (dgv.Columns.Contains("IsHeader")) dgv.Columns["IsHeader"].Visible = false;

                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgv.MultiSelect = true;
                
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.Name != "SoLuong" && col.Name != "GhiChu" && col.Name != "TenHang")
                    {
                        col.ReadOnly = true;
                    }
                }
            }
            catch (Exception) { /* Ignore lifecycle exceptions */ }
        }

        private void DgvParentProducts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < configProducts.Count)
            {
                var item = configProducts[e.RowIndex];
                if (item.IsHeader)
                {
                    e.CellStyle.BackColor = Color.LightGreen;
                    e.CellStyle.ForeColor = Color.Black;
                    e.CellStyle.SelectionBackColor = Color.LimeGreen; // Giữ lại chút xanh lá khi được tô đậm
                    e.CellStyle.SelectionForeColor = Color.Black;
                    e.CellStyle.Font = new Font(dgvParentProducts.Font, FontStyle.Bold);
                }
            }
        }

        private void UpdateGridSelector(DataGridView dgv, List<Products> source)
        {
            dgv.DataSource = null;
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
    }
}
