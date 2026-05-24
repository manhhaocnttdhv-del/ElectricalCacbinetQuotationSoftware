using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ECQ_Soft
{
    /// <summary>
    /// Form nhúng WebView2 mở trực tiếp Google Sheets (sheet "Tính toán đồng thanh cái").
    /// Nhân viên thao tác y hệt như trên trình duyệt.
    /// </summary>
    public class FrmBusbarWebView : Form
    {
        private Microsoft.Web.WebView2.WinForms.WebView2 _webView;
        private readonly string _spreadsheetId;
        private readonly Google.Apis.Sheets.v4.SheetsService _service;

        public FrmBusbarWebView(string spreadsheetId, Google.Apis.Sheets.v4.SheetsService service = null)
        {
            _spreadsheetId = spreadsheetId;
            _service = service;
            this.Text = "Google Sheets — Tính toán đồng thanh cái";
            this.Size = new Size(1400, 850);
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowIcon = false;
            this.MinimumSize = new Size(800, 500);

            _webView = new Microsoft.Web.WebView2.WinForms.WebView2
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(_webView);
            this.Load += async (s, e) => await InitWebViewAsync();
        }

        private async Task InitWebViewAsync()
        {
            try
            {
                await _webView.EnsureCoreWebView2Async(null);

                // Tìm gid của sheet "Tính toán đồng thanh cái"
                int gid = 0;
                if (_service != null)
                {
                    try
                    {
                        var spreadsheet = await _service.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
                        var targetSheet = spreadsheet.Sheets?.FirstOrDefault(
                            sh => sh.Properties.Title == "Tính toán đồng thanh cái");
                        if (targetSheet != null)
                            gid = targetSheet.Properties.SheetId ?? 0;
                    }
                    catch { /* fallback gid=0 */ }
                }

                string url = $"https://docs.google.com/spreadsheets/d/{_spreadsheetId}/edit#gid={gid}";
                _webView.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể khởi tạo WebView2.\nLỗi: " + ex.Message +
                    "\n\nĐảm bảo máy đã cài Microsoft Edge WebView2 Runtime.",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _webView?.Dispose();
            base.Dispose(disposing);
        }
    }
}
