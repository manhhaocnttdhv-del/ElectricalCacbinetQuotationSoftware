using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ECQ_Soft
{
    public partial class FrmSheetSelector : Form
    {
        private readonly string _spreadsheetId;
        private SheetsService _sheetsService;

        // Lưu sheetId theo tên để dùng khi format màu
        private readonly Dictionary<string, int> _sheetIdMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public string SelectedSheetName { get; private set; }

        public FrmSheetSelector(string spreadsheetId, SheetsService sheetsService)
        {
            InitializeComponent();
            _spreadsheetId = spreadsheetId;
            _sheetsService = sheetsService;
        }

        private async void FrmSheetSelector_Load(object sender, EventArgs e)
        {
            // Mặc định chọn "Dùng tab cũ"
            rdoExisting.Checked = true;
            UpdateUIMode();

            // Gán sự kiện giả placeholder
            txtNewName.GotFocus += TxtNewName_GotFocus;
            txtNewName.LostFocus += TxtNewName_LostFocus;

            await LoadExistingSheetsAsync();
        }

        private async Task LoadExistingSheetsAsync()
        {
            try
            {
                lblStatus.Text = "Đang tải danh sách tab...";
                cboExisting.Enabled = false;

                var spreadsheet = await _sheetsService.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();

                _sheetIdMap.Clear();
                cboExisting.Items.Clear();

                // Các tab hệ thống cần ẩn khỏi danh sách chọn
                var hiddenTabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Products_Table", "Products_Relatation"
                };

                var sheetNames = spreadsheet.Sheets
                    .Where(s => !hiddenTabs.Contains(s.Properties.Title))
                    .OrderBy(s => s.Properties.Title)
                    .ToList();

                foreach (var sheet in sheetNames)
                {
                    cboExisting.Items.Add(sheet.Properties.Title);
                    _sheetIdMap[sheet.Properties.Title] = sheet.Properties.SheetId.Value;
                }

                if (cboExisting.Items.Count > 0)
                    cboExisting.SelectedIndex = 0;

                cboExisting.Enabled = true;
                lblStatus.Text = $"Tìm thấy {sheetNames.Count} tab.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Lỗi khi tải danh sách tab.";
                MessageBox.Show("Không thể tải danh sách tab từ Google Sheets:\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void rdoNew_CheckedChanged(object sender, EventArgs e) => UpdateUIMode();
        private void rdoExisting_CheckedChanged(object sender, EventArgs e) => UpdateUIMode();

        private const string PLACEHOLDER = "Nhập tên tab mới...";

        private void UpdateUIMode()
        {
            bool isNew = rdoNew.Checked;
            txtNewName.Visible = isNew;
            lblNewName.Visible = isNew;
            cboExisting.Visible = !isNew;
            lblExisting.Visible = !isNew;
        }

        private void TxtNewName_GotFocus(object sender, EventArgs e)
        {
            if (txtNewName.Text == PLACEHOLDER && txtNewName.ForeColor == System.Drawing.Color.Gray)
            {
                txtNewName.Text = "";
                txtNewName.ForeColor = System.Drawing.Color.Black;
            }
        }

        private void TxtNewName_LostFocus(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNewName.Text))
            {
                txtNewName.Text = PLACEHOLDER;
                txtNewName.ForeColor = System.Drawing.Color.Gray;
            }
        }

        private string GetNewTabName()
        {
            string val = txtNewName.Text.Trim();
            return val == PLACEHOLDER ? "" : val;
        }

        private async void btnConfirm_Click(object sender, EventArgs e)
        {
            if (rdoNew.Checked)
            {
                string newName = GetNewTabName();
                if (string.IsNullOrEmpty(newName))
                {
                    MessageBox.Show("Vui lòng nhập tên tab mới!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kiểm tra xem tên đã tồn tại chưa
                if (cboExisting.Items.Cast<string>().Any(s =>
                    s.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                {
                    var result = MessageBox.Show(
                        $"Tab '{newName}' đã tồn tại. Bạn có muốn dùng tab này không?",
                        "Tab đã tồn tại", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        SelectedSheetName = newName;
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    return;
                }

                // Tạo sheet mới
                try
                {
                    btnConfirm.Enabled = false;
                    lblStatus.Text = $"Đang tạo tab '{newName}'...";

                    // 1. Tạo sheet mới và lấy sheetId
                    var addSheetRequest = new Google.Apis.Sheets.v4.Data.AddSheetRequest
                    {
                        Properties = new Google.Apis.Sheets.v4.Data.SheetProperties
                        {
                            Title = newName
                        }
                    };

                    var batchRequest = new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest
                    {
                        Requests = new List<Google.Apis.Sheets.v4.Data.Request>
                        {
                            new Google.Apis.Sheets.v4.Data.Request { AddSheet = addSheetRequest }
                        }
                    };

                    var batchResponse = await _sheetsService.Spreadsheets.BatchUpdate(batchRequest, _spreadsheetId).ExecuteAsync();
                    int newSheetId = batchResponse.Replies[0].AddSheet.Properties.SheetId.Value;

                    // Ghi headers dùng method chung
                    lblStatus.Text = "Đang tạo tiêu đề...";
                    await EnsureHeadersAsync(newName, newSheetId);

                    SelectedSheetName = newName;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    btnConfirm.Enabled = true;
                    lblStatus.Text = "Lỗi khi tạo tab mới.";
                    MessageBox.Show("Không thể tạo tab mới:\n" + ex.Message,
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            else // dùng tab cũ
            {
                if (cboExisting.SelectedItem == null)
                {
                    MessageBox.Show("Vui lòng chọn một tab!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string existingName = cboExisting.SelectedItem.ToString();
                try
                {
                    btnConfirm.Enabled = false;
                    lblStatus.Text = "Đang kiểm tra tab...";

                    // Nếu tab đang rỗng thì tự động thêm header
                    if (_sheetIdMap.TryGetValue(existingName, out int sid))
                        await EnsureHeadersAsync(existingName, sid);

                    SelectedSheetName = existingName;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    btnConfirm.Enabled = true;
                    lblStatus.Text = "Lỗi.";
                    MessageBox.Show("Lỗi khi kiểm tra tab:\n" + ex.Message,
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Ghi dòng tiêu đề vào hàng 1 nếu sheet đang rỗng.
        /// </summary>
        private async Task EnsureHeadersAsync(string sheetName, int sheetId)
        {
            // Kiểm tra A1 có dữ liệu chưa
            var checkRequest = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, $"'{sheetName}'!A1");
            var checkResponse = await checkRequest.ExecuteAsync();
            if (checkResponse.Values != null && checkResponse.Values.Count > 0)
                return; // Đã có dữ liệu, không ghi đè

            // Ghi tiêu đề
            var headers = new List<object>
            {
                "STT", "Tên hàng", "Mã hàng", "Xuất xứ", "Đơn vị",
                "Số lượng", "Đơn giá\n(VNĐ)", "Thành tiền\n(VNĐ )",
                "Ghi chú", "Giá Nhập", "Thành Tiền", "Bảng Giá"
            };
            var headerRange = new Google.Apis.Sheets.v4.Data.ValueRange
            {
                Values = new List<IList<object>> { headers }
            };
            var writeReq = _sheetsService.Spreadsheets.Values.Update(
                headerRange, _spreadsheetId, $"'{sheetName}'!A1:L1");
            writeReq.ValueInputOption = Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await writeReq.ExecuteAsync();

            // Tô màu vàng
            var formatReq = new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Google.Apis.Sheets.v4.Data.Request>
                {
                    new Google.Apis.Sheets.v4.Data.Request
                    {
                        RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                        {
                            Range = new Google.Apis.Sheets.v4.Data.GridRange
                            {
                                SheetId = sheetId,
                                StartRowIndex = 0, EndRowIndex = 1,
                                StartColumnIndex = 0, EndColumnIndex = 12
                            },
                            Cell = new Google.Apis.Sheets.v4.Data.CellData
                            {
                                UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat
                                {
                                    BackgroundColor = new Google.Apis.Sheets.v4.Data.Color
                                    {
                                        Red = 1.0f, Green = 0.85f, Blue = 0.0f
                                    },
                                    TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true },
                                    HorizontalAlignment = "CENTER",
                                    VerticalAlignment = "MIDDLE",
                                    WrapStrategy = "WRAP"
                                }
                            },
                            Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"
                        }
                    }
                }
            };
            await _sheetsService.Spreadsheets.BatchUpdate(formatReq, _spreadsheetId).ExecuteAsync();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
