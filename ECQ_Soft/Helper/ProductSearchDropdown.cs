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
        private bool _isSelfFocusing;

        public Products SelectedProduct { get; private set; }
        public event EventHandler<Products> ProductSelected;

        public ProductSearchDropdown()
        {
            this.DropDownStyle = ComboBoxStyle.DropDown;
            this.AutoCompleteMode = AutoCompleteMode.None;
            this.AutoCompleteSource = AutoCompleteSource.None;
            this.DropDownHeight = 1; // Ẩn dropdown gốc của WinForms

            _typingTimer = new Timer { Interval = 300 }; // Tăng lên 300ms để gõ mượt hơn, tránh reset liên tục
            _typingTimer.Tick += TypingTimer_Tick;

            // VÔ HIỆU HÓA bôi đen tự động khi nhấn vào ô, giữ vị trí con trỏ hiện tại
            this.GotFocus += (s, ev) => 
            {
                // Chỉ reset con trỏ về cuối nếu text vừa được set mới (SelectionLength == Text.Length)
                // và chúng ta KHÔNG đang gõ (nếu đang gõ SelectionLength thường là 0)
                if (this.SelectionLength > 0 && this.SelectionLength == this.Text.Length)
                {
                    this.BeginInvoke(new Action(() => {
                        this.SelectionStart = this.Text.Length;
                        this.SelectionLength = 0;
                    }));
                }
            };

            InitPopupGrid();

            this.TextChanged += ProductSearchDropdown_TextChanged;
            this.Leave += (s, e) => { if (!_dropDown.Focused && !_grid.Focused) _dropDown.Close(); };

            // Tự đóng khi ComboBox bị di chuyển (ví dụ do kéo splitter hoặc cuộn trang)
            this.LocationChanged += (s, e) => { if (_dropDown.Visible) _dropDown.Close(); };
            this.ParentChanged += (s, e) => SubscribeToParentEvents();
            this.HandleCreated += (s, e) => SubscribeToParentEvents();
        }

        private bool _eventsSubscribed = false;
        private void SubscribeToParentEvents()
        {
            if (_eventsSubscribed) return;
            _eventsSubscribed = true;
            Control p = this.Parent;
            while (p != null)
            {
                // Sử dụng Move thay vì LocationChanged cho chắc chắn
                p.Move += (s, ev) => { if (_dropDown.Visible) _dropDown.Close(); };
                if (p is ScrollableControl sc)
                {
                    sc.Scroll += (s, ev) => { if (_dropDown.Visible) _dropDown.Close(); };
                }
                
                // Bắt sự kiện lăn chuột
                p.MouseWheel += (s, ev) => { if (_dropDown.Visible) _dropDown.Close(); };
                
                p = p.Parent;
            }
            
            // Đóng khi Form di chuyển/resize
            var form = this.FindForm();
            if (form != null)
            {
                form.LocationChanged += (s, ev) => { if (_dropDown.Visible) _dropDown.Close(); };
                form.Resize += (s, ev) => { if (_dropDown.Visible) _dropDown.Close(); };
            }
        }

        private void InitPopupGrid()
        {
            _grid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                Font = new Font("Segoe UI", 9f),
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
            
            // Bật DoubleBuffered để mượt hơn, giảm flickering khi lọc dữ liệu
            typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(_grid, true, null);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Tên sản phẩm", FillWeight = 40 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colModel", HeaderText = "Model", FillWeight = 20 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSKU", HeaderText = "SKU", FillWeight = 20 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "Thông số (Giá, D×R×C)", FillWeight = 20 });

            _grid.CellDoubleClick += Grid_CellDoubleClick;
            _grid.KeyDown += Grid_KeyDown;

            _host = new ToolStripControlHost(_grid) { 
                AutoSize = false, 
                Padding = Padding.Empty, 
                Margin = Padding.Empty
            };
            _dropDown = new GhostDropDown { Padding = Padding.Empty };
            _dropDown.Items.Add(_host);
        }

        public void LoadData(List<Products> products)
        {
            _allProducts = products ?? new List<Products>();
        }

        protected override void OnDropDown(EventArgs e)
        {
            // KHÔNG gọi SearchAndShowPopup ở đây để tránh hiện popup khi chỉ nhấn nút tam giác
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            // Không SelectAll() ở đây vì sẽ làm mất vị trí con trỏ khi user click vào để sửa
            if (!_dropDown.Visible) SearchAndShowPopup();
        }

        private void ProductSearchDropdown_TextChanged(object sender, EventArgs e)
        {
            if (_suppressTextChange) return;

            // Hủy timer cũ
            _typingTimer.Stop();

            if (string.IsNullOrWhiteSpace(this.Text))
            {
                SelectedProduct = null;
                ProductSelected?.Invoke(this, null);
                _dropDown.Close();
                return;
            }
            else
            {
                // Nếu đang gõ chữ thì đợi 300ms rồi mới search để tránh giật lag
                _typingTimer.Start();
            }
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
                // Mặc định gợi ý sản phẩm gốc
                results = _allProducts.Take(100).ToList();
                SelectedProduct = null;
            }
            else
            {
                // Tách từ khóa và tìm kiếm đa trường (Name, SKU, Model) cực nhạy
                var tokens = keyword.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                results = _allProducts.Where(p =>
                {
                    string searchable = $"{(p.Name ?? "")} {(p.SKU ?? "")} {(p.Model ?? "")}".ToLower();
                    return tokens.All(t => searchable.Contains(t));
                }).Take(100).ToList();
            }

            _grid.SuspendLayout();
            try
            {
                _grid.Rows.Clear();
                foreach (var p in results)
                {
                    decimal.TryParse(p.Price?.Replace(".", "").Replace(",", ""), out decimal price);

                    string sizeStr = "";
                    bool hasL = decimal.TryParse(p.Length, out decimal L) && L > 0;
                    bool hasW = decimal.TryParse(p.Width, out decimal W) && W > 0;
                    bool hasH = decimal.TryParse(p.Height, out decimal H) && H > 0;
                    if (hasL || hasW || hasH) sizeStr = $"{(hasL ? L.ToString("0.##") : "?")}×{(hasW ? W.ToString("0.##") : "?")}×{(hasH ? H.ToString("0.##") : "?")}";

                    string priceCol = price > 0 ? price.ToString("N0") + "₫" : "";
                    if (!string.IsNullOrEmpty(sizeStr)) priceCol += $" | {sizeStr}";

                    int idx = _grid.Rows.Add(p.Name, p.Model, p.SKU, priceCol);
                    _grid.Rows[idx].Tag = p;
                }
            }
            finally
            {
                _grid.ResumeLayout();
            }

            if (results.Count > 0)
            {
                int popupWidth = Math.Max(this.Width, 850); 
                int rowCount = Math.Min(results.Count, 12); 
                int popupHeight = _grid.ColumnHeadersHeight + (rowCount * _grid.RowTemplate.Height) + 4;

                _grid.Size = new Size(popupWidth, popupHeight);
                _host.Size = new Size(popupWidth, popupHeight);

                if (!this.IsDisposed)
                {
                    // LƯU VỊ TRÍ CON TRỎ TRƯỚC KHI HIỂN THỊ DROPDOWN
                    int selStart = this.SelectionStart;
                    int selLen = this.SelectionLength;

                    if (!_dropDown.Visible)
                    {
                        Point screenPos = this.PointToScreen(new Point(0, this.Height));
                        _dropDown.Show(screenPos);
                        
                        // Đảm bảo ComboBox vẫn giữ focus sau khi hiện dropdown (để gõ tiếp được)
                        if (!this.Focused) this.Focus();
                    }
                    else
                    {
                        _dropDown.Size = new Size(popupWidth, popupHeight);
                    }

                    // KHÔI PHỤC VỊ TRÍ CON TRỎ để không bị nhảy khi dropdown hiện ra
                    if (this.SelectionStart != selStart || this.SelectionLength != selLen)
                    {
                        this.SelectionStart = selStart;
                        this.SelectionLength = selLen;
                    }
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
            if (p != null) HandleProductSelection(p);
        }

        private void HandleProductSelection(Products p)
        {
            SelectedProduct = p;

            _suppressTextChange = true;
            this.Text = p.Name;
            _suppressTextChange = false;

            _dropDown.Close();
            ProductSelected?.Invoke(this, p);
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            SelectProductFromGrid(e.RowIndex);
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && _grid.CurrentRow != null)
            {
                SelectProductFromGrid(_grid.CurrentRow.Index);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }


        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            // Cho phép các phím điều hướng tác động vào ComboBox
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Enter)
            {
                e.IsInputKey = true;
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {

            if (_dropDown.Visible)
            {
                if (e.KeyCode == Keys.Down)
                {
                    if (_grid.Rows.Count > 0)
                    {
                        _grid.Focus();
                        int nextIdx = (_grid.SelectedRows.Count > 0) ? _grid.SelectedRows[0].Index + 1 : 0;
                        if (nextIdx < _grid.Rows.Count)
                        {
                            _grid.Rows[nextIdx].Selected = true;
                            _grid.CurrentCell = _grid.Rows[nextIdx].Cells[0];
                        }
                    }
                    e.Handled = true;
                    return;
                }
                if (e.KeyCode == Keys.Up)
                {
                    if (_grid.Rows.Count > 0 && _grid.SelectedRows.Count > 0)
                    {
                        int prevIdx = _grid.SelectedRows[0].Index - 1;
                        if (prevIdx >= 0)
                        {
                            _grid.Rows[prevIdx].Selected = true;
                            _grid.CurrentCell = _grid.Rows[prevIdx].Cells[0];
                        }
                    }
                    e.Handled = true;
                    return;
                }
                if (e.KeyCode == Keys.Enter)
                {
                    if (_grid.SelectedRows.Count > 0)
                    {
                        var p = _grid.SelectedRows[0].Tag as Products;
                        if (p != null) HandleProductSelection(p);
                    }
                    e.Handled = true;
                    return;
                }
                if (e.KeyCode == Keys.Escape)
                {
                    _dropDown.Close();
                    e.Handled = true;
                    return;
                }
            }
            base.OnKeyDown(e);
        }
    }

    /// <summary>
    /// DropDown tùy chỉnh không cướp tiêu điểm (Focus) khi hiển thị.
    /// Giúp việc gõ phím và xóa (Backspace) cực kỳ mượt mà.
    /// </summary>
    internal class GhostDropDown : ToolStripDropDown
    {
        public GhostDropDown()
        {
            this.AutoClose = false; // Tắt tự động đóng để không cướp focus ngầm
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }
    }
}
