
using ECQ_Soft.Model;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

//using Excel = Microsoft.Office.Interop.Excel;

namespace ECQ_Soft
{
    public partial class FrmMain : Form
    {
        private FrmQuotation _frmQuotation;
        private FrmRelation  _frmRelation;
        private FrmConfig    _frmConfig;

        // Tab index của tab "Cấu hình" (tabPage3)
        private const int CONFIG_TAB_INDEX = 2;
        // Lưu tab trước đó để rollback nếu người dùng bấm Cancel trong modal
        private int _previousTabIndex = 0;
        // Cờ để tránh xử lý sự kiện SelectedIndexChanged đệ quy
        private bool _isHandlingTabChange = false;

        public FrmMain()
        {
            InitializeComponent();
        }

        public async Task LoadDataAsync()
        {
            // Buộc tạo handle để đảm bảo các control được khởi tạo
            var h1 = tabPage1.Handle;
            var h2 = tabPage2.Handle;
            var h3 = tabPage3.Handle;

            _frmQuotation = new FrmQuotation();
            _frmQuotation.Dock = DockStyle.Fill;
            tabPage1.Controls.Add(_frmQuotation);

            _frmRelation = new FrmRelation();
            _frmRelation.Dock = DockStyle.Fill;
            tabPage2.Controls.Add(_frmRelation);

            _frmConfig = new FrmConfig();
            _frmConfig.Dock = DockStyle.Fill;
            tabPage3.Controls.Add(_frmConfig);

            // Chạy tất cả các tác vụ tải dữ liệu song song
            var loadTasks = new List<Task>
            {
                _frmQuotation.LoadDataAsync(),
                _frmRelation.LoadDataAsync(),
                _frmConfig.LoadDataAsync()
            };

            await Task.WhenAll(loadTasks);

            _frmQuotation.Show();
            _frmRelation.Show();
            _frmConfig.Show();

            // Gán event sau khi mọi thứ đã tải xong
            tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string userName = Settings.Default.Name;
            lbUserName.Text = "Xin chào, " + userName;

            tabPage1.Text = "Báo giá";
            tabPage2.Text = "Đối tượng";
            tabPage3.Text = "Cấu hình";
            tabControl1.SelectedTab = tabPage1;
        }

        private async void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Chỉ bắt khi chuyển sang tab Cấu hình
            if (_isHandlingTabChange) return;
            if (tabControl1.SelectedIndex != CONFIG_TAB_INDEX) 
            {
                _previousTabIndex = tabControl1.SelectedIndex;
                return;
            }

            // Nếu đã chọn sheet rồi thì không hỏi lại (tránh reset danh sách)
            if (!string.IsNullOrEmpty(_frmConfig.GetConfigSheetName()))
                return;

            _isHandlingTabChange = true;
            try
            {
                // Chỉ hiển thị modal lần ĐẦU TIÊN (chưa chọn sheet)
                var service = _frmConfig.GetSheetsService();
                var spreadsheetId = _frmConfig.GetSpreadsheetId();

                string selectedSheet = null;
                bool cancelled = false;

                using (var selector = new FrmSheetSelector(spreadsheetId, service))
                {
                    var result = selector.ShowDialog(this);
                    if (result == DialogResult.OK && !string.IsNullOrEmpty(selector.SelectedSheetName))
                        selectedSheet = selector.SelectedSheetName;
                    else
                        cancelled = true;
                }

                // Thả cờ NGAY SAU KHI dialog đóng để click nhanh không bị chặn
                _isHandlingTabChange = false;

                if (cancelled)
                    tabControl1.SelectedIndex = _previousTabIndex;
                else
                    await _frmConfig.SetConfigSheet(selectedSheet);
            }
            finally
            {
                // Đảm bảo flag luôn được thả dù có exception
                _isHandlingTabChange = false;
            }
        }


        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            this.Hide();
            FrmLogin frmLogin = new FrmLogin();
            frmLogin.ShowDialog();
        }
    }
}
