using ECQ_Soft.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ECQ_Soft
{
    public partial class FrmProductSearch : Form
    {
        private List<Products> _allProducts;
        public event Action<List<Products>> OnProductsSelected;

        public event Action OnAdvancedConfigRequested;
        public event Action<string, string> OnHeaderAdded;

        // Theo dõi số lượng đã thêm cho mỗi SKU trong phiên làm việc
        private Dictionary<string, int> _addedQty = new Dictionary<string, int>();
        private bool _isFormatting = false;

        public FrmProductSearch(List<Products> products, bool isForQuote = false)
        {
            InitializeComponent();
            _allProducts = products;

            // ── Đặt thuộc tính Form TRƯỚC khi Show() để tránh recreate handle ──
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Size = new Size(1100, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Nếu không phải cho Bảng báo giá thì ẩn phần "Thêm tiêu đề/ Custom mục"
            if (!isForQuote)
            {
                lblAddHeader.Visible = false;
                txtHeaderSTT.Visible = false;
                cboHeaderName.Visible = false;
                btnAddHeaderToQuote.Visible = false;
            }
            else
            {
                cboHeaderName.Items.AddRange(new object[] {
                    "I. THIẾT BỊ ĐẦU VÀO",
                    "II. THIẾT BỊ ĐẦU RA",
                    "III. THIẾT BỊ ĐIỀU KHIỂN",
                    "Bộ khởi động DOL<3kW",
                    "Bộ khởi động VFD 37kW",
                    "II VẬT TƯ",
                    "VẬT TƯ PHỤ"
                });
            }

            this.Load += (s, e) => SetupSearchInterface();
        }

        private void SetupSearchInterface()
        {
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F);

            // Load categories
            var rawCategories = _allProducts
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();
            var treeNodes = Helper.CategoryParser.ParseToTreeNodes(rawCategories);
            cboCategory.LoadTree(treeNodes);

            SetupEvents();
            FilterData();
        }

        private void SetupEvents()
        {
            // Tự động lọc khi người dùng gõ text
            txtSearch.TextChanged += (s, e) => FilterData();

            // Tự động lọc khi chọn danh mục
            cboCategory.SelectionChanged += (s, fullPath) => FilterData();

            // Nút ⟳ góc trên phải: Vẽ icon đẹp bằng GDI+ và cài đặt sự kiện click
            btnRefresh.Text = "";
            btnRefresh.Click += (s, e) => FilterData();
            
            // Thêm hiệu ứng hover đổi màu nền mượt mà
            Color refreshNormalColor = Color.FromArgb(23, 162, 184);
            Color refreshHoverColor = Color.FromArgb(17, 122, 139);
            btnRefresh.MouseEnter += (s, e) => { btnRefresh.BackColor = refreshHoverColor; };
            btnRefresh.MouseLeave += (s, e) => { btnRefresh.BackColor = refreshNormalColor; };

            btnRefresh.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Tính toán vị trí giữa nút
                int size = 20;
                int cx = btnRefresh.Width / 2;
                int cy = btnRefresh.Height / 2;
                Rectangle rect = new Rectangle(cx - size / 2, cy - size / 2, size, size);

                // Vẽ vòng cung tròn (3/4 đường tròn)
                using (Pen pen = new Pen(Color.White, 2.5f))
                {
                    // Góc vẽ từ 45 độ đến 270 độ tạo thành khuyết ở trên phải
                    g.DrawArc(pen, rect, 45, 270);
                }

                // Vẽ mũi tên nhỏ ở đầu vòng cung
                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    Point[] arrowPoints = new Point[]
                    {
                        new Point(cx + 4, cy - 10),  // Đỉnh
                        new Point(cx + 11, cy - 6),  // Phải
                        new Point(cx + 4, cy - 2)   // Trái
                    };
                    g.FillPolygon(brush, arrowPoints);
                }
            };

            // Nút Thêm vào
            btnAddTo.Click += (s, e) => AddSelectedProducts();

            btnCancel.Click += (s, e) => this.Close();



            if (btnAddHeaderToQuote != null)
            {
                btnAddHeaderToQuote.Click += (s, e) => {
                    string stt = txtHeaderSTT.Text.Trim();
                    string name = cboHeaderName.Text.Trim();
                    if (string.IsNullOrEmpty(name)) {
                        MessageBox.Show("Vui lòng nhập tên tiêu đề!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    OnHeaderAdded?.Invoke(stt, name);
                    MessageBox.Show("Đã thêm tiêu đề vào bảng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtHeaderSTT.Text = "";
                    cboHeaderName.Text = "";
                };
            }

            // Double-click: thêm ngay sản phẩm được nháy đúp
            dgvProducts.CellDoubleClick += (s, e) => {
                if (e.RowIndex >= 0)
                {
                    var product = dgvProducts.Rows[e.RowIndex].DataBoundItem as Products;
                    if (product != null) AddProduct(product);
                }
            };
        }

        private void AddSelectedProducts()
        {
            var selected = dgvProducts.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as Products)
                .Where(p => p != null)
                .ToList();

            if (selected.Count > 0)
            {
                foreach (var p in selected) AddProduct(p);
            }
            else
            {
                MessageBox.Show("Vui lòng chọn ít nhất một sản phẩm!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void AddProduct(Products product)
        {
            string key = product.SKU ?? product.Name ?? "";

            // Cộng dồn số lượng trong tracking
            if (_addedQty.ContainsKey(key))
                _addedQty[key]++;
            else
                _addedQty[key] = 1;

            // Cập nhật cột SL trong grid
            dgvProducts.Invalidate();

            // Gửi sản phẩm với số lượng = 1 (tăng dần ở FrmConfig/FrmQuotation)
            var clone = CloneProduct(product);
            clone.SoLuong = 1;
            OnProductsSelected?.Invoke(new List<Products> { clone });
        }

        private Products CloneProduct(Products p)
        {
            return new Products
            {
                Id = p.Id, SheetRowIndex = p.SheetRowIndex,
                Name = p.Name, Model = p.Model, SKU = p.SKU,
                Price = p.Price, PriceCost = p.PriceCost,
                Weight = p.Weight, Length = p.Length, Width = p.Width, Height = p.Height,
                Category = p.Category, Type = p.Type, HÃNG = p.HÃNG,
                TrangThai = p.TrangThai, Pole = p.Pole, Ir = p.Ir, Icu = p.Icu,
                PriceList = p.PriceList, SoLuong = p.SoLuong,
                ExtraAttributes = new Dictionary<string, string>(p.ExtraAttributes)
            };
        }

        private void FilterData()
        {
            string search = txtSearch.Text.ToLower().Trim();
            string category = cboCategory.SelectedFullPath;

            var filtered = _allProducts.Where(p =>
                (string.IsNullOrEmpty(search) ||
                 (p.Name ?? "").ToLower().Contains(search) ||
                 (p.SKU ?? "").ToLower().Contains(search) ||
                 (p.Model ?? "").ToLower().Contains(search)) &&
                (string.IsNullOrEmpty(category) ||
                 (p.Category ?? "").StartsWith(category, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            dgvProducts.DataSource = filtered;
            
            // Gọi trực tiếp FormatGrid để vẽ lại cột & header ngay lập tức, tránh bị WinForms bỏ qua
            FormatGrid();
        }

        private void FormatGrid()
        {
            if (dgvProducts == null || dgvProducts.Columns.Count == 0) return;
            if (_isFormatting) return;
            
            _isFormatting = true;
            try
            {
                dgvProducts.EnableHeadersVisualStyles = false;
                dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.Yellow;
                dgvProducts.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
                dgvProducts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                
                // Ép kiểu tiêu đề
                dgvProducts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                dgvProducts.ColumnHeadersHeight = 35;
                dgvProducts.ColumnHeadersVisible = true;

                dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvProducts.RowHeadersVisible = false;
                dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvProducts.BorderStyle = BorderStyle.FixedSingle;
                dgvProducts.GridColor = Color.LightGray;
                dgvProducts.ReadOnly = true;
                dgvProducts.AllowUserToAddRows = false;
                dgvProducts.AllowUserToDeleteRows = false;
                dgvProducts.EditMode = DataGridViewEditMode.EditProgrammatically;

                // Ẩn/hiện cột – ẩn IsSelected (cột checkbox "Chọn")
                var visibleCols = new[] { "Name", "Model", "SKU", "Price", "PriceCost", "Category", "Type", "HÃNG" };
                foreach (DataGridViewColumn col in dgvProducts.Columns)
                {
                    if (col.Name == "STT_Col") { col.Visible = true; continue; }
                    col.Visible = visibleCols.Contains(col.Name);
                }

                // Thiết lập cấu trúc cột co giãn thông minh (Responsive)
                if (dgvProducts.Columns.Contains("Name"))
                {
                    var col = dgvProducts.Columns["Name"];
                    col.HeaderText = "Tên sản phẩm";
                    col.FillWeight = 80;
                    col.MinimumWidth = 200;
                }
                if (dgvProducts.Columns.Contains("Model"))
                {
                    var col = dgvProducts.Columns["Model"];
                    col.HeaderText = "Model";
                    col.FillWeight = 22;
                    col.MinimumWidth = 80;
                }
                if (dgvProducts.Columns.Contains("SKU"))
                {
                    var col = dgvProducts.Columns["SKU"];
                    col.HeaderText = "Mã SKU";
                    col.FillWeight = 22;
                    col.MinimumWidth = 80;
                }
                if (dgvProducts.Columns.Contains("Price"))
                {
                    var col = dgvProducts.Columns["Price"];
                    col.HeaderText = "Giá bán";
                    col.FillWeight = 22;
                    col.MinimumWidth = 80;
                    col.DefaultCellStyle.Format = "N0";
                }
                if (dgvProducts.Columns.Contains("PriceCost"))
                {
                    var col = dgvProducts.Columns["PriceCost"];
                    col.HeaderText = "Giá nhập";
                    col.FillWeight = 22;
                    col.MinimumWidth = 80;
                    col.DefaultCellStyle.Format = "N0";
                }
                if (dgvProducts.Columns.Contains("Category"))
                {
                    var col = dgvProducts.Columns["Category"];
                    col.HeaderText = "Danh mục";
                    col.FillWeight = 35;
                    col.MinimumWidth = 110;
                }
                if (dgvProducts.Columns.Contains("Type"))
                {
                    var col = dgvProducts.Columns["Type"];
                    col.HeaderText = "Type";
                    col.FillWeight = 18;
                    col.MinimumWidth = 70;
                }
                if (dgvProducts.Columns.Contains("HÃNG"))
                {
                    var col = dgvProducts.Columns["HÃNG"];
                    col.HeaderText = "Hãng";
                    col.FillWeight = 16;
                    col.MinimumWidth = 60;
                }

                // Thêm cột STT nếu chưa có
                if (!dgvProducts.Columns.Contains("STT_Col"))
                {
                    var sttCol = new DataGridViewTextBoxColumn
                    {
                        Name = "STT_Col", HeaderText = "STT",
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                        Width = 38,
                        Resizable = DataGridViewTriState.False,
                        ReadOnly = true
                    };
                    dgvProducts.Columns.Insert(0, sttCol);
                }



                // Đảm bảo tiêu đề luôn hiển thị sau khi cấu trúc cột thay đổi
                dgvProducts.ColumnHeadersVisible = true;

                // CellFormatting: điền STT và SL
                dgvProducts.CellFormatting -= DgvProducts_CellFormatting; // tránh đăng ký nhiều lần
                dgvProducts.CellFormatting += DgvProducts_CellFormatting;
            }
            finally
            {
                _isFormatting = false;
            }
        }

        private void DgvProducts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string colName = dgvProducts.Columns[e.ColumnIndex].Name;

            if (colName == "STT_Col")
            {
                e.Value = (e.RowIndex + 1).ToString();
                e.FormattingApplied = true;
            }

        }
    }
}
