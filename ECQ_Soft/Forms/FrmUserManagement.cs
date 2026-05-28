using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ECQ_Soft.Services;
using ECQ_Soft.Utils;

namespace ECQ_Soft
{
    public partial class FrmUserManagement : Form
    {
        private DataTable _usersTable;
        private bool _isLoadingPermissions = false;

        public FrmUserManagement()
        {
            InitializeComponent();
            Utils.FunctionUtils.SetDoubleBufferedRecursive(this);
            ApplyUiOptimizations();
            WireEvents();
        }

        private void WireEvents()
        {
            // Đăng ký các sự kiện tại runtime để giữ code designer sạch sẽ và tránh lỗi
            tvDepartments.AfterSelect += TvDepartments_AfterSelect;
            lstRoles.SelectedIndexChanged += LstRoles_SelectedIndexChanged;
            btnSavePermissions.Click += BtnSavePermissions_Click;
            tvPermissions.AfterCheck += TvPermissions_AfterCheck;
            
            btnAddDept.Click += BtnAddDept_Click;
            btnEditDept.Click += BtnEditDept_Click;
            btnDeleteDept.Click += BtnDeleteDept_Click;
            
            btnSearch.Click += BtnSearch_Click;
            btnNewUser.Click += BtnNewUser_Click;
            dgvUsers.CellDoubleClick += DgvUsers_CellDoubleClick;
            dgvUsers.CellFormatting += DgvUsers_CellFormatting;
            
            txtSearch.Enter += TxtSearch_Enter;
            txtSearch.Leave += TxtSearch_Leave;
            txtSearch.KeyDown += TxtSearch_KeyDown;
        }

        private void ApplyUiOptimizations()
        {
            // Kế thừa tỷ lệ scaling từ form cha để tránh méo giao diện trên màn hình DPI cao
            this.AutoScaleMode = AutoScaleMode.Inherit;
            this.Padding = Padding.Empty;

            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;

            tabUsers.Padding = new Padding(8);
            tabRoles.Padding = new Padding(8);

            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer1.Panel1MinSize = 220;
            splitContainer1.SplitterWidth = 6;
            splitContainer1.SplitterDistance = 260;
            splitContainer1.Resize += (s, e) => UpdateSplitContainerDistances();

            splitContainer2.FixedPanel = FixedPanel.Panel1;
            splitContainer2.Panel1MinSize = 260;
            splitContainer2.SplitterWidth = 6;
            splitContainer2.SplitterDistance = 300;
            splitContainer2.Resize += (s, e) => UpdateSplitContainerDistances();

            // 1. Căn chỉnh lại kích thước các nút Phòng ban
            btnAddDept.Width = 88;
            btnAddDept.Height = 34;
            btnAddDept.Location = new Point(6, 8);
            btnAddDept.TextAlign = ContentAlignment.MiddleCenter;
            btnAddDept.UseCompatibleTextRendering = false;

            btnEditDept.Width = 88;
            btnEditDept.Height = 34;
            btnEditDept.Location = new Point(98, 8);
            btnEditDept.TextAlign = ContentAlignment.MiddleCenter;
            btnEditDept.UseCompatibleTextRendering = false;

            btnDeleteDept.Width = 88;
            btnDeleteDept.Height = 34;
            btnDeleteDept.Location = new Point(190, 8);
            btnDeleteDept.TextAlign = ContentAlignment.MiddleCenter;
            btnDeleteDept.UseCompatibleTextRendering = false;
            panelDeptActions.Height = 52;
            panelDeptActions.Resize += (s, e) => LayoutDepartmentButtons();

            // 2. Căn chỉnh lại panel tìm kiếm và nút thêm nhân viên mới
            btnSearch.Width = 110;
            btnSearch.Height = 33;
            btnSearch.Location = new Point(315, 11);
            btnSearch.TextAlign = ContentAlignment.MiddleCenter;
            btnSearch.UseCompatibleTextRendering = false;

            btnNewUser.Width = 180;
            btnNewUser.Height = 33;
            btnNewUser.Location = new Point(panelSearch.Width - btnNewUser.Width - 10, 11);
            btnNewUser.TextAlign = ContentAlignment.MiddleCenter;
            btnNewUser.UseCompatibleTextRendering = false;
            
            panelSearch.Height = 52;
            panelSearch.Resize += (s, e) => LayoutSearchPanel();

            dgvUsers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvUsers.ColumnHeadersHeight = 38;
            dgvUsers.RowTemplate.Height = 34;
            dgvUsers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvUsers.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

            // 3. Định hình lại phong cách Flat modern cho toàn bộ các nút
            var flatButtons = new List<Button> { btnAddDept, btnEditDept, btnDeleteDept, btnSearch, btnNewUser, btnSavePermissions };
            foreach (var btn in flatButtons)
            {
                if (btn == null) continue;
                UIService.StyleFlatButton(btn, btn == btnAddDept || btn == btnNewUser || btn == btnSavePermissions);
                // Thêm viền mỏng cho các nút phụ, bỏ viền cho các nút chính (Save, Add)
                btn.FlatAppearance.BorderSize = (btn == btnAddDept || btn == btnNewUser || btn == btnSavePermissions) ? 0 : 1;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 243, 244); // Hover màu xám sáng
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(215, 230, 252); // Click màu xanh nhạt
            }

