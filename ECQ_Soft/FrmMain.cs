
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
        private FrmConfig    _frmConfig;

        public FrmMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Visible = false;
            FrmSplashScreen frmSplash = new FrmSplashScreen();

            // Tên người đăng nhập
            string userName = Settings.Default.Name;
            lbUserName.Text = "Xin chào, " + userName;

            // Nhúng FrmQuotation vào Tab 1
            _frmQuotation = new FrmQuotation();
            _frmQuotation.TopLevel        = false;
            _frmQuotation.FormBorderStyle = FormBorderStyle.None;
            _frmQuotation.Dock            = DockStyle.Fill;
            tabPage1.Controls.Add(_frmQuotation);
            _frmQuotation.Show();

            // Nhúng FrmConfig vào Tab 2
            _frmConfig = new FrmConfig();
            _frmConfig.TopLevel        = false;
            _frmConfig.FormBorderStyle = FormBorderStyle.None;
            _frmConfig.Dock            = DockStyle.Fill;
            tabPage2.Controls.Add(_frmConfig);
            _frmConfig.Show();

            frmSplash.Close();
            this.Visible = true;           
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
