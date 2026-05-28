namespace ECQ_Soft
{
    partial class FrmMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            this.panel1 = new System.Windows.Forms.Panel();
            this.lbUserName = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panelNavigation = new System.Windows.Forms.Panel();
            this.btnTabUser = new System.Windows.Forms.Button();
            this.btnTabConfig = new System.Windows.Forms.Button();
            this.btnTabRelation = new System.Windows.Forms.Button();
            this.btnTabQuotation = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.panel1.SuspendLayout();
            this.panelNavigation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Padding = new System.Windows.Forms.Padding(24, 0, 24, 0);
            this.panel1.Controls.Add(this.lbUserName);
            this.panel1.Controls.Add(this.btnRefresh);
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1924, 72);
            this.panel1.TabIndex = 23;
            this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // lbUserName
            // 
            this.lbUserName.BackColor = System.Drawing.Color.White;
            this.lbUserName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbUserName.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbUserName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(74)))), ((int)(((byte)(85)))), ((int)(((byte)(104)))));
            this.lbUserName.Location = new System.Drawing.Point(236, 0);
            this.lbUserName.Name = "lbUserName";
            this.lbUserName.Size = new System.Drawing.Size(1344, 72);
            this.lbUserName.TabIndex = 1;
            this.lbUserName.Text = "Xin chào, Admin";
            this.lbUserName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // button2
            // 
            this.button2.Dock = System.Windows.Forms.DockStyle.Right;
            this.button2.FlatAppearance.BorderSize = 0;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button2.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(83)))), ((int)(((byte)(79)))));
            this.button2.Location = new System.Drawing.Point(1760, 0);
            this.button2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(140, 72);
            this.button2.TabIndex = 2;
            this.button2.Text = "Đăng xuất";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click_1);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRefresh.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(115)))), ((int)(((byte)(232)))));
            this.btnRefresh.Location = new System.Drawing.Point(1580, 0);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(180, 72);
            this.btnRefresh.TabIndex = 3;
            this.btnRefresh.Text = "Tải lại dữ liệu";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.pictureBox1.Image = global::ECQ_Soft.Properties.Resources.VneccoLogo;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(212, 72);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // panelNavigation
            // 
            this.panelNavigation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.panelNavigation.Padding = new System.Windows.Forms.Padding(24, 0, 24, 3);
            this.panelNavigation.Controls.Add(this.btnTabUser);
            this.panelNavigation.Controls.Add(this.btnTabConfig);
            this.panelNavigation.Controls.Add(this.btnTabRelation);
            this.panelNavigation.Controls.Add(this.btnTabQuotation);
            this.panelNavigation.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelNavigation.Location = new System.Drawing.Point(0, 72);
            this.panelNavigation.Name = "panelNavigation";
            this.panelNavigation.Size = new System.Drawing.Size(1924, 50);
            this.panelNavigation.TabIndex = 25;
            this.panelNavigation.Paint += new System.Windows.Forms.PaintEventHandler(this.panelNavigation_Paint);
            // 
            // btnTabQuotation
            // 
            this.btnTabQuotation.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnTabQuotation.FlatAppearance.BorderSize = 0;
            this.btnTabQuotation.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabQuotation.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTabQuotation.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(74)))), ((int)(((byte)(85)))), ((int)(((byte)(104)))));
            this.btnTabQuotation.Location = new System.Drawing.Point(24, 0);
            this.btnTabQuotation.Name = "btnTabQuotation";
            this.btnTabQuotation.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnTabQuotation.Size = new System.Drawing.Size(260, 47);
            this.btnTabQuotation.TabIndex = 0;
            this.btnTabQuotation.Text = "Vỏ tủ và Thang máng";
            this.btnTabQuotation.UseVisualStyleBackColor = true;
            this.btnTabQuotation.Click += new System.EventHandler(this.btnTab_Click);
            // 
            // btnTabRelation
            // 
            this.btnTabRelation.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnTabRelation.FlatAppearance.BorderSize = 0;
            this.btnTabRelation.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabRelation.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTabRelation.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(74)))), ((int)(((byte)(85)))), ((int)(((byte)(104)))));
            this.btnTabRelation.Location = new System.Drawing.Point(284, 0);
            this.btnTabRelation.Name = "btnTabRelation";
            this.btnTabRelation.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnTabRelation.Size = new System.Drawing.Size(240, 47);
            this.btnTabRelation.TabIndex = 1;
            this.btnTabRelation.Text = "Liên kết sản phẩm";
            this.btnTabRelation.UseVisualStyleBackColor = true;
            this.btnTabRelation.Click += new System.EventHandler(this.btnTab_Click);
            // 
            // btnTabConfig
            // 
            this.btnTabConfig.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnTabConfig.FlatAppearance.BorderSize = 0;
            this.btnTabConfig.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabConfig.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTabConfig.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(74)))), ((int)(((byte)(85)))), ((int)(((byte)(104)))));
            this.btnTabConfig.Location = new System.Drawing.Point(524, 0);
            this.btnTabConfig.Name = "btnTabConfig";
            this.btnTabConfig.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnTabConfig.Size = new System.Drawing.Size(240, 47);
            this.btnTabConfig.TabIndex = 2;
            this.btnTabConfig.Text = "Báo giá và Tính toán";
            this.btnTabConfig.UseVisualStyleBackColor = true;
            this.btnTabConfig.Click += new System.EventHandler(this.btnTab_Click);
            // 
            // btnTabUser
            // 
            this.btnTabUser.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnTabUser.FlatAppearance.BorderSize = 0;
            this.btnTabUser.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabUser.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTabUser.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(74)))), ((int)(((byte)(85)))), ((int)(((byte)(104)))));
            this.btnTabUser.Location = new System.Drawing.Point(764, 0);
            this.btnTabUser.Name = "btnTabUser";
            this.btnTabUser.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.btnTabUser.Size = new System.Drawing.Size(240, 47);
            this.btnTabUser.TabIndex = 3;
            this.btnTabUser.Text = "Quản trị nhân viên";
            this.btnTabUser.UseVisualStyleBackColor = true;
            this.btnTabUser.Click += new System.EventHandler(this.btnTab_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.ItemSize = new System.Drawing.Size(0, 1);
            this.tabControl1.Location = new System.Drawing.Point(0, 122);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1924, 933);
            this.tabControl1.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabControl1.TabIndex = 24;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 5);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1916, 941);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(4, 5);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(1916, 941);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "tabPage3";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Location = new System.Drawing.Point(4, 5);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(1916, 941);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "tabPage4";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1924, 1055);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.panelNavigation);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ECQ Soft";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panel1.ResumeLayout(false);
            this.panelNavigation.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Label lbUserName;
        private System.Windows.Forms.Panel panelNavigation;
        private System.Windows.Forms.Button btnTabQuotation;
        private System.Windows.Forms.Button btnTabRelation;
        private System.Windows.Forms.Button btnTabConfig;
        private System.Windows.Forms.Button btnTabUser;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
    }
}

