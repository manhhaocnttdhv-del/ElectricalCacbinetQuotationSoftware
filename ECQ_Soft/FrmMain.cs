
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
