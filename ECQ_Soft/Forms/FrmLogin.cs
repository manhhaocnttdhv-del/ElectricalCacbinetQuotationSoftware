using ECQ_Soft.Helper;
using ECQ_Soft.Properties;
using ECQ_Soft.Services;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace ECQ_Soft
{
    public partial class FrmLogin : Form
    {
        public FrmLogin()
        {
            InitializeComponent();
            DatabaseService.InitializeDatabase();
        }

        private bool LoginCheck(string loginName, string password)
        {
            try
            {
                string sql = @"
                    SELECT TOP 1
                        c.Id,
                        c.Username,
                        c.Email,
                        c.Title,
                        c.Active,
                        c.Deleted,
                        c.EsyPassword,
                        cp.Password,
                        cp.PasswordFormatId,
                        cp.PasswordSalt,
                        cr.Id AS role_id,
                        cr.Name AS role_name,
                        cr.SystemName AS role_code
                    FROM [dbo].[Customer] c
                    OUTER APPLY (
                        SELECT TOP 1 Password, PasswordFormatId, PasswordSalt
                        FROM [dbo].[CustomerPassword]
                        WHERE CustomerId = c.Id
                        ORDER BY CreatedOnUtc DESC, Id DESC
                    ) cp
                    OUTER APPLY (
                        SELECT TOP 1 r.Id, r.Name, r.SystemName
                        FROM [dbo].[Customer_CustomerRole_Mapping] m
                        INNER JOIN [dbo].[CustomerRole] r ON r.Id = m.CustomerRole_Id
                        WHERE m.Customer_Id = c.Id AND r.Active = 1
                        ORDER BY CASE WHEN r.SystemName = 'Administrators' THEN 0 ELSE 1 END, r.Id
                    ) cr
                    WHERE (c.Email = @login OR c.Username = @login) AND c.Deleted = 0";

                var parameters = new[]
                {
                    new SqlParameter("@login", loginName.Trim())
                };

                DataTable dt = DatabaseService.ExecuteQuery(sql, parameters);
                if (dt == null || dt.Rows.Count == 0) return false;

                DataRow row = dt.Rows[0];

                bool active = row["Active"] != DBNull.Value && Convert.ToBoolean(row["Active"]);
                if (!active)
                {
                    MessageBox.Show("Tai khoan nay da ngung hoat dong!", "Loi dang nhap", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (!VerifyCustomerPassword(password, row))
                {
                    return false;
                }

                UserSession.UserId = Convert.ToInt32(row["Id"]);
                UserSession.Username = row["Username"]?.ToString();
                UserSession.FullName = GetDisplayName(row);
                UserSession.RoleId = row["role_id"] != DBNull.Value ? Convert.ToInt32(row["role_id"]) : 0;
                UserSession.RoleCode = GetRoleCode(row);
                UserSession.DepartmentId = null;
                UserSession.Permissions = DatabaseService.GetUserPermissions(UserSession.RoleId);

                if (string.Equals(UserSession.RoleCode, "ADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    Settings.Default.isAdmin = true;
                }
                else
                {
                    Settings.Default.isAdmin = false;
                    Settings.Default.Role = UserSession.Role;
                }

                Settings.Default.Name = UserSession.FullName;
                Settings.Default.Save();

                TryUpdateLastLogin(UserSession.UserId);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Loi ket noi co so du lieu:\n" + ex.Message, "Loi he thong", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        private static bool VerifyCustomerPassword(string inputPassword, DataRow row)
        {
            string esyPassword = row["EsyPassword"]?.ToString();
            if (!string.IsNullOrWhiteSpace(esyPassword))
            {
                if (string.Equals(inputPassword, esyPassword, StringComparison.Ordinal))
                {
                    return true;
                }

                if (PasswordHelper.VerifyPassword(inputPassword, esyPassword))
                {
                    return true;
                }
            }

            string storedPassword = row["Password"]?.ToString();
            if (string.IsNullOrWhiteSpace(storedPassword))
            {
                return false;
            }

            int passwordFormatId = row["PasswordFormatId"] != DBNull.Value ? Convert.ToInt32(row["PasswordFormatId"]) : 1;
            string salt = row["PasswordSalt"]?.ToString() ?? string.Empty;

            if (passwordFormatId == 0)
            {
                return string.Equals(inputPassword, storedPassword, StringComparison.Ordinal);
            }

            if (passwordFormatId == 1)
            {
                return string.Equals(CreateHash(inputPassword + salt), storedPassword, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(CreateHash(salt + inputPassword), storedPassword, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static string CreateHash(string value)
        {
            using (HashAlgorithm algorithm = SHA1.Create())
            {
                byte[] hashBytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
                StringBuilder builder = new StringBuilder(hashBytes.Length * 2);
                foreach (byte b in hashBytes)
                {
                    builder.Append(b.ToString("X2"));
                }
                return builder.ToString();
            }
        }

        private static string GetDisplayName(DataRow row)
        {
            string title = row["Title"]?.ToString();
            if (!string.IsNullOrWhiteSpace(title)) return title;

            string username = row["Username"]?.ToString();
            if (!string.IsNullOrWhiteSpace(username)) return username;

            return row["Email"]?.ToString() ?? string.Empty;
        }

        private static string GetRoleCode(DataRow row)
        {
            string roleName = row["role_name"]?.ToString();
            string roleCode = row["role_code"]?.ToString();

            if (string.Equals(roleCode, "Administrators", StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(roleName) && roleName.IndexOf("Administrators", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return "ADMIN";
            }

            return !string.IsNullOrWhiteSpace(roleCode) ? roleCode.ToUpperInvariant() : (roleName ?? "CUSTOMER").ToUpperInvariant();
        }

        private static void TryUpdateLastLogin(int customerId)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                using (var cmd = new SqlCommand("UPDATE [dbo].[Customer] SET LastLoginDateUtc = SYSUTCDATETIME(), LastActivityDateUtc = SYSUTCDATETIME() WHERE Id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", customerId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
            }
        }

        private async void btnLogin_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUserName.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Vui long nhap day du tai khoan va mat khau!", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (LoginCheck(txtUserName.Text, txtPassword.Text))
            {
                FrmMain frmMain = new FrmMain();
                using (new LoadingOverlay(this, "Dang tai cau hinh va du lieu he thong tu Google Sheets..."))
                {
                    await frmMain.LoadDataAsync();
                }

                Hide();
                frmMain.ShowDialog();
                Close();
            }
            else
            {
                MessageBox.Show("Sai tai khoan hoac mat khau!", "Loi dang nhap", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ckShowHidePassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.PasswordChar = ckShowHidePassword.Checked ? '\0' : '*';
        }

        private void FrmLogin_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
        }
    }
}
