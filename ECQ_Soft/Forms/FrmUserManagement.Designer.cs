namespace ECQ_Soft
{
    partial class FrmUserManagement
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabUsers = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.groupDept = new System.Windows.Forms.GroupBox();
            this.tvDepartments = new System.Windows.Forms.TreeView();
            this.panelDeptActions = new System.Windows.Forms.Panel();
            this.btnDeleteDept = new System.Windows.Forms.Button();
            this.btnEditDept = new System.Windows.Forms.Button();
            this.btnAddDept = new System.Windows.Forms.Button();
            this.groupUsers = new System.Windows.Forms.GroupBox();
            this.dgvUsers = new System.Windows.Forms.DataGridView();
            this.panelSearch = new System.Windows.Forms.Panel();
            this.btnNewUser = new System.Windows.Forms.Button();
            this.btnSearch = new System.Windows.Forms.Button();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.tabRoles = new System.Windows.Forms.TabPage();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.groupRoles = new System.Windows.Forms.GroupBox();
            this.lstRoles = new System.Windows.Forms.ListBox();
            this.groupPermissions = new System.Windows.Forms.GroupBox();
            this.btnSavePermissions = new System.Windows.Forms.Button();
            this.tvPermissions = new System.Windows.Forms.TreeView();
            this.tabControl1.SuspendLayout();
            this.tabUsers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupDept.SuspendLayout();
            this.panelDeptActions.SuspendLayout();
            this.groupUsers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUsers)).BeginInit();
            this.panelSearch.SuspendLayout();
            this.tabRoles.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.groupRoles.SuspendLayout();
            this.groupPermissions.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabUsers);
            this.tabControl1.Controls.Add(this.tabRoles);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1200, 700);
            this.tabControl1.TabIndex = 0;
            // 
            // tabUsers
            // 
            this.tabUsers.Controls.Add(this.splitContainer1);
            this.tabUsers.Location = new System.Drawing.Point(4, 32);
            this.tabUsers.Name = "tabUsers";
            this.tabUsers.Padding = new System.Windows.Forms.Padding(10);
            this.tabUsers.Size = new System.Drawing.Size(1192, 664);
            this.tabUsers.TabIndex = 0;
            this.tabUsers.Text = "Nhân viên & Phòng ban";
            this.tabUsers.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(10, 10);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupDept);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.groupUsers);
            this.splitContainer1.Size = new System.Drawing.Size(1172, 644);
            this.splitContainer1.SplitterDistance = 290;
            this.splitContainer1.TabIndex = 0;
            // 
            // groupDept
            // 
            this.groupDept.Controls.Add(this.tvDepartments);
            this.groupDept.Controls.Add(this.panelDeptActions);
            this.groupDept.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupDept.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold);
            this.groupDept.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(115)))), ((int)(((byte)(232)))));
            this.groupDept.Location = new System.Drawing.Point(0, 0);
            this.groupDept.Name = "groupDept";
            this.groupDept.Size = new System.Drawing.Size(290, 644);
            this.groupDept.TabIndex = 0;
            this.groupDept.TabStop = false;
            this.groupDept.Text = "Phòng ban";
            // 
            // tvDepartments
            // 
            this.tvDepartments.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tvDepartments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvDepartments.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tvDepartments.FullRowSelect = true;
            this.tvDepartments.HideSelection = false;
            this.tvDepartments.ItemHeight = 28;
            this.tvDepartments.Location = new System.Drawing.Point(3, 27);
            this.tvDepartments.Name = "tvDepartments";
            this.tvDepartments.Size = new System.Drawing.Size(284, 564);
            this.tvDepartments.TabIndex = 0;
            // 
            // panelDeptActions
            // 
            this.panelDeptActions.Controls.Add(this.btnDeleteDept);
            this.panelDeptActions.Controls.Add(this.btnEditDept);
            this.panelDeptActions.Controls.Add(this.btnAddDept);
            this.panelDeptActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelDeptActions.Location = new System.Drawing.Point(3, 591);
            this.panelDeptActions.Name = "panelDeptActions";
            this.panelDeptActions.Size = new System.Drawing.Size(284, 50);
            this.panelDeptActions.TabIndex = 1;
            // 
            // btnDeleteDept
            // 
            this.btnDeleteDept.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.btnDeleteDept.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(232)))), ((int)(((byte)(240)))));
            this.btnDeleteDept.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeleteDept.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnDeleteDept.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(83)))), ((int)(((byte)(79)))));
            this.btnDeleteDept.Location = new System.Drawing.Point(190, 10);
            this.btnDeleteDept.Name = "btnDeleteDept";
            this.btnDeleteDept.Size = new System.Drawing.Size(85, 32);
            this.btnDeleteDept.TabIndex = 2;
            this.btnDeleteDept.Text = "Xóa";
            this.btnDeleteDept.UseVisualStyleBackColor = false;
            // 
            // btnEditDept
            // 
            this.btnEditDept.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.btnEditDept.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(232)))), ((int)(((byte)(240)))));
            this.btnEditDept.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEditDept.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnEditDept.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(74)))), ((int)(((byte)(85)))), ((int)(((byte)(104)))));
            this.btnEditDept.Location = new System.Drawing.Point(98, 10);
            this.btnEditDept.Name = "btnEditDept";
            this.btnEditDept.Size = new System.Drawing.Size(85, 32);
            this.btnEditDept.TabIndex = 1;
            this.btnEditDept.Text = "Sửa";
            this.btnEditDept.UseVisualStyleBackColor = false;
            // 
            // btnAddDept
            // 
            this.btnAddDept.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(115)))), ((int)(((byte)(232)))));
            this.btnAddDept.FlatAppearance.BorderSize = 0;
            this.btnAddDept.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddDept.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnAddDept.ForeColor = System.Drawing.Color.White;
            this.btnAddDept.Location = new System.Drawing.Point(6, 10);
            this.btnAddDept.Name = "btnAddDept";
            this.btnAddDept.Size = new System.Drawing.Size(85, 32);
            this.btnAddDept.TabIndex = 0;
            this.btnAddDept.Text = "Thêm";
            this.btnAddDept.UseVisualStyleBackColor = false;
            // 
            // groupUsers
            // 
            this.groupUsers.Controls.Add(this.dgvUsers);
            this.groupUsers.Controls.Add(this.panelSearch);
            this.groupUsers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupUsers.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold);
            this.groupUsers.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(115)))), ((int)(((byte)(232)))));
            this.groupUsers.Location = new System.Drawing.Point(0, 0);
            this.groupUsers.Name = "groupUsers";
            this.groupUsers.Size = new System.Drawing.Size(878, 644);
            this.groupUsers.TabIndex = 0;
            this.groupUsers.TabStop = false;
            this.groupUsers.Text = "Nhân viên";
            // 
            // dgvUsers
            // 
            this.dgvUsers.AllowUserToAddRows = false;
            this.dgvUsers.AllowUserToDeleteRows = false;
            this.dgvUsers.BackgroundColor = System.Drawing.Color.White;
            this.dgvUsers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvUsers.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvUsers.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(74)))), ((int)(((byte)(85)))), ((int)(((byte)(104)))));
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvUsers.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvUsers.ColumnHeadersHeight = 35;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(55)))), ((int)(((byte)(72)))));
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(240)))), ((int)(((byte)(254)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(115)))), ((int)(((byte)(232)))));
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvUsers.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvUsers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvUsers.EnableHeadersVisualStyles = false;
            this.dgvUsers.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(232)))), ((int)(((byte)(240)))));
            this.dgvUsers.Location = new System.Drawing.Point(3, 82);
            this.dgvUsers.MultiSelect = false;
            this.dgvUsers.Name = "dgvUsers";
            this.dgvUsers.ReadOnly = true;
            this.dgvUsers.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvUsers.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvUsers.RowHeadersVisible = false;
            this.dgvUsers.RowTemplate.Height = 35;
            this.dgvUsers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvUsers.Size = new System.Drawing.Size(872, 559);
            this.dgvUsers.TabIndex = 1;
            // 
            // panelSearch
            // 
            this.panelSearch.Controls.Add(this.btnNewUser);
            this.panelSearch.Controls.Add(this.btnSearch);
            this.panelSearch.Controls.Add(this.txtSearch);
            this.panelSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelSearch.Location = new System.Drawing.Point(3, 27);
            this.panelSearch.Name = "panelSearch";
            this.panelSearch.Size = new System.Drawing.Size(872, 55);
            this.panelSearch.TabIndex = 0;
            // 
            // btnNewUser
            // 
            this.btnNewUser.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnNewUser.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(115)))), ((int)(((byte)(232)))));
            this.btnNewUser.FlatAppearance.BorderSize = 0;
            this.btnNewUser.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNewUser.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnNewUser.ForeColor = System.Drawing.Color.White;
            this.btnNewUser.Location = new System.Drawing.Point(716, 11);
            this.btnNewUser.Name = "btnNewUser";
            this.btnNewUser.Size = new System.Drawing.Size(148, 33);
            this.btnNewUser.TabIndex = 2;
            this.btnNewUser.Text = "+ Thêm nhân viên";
            this.btnNewUser.UseVisualStyleBackColor = false;
            // 
            // btnSearch
            // 
            this.btnSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.btnSearch.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(232)))), ((int)(((byte)(240)))));
            this.btnSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSearch.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnSearch.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(74)))), ((int)(((byte)(85)))), ((int)(((byte)(104)))));
            this.btnSearch.Location = new System.Drawing.Point(315, 11);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(100, 33);
            this.btnSearch.TabIndex = 1;
            this.btnSearch.Text = "Tìm kiếm";
            this.btnSearch.UseVisualStyleBackColor = false;
            // 
            // txtSearch
            // 
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtSearch.ForeColor = System.Drawing.Color.Gray;
            this.txtSearch.Location = new System.Drawing.Point(9, 13);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(300, 30);
            this.txtSearch.TabIndex = 0;
            this.txtSearch.Text = "Tìm theo tên hoặc tài khoản...";
            // 
            // tabRoles
            // 
            this.tabRoles.Controls.Add(this.splitContainer2);
            this.tabRoles.Location = new System.Drawing.Point(4, 32);
            this.tabRoles.Name = "tabRoles";
            this.tabRoles.Padding = new System.Windows.Forms.Padding(10);
            this.tabRoles.Size = new System.Drawing.Size(1192, 664);
            this.tabRoles.TabIndex = 1;
            this.tabRoles.Text = "Vai trò & Phân quyền";
            this.tabRoles.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(10, 10);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.groupRoles);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.groupPermissions);
            this.splitContainer2.Size = new System.Drawing.Size(1172, 644);
            this.splitContainer2.SplitterDistance = 350;
            this.splitContainer2.TabIndex = 0;
            // 
            // groupRoles
            // 
            this.groupRoles.Controls.Add(this.lstRoles);
            this.groupRoles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupRoles.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold);
            this.groupRoles.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(115)))), ((int)(((byte)(232)))));
            this.groupRoles.Location = new System.Drawing.Point(0, 0);
            this.groupRoles.Name = "groupRoles";
            this.groupRoles.Size = new System.Drawing.Size(350, 644);
            this.groupRoles.TabIndex = 0;
            this.groupRoles.TabStop = false;
            this.groupRoles.Text = "Vai trò";
            // 
            // lstRoles
            // 
            this.lstRoles.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstRoles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstRoles.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.lstRoles.FormattingEnabled = true;
            this.lstRoles.ItemHeight = 25;
            this.lstRoles.Location = new System.Drawing.Point(3, 27);
            this.lstRoles.Name = "lstRoles";
            this.lstRoles.Size = new System.Drawing.Size(344, 614);
            this.lstRoles.TabIndex = 0;
            // 
            // groupPermissions
            // 
            this.groupPermissions.Controls.Add(this.btnSavePermissions);
            this.groupPermissions.Controls.Add(this.tvPermissions);
            this.groupPermissions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupPermissions.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold);
            this.groupPermissions.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(115)))), ((int)(((byte)(232)))));
            this.groupPermissions.Location = new System.Drawing.Point(0, 0);
            this.groupPermissions.Name = "groupPermissions";
            this.groupPermissions.Size = new System.Drawing.Size(818, 644);
            this.groupPermissions.TabIndex = 0;
            this.groupPermissions.TabStop = false;
            this.groupPermissions.Text = "Danh sách quyền";
            // 
            // btnSavePermissions
            // 
            this.btnSavePermissions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSavePermissions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(115)))), ((int)(((byte)(232)))));
            this.btnSavePermissions.FlatAppearance.BorderSize = 0;
            this.btnSavePermissions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSavePermissions.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSavePermissions.ForeColor = System.Drawing.Color.White;
            this.btnSavePermissions.Location = new System.Drawing.Point(628, 591);
            this.btnSavePermissions.Name = "btnSavePermissions";
            this.btnSavePermissions.Size = new System.Drawing.Size(184, 38);
            this.btnSavePermissions.TabIndex = 1;
            this.btnSavePermissions.Text = "Lưu phân quyền";
            this.btnSavePermissions.UseVisualStyleBackColor = false;
            // 
            // tvPermissions
            // 
            this.tvPermissions.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tvPermissions.CheckBoxes = true;
            this.tvPermissions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvPermissions.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tvPermissions.Location = new System.Drawing.Point(3, 27);
            this.tvPermissions.Name = "tvPermissions";
            this.tvPermissions.Size = new System.Drawing.Size(812, 614);
            this.tvPermissions.TabIndex = 0;
            // 
            // FrmUserManagement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FrmUserManagement";
            this.Text = "Quản lý nhân sự";
            this.Load += new System.EventHandler(this.FrmUserManagement_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabUsers.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupDept.ResumeLayout(false);
            this.panelDeptActions.ResumeLayout(false);
            this.groupUsers.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvUsers)).EndInit();
            this.panelSearch.ResumeLayout(false);
            this.panelSearch.PerformLayout();
            this.tabRoles.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.groupRoles.ResumeLayout(false);
            this.groupPermissions.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabUsers;
        private System.Windows.Forms.TabPage tabRoles;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.GroupBox groupDept;
        private System.Windows.Forms.TreeView tvDepartments;
        private System.Windows.Forms.Panel panelDeptActions;
        private System.Windows.Forms.Button btnDeleteDept;
        private System.Windows.Forms.Button btnEditDept;
        private System.Windows.Forms.Button btnAddDept;
        private System.Windows.Forms.GroupBox groupUsers;
        private System.Windows.Forms.Panel panelSearch;
        private System.Windows.Forms.Button btnNewUser;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.DataGridView dgvUsers;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.GroupBox groupRoles;
        private System.Windows.Forms.ListBox lstRoles;
        private System.Windows.Forms.GroupBox groupPermissions;
        private System.Windows.Forms.TreeView tvPermissions;
        private System.Windows.Forms.Button btnSavePermissions;
    }
}