            UpdateSplitContainerDistances();
            LayoutDepartmentButtons();
            LayoutSearchPanel();
        }

        private void UpdateSplitContainerDistances()
        {
            if (splitContainer1.Width > 0)
            {
                int desired = Math.Min(280, Math.Max(230, splitContainer1.Width / 5));
                if (splitContainer1.SplitterDistance != desired && desired < splitContainer1.Width - splitContainer1.Panel2MinSize)
                    splitContainer1.SplitterDistance = desired;
            }

            if (splitContainer2.Width > 0)
            {
                int desired = Math.Min(320, Math.Max(270, splitContainer2.Width / 4));
                if (splitContainer2.SplitterDistance != desired && desired < splitContainer2.Width - splitContainer2.Panel2MinSize)
                    splitContainer2.SplitterDistance = desired;
            }
        }

        private void LayoutDepartmentButtons()
        {
            int gap = 6;
            int top = 9;
            int width = Math.Max(64, (panelDeptActions.ClientSize.Width - gap * 4) / 3);

            btnAddDept.SetBounds(gap, top, width, 34);
            btnEditDept.SetBounds(gap * 2 + width, top, width, 34);
            btnDeleteDept.SetBounds(gap * 3 + width * 2, top, width, 34);
        }

        private void LayoutSearchPanel()
        {
            int top = 9;
            int left = 8;
            int gap = 8;

            btnNewUser.SetBounds(Math.Max(left, panelSearch.ClientSize.Width - btnNewUser.Width - left), top, 180, 34);

            int maxSearchWidth = Math.Max(220, btnNewUser.Left - left - btnSearch.Width - gap * 2);
            int searchWidth = Math.Min(380, maxSearchWidth);
            txtSearch.SetBounds(left, top + 2, searchWidth, 30);
            btnSearch.SetBounds(txtSearch.Right + gap, top, 110, 34);
        }

        private async void FrmUserManagement_Load(object sender, EventArgs e)
        {
            await LoadDataAsync();
            ApplyPermissions();
        }

        private static DataTable CreateEmptyDepartmentsTable()
        {
            var table = new DataTable();
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("name", typeof(string));
            return table;
        }

