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
    public partial class FrmRelation : UserControl
    {
        private SheetsService _sheetsService;
        string spreadsheetId = "10gNCH_pG4LmkQ1g109H1WEM4nwBk4UBff_IDHar0Hd8";
        string sheetName = "Products_Table";
        private List<CategoryItem> categoryTree = new List<CategoryItem>();
        private List<Products> allProducts = new List<Products>(); 
        private List<Products> parentProducts = new List<Products>();
        private List<Products> childProducts = new List<Products>();
        public FrmRelation()
        {
            InitializeComponent();
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

        private void FrmRelation_Load(object sender, EventArgs e)
        {
            InitGoogleSheetsService();
            // LoadData(); // Sẽ được gọi từ FrmMain để preload song song

            // Gán sự kiện cho các nút
            btnSearch.Click += BtnSearch_Click;
            btnAddParent.Click += BtnAddParent_Click;
            btnAddChild.Click += BtnAddChild_Click;
            btnSaveRelation.Click += BtnSaveRelation_Click;
            btnRemoveParent.Click += BtnRemoveParent_Click;
            btnRemoveChild.Click += BtnRemoveChild_Click;
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

                    // 2. Load Hãng vào comboBox2 (Bên trái - Hãng sản xuất)
                    var brandList = rawBrands.OrderBy(b => b).ToList();
                    brandList.Insert(0, "-- Tất cả hãng --");
                    comboBox2.DataSource = null;
                    comboBox2.DataSource = brandList;

                    // 3. Hiển thị lên DataGridView
                    dgvAllProducts.DataSource = null;
                    dgvAllProducts.DataSource = allProducts;
                    FormatDataGridView(dgvAllProducts);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu cấu hình: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatDataGridView(DataGridView dgv)
        {
            if (dgv == null || dgv.Columns.Count == 0) return;

            // Ẩn các cột không cần thiết
            string[] hideCols = { "Id", "Weight", "Length", "Width", "Height" };
            foreach (var colName in hideCols)
            {
                if (dgv.Columns.Contains(colName)) dgv.Columns[colName].Visible = false;
            }

            // Đặt Header đúng tên
            if (dgv.Columns.Contains("Name")) dgv.Columns["Name"].HeaderText = "Tên sản phẩm";
            if (dgv.Columns.Contains("Model")) dgv.Columns["Model"].HeaderText = "Model";
            if (dgv.Columns.Contains("SKU")) dgv.Columns["SKU"].HeaderText = "Mã SKU";
            if (dgv.Columns.Contains("Price")) dgv.Columns["Price"].HeaderText = "Giá bán";
            if (dgv.Columns.Contains("HÃNG")) dgv.Columns["HÃNG"].HeaderText = "Hãng";
            if (dgv.Columns.Contains("Category")) dgv.Columns["Category"].HeaderText = "Danh mục";

            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = true;
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            string searchText = textBox1.Text.ToLower().Trim();
            string selectedBrand = comboBox2.SelectedItem?.ToString();
            string selectedCategoryPath = comboBox1.SelectedValue?.ToString();

            var filtered = allProducts.Where(p =>
                (string.IsNullOrEmpty(searchText) || p.Name.ToLower().Contains(searchText) || p.Model.ToLower().Contains(searchText) || p.SKU.ToLower().Contains(searchText)) &&
                (selectedBrand == "-- Tất cả hãng --" || p.HÃNG == selectedBrand) &&
                (string.IsNullOrEmpty(selectedCategoryPath) || p.Category.Contains(selectedCategoryPath))
            ).ToList();

            dgvAllProducts.DataSource = null;
            dgvAllProducts.DataSource = filtered;
            FormatDataGridView(dgvAllProducts);
        }

        // ham xử li
        private void BtnAddParent_Click(object sender, EventArgs e)
        {
            if (dgvAllProducts.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvAllProducts.SelectedRows)
                {
                    var product = row.DataBoundItem as Products;
                    if (product == null) continue;

                    // Kiểm tra nếu sản phẩm đã có trong danh sách Con
                    if (childProducts.Any(p => p.Id == product.Id))
                    {
                        MessageBox.Show($"Sản phẩm '{product.Name}' đã có trong danh sách Con, không thể thêm vào danh sách Cha!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    if (!parentProducts.Any(p => p.Id == product.Id))
                    {
                        parentProducts.Add(product);
                    }
                }
                UpdateGridSelector(dgvParentProducts, parentProducts);
                dgvAllProducts.ClearSelection();
            }
        }

        private void BtnAddChild_Click(object sender, EventArgs e)
        {
            if (dgvAllProducts.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvAllProducts.SelectedRows)
                {
                    var product = row.DataBoundItem as Products;
                    if (product == null) continue;

                    // Kiểm tra nếu sản phẩm đã có trong danh sách Cha
                    if (parentProducts.Any(p => p.Id == product.Id))
                    {
                        MessageBox.Show($"Sản phẩm '{product.Name}' đã có trong danh sách Cha, không thể thêm vào danh sách Con!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    if (!childProducts.Any(p => p.Id == product.Id))
                    {
                        childProducts.Add(product);
                    }
                }
                UpdateGridSelector(dgvChildProducts, childProducts);
                dgvAllProducts.ClearSelection();
            }
        }

        private void BtnRemoveParent_Click(object sender, EventArgs e)
        {
            if (dgvParentProducts.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvParentProducts.SelectedRows)
                {
                    var product = row.DataBoundItem as Products;
                    if (product != null)
                    {
                        parentProducts.RemoveAll(p => p.Id == product.Id);
                    }
                }
                UpdateGridSelector(dgvParentProducts, parentProducts);
            }
        }

        private void BtnRemoveChild_Click(object sender, EventArgs e)
        {
            if (dgvChildProducts.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvChildProducts.SelectedRows)
                {
                    var product = row.DataBoundItem as Products;
                    if (product != null)
                    {
                        childProducts.RemoveAll(p => p.Id == product.Id);
                    }
                }
                UpdateGridSelector(dgvChildProducts, childProducts);
            }
        }

        private void UpdateGridSelector(DataGridView dgv, List<Products> source)
        {
            dgv.DataSource = null;
            dgv.DataSource = source.ToList();
            FormatDataGridView(dgv);
        }

        private async void BtnSaveRelation_Click(object sender, EventArgs e)
        {
            string relCategory = textBox2.Text.Trim();
            if (string.IsNullOrEmpty(relCategory))
            {
                MessageBox.Show("Vui lòng nhập tên Danh mục quan hệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (parentProducts.Count == 0 || childProducts.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một sản phẩm Cha và một sản phẩm Con!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSaveRelation.Enabled = false;
            btnSaveRelation.Text = "Đang lưu...";

            try
            {
                // Chuẩn bị dữ liệu để append vào Sheet Products_Relatation
                // Cấu trúc: A:ID, B:ID_Product_Main (SKU Cha), C:ID_Product_Child (SKU Con), D:Category_PR
                var values = new List<IList<object>>();

                foreach (var parent in parentProducts)
                {
                    foreach (var child in childProducts)
                    {
                        var row = new List<object>
                        {
                            "", // ID (Để trống hoặc tự tăng nếu sheet có công thức)
                            parent.Id,
                            child.Id,
                            relCategory
                        };
                        values.Add(row);
                    }
                }

                var valueRange = new Google.Apis.Sheets.v4.Data.ValueRange { Values = values };
                var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, spreadsheetId, "Products_Relatation!A2:D");
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                
                await appendRequest.ExecuteAsync();

                MessageBox.Show($"Đã lưu thành công {values.Count} mối quan hệ vào Google Sheets!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Clear sau khi lưu thành công
                parentProducts.Clear();
                childProducts.Clear();
                textBox2.Clear();
                UpdateGridSelector(dgvParentProducts, parentProducts);
                UpdateGridSelector(dgvChildProducts, childProducts);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu quan hệ: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSaveRelation.Enabled = true;
                btnSaveRelation.Text = "Lưu";
            }
        }
    }
}
