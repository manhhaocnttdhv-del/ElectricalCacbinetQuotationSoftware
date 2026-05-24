using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ECQ_Soft.Model;

namespace ECQ_Soft
{
    public partial class FrmSavePackage : Form
    {
        public string SheetName { get; private set; }
        public string ConfigName { get; private set; }
        public bool IsOverwrite { get; private set; }

        // Map: displayLabel → actualSheetName (empty string = create new)
        private Dictionary<string, string> _sheetDisplayMap;
        private List<ConfigProductItem> _currentItems;

        public FrmSavePackage(List<ConfigProductItem> items, Dictionary<string, string> sheetDisplayMap, string defaultSheetDisplay = null, string defaultConfigName = null)
        {
            InitializeComponent();
            _currentItems = items;
            _sheetDisplayMap = sheetDisplayMap ?? new Dictionary<string, string>();

            // Setup Preview Grid
            dgvPreview.DataSource = new System.ComponentModel.BindingList<ConfigProductItem>(_currentItems);
            FormatPreviewGrid();

            // Populate dropdown chỉ với tên sheet (không lặp)
            cmbSheetName.Items.Clear();
            var uniqueSheets = _sheetDisplayMap.Values
                .Where(v => !string.IsNullOrEmpty(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v)
                .ToList();
            foreach (var sName in uniqueSheets)
                cmbSheetName.Items.Add(sName);

            // Mặc định chọn sheet tương ứng với defaultSheetDisplay
            if (!string.IsNullOrEmpty(defaultSheetDisplay) && _sheetDisplayMap.TryGetValue(defaultSheetDisplay, out string defaultSheet)
                && !string.IsNullOrEmpty(defaultSheet))
            {
                cmbSheetName.SelectedItem = defaultSheet;
                chkOverwrite.Checked = true; // Nếu load từ search thì mặc định là ghi đè
            }
            else
            {
                cmbSheetName.Text = "Donggoi_";
            }

            if (!string.IsNullOrEmpty(defaultConfigName))
            {
                txtConfigName.Text = defaultConfigName;
            }

            lblNote.Text = "Lưu ý: Chọn sheet có sẵn hoặc nhập tên mới (sẽ tự thêm tiền tố Donggoi_).";
        }

        private void FormatPreviewGrid()
        {
            dgvPreview.ReadOnly = true;
            dgvPreview.AllowUserToAddRows = false;
            dgvPreview.RowHeadersVisible = false;
            dgvPreview.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPreview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            foreach (DataGridViewColumn col in dgvPreview.Columns)
            {
                if (col.Name == "TenHang") col.HeaderText = "Tên sản phẩm";
                else if (col.Name == "MaHang") col.HeaderText = "Mã SKU";
                else if (col.Name == "SoLuong") col.HeaderText = "SL";
                else col.Visible = false;
            }
            if (dgvPreview.Columns.Contains("TenHang")) dgvPreview.Columns["TenHang"].FillWeight = 200;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string inputText = cmbSheetName.Text.Trim();
            string cName = txtConfigName.Text.Trim();

            if (string.IsNullOrEmpty(inputText))
            {
                MessageBox.Show("Vui lòng chọn hoặc nhập tên Sheet.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(cName))
            {
                MessageBox.Show("Vui lòng nhập tên cho cấu hình (ví dụ: tủ điện).", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Resolve sheet name: dropdown giờ chứa tên sheet thực, hoặc user tự gõ
            string resolvedSheet;
            bool isExistingSheet = _sheetDisplayMap.Values
                .Any(v => string.Equals(v, inputText, StringComparison.OrdinalIgnoreCase));

            if (isExistingSheet)
            {
                resolvedSheet = inputText;
            }
            else
            {
                // User tự gõ → đảm bảo có tiền tố Donggoi_
                resolvedSheet = inputText.StartsWith("Donggoi_") ? inputText : "Donggoi_" + inputText;
            }

            SheetName = resolvedSheet;
            ConfigName = cName;
            IsOverwrite = chkOverwrite.Checked;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void CmbSheetName_TextChanged(object sender, EventArgs e)
        {
            UpdateValidation();
        }

        private void TxtConfigName_TextChanged(object sender, EventArgs e)
        {
            UpdateValidation();
        }

        private void UpdateValidation()
        {
            lblStatus.Text = "";
            lblStatus.ForeColor = Color.DimGray;

            string inputText = cmbSheetName.Text.Trim();
            if (string.IsNullOrEmpty(inputText)) return;

            // Kiểm tra xem tên đang nhập/chọn có phải sheet đã tồn tại không
            string resolvedName = inputText.StartsWith("Donggoi_") ? inputText : "Donggoi_" + inputText;
            bool sheetExists = _sheetDisplayMap.Values.Any(v => string.Equals(v, resolvedName, StringComparison.OrdinalIgnoreCase))
                            || _sheetDisplayMap.Values.Any(v => string.Equals(v, inputText, StringComparison.OrdinalIgnoreCase));

            if (sheetExists)
            {
                string displayName = _sheetDisplayMap.Values.FirstOrDefault(v =>
                    string.Equals(v, inputText, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(v, resolvedName, StringComparison.OrdinalIgnoreCase)) ?? inputText;
                lblStatus.Text = $"Sheet \"{displayName}\" đã tồn tại. Sẽ nối thêm hoặc ghi đè nhóm.";
                lblStatus.ForeColor = Color.DarkOrange;
            }
            else
            {
                lblStatus.Text = $"Sheet mới \"{resolvedName}\" sẽ được tạo.";
                lblStatus.ForeColor = Color.SeaGreen;
            }
        }
    }
}
