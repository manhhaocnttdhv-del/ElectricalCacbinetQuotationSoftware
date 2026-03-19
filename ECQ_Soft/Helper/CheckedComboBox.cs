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
        public event EventHandler Confirmed;

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
            _checkedListBox.IntegralHeight = false; // Tắt làm tròn để co giãn khít hơn
            _checkedListBox.ItemCheck += CheckedListBox_ItemCheck;

            _btnConfirm = new Button();
            _btnConfirm.Text = "Xác nhận";
            _btnConfirm.Dock = DockStyle.Bottom;
            _btnConfirm.Height = 30;
            _btnConfirm.FlatStyle = FlatStyle.Flat;
            _btnConfirm.BackColor = Color.FromArgb(0, 122, 204);
            _btnConfirm.ForeColor = Color.White;
            _btnConfirm.Cursor = Cursors.Hand;
            _btnConfirm.Click += (s, e) => 
            {
                Confirmed?.Invoke(this, EventArgs.Empty);
                _dropDown.Close();
            };

            _container = new Panel();
            _container.BorderStyle = BorderStyle.FixedSingle;
            _container.Controls.Add(_checkedListBox);
            _container.Controls.Add(_btnConfirm);

            _controlHost = new ToolStripControlHost(_container);
            _controlHost.Padding = Padding.Empty;
            _controlHost.Margin = Padding.Empty;
            _controlHost.AutoSize = false;

            _dropDown = new ToolStripDropDown();
            _dropDown.Padding = Padding.Empty;
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
            this.BeginInvoke(new MethodInvoker(UpdateText));
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
            int itemHeight = _checkedListBox.ItemHeight;
            int count = _checkedListBox.Items.Count;
            
            // 1. Tính toán chiều rộng động dựa trên nội dung dài nhất
            int maxWidth = this.Width; // Tối thiểu bằng chiều rộng combo box
            using (Graphics g = _checkedListBox.CreateGraphics())
            {
                foreach (var item in _checkedListBox.Items)
                {
                    // Lấy chiều rộng của text + khoảng đệm cho checkbox (25px) + lề (10px)
                    int itemWidth = (int)g.MeasureString(item.ToString(), _checkedListBox.Font).Width + 35;
                    if (itemWidth > maxWidth) maxWidth = itemWidth;
                }
            }
            // Nếu có scrollbar thì cộng thêm 20px nữa
            if (count > 10) maxWidth += 20;
            // Giới hạn chiều rộng tối đa để không tràn màn hình (ví dụ 500px)
            maxWidth = Math.Min(maxWidth, 600);

            // 2. Tính toán chiều cao (tối đa 10 mục)
            int displayCount = Math.Min(count, 10);
            int listHeight = (displayCount * itemHeight) + 12; // Tăng đệm lên 12px
            if (count == 0) listHeight = 25;

            int finalHeight = listHeight + 35; // Tăng chiều cao vùng nút lên 35px

            _controlHost.Size = new Size(maxWidth, finalHeight);
            _dropDown.Show(this, 0, this.Height);
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