        public async Task LoadDataAsync()
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                // Tải dữ liệu bất đồng bộ từ MySQL
                DataTable dtDepts = CreateEmptyDepartmentsTable();
                DataTable dtRoles = await Task.Run(() => DatabaseService.ExecuteQuery("SELECT Id AS id, Name AS name FROM [dbo].[CustomerRole] WHERE Active = 1 ORDER BY Id"));
                DataTable dtPerms = await Task.Run(() => DatabaseService.ExecuteQuery("SELECT Id AS id, Name AS name, SystemName AS code, Category AS group_name FROM [dbo].[PermissionRecord] ORDER BY Category, Name"));

                // 1. Hiển thị danh sách phòng ban lên TreeView tvDepartments
                tvDepartments.Nodes.Clear();
                TreeNode rootNode = new TreeNode("Tất cả phòng ban")
                {
                    Tag = -1,
                    ImageIndex = 0,
                    SelectedImageIndex = 0
                };
                tvDepartments.Nodes.Add(rootNode);

                foreach (DataRow row in dtDepts.Rows)
                {
                    TreeNode deptNode = new TreeNode(row["name"].ToString())
                    {
                        Tag = Convert.ToInt32(row["id"]),
                        ImageIndex = 1,
                        SelectedImageIndex = 1
                    };
                    rootNode.Nodes.Add(deptNode);
                }
                tvDepartments.ExpandAll();
                tvDepartments.SelectedNode = rootNode;

                // 2. Hiển thị danh sách vai trò lên ListBox lstRoles
                lstRoles.DataSource = dtRoles;
                lstRoles.DisplayMember = "name";
                lstRoles.ValueMember = "id";

                // 3. Hiển thị danh sách quyền lên TreeView tvPermissions
                tvPermissions.Nodes.Clear();
                var groups = dtPerms.AsEnumerable()
                                    .GroupBy(row => row.Field<string>("group_name"))
                                    .OrderBy(g => g.Key);

