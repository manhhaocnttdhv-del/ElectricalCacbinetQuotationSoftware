using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ECQ_Soft.Helper
{
    /// <summary>
    /// ComboBox tìm kiếm Danh mục (Category) tương tự như tìm sản phẩm.
    /// Cho phép gõ để lọc danh sách các danh mục có sẵn.
    /// </summary>
    public class CategorySearchDropdown : ComboBox
    {
        private List<string> _allCategories = new List<string>();
        private DataGridView _grid;
        private ToolStripControlHost _host;
        private ToolStripDropDown _dropDown;
        private Timer _typingTimer;
        private bool _suppressTextChange;

        public event EventHandler<string> SelectionChanged;

        public CategorySearchDropdown()
        {
            this.DropDownStyle = ComboBoxStyle.DropDown;
            this.DropDownHeight = 1; // Ẩn dropdown gốc

            _typingTimer = new Timer { Interval = 250 };
            _typingTimer.Tick += (s, e) => { _typingTimer.Stop(); SearchAndShowPopup(); };

            InitPopupGrid();
            this.TextChanged += (s, e) => { if (!_suppressTextChange) { _typingTimer.Stop(); _typingTimer.Start(); } };
        }

        private void InitPopupGrid()
        {
            _grid = new DataGridView
            {
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.None,
                ColumnHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 30 }
            };

            _grid.Columns.Add("colCat", "Danh mục");
            _grid.CellDoubleClick += (s, e) => SelectFromGrid(e.RowIndex);
            
            _host = new ToolStripControlHost(_grid) { AutoSize = false, Padding = Padding.Empty, Margin = Padding.Empty };
            _dropDown = new ToolStripDropDown { AutoClose = true, Padding = Padding.Empty };
            _dropDown.Items.Add(_host);
        }

        public void LoadData(IEnumerable<string> categories)
        {
            _allCategories = categories?.Distinct().OrderBy(c => c).ToList() ?? new List<string>();
        }

        protected override void OnDropDown(EventArgs e) { SearchAndShowPopup(); }
        protected override void OnClick(EventArgs e) { base.OnClick(e); if (!_dropDown.Visible) SearchAndShowPopup(); }

        private void SearchAndShowPopup()
        {
            if (_allCategories == null) return;
            string keyword = this.Text.Trim().ToLower();

            var results = string.IsNullOrEmpty(keyword)
                ? _allCategories.Take(100).ToList()
                : _allCategories.Where(c => c.ToLower().Contains(keyword)).Take(100).ToList();

            _grid.Rows.Clear();
            foreach (var cat in results) _grid.Rows.Add(cat);

            if (results.Count > 0)
            {
                int w = Math.Max(this.Width, 400);
                int h = Math.Min(results.Count, 10) * 30 + 2;
                _grid.Size = new Size(w, h);
                _host.Size = new Size(w, h);
                if (!_dropDown.Visible) _dropDown.Show(this, 0, this.Height + 2);
            }
            else _dropDown.Close();
        }

        private void SelectFromGrid(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _grid.Rows.Count) return;
            string val = _grid.Rows[rowIndex].Cells[0].Value?.ToString();
            if (val != null)
            {
                _suppressTextChange = true;
                this.Text = val;
                _suppressTextChange = false;
                _dropDown.Close();
                SelectionChanged?.Invoke(this, val);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_dropDown.Visible)
            {
                if (keyData == Keys.Down) { _grid.Focus(); return true; }
                if (keyData == Keys.Enter) { SelectFromGrid(_grid.CurrentRow?.Index ?? 0); return true; }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
