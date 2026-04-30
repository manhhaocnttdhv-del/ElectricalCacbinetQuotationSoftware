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

            // Khởi tạo tiền tố mặc định
            txtNewName.Text = "KH_";
            txtNewName.ForeColor = System.Drawing.Color.Black;

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

            var sheetNames = spreadsheet.Sheets
                .Where(s => s.Properties.Title.StartsWith("KH_", StringComparison.OrdinalIgnoreCase))
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
        private void rdoRename_CheckedChanged(object sender, EventArgs e) => UpdateUIMode();

        private void UpdateUIMode()
        {
            if (rdoNew.Checked)
            {
                lblExisting.Visible = false;
                cboExisting.Visible = false;

                lblNewName.Visible = true;
                lblNewName.Text = "Tên tab mới:";
                lblNewName.Top = 56;
                
                txtNewName.Visible = true;
                txtNewName.Top = 78;
            }
            else if (rdoRename.Checked)
            {
                lblExisting.Visible = true;
                lblExisting.Text = "Chọn tab cần đổi tên:";
                lblExisting.Top = 46;
                
                cboExisting.Visible = true;
                cboExisting.Top = 66;

                lblNewName.Visible = true;
                lblNewName.Text = "Tên mới:";
                lblNewName.Top = 100;

                txtNewName.Visible = true;
                txtNewName.Top = 120;
            }
            else // rdoExisting.Checked
            {
                lblExisting.Visible = true;
                lblExisting.Text = "Chọn tab hiện có:";
                lblExisting.Top = 56;
                
                cboExisting.Visible = true;
                cboExisting.Top = 78;

                lblNewName.Visible = false;
                txtNewName.Visible = false;
            }
        }

        private async void btnConfirm_Click(object sender, EventArgs e)
        {
            if (rdoNew.Checked)
            {
                string newName = txtNewName.Text.Trim();
                if (string.IsNullOrEmpty(newName) || newName.Equals("KH_", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Vui lòng nhập tên khách hàng (sau tiền tố KH_)!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!newName.StartsWith("KH_", StringComparison.OrdinalIgnoreCase))
                {
                    newName = "KH_" + newName;
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
            else if (rdoRename.Checked)
            {
                string oldName = cboExisting.Text.Trim();
                string newName = txtNewName.Text.Trim();

                if (string.IsNullOrEmpty(oldName) || !_sheetIdMap.TryGetValue(oldName, out int sheetId))
                {
                    MessageBox.Show("Vui lòng chọn một tab hợp lệ để đổi tên!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(newName) || newName.Equals("KH_", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Vui lòng nhập tên mới cho tab!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!newName.StartsWith("KH_", StringComparison.OrdinalIgnoreCase))
                {
                    newName = "KH_" + newName;
                }

                if (cboExisting.Items.Cast<string>().Any(s => s.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show($"Tab '{newName}' đã tồn tại!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    btnConfirm.Enabled = false;
                    lblStatus.Text = $"Đang đổi tên thành '{newName}'...";

                    var requests = new List<Google.Apis.Sheets.v4.Data.Request>
                    {
                        new Google.Apis.Sheets.v4.Data.Request
                        {
                            UpdateSheetProperties = new Google.Apis.Sheets.v4.Data.UpdateSheetPropertiesRequest
                            {
                                Properties = new Google.Apis.Sheets.v4.Data.SheetProperties
                                {
                                    SheetId = sheetId,
                                    Title = newName
                                },
                                Fields = "title"
                            }
                        }
                    };

                    var batchRequest = new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest { Requests = requests };
                    await _sheetsService.Spreadsheets.BatchUpdate(batchRequest, _spreadsheetId).ExecuteAsync();

                    SelectedSheetName = newName;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    btnConfirm.Enabled = true;
                    lblStatus.Text = "Lỗi khi đổi tên tab.";
                    MessageBox.Show("Không thể đổi tên tab:\n" + ex.Message,
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else // dùng tab cũ
            {
                string existingName = cboExisting.Text.Trim();
                if (string.IsNullOrEmpty(existingName))
                {
                    MessageBox.Show("Vui lòng chọn hoặc nhập tên một tab!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!_sheetIdMap.ContainsKey(existingName))
                {
                    MessageBox.Show($"Tab '{existingName}' không có sẵn. Vui lòng chọn tab từ danh sách hoặc tạo tab mới.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
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
                "Số lượng", "Đơn giá (VNĐ)", "Thành tiền (VNĐ)",
                "Ghi chú", "Giá Nhập", "Thành Tiền", "Lợi Nhuận", "Bảng Giá"
            };
            var headerRange = new Google.Apis.Sheets.v4.Data.ValueRange
            {
                Values = new List<IList<object>> { headers }
            };
            var writeReq = _sheetsService.Spreadsheets.Values.Update(
                headerRange, _spreadsheetId, $"'{sheetName}'!A1:M1");
            writeReq.ValueInputOption = Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await writeReq.ExecuteAsync();

            // Áp dụng định dạng (Column Widths, Colors, Borders)
            var requests = new List<Google.Apis.Sheets.v4.Data.Request>();

            // Thiết lập độ rộng cột
            Action<int, int> setColWidth = (colIdx, width) =>
            {
                requests.Add(new Google.Apis.Sheets.v4.Data.Request
                {
                    UpdateDimensionProperties = new Google.Apis.Sheets.v4.Data.UpdateDimensionPropertiesRequest
                    {
                        Range = new Google.Apis.Sheets.v4.Data.DimensionRange { SheetId = sheetId, Dimension = "COLUMNS", StartIndex = colIdx, EndIndex = colIdx + 1 },
                        Properties = new Google.Apis.Sheets.v4.Data.DimensionProperties { PixelSize = width },
                        Fields = "pixelSize"
                    }
                });
            };
            setColWidth(0, 45);  // STT
            setColWidth(1, 300); // Tên hàng
            setColWidth(2, 100); // Mã hàng
            setColWidth(3, 80);  // Xuất xứ
            setColWidth(4, 55);  // Đơn vị
            setColWidth(5, 60);  // Số lượng
            setColWidth(6, 110); // Đơn giá
            setColWidth(7, 120); // Thành tiền
            setColWidth(8, 80);  // Ghi chú
            setColWidth(9, 110); // Giá nhập
            setColWidth(10, 120); // Thành tiền nhập
            setColWidth(11, 100); // Lợi nhuận
            setColWidth(12, 80);  // Bảng giá

            // Kẻ viền (Borders) cho dòng tiêu đề
            requests.Add(new Google.Apis.Sheets.v4.Data.Request
            {
                UpdateBorders = new Google.Apis.Sheets.v4.Data.UpdateBordersRequest
                {
                    Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = 0, EndRowIndex = 1, StartColumnIndex = 0, EndColumnIndex = 13 },
                    Top = new Google.Apis.Sheets.v4.Data.Border { Style = "SOLID", Color = new Google.Apis.Sheets.v4.Data.Color { Red = 0, Green = 0, Blue = 0 } },
                    Bottom = new Google.Apis.Sheets.v4.Data.Border { Style = "SOLID", Color = new Google.Apis.Sheets.v4.Data.Color { Red = 0, Green = 0, Blue = 0 } },
                    Left = new Google.Apis.Sheets.v4.Data.Border { Style = "SOLID", Color = new Google.Apis.Sheets.v4.Data.Color { Red = 0, Green = 0, Blue = 0 } },
                    Right = new Google.Apis.Sheets.v4.Data.Border { Style = "SOLID", Color = new Google.Apis.Sheets.v4.Data.Color { Red = 0, Green = 0, Blue = 0 } },
                    InnerVertical = new Google.Apis.Sheets.v4.Data.Border { Style = "SOLID", Color = new Google.Apis.Sheets.v4.Data.Color { Red = 0, Green = 0, Blue = 0 } }
                }
            });

            // 0..8: Vàng
            requests.Add(new Google.Apis.Sheets.v4.Data.Request
            {
                RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                {
                    Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = 0, EndRowIndex = 1, StartColumnIndex = 0, EndColumnIndex = 9 },
                    Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 1f, Green = 1f, Blue = 0f }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true, ForegroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0.12f, Green = 0.286f, Blue = 0.49f } }, HorizontalAlignment = "CENTER", VerticalAlignment = "MIDDLE", WrapStrategy = "WRAP" } },
                    Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"
                }
            });
            // 9..10: Cyan
            requests.Add(new Google.Apis.Sheets.v4.Data.Request
            {
                RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                {
                    Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = 0, EndRowIndex = 1, StartColumnIndex = 9, EndColumnIndex = 11 },
                    Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0f, Green = 1f, Blue = 1f }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true, ForegroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0f, Green = 0f, Blue = 0f } }, HorizontalAlignment = "CENTER", VerticalAlignment = "MIDDLE", WrapStrategy = "WRAP" } },
                    Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"
                }
            });
            // 11: Lợi nhuận (Vàng, đỏ text)
            requests.Add(new Google.Apis.Sheets.v4.Data.Request
            {
                RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                {
                    Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = 0, EndRowIndex = 1, StartColumnIndex = 11, EndColumnIndex = 12 },
                    Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 1f, Green = 1f, Blue = 0f }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true, ForegroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 1f, Green = 0f, Blue = 0f } }, HorizontalAlignment = "CENTER", VerticalAlignment = "MIDDLE", WrapStrategy = "WRAP" } },
                    Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"
                }
            });
            // 12: Bảng Giá (Xanh nước biển)
            requests.Add(new Google.Apis.Sheets.v4.Data.Request
            {
                RepeatCell = new Google.Apis.Sheets.v4.Data.RepeatCellRequest
                {
                    Range = new Google.Apis.Sheets.v4.Data.GridRange { SheetId = sheetId, StartRowIndex = 0, EndRowIndex = 1, StartColumnIndex = 12, EndColumnIndex = 13 },
                    Cell = new Google.Apis.Sheets.v4.Data.CellData { UserEnteredFormat = new Google.Apis.Sheets.v4.Data.CellFormat { BackgroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0.39f, Green = 0.58f, Blue = 0.93f }, TextFormat = new Google.Apis.Sheets.v4.Data.TextFormat { Bold = true, ForegroundColor = new Google.Apis.Sheets.v4.Data.Color { Red = 0f, Green = 0f, Blue = 0f } }, HorizontalAlignment = "CENTER", VerticalAlignment = "MIDDLE", WrapStrategy = "WRAP" } },
                    Fields = "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment,verticalAlignment,wrapStrategy)"
                }
            });

            var formatReq = new Google.Apis.Sheets.v4.Data.BatchUpdateSpreadsheetRequest { Requests = requests };
            await _sheetsService.Spreadsheets.BatchUpdate(formatReq, _spreadsheetId).ExecuteAsync();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
