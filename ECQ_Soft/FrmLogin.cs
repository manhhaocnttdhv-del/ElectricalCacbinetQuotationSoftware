using ECQ_Soft.Model;
using ECQ_Soft.Properties;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace ECQ_Soft
{
    public partial class FrmLogin : Form
    {
        
        private SheetsService _sheetsService;

        string spreadsheetId = "1swdiFIwhoZaXf4c5R_Lzp2pgZng5RcdOKii2DYkN_Uc";
        string sheetName = "Sheet1";

        private List<UserInfo> userInfors = new List<UserInfo>();
        public FrmLogin()
        {
            InitializeComponent();
        }

        private void InitGoogleSheetsService()
        {
            try
            {
                GoogleCredential credential;

                using (var stream = new FileStream("credential.json", FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(SheetsService.Scope.Spreadsheets);
                }


                _sheetsService = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "GSheetUpdater",
                });
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show("Không tìm thấy file 'credentials.json'.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show("Lỗi khi đọc file credentials.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show("Lỗi xác thực với Google API.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi không xác định khi kết nối Google Sheets.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GetUserInfo()
        {
            try
            {
                // Đọc dữ liệu từ Google Sheet
                string range = $"{sheetName}!P2:S"; // Bỏ dòng tiêu đề
                var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
                var response = request.Execute();
                IList<IList<object>> rows = response.Values;

                if (rows == null || rows.Count == 0)
                {
                    MessageBox.Show("Không có dữ liệu người dùng trong Google Sheet!",
                        "Lỗi đăng nhập", MessageBoxButtons.OK, MessageBoxIcon.Warning);                    
                }

                for(int i = 0; i< rows.Count; i++)
                {
                    var row = rows[i];

                    string ggusername = row.Count > 0 ? row[0].ToString().Trim() : "";
                    string ggpassword = row.Count > 1 ? row[1].ToString().Trim() : "";
                    string ggfullname = row.Count > 2 ? row[2].ToString().Trim() : "";
                    string ggrole = row.Count > 3 ? row[3].ToString().Trim() : "";

                    UserInfo user = new UserInfo
                    {
                       Stt = i + 1,
                       UserName = ggusername,
                       Password = ggpassword,
                       FullName = ggfullname,
                       Role = ggrole,
                    };

                    userInfors.Add(user);
                }
                    
            }
            catch (Google.GoogleApiException ex) when (ex.Message.Contains("Unable to parse range"))
            {
                MessageBox.Show($"Không tìm thấy sheet có tên '{sheetName}'. Vui lòng kiểm tra lại.\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Lỗi khi truy cập Google Sheets:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi không xác định khi đọc Google Sheet:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool LoginCheck(string userName, string password)
        {

            for (int i = 0; i < userInfors.Count; i++)
            {
                var user = userInfors[i];
                if (user.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)
                            && user.Password == password) // So sánh đúng mật khẩu
                {
                    if (user.Role == "admin")
                    {
                        Settings.Default.isAdmin = true;
                    }
                    else
                    {
                        Settings.Default.isAdmin = false;
                        Settings.Default.Role = user.Role;
                    }

                    Settings.Default.Name = user.FullName;
                    Settings.Default.Save();
                    return true;
                }
            }
            return false;
            
        }

        private void btnLogin_Click_1(object sender, EventArgs e)
        {
            if (LoginCheck(txtUserName.Text, txtPassword.Text))
            {
                this.Hide();
                FrmMain _frm1 = new FrmMain();
                _frm1.Show();
            }
            else
            {
                MessageBox.Show("Sai tài khoản hoặc mật khẩu!", "Lỗi đăng nhập",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FrmLogin_Load_1(object sender, EventArgs e)
        {
            InitGoogleSheetsService();
            GetUserInfo();
        }

        private void ckShowHidePassword_CheckedChanged(object sender, EventArgs e)
        {
            if(ckShowHidePassword.Checked)
            {
                txtPassword.PasswordChar = '\0';
            }
            else
            {
                txtPassword.PasswordChar = '*';
                
            }    
        }

        private void FrmLogin_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
    }
}