                foreach (var grp in groups)
                {
                    TreeNode groupNode = new TreeNode(grp.Key)
                    {
                        ImageIndex = 0,
                        SelectedImageIndex = 0
                    };
                    
                    foreach (var row in grp)
                    {
                        PermissionItem perm = new PermissionItem
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Name = row["name"].ToString(),
                            Code = row["code"].ToString(),
                            GroupName = row["group_name"].ToString()
                        };
                        
                        TreeNode permNode = new TreeNode(perm.Name)
                        {
                            Tag = perm,
                            ImageIndex = 1,
                            SelectedImageIndex = 1
                        };
                        groupNode.Nodes.Add(permNode);
                    }
                    tvPermissions.Nodes.Add(groupNode);
                }
                tvPermissions.ExpandAll();

                // 4. Tải danh sách nhân viên
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu quản trị: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void LoadUsers()
        {
            try
            {
                string sql = @"
                    SELECT
                        c.Id AS id,
                        COALESCE(c.Username, c.Email) AS username,
                        c.Email AS email,
                        COALESCE(c.Title, c.Username, c.Email) AS full_name,
                        cr.Id AS role_id,
                        cr.Name AS role_name,
                        CAST(NULL AS int) AS department_id,
                        CAST(NULL AS nvarchar(100)) AS department_name,
                        CASE WHEN c.Active = 1 AND c.Deleted = 0 THEN 'active' ELSE 'inactive' END AS status
                    FROM [dbo].[Customer] c
                    OUTER APPLY (
                        SELECT TOP 1 r.Id, r.Name
                        FROM [dbo].[Customer_CustomerRole_Mapping] m
                        INNER JOIN [dbo].[CustomerRole] r ON r.Id = m.CustomerRole_Id
                        WHERE m.Customer_Id = c.Id AND r.Active = 1
                        ORDER BY CASE WHEN r.SystemName = 'Administrators' THEN 0 ELSE 1 END, r.Id
                    ) cr
                    WHERE c.Deleted = 0
                    ORDER BY c.Id";
                _usersTable = DatabaseService.ExecuteQuery(sql);
                FilterAndDisplayUsers();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải người dùng: " + ex.Message);
            }
        }

        private void FilterAndDisplayUsers()
        {
            if (_usersTable == null) return;

            DataView dv = new DataView(_usersTable);
            string filter = "";

            // Lọc theo phòng ban đã chọn ở TreeView
            if (tvDepartments.SelectedNode != null && tvDepartments.SelectedNode.Tag != null)
            {
                int deptId = (int)tvDepartments.SelectedNode.Tag;
                if (deptId != -1)
                {
                    filter += "department_id = " + deptId;
                }
            }

            // Lọc theo từ khóa tìm kiếm
            string keyword = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(keyword) && keyword != "Tìm theo tên hoặc tài khoản...")
            {
                if (filter.Length > 0) filter += " AND ";
                // Escape các ký tự đặc biệt để tránh lỗi cú pháp filter
                string cleanKeyword = FunctionUtils.EscapeDataViewFilterValue(keyword);
                filter += string.Format("(full_name LIKE '%{0}%' OR username LIKE '%{0}%' OR email LIKE '%{0}%')", cleanKeyword);
            }

            dv.RowFilter = filter;
            dgvUsers.DataSource = dv;

            // Việt hóa tiêu đề các cột của DataGridView
            if (dgvUsers.Columns.Count > 0)
            {
                dgvUsers.Columns["id"].Visible = false;
                dgvUsers.Columns["role_id"].Visible = false;
                dgvUsers.Columns["department_id"].Visible = false;

                dgvUsers.Columns["username"].HeaderText = "Tên đăng nhập";
                dgvUsers.Columns["full_name"].HeaderText = "Họ và tên";
                dgvUsers.Columns["email"].HeaderText = "Email";
                dgvUsers.Columns["role_name"].HeaderText = "Vai trò";
                dgvUsers.Columns["department_name"].HeaderText = "Phòng ban";
                dgvUsers.Columns["status"].HeaderText = "Trạng thái";

                dgvUsers.Columns["username"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dgvUsers.Columns["username"].Width = 150;
                dgvUsers.Columns["email"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvUsers.Columns["email"].MinimumWidth = 180;
                dgvUsers.Columns["full_name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvUsers.Columns["full_name"].MinimumWidth = 180;
                dgvUsers.Columns["role_name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dgvUsers.Columns["role_name"].Width = 140;
                dgvUsers.Columns["department_name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dgvUsers.Columns["department_name"].Width = 140;
                dgvUsers.Columns["status"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dgvUsers.Columns["status"].Width = 120;
            }
        }

        private void DgvUsers_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvUsers.Columns[e.ColumnIndex].Name == "status" && e.Value != null)
            {
                string val = e.Value.ToString();
                if (val == "active") e.Value = "Hoạt động";
                else if (val == "inactive") e.Value = "Ngưng hoạt động";
                else if (val == "suspended") e.Value = "Tạm khóa";
            }
        }

        private void TvDepartments_AfterSelect(object sender, TreeViewEventArgs e)
        {
            FilterAndDisplayUsers();
        }

        private void LstRoles_SelectedIndexChanged(object sender, EventArgs e)
        {
            int roleId;
            if (lstRoles.SelectedValue is DataRowView drv)
                roleId = Convert.ToInt32(drv["id"]);
            else if (lstRoles.SelectedValue is int idVal)
                roleId = idVal;
            else if (lstRoles.SelectedValue != null && int.TryParse(lstRoles.SelectedValue.ToString(), out int parsedId))
                roleId = parsedId;
            else
                return;

            _isUpdatingTreeSelection = true;
            _isLoadingPermissions = true;
            try
            {
                // Tải các quyền hiện tại của vai trò
                List<string> currentPermissions = DatabaseService.GetUserPermissions(roleId);

                // Đi qua tất cả các node để set Checked
                foreach (TreeNode parentNode in tvPermissions.Nodes)
                {
                    bool anyChildChecked = false;
                    foreach (TreeNode childNode in parentNode.Nodes)
                    {
                        var item = childNode.Tag as PermissionItem;
                        if (item != null)
                        {
                            childNode.Checked = currentPermissions.Contains(item.Code);
                            if (childNode.Checked) anyChildChecked = true;
                        }
                    }
                    parentNode.Checked = anyChildChecked;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi đồng bộ quyền: " + ex.Message);
            }
            finally
            {
                _isLoadingPermissions = false;
                _isUpdatingTreeSelection = false;
            }
        }

        private void BtnSavePermissions_Click(object sender, EventArgs e)
        {
            if (!ECQ_Soft.Helper.UserSession.HasPermission("role:manage"))
            {
                MessageBox.Show("Bạn không có quyền lưu cấu hình phân quyền!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int roleId;
            if (lstRoles.SelectedValue is DataRowView drv)
                roleId = Convert.ToInt32(drv["id"]);
            else if (lstRoles.SelectedValue is int idVal)
                roleId = idVal;
            else if (lstRoles.SelectedValue != null && int.TryParse(lstRoles.SelectedValue.ToString(), out int parsedId))
                roleId = parsedId;
            else
            {
                MessageBox.Show("Vui lòng chọn vai trò để lưu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Xóa toàn bộ liên kết quyền cũ
                DatabaseService.ExecuteNonQuery("DELETE FROM [dbo].[PermissionRecord_Role_Mapping] WHERE CustomerRole_Id = @roleId", new SqlParameter[] {
                    new SqlParameter("@roleId", roleId)
                });

                // Lưu các liên kết quyền mới bằng cách duyệt qua TreeView
                foreach (TreeNode parentNode in tvPermissions.Nodes)
                {
                    foreach (TreeNode childNode in parentNode.Nodes)
                    {
                        if (childNode.Checked)
                        {
                            var item = childNode.Tag as PermissionItem;
                            if (item != null)
                            {
                                DatabaseService.ExecuteNonQuery("INSERT INTO [dbo].[PermissionRecord_Role_Mapping] (CustomerRole_Id, PermissionRecord_Id) VALUES (@roleId, @permId)", new SqlParameter[] {
                                    new SqlParameter("@roleId", roleId),
                                    new SqlParameter("@permId", item.Id)
                                });
                            }
                        }
                    }
                }

                MessageBox.Show("Đã lưu cấu hình phân quyền thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Cập nhật lại session ngay lập tức nếu thay đổi quyền của chính user hiện tại
                if (Helper.UserSession.RoleId == roleId)
                {
                    Helper.UserSession.Permissions = DatabaseService.GetUserPermissions(roleId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu phân quyền: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool _isUpdatingTreeSelection = false;
        private void TvPermissions_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_isUpdatingTreeSelection) return;
            _isUpdatingTreeSelection = true;
            try
            {
                // Nếu click vào Node cha (Nhóm quyền), tự động check/uncheck tất cả các Node con
                if (e.Node.Nodes.Count > 0)
                {
                    foreach (TreeNode child in e.Node.Nodes)
                    {
                        child.Checked = e.Node.Checked;
                    }
                }
                // Nếu click vào Node con, cập nhật trạng thái Node cha
                else if (e.Node.Parent != null)
                {
                    bool anyChecked = false;
                    foreach (TreeNode child in e.Node.Parent.Nodes)
                    {
                        if (child.Checked)
                        {
                            anyChecked = true;
                            break;
                        }
                    }
                    e.Node.Parent.Checked = anyChecked;
                }
            }
            finally
            {
                _isUpdatingTreeSelection = false;
            }
        }

        // --- Quản lý Phòng ban ---

        private void BtnAddDept_Click(object sender, EventArgs e)
        {
            if (!ECQ_Soft.Helper.UserSession.HasPermission("user:manage"))
            {
                MessageBox.Show("Bạn không có quyền thêm phòng ban!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string name = ShowInputBox("Thêm phòng ban", "Nhập tên phòng ban mới:");
            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                DatabaseService.ExecuteNonQuery("INSERT INTO departments (name) VALUES (@name)", new SqlParameter[] {
                    new SqlParameter("@name", name.Trim())
                });
                MessageBox.Show("Thêm phòng ban thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _ = LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi thêm phòng ban: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEditDept_Click(object sender, EventArgs e)
        {
            if (!ECQ_Soft.Helper.UserSession.HasPermission("user:manage"))
            {
                MessageBox.Show("Bạn không có quyền sửa phòng ban!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (tvDepartments.SelectedNode == null || tvDepartments.SelectedNode.Tag == null)
            {
                MessageBox.Show("Vui lòng chọn phòng ban cần sửa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int deptId = (int)tvDepartments.SelectedNode.Tag;
            if (deptId == -1)
            {
                MessageBox.Show("Không thể chỉnh sửa nút gốc!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string oldName = tvDepartments.SelectedNode.Text;
            string newName = ShowInputBox("Sửa phòng ban", "Nhập tên phòng ban mới:", oldName);
            if (string.IsNullOrWhiteSpace(newName) || newName == oldName) return;

            try
            {
                DatabaseService.ExecuteNonQuery("UPDATE departments SET name = @name WHERE id = @id", new SqlParameter[] {
                    new SqlParameter("@name", newName.Trim()),
                    new SqlParameter("@id", deptId)
                });
                MessageBox.Show("Cập nhật phòng ban thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _ = LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật phòng ban: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteDept_Click(object sender, EventArgs e)
        {
            if (!ECQ_Soft.Helper.UserSession.HasPermission("user:manage"))
            {
                MessageBox.Show("Bạn không có quyền xóa phòng ban!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (tvDepartments.SelectedNode == null || tvDepartments.SelectedNode.Tag == null)
            {
                MessageBox.Show("Vui lòng chọn phòng ban cần xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int deptId = (int)tvDepartments.SelectedNode.Tag;
            if (deptId == -1)
            {
                MessageBox.Show("Không thể xóa nút gốc!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Kiểm tra xem phòng ban có nhân viên nào không
                DataTable dt = DatabaseService.ExecuteQuery("SELECT COUNT(*) FROM users WHERE department_id = @id", new SqlParameter[] {
                    new SqlParameter("@id", deptId)
                });
                int usersInDept = Convert.ToInt32(dt.Rows[0][0]);
                if (usersInDept > 0)
                {
                    MessageBox.Show($"Không thể xóa phòng ban này vì đang có {usersInDept} nhân viên thuộc phòng!", "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult result = MessageBox.Show($"Bạn có chắc chắn muốn xóa phòng ban '{tvDepartments.SelectedNode.Text}'?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    DatabaseService.ExecuteNonQuery("DELETE FROM departments WHERE id = @id", new SqlParameter[] {
                        new SqlParameter("@id", deptId)
                    });
                    MessageBox.Show("Xóa phòng ban thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _ = LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xóa phòng ban: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- Tìm kiếm & Quản lý Nhân viên ---

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            FilterAndDisplayUsers();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                FilterAndDisplayUsers();
                e.Handled = true;
                e.SuppressKeyPress = true; // Chặn tiếng kêu bip bip từ hệ thống
            }
        }

        private void TxtSearch_Enter(object sender, EventArgs e)
        {
            if (txtSearch.Text == "Tìm theo tên hoặc tài khoản...")
            {
                txtSearch.Text = "";
                txtSearch.ForeColor = Color.Black;
            }
        }

        private void TxtSearch_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Tìm theo tên hoặc tài khoản...";
                txtSearch.ForeColor = Color.Gray;
            }
        }

        private void BtnNewUser_Click(object sender, EventArgs e)
        {
            if (!ECQ_Soft.Helper.UserSession.HasPermission("user:manage"))
            {
                MessageBox.Show("Bạn không có quyền thêm nhân viên mới!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var modal = new FrmUserEditModal())
            {
                modal.UserId = 0; // Chế độ thêm mới
                if (modal.ShowDialog(this) == DialogResult.OK)
                {
                    LoadUsers(); // Tải lại danh sách nhân viên
                }
            }
        }

        private void DgvUsers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!ECQ_Soft.Helper.UserSession.HasPermission("user:manage"))
            {
                MessageBox.Show("Bạn không có quyền chỉnh sửa nhân viên!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (e.RowIndex < 0) return; // Bỏ qua nhấp đúp vào tiêu đề cột

            var row = dgvUsers.Rows[e.RowIndex];
            int userId = Convert.ToInt32(row.Cells["id"].Value);

            using (var modal = new FrmUserEditModal())
            {
                modal.UserId = userId; // Chế độ chỉnh sửa
                if (modal.ShowDialog(this) == DialogResult.OK)
                {
                    LoadUsers(); // Tải lại
                }
            }
        }

        // --- Hộp thoại nhập văn bản tự tạo (Input Box) để tránh dùng VisualBasic ---
        private string ShowInputBox(string title, string promptText, string defaultValue = "")
        {
            using (Form form = new Form())
            {
                Label label = new Label();
                TextBox textBox = new TextBox();
                Button buttonOk = new Button();
                Button buttonCancel = new Button();

                form.Text = title;
                label.Text = promptText;
                textBox.Text = defaultValue;

                buttonOk.Text = "OK";
                buttonCancel.Text = "Hủy";
                buttonOk.DialogResult = DialogResult.OK;
                buttonCancel.DialogResult = DialogResult.Cancel;

                label.SetBounds(12, 15, 372, 18);
                textBox.SetBounds(12, 36, 372, 25);
                buttonOk.SetBounds(228, 75, 75, 28);
                buttonCancel.SetBounds(309, 75, 75, 28);

                label.AutoSize = true;
                textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
                buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

                form.ClientSize = new Size(396, 115);
                form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.AcceptButton = buttonOk;
                form.CancelButton = buttonCancel;

                // Thêm CSS Styling giống tông màu ứng dụng
                form.BackColor = Color.White;
                label.Font = new Font("Segoe UI", 9.5F);
                textBox.Font = new Font("Segoe UI", 10F);
                buttonOk.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                buttonOk.BackColor = Color.FromArgb(26, 115, 232);
                buttonOk.ForeColor = Color.White;
                buttonOk.FlatStyle = FlatStyle.Flat;
                buttonOk.FlatAppearance.BorderSize = 0;
                buttonCancel.Font = new Font("Segoe UI", 9.5F);
                buttonCancel.FlatStyle = FlatStyle.Flat;
                buttonCancel.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);

                DialogResult dialogResult = form.ShowDialog();
                return dialogResult == DialogResult.OK ? textBox.Text : null;
            }
        }

        private void ApplyPermissions()
        {
            bool hasUserManage = ECQ_Soft.Helper.UserSession.HasPermission("user:manage");
            bool hasRoleManage = ECQ_Soft.Helper.UserSession.HasPermission("role:manage");

            btnNewUser.Enabled = hasUserManage;
            if (!hasUserManage) btnNewUser.BackColor = Color.Gray;

            btnAddDept.Enabled = false;
            btnEditDept.Enabled = false;
            btnDeleteDept.Enabled = false;
            btnAddDept.BackColor = Color.Gray;
            btnEditDept.BackColor = Color.Gray;
            btnDeleteDept.BackColor = Color.Gray;

            btnSavePermissions.Enabled = hasRoleManage;
            if (!hasRoleManage) btnSavePermissions.BackColor = Color.Gray;

            tvPermissions.Enabled = hasRoleManage;
        }
    }
}
