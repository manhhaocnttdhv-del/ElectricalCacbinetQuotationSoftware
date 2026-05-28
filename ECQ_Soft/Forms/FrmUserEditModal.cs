using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using ECQ_Soft.Helper;
using ECQ_Soft.Services;
using ECQ_Soft.Utils;

namespace ECQ_Soft
{
    public partial class FrmUserEditModal : Form
    {
        public int UserId { get; set; } = 0; // 0 = Thêm mới, > 0 = Chỉnh sửa

        public FrmUserEditModal()
        {
            InitializeComponent();
        }

        private void FrmUserEditModal_Load(object sender, EventArgs e)
        {
            LoadComboBoxData();

            if (UserId > 0)
            {
                lblTitle.Text = "Cập nhật nhân viên";
                lblPassword.Text = "Mật khẩu (để trống nếu không đổi):";
                LoadUserData();
            }
            else
            {
                lblTitle.Text = "Thêm mới nhân viên";
                lblPassword.Text = "Mật khẩu:";
                cboStatus.SelectedValue = "active";
            }
        }

        private void LoadComboBoxData()
        {
            try
            {
                DataTable dtRoles = DatabaseService.ExecuteQuery("SELECT Id AS id, Name AS name FROM [dbo].[CustomerRole] WHERE Active = 1 ORDER BY Name");
                cboRole.ValueMember = "id";
                cboRole.DisplayMember = "name";
                cboRole.DataSource = dtRoles;

                DataTable dtDepts = new DataTable();
                dtDepts.Columns.Add("id", typeof(int));
                dtDepts.Columns.Add("name", typeof(string));
                dtDepts.Rows.Add(0, "");
                cboDepartment.ValueMember = "id";
                cboDepartment.DisplayMember = "name";
                cboDepartment.DataSource = dtDepts;

                // 3. Load Status
                DataTable dtStatus = new DataTable();
                dtStatus.Columns.Add("Value", typeof(string));
                dtStatus.Columns.Add("Text", typeof(string));
                dtStatus.Rows.Add("active", "Hoạt động");
                dtStatus.Rows.Add("inactive", "Ngưng hoạt động");
                dtStatus.Rows.Add("suspended", "Tạm khóa");

                cboStatus.ValueMember = "Value";
                cboStatus.DisplayMember = "Text";
                cboStatus.DataSource = dtStatus;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh mục vai trò/phòng ban: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadUserData()
        {
            try
            {
                string sql = @"
                    SELECT
                        c.Username AS username,
                        c.Email AS email,
                        COALESCE(c.Title, c.Username, c.Email) AS full_name,
                        cr.Id AS role_id,
                        CAST(0 AS int) AS department_id,
                        CASE WHEN c.Active = 1 AND c.Deleted = 0 THEN 'active' ELSE 'inactive' END AS status
                    FROM [dbo].[Customer] c
                    OUTER APPLY (
                        SELECT TOP 1 r.Id
                        FROM [dbo].[Customer_CustomerRole_Mapping] m
                        INNER JOIN [dbo].[CustomerRole] r ON r.Id = m.CustomerRole_Id
                        WHERE m.Customer_Id = c.Id AND r.Active = 1
                        ORDER BY CASE WHEN r.SystemName = 'Administrators' THEN 0 ELSE 1 END, r.Id
                    ) cr
                    WHERE c.Id = @id";
                var parameters = new SqlParameter[] { new SqlParameter("@id", UserId) };
                DataTable dt = DatabaseService.ExecuteQuery(sql, parameters);

                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    txtUsername.Text = row["username"].ToString();
                    txtFullName.Text = row["full_name"].ToString();
                    txtEmail.Text = row["email"].ToString();
                    
                    if (row["role_id"] != DBNull.Value)
                        cboRole.SelectedValue = Convert.ToInt32(row["role_id"]);
                    
                    if (row["department_id"] != DBNull.Value)
                        cboDepartment.SelectedValue = Convert.ToInt32(row["department_id"]);
                    
                    cboStatus.SelectedValue = row["status"]?.ToString() ?? "active";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải thông tin nhân viên: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string fullname = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            // 1. Validation
            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }
            if (username.Length < 3)
            {
                MessageBox.Show("Tên đăng nhập phải chứa ít nhất 3 ký tự!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(fullname))
            {
                MessageBox.Show("Vui lòng nhập họ và tên!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFullName.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Vui lòng nhập email!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return;
            }
            if (!FunctionUtils.IsValidEmail(email))
            {
                MessageBox.Show("Địa chỉ email không đúng định dạng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return;
            }

            if (UserId == 0 && string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }
            if (!string.IsNullOrWhiteSpace(password) && password.Length < 6)
            {
                MessageBox.Show("Mật khẩu phải chứa ít nhất 6 ký tự!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            int roleId = cboRole.SelectedValue != null ? Convert.ToInt32(cboRole.SelectedValue) : 0;
            string status = cboStatus.SelectedValue?.ToString() ?? "active";

            // 2. Check unique constraint
            try
            {
                string checkSql = "SELECT Id FROM [dbo].[Customer] WHERE (Username = @username OR Email = @email) AND Id != @id AND Deleted = 0";
                var checkParams = new SqlParameter[] {
                    new SqlParameter("@username", username),
                    new SqlParameter("@email", email),
                    new SqlParameter("@id", UserId)
                };
                DataTable checkDt = DatabaseService.ExecuteQuery(checkSql, checkParams);
                if (checkDt != null && checkDt.Rows.Count > 0)
                {
                    MessageBox.Show("Tên đăng nhập hoặc Email đã tồn tại trong hệ thống!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 3. Save to DB
                if (UserId == 0)
                {
                    string insertSql = @"
                        INSERT INTO [dbo].[Customer]
                            (CustomerGuid, Username, Email, Title, Active, Deleted, IsTaxExempt, AffiliateId, IsSystemAccount,
                             CreatedOnUtc, LastActivityDateUtc, VendorId, HasShoppingCartItems, EsyPassword,
                             RequireReLogin, FailedLoginAttempts, RegisteredInStoreId)
                        OUTPUT INSERTED.Id
                        VALUES
                            (NEWID(), @username, @email, @fullname, @active, 0, 0, 0, 0,
                             SYSUTCDATETIME(), SYSUTCDATETIME(), 0, 0, @password,
                             0, 0, 1)";
                    
                    var insertParams = new SqlParameter[] {
                        new SqlParameter("@username", username),
                        new SqlParameter("@email", email),
                        new SqlParameter("@fullname", fullname),
                        new SqlParameter("@active", status == "active"),
                        new SqlParameter("@password", password)
                    };
                    DataTable inserted = DatabaseService.ExecuteQuery(insertSql, insertParams);
                    int newUserId = Convert.ToInt32(inserted.Rows[0][0]);
                    SaveCustomerRole(newUserId, roleId);
                    MessageBox.Show("Thêm mới nhân viên thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    string updateSql = @"
                        UPDATE [dbo].[Customer]
                        SET Username = @username, Email = @email, Title = @fullname, Active = @active
                        WHERE Id = @id";
                    
                    var updateParams = new SqlParameter[] {
                        new SqlParameter("@username", username),
                        new SqlParameter("@email", email),
                        new SqlParameter("@fullname", fullname),
                        new SqlParameter("@active", status == "active"),
                        new SqlParameter("@id", UserId)
                    };
                    DatabaseService.ExecuteNonQuery(updateSql, updateParams);
                    SaveCustomerRole(UserId, roleId);

                    // Update password if entered
                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        DatabaseService.ExecuteNonQuery("UPDATE [dbo].[Customer] SET EsyPassword = @password WHERE Id = @id", new SqlParameter[] {
                            new SqlParameter("@password", password),
                            new SqlParameter("@id", UserId)
                        });
                    }
                    MessageBox.Show("Cập nhật thông tin nhân viên thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu thông tin: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void SaveCustomerRole(int customerId, int roleId)
        {
            if (roleId <= 0) return;

            DatabaseService.ExecuteNonQuery("DELETE FROM [dbo].[Customer_CustomerRole_Mapping] WHERE Customer_Id = @customerId", new SqlParameter[] {
                new SqlParameter("@customerId", customerId)
            });

            DatabaseService.ExecuteNonQuery("INSERT INTO [dbo].[Customer_CustomerRole_Mapping] (Customer_Id, CustomerRole_Id) VALUES (@customerId, @roleId)", new SqlParameter[] {
                new SqlParameter("@customerId", customerId),
                new SqlParameter("@roleId", roleId)
            });
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
