using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ECQ_Soft.Helper
{
    public class CheckedComboBox : ComboBox
    {
        private CheckedListBox _checkedListBox;
        private ToolStripControlHost _controlHost;
        private ToolStripDropDown _dropDown;
        private Panel _container;
        private Button _btnConfirm;
        private CheckBox _chkSelectAll;
        public event EventHandler Confirmed;

        private bool _isUpdatingSelectAll = false;

        private bool _isUpdatingText = false;
        private const int CB_SETCUEBANNER = 0x1703;
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lParam);

        private string _placeholder = "-- Chọn cấu hình --";
        public string Placeholder 
        { 
            get => _placeholder;
            set 
            {
                _placeholder = value;
                UpdatePlaceholder();
            }
        }

        public CheckedListBox.CheckedItemCollection CheckedItems => _checkedListBox.CheckedItems;
        public CheckedListBox.ObjectCollection Items => _checkedListBox.Items;

        public CheckedComboBox()
        {
            _checkedListBox = new CheckedListBox();
            _checkedListBox.CheckOnClick = true;
            _checkedListBox.BorderStyle = BorderStyle.None;
            _checkedListBox.Dock = DockStyle.Fill;
            _checkedListBox.IntegralHeight = false;
            _checkedListBox.ItemCheck += CheckedListBox_ItemCheck;
            _checkedListBox.Font = new Font("Segoe UI", 9.5f);
            _checkedListBox.BackColor = Color.White;
            _checkedListBox.Cursor = Cursors.Hand;

            _btnConfirm = new Button();
            _btnConfirm.Text = "Xác nhận";
            _btnConfirm.Dock = DockStyle.Fill;
            _btnConfirm.FlatStyle = FlatStyle.Flat;
            _btnConfirm.FlatAppearance.BorderSize = 0;
            _btnConfirm.BackColor = Color.FromArgb(0, 120, 215);
            _btnConfirm.ForeColor = Color.White;
            _btnConfirm.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _btnConfirm.Cursor = Cursors.Hand;
            _btnConfirm.Click += (s, e) => 
            {
                Confirmed?.Invoke(this, EventArgs.Empty);
                _dropDown.Close();
            };

            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Color.WhiteSmoke };
            pnlBottom.Padding = new Padding(10, 8, 10, 8);
            pnlBottom.Controls.Add(_btnConfirm);
            Panel sepBottom = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230, 230, 230) };
            pnlBottom.Controls.Add(sepBottom);

            _chkSelectAll = new CheckBox();
            _chkSelectAll.Text = "Chọn tất cả";
            _chkSelectAll.Dock = DockStyle.Fill;
            _chkSelectAll.Padding = new Padding(12, 0, 0, 0);
            _chkSelectAll.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _chkSelectAll.ForeColor = Color.FromArgb(30, 30, 30);
            _chkSelectAll.BackColor = Color.White;
            _chkSelectAll.Cursor = Cursors.Hand;
            _chkSelectAll.CheckedChanged += ChkSelectAll_CheckedChanged;

            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.White };
            pnlTop.Controls.Add(_chkSelectAll);
            Panel sepTop = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(230, 230, 230) };
            pnlTop.Controls.Add(sepTop);

            Panel pnlMiddle = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlMiddle.Padding = new Padding(10, 6, 10, 6);
            pnlMiddle.Controls.Add(_checkedListBox);

            _container = new Panel();
            _container.BackColor = Color.White;
            _container.BorderStyle = BorderStyle.None;
            // The order of adding determines Z-order for docking.
            // Controls added first have highest Z-order (Fill takes remaining space)
            _container.Controls.Add(pnlMiddle);
            _container.Controls.Add(pnlTop);
            _container.Controls.Add(pnlBottom);

            _container.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, _container.ClientRectangle, Color.FromArgb(204, 204, 204), ButtonBorderStyle.Solid);
            };

            _controlHost = new ToolStripControlHost(_container);
            _controlHost.Padding = Padding.Empty;
            _controlHost.Margin = Padding.Empty;
            _controlHost.AutoSize = false;

            _dropDown = new ToolStripDropDown();
            _dropDown.Padding = Padding.Empty;
            _dropDown.DropShadowEnabled = true;
            _dropDown.Items.Add(_controlHost);

            this.DropDownHeight = 1;
            this.DropDownStyle = ComboBoxStyle.DropDown;
            this.KeyPress += (s, e) => e.Handled = true;
            
            this.HandleCreated += (s, e) => UpdatePlaceholder();
            UpdateText();
        }

        private void UpdatePlaceholder()
        {
            if (this.IsHandleCreated && !string.IsNullOrEmpty(_placeholder))
            {
                SendMessage(this.Handle, CB_SETCUEBANNER, 0, _placeholder);
            }
        }

        private void CheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            this.BeginInvoke(new MethodInvoker(() => 
            {
                UpdateText();
                UpdateSelectAllState();
            }));
        }

        private void UpdateSelectAllState()
        {
            if (_isUpdatingSelectAll) return;
            _isUpdatingSelectAll = true;
            if (_checkedListBox.Items.Count > 0)
            {
                _chkSelectAll.Checked = _checkedListBox.CheckedItems.Count == _checkedListBox.Items.Count;
            }
            else
            {
                _chkSelectAll.Checked = false;
            }
            _isUpdatingSelectAll = false;
        }

        private void ChkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            if (_isUpdatingSelectAll) return;
            
            bool isChecked = _chkSelectAll.Checked;
            _isUpdatingText = true; // Ngăn không cho tính toán lại text cho từng mục
            _isUpdatingSelectAll = true; // Tránh đệ quy

            for (int i = 0; i < _checkedListBox.Items.Count; i++)
            {
                _checkedListBox.SetItemChecked(i, isChecked);
            }

            _isUpdatingSelectAll = false;
            _isUpdatingText = false;
            UpdateText();
        }

        private void UpdateText()
        {
            if (_isUpdatingText) return;
            _isUpdatingText = true;

            var checkedItems = _checkedListBox.CheckedItems.Cast<object>().Select(x => x.ToString()).ToList();
            // Nếu có chọn thì hiện danh sách, nếu không chọn thì để trống để Cue Banner hiện lên
            string newText = checkedItems.Count > 0 ? string.Join(", ", checkedItems) : "";
            
            if (this.Text != newText)
            {
                this.Text = newText;
            }

            _isUpdatingText = false;
        }

        protected override void OnDropDown(EventArgs e)
        {
            // Chặn dropdown mặc định
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            ShowDropDown();
        }

        public void ShowDropDown()
        {
            int itemHeight = _checkedListBox.ItemHeight > 0 ? _checkedListBox.ItemHeight : 18;
            int count = _checkedListBox.Items.Count;
            
            // 1. Tính toán chiều rộng động
            int maxWidth = this.Width;
            using (Graphics g = _checkedListBox.CreateGraphics())
            {
                foreach (var item in _checkedListBox.Items)
                {
                    // Lấy chiều rộng của text + checkbox width + padding
                    int itemWidth = (int)g.MeasureString(item.ToString(), _checkedListBox.Font).Width + 50;
                    if (itemWidth > maxWidth) maxWidth = itemWidth;
                }
            }
            if (count > 10) maxWidth += 20; // Scrollbar
            maxWidth = Math.Min(maxWidth, 600);

            // 2. Tính toán chiều cao
            int displayCount = Math.Min(count, 10);
            int listHeight = (displayCount * itemHeight) + 12; // + padding top/bottom
            if (count == 0) listHeight = 30;

            // finalHeight = middle(listHeight) + top(36) + bottom(45)
            int finalHeight = listHeight + 36 + 45;

            _controlHost.Size = new Size(maxWidth, finalHeight);
            _dropDown.Show(this, 0, this.Height + 2); // Show slightly below the combobox
        }

        // Hỗ trợ thêm các phương thức tiện ích
        public void AddItem(object item)
        {
            _checkedListBox.Items.Add(item);
        }

        public void ClearItems()
        {
            _checkedListBox.Items.Clear();
            this.Text = "";
        }

        public void SetItemChecked(int index, bool isChecked)
        {
            if (index >= 0 && index < _checkedListBox.Items.Count)
            {
                _checkedListBox.SetItemChecked(index, isChecked);
                UpdateText();
            }
        }
    }
}
