using ECQ_Soft.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ECQ_Soft.Helper
{
    /// <summary>
    /// Custom DropDown ComboBox dạng AutoComplete tích hợp DataGridView,
    /// Dùng để tìm kiếm và chọn sản phẩm thời gian thực (realtime).
    /// </summary>
    public class ProductSearchDropdown : ComboBox
    {
        private List<Products> _allProducts = new List<Products>();
        private DataGridView _grid;
        private ToolStripControlHost _host;
        private ToolStripDropDown _dropDown;
        private Timer _typingTimer;
        private bool _suppressTextChange;

        public Products SelectedProduct { get; private set; }
        public event EventHandler<Products> ProductSelected;

        public ProductSearchDropdown()
        {
            this.DropDownStyle = ComboBoxStyle.DropDown;
            this.DropDownHeight = 1; // Ẩn dropdown gốc của WinForms

            // Debounce timer (đợi user gõ xong khoảng 300ms mới search)
            _typingTimer = new Timer { Interval = 300 };
            _typingTimer.Tick += TypingTimer_Tick;

            InitPopupGrid();

            this.TextChanged += ProductSearchDropdown_TextChanged;
        }

        private void InitPopupGrid()
        {
            _grid = new DataGridView
            {
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, Font = new Font("Segoe UI", 9f), 
                BorderStyle = BorderStyle.None,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle 
                { 
                    BackColor = Color.FromArgb(220, 235, 255), 
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold), 
                    ForeColor = Color.FromArgb(30, 60, 130) 
                },
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 28 },
                ScrollBars = ScrollBars.Vertical
            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Tên sản phẩm", FillWeight = 40 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colModel", HeaderText = "Model", FillWeight = 20 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSKU", HeaderText = "SKU", FillWeight = 20 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "Thông số (Giá, D×R×C)", FillWeight = 20 });

            _grid.CellDoubleClick += Grid_CellDoubleClick;
            _grid.KeyDown += Grid_KeyDown;

            _host = new ToolStripControlHost(_grid) { AutoSize = false, Padding = Padding.Empty, Margin = Padding.Empty };
            _dropDown = new ToolStripDropDown { AutoClose = true, Padding = Padding.Empty };
            _dropDown.Items.Add(_host);
        }

        public void LoadData(List<Products> products)
        {
            _allProducts = products ?? new List<Products>();
        }

        protected override void OnDropDown(EventArgs e) 
        { 
            // Chặn dropdown mặc định
            SearchAndShowPopup();
        } 

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (!_dropDown.Visible) SearchAndShowPopup();
        }

        private void ProductSearchDropdown_TextChanged(object sender, EventArgs e)
        {
            if (_suppressTextChange) return;
            
            // Hủy timer cũ, khởi chạy timer mới khi gõ chữ
            _typingTimer.Stop();
            _typingTimer.Start();
        }

        private void TypingTimer_Tick(object sender, EventArgs e)
        {
            _typingTimer.Stop();
            
            // Xử lý tìm kiếm
            SearchAndShowPopup();
        }

        private void SearchAndShowPopup()
        {
            if (_allProducts == null || _allProducts.Count == 0) return;

            string keyword = this.Text.Trim().ToLower();
            List<Products> results;

            if (string.IsNullOrEmpty(keyword))
            {
                // Mặc định không nhập chữ thì gợi ý khoảng 150 sp gốc
                results = _allProducts.Take(150).ToList();
            }
            else
            {
                // Lọc
                results = _allProducts.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(keyword)) ||
                    (p.SKU != null && p.SKU.ToLower().Contains(keyword)) ||
                    (p.Model != null && p.Model.ToLower().Contains(keyword))
                ).Take(150).ToList();
            }

            _grid.Rows.Clear();
            foreach (var p in results)
            {
                decimal.TryParse(p.Price?.Replace(".", "").Replace(",", ""), out decimal price);
                
                string sizeStr = "";
                bool hasL = decimal.TryParse(p.Length, out decimal L) && L > 0;
                bool hasW = decimal.TryParse(p.Width,  out decimal W) && W > 0;
                bool hasH = decimal.TryParse(p.Height, out decimal H) && H > 0;
                if (hasL || hasW || hasH) sizeStr = $"{(hasL?L.ToString("0.##"):"?")}×{(hasW?W.ToString("0.##"):"?")}×{(hasH?H.ToString("0.##"):"?")}";

                string priceCol = price > 0 ? price.ToString("N0") + "₫" : "";
                if (!string.IsNullOrEmpty(sizeStr)) priceCol += $" | {sizeStr}";

                int idx = _grid.Rows.Add(p.Name, p.Model, p.SKU, priceCol);
                _grid.Rows[idx].Tag = p;
            }

            if (results.Count > 0)
            {
                // Tính toán chiều cao hiển thị phù hợp
                int popupWidth = Math.Max(this.Width, 680); // Rộng tối thiểu 680px để đọc thông số
                int rowCount = Math.Min(results.Count, 12); // Tối đa hiển thị 12 dòng rồi cuộn
                int popupHeight = _grid.ColumnHeadersHeight + (rowCount * _grid.RowTemplate.Height) + 4;

                _grid.Size = new Size(popupWidth, popupHeight);
                _host.Size = new Size(popupWidth, popupHeight);
                
                if (!_dropDown.Visible && !this.IsDisposed)
                {
                    // Lệch phải 1 chút nếu muốn
                    _dropDown.Show(this, 0, this.Height + 2);
                }
            }
            else
            {
                _dropDown.Close();
            }
        }

        private void SelectProductFromGrid(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _grid.Rows.Count) return;

            var p = _grid.Rows[rowIndex].Tag as Products;
            if (p != null)
            {
                SelectedProduct = p;

                _suppressTextChange = true;
                this.Text = p.Name;
                _suppressTextChange = false;

                _dropDown.Close();
                ProductSelected?.Invoke(this, p);
            }
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            SelectProductFromGrid(e.RowIndex);
        }

        // Bắt Enter và trỏ xuống Grid
        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && _grid.CurrentRow != null)
            {
                SelectProductFromGrid(_grid.CurrentRow.Index);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_dropDown.Visible)
            {
                if (keyData == Keys.Down)
                {
                    _grid.Focus();
                    if (_grid.Rows.Count > 0 && _grid.CurrentRow == null)
                        _grid.CurrentCell = _grid.Rows[0].Cells[0];
                    return true;
                }
                else if (keyData == Keys.Enter)
                {
                    if (_grid.Rows.Count > 0)
                    {
                        SelectProductFromGrid(_grid.CurrentRow != null ? _grid.CurrentRow.Index : 0);
                        return true;
                    }
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
