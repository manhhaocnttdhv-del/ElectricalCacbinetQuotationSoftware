using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ECQ_Soft
{
    public partial class FrmConfig : Form
    {
        private SheetsService _sheetsService;

        public FrmConfig()
        {
            InitializeComponent();
        }

        private void InitGoogleSheetsService()
        {
            try
            {
                string jsonCredentials = Properties.Resources.GoogleCredentialJson2;

                if (string.IsNullOrWhiteSpace(jsonCredentials))
                {
                    MessageBox.Show("Không tìm thấy cấu hình Google Credential trong Resources (GoogleCredentialJson2).",
                        "Lỗi cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                GoogleCredential credential = null;

                // Nếu resource chứa một array JSON (nhiều credential), thử từng cái
                var trimmed = jsonCredentials.Trim();
                if (trimmed.StartsWith("["))
                {
                    var arr = Newtonsoft.Json.Linq.JArray.Parse(trimmed);
                    Exception lastEx = null;
                    foreach (var item in arr)
                    {
                        try
                        {
                            credential = GoogleCredential.FromJson(item.ToString())
                                            .CreateScoped(SheetsService.Scope.Spreadsheets);
                            break; // dùng credential đầu tiên thành công
                        }
                        catch (Exception ex) { lastEx = ex; }
                    }
                    if (credential == null) throw lastEx ?? new Exception("Không có credential hợp lệ trong danh sách.");
                }
                else
                {
                    credential = GoogleCredential.FromJson(trimmed)
                                    .CreateScoped(SheetsService.Scope.Spreadsheets);
                }

                _sheetsService = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "GSheetUpdater",
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi tạo truy cập Google Sheets:\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FrmConfig_Load(object sender, EventArgs e)
        {
            InitGoogleSheetsService();
        }
    }
}
