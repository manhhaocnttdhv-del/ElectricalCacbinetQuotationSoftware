namespace ECQ_Soft
{
    partial class FrmConfig
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
            this.dgvParentProducts = new System.Windows.Forms.DataGridView();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.button3 = new FontAwesome.Sharp.IconButton();
            this.button7 = new FontAwesome.Sharp.IconButton();
            this.btnSearch = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnOpenSearchModal = new FontAwesome.Sharp.IconButton();
            this.btn_baogia = new FontAwesome.Sharp.IconButton();
            this.btnAdvancedConfigBuild = new FontAwesome.Sharp.IconButton();
            this.button8 = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lblCurrentTab = new System.Windows.Forms.Label();
            this.btnChangeSheet = new FontAwesome.Sharp.IconButton();
            this.button10 = new FontAwesome.Sharp.IconButton();
            this.button4 = new FontAwesome.Sharp.IconButton();
            this.lstSavedConfigs = new ECQ_Soft.Helper.CheckedComboBox();
            this.btnOpenSearchModalForQuote = new FontAwesome.Sharp.IconButton();
            this.btnAdvancedConfigForQuotation = new FontAwesome.Sharp.IconButton();
            this.button5 = new FontAwesome.Sharp.IconButton();
            this.button6 = new FontAwesome.Sharp.IconButton();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            ((System.ComponentModel.ISupportInitialize)(this.dgvParentProducts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvParentProducts
            // 
            this.dgvParentProducts.AllowUserToAddRows = false;
            this.dgvParentProducts.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvParentProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvParentProducts.Location = new System.Drawing.Point(10, 80);
            this.dgvParentProducts.Name = "dgvParentProducts";
            this.dgvParentProducts.RowHeadersWidth = 51;
            this.dgvParentProducts.RowTemplate.Height = 36;
            this.dgvParentProducts.Size = new System.Drawing.Size(1454, 230);
            this.dgvParentProducts.TabIndex = 10;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(7, 82);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.RowTemplate.Height = 36;
            this.dataGridView1.Size = new System.Drawing.Size(1457, 232);
            this.dataGridView1.TabIndex = 33;
            // 
            // button3
            // 
            this.button3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(100)))), ((int)(((byte)(180)))));
            this.button3.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(100)))), ((int)(((byte)(180)))));
            this.button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button3.ForeColor = System.Drawing.Color.White;
            this.button3.Location = new System.Drawing.Point(310, 48);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(160, 32);
            this.button3.TabIndex = 7;
            this.button3.Text = "Đóng gói cấu hình";
            this.button3.IconChar = FontAwesome.Sharp.IconChar.BoxOpen;
            this.button3.IconColor = System.Drawing.Color.White;
            this.button3.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.button3.IconSize = 18;
            this.button3.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.button3.UseVisualStyleBackColor = false;
            // 
            // button7
            // 
            this.button7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(75)))), ((int)(((byte)(57)))));
            this.button7.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(75)))), ((int)(((byte)(57)))));
            this.button7.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button7.ForeColor = System.Drawing.Color.White;
            this.button7.Location = new System.Drawing.Point(1180, 48);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(120, 32);
            this.button7.TabIndex = 49;
            this.button7.Text = "Xóa tất cả";
            this.button7.IconChar = FontAwesome.Sharp.IconChar.TrashAlt;
            this.button7.IconColor = System.Drawing.Color.White;
            this.button7.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.button7.IconSize = 18;
            this.button7.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.button7.UseVisualStyleBackColor = false;
            // 
            // btnSearch
            // 
            this.btnSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(220)))));
            this.btnSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSearch.ForeColor = System.Drawing.Color.White;
            this.btnSearch.Location = new System.Drawing.Point(213, 48);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(91, 32);
            this.btnSearch.TabIndex = 59;
            this.btnSearch.Text = "Tìm kiếm";
            this.btnSearch.UseVisualStyleBackColor = false;
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(7, 52);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(200, 24);
            this.comboBox1.TabIndex = 57;
            // 
            // label3
            // 
            this.label3.AutoSize = false;
            this.label3.Location = new System.Drawing.Point(4, 32);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(150, 20);
            this.label3.TabIndex = 56;
            this.label3.Text = "Đóng gói cấu hình";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnOpenSearchModal);
            this.groupBox1.Controls.Add(this.btn_baogia);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.comboBox1);
            this.groupBox1.Controls.Add(this.btnSearch);
            this.groupBox1.Controls.Add(this.btnAdvancedConfigBuild);
            this.groupBox1.Controls.Add(this.button7);
            this.groupBox1.Controls.Add(this.button3);
            this.groupBox1.Controls.Add(this.dataGridView1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1480, 325);
            this.groupBox1.TabIndex = 73;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "XÂY DỰNG CẤU HÌNH   ";
            // 
            // btnOpenSearchModal
            // 
            this.btnOpenSearchModal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenSearchModal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(141)))), ((int)(((byte)(188)))));
            this.btnOpenSearchModal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenSearchModal.ForeColor = System.Drawing.Color.White;
            this.btnOpenSearchModal.Location = new System.Drawing.Point(1310, 48);
            this.btnOpenSearchModal.Name = "btnOpenSearchModal";
            this.btnOpenSearchModal.Size = new System.Drawing.Size(150, 32);
            this.btnOpenSearchModal.TabIndex = 56;
            this.btnOpenSearchModal.Text = "Thêm Sản Phẩm";
            this.btnOpenSearchModal.IconChar = FontAwesome.Sharp.IconChar.Plus;
            this.btnOpenSearchModal.IconColor = System.Drawing.Color.White;
            this.btnOpenSearchModal.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnOpenSearchModal.IconSize = 18;
            this.btnOpenSearchModal.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOpenSearchModal.UseVisualStyleBackColor = false;
            // 
            // btn_baogia
            // 
            this.btn_baogia.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_baogia.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(239)))));
            this.btn_baogia.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_baogia.ForeColor = System.Drawing.Color.White;
            this.btn_baogia.Location = new System.Drawing.Point(1000, 48);
            this.btn_baogia.Name = "btn_baogia";
            this.btn_baogia.Size = new System.Drawing.Size(170, 32);
            this.btn_baogia.TabIndex = 61;
            this.btn_baogia.Text = "Lưu xuống báo giá";
            this.btn_baogia.IconChar = FontAwesome.Sharp.IconChar.ArrowCircleDown;
            this.btn_baogia.IconColor = System.Drawing.Color.White;
            this.btn_baogia.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btn_baogia.IconSize = 18;
            this.btn_baogia.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btn_baogia.UseVisualStyleBackColor = false;
            this.btn_baogia.Click += new System.EventHandler(this.btn_baogia_Click);
            // 
            // btnAdvancedConfigBuild
            // 
            this.btnAdvancedConfigBuild.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAdvancedConfigBuild.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(102)))), ((int)(((byte)(102)))));
            this.btnAdvancedConfigBuild.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdvancedConfigBuild.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnAdvancedConfigBuild.ForeColor = System.Drawing.Color.White;
            this.btnAdvancedConfigBuild.Location = new System.Drawing.Point(810, 48);
            this.btnAdvancedConfigBuild.Name = "btnAdvancedConfigBuild";
            this.btnAdvancedConfigBuild.Size = new System.Drawing.Size(180, 32);
            this.btnAdvancedConfigBuild.TabIndex = 83;
            this.btnAdvancedConfigBuild.Text = "Cấu hình nâng cao";
            this.btnAdvancedConfigBuild.IconChar = FontAwesome.Sharp.IconChar.Tools;
            this.btnAdvancedConfigBuild.IconColor = System.Drawing.Color.White;
            this.btnAdvancedConfigBuild.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnAdvancedConfigBuild.IconSize = 18;
            this.btnAdvancedConfigBuild.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnAdvancedConfigBuild.UseVisualStyleBackColor = false;
            this.btnAdvancedConfigBuild.Click += new System.EventHandler(this.btnAdvancedConfigBuild_Click);
            // 
            // button8
            // 
            this.button8.BackColor = System.Drawing.Color.White;
            this.button8.Enabled = false;
            this.button8.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button8.Location = new System.Drawing.Point(721, 153);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(32, 32);
            this.button8.TabIndex = 74;
            this.button8.UseVisualStyleBackColor = false;
            this.button8.Visible = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lblCurrentTab);
            this.groupBox2.Controls.Add(this.btnChangeSheet);
            this.groupBox2.Controls.Add(this.button10);
            this.groupBox2.Controls.Add(this.button4);
            this.groupBox2.Controls.Add(this.dgvParentProducts);
            this.groupBox2.Controls.Add(this.lstSavedConfigs);
            this.groupBox2.Controls.Add(this.btnOpenSearchModalForQuote);
            this.groupBox2.Controls.Add(this.button6);
            this.groupBox2.Controls.Add(this.btnAdvancedConfigForQuotation);
            this.groupBox2.Controls.Add(this.button5);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(1480, 321);
            this.groupBox2.TabIndex = 75;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "BẢNG BÁO GIÁ/ DỰ TOÁN   ";
            // 
            // lblCurrentTab
            // 
            this.lblCurrentTab.AutoSize = false;
            this.lblCurrentTab.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblCurrentTab.ForeColor = System.Drawing.Color.MediumBlue;
            this.lblCurrentTab.Location = new System.Drawing.Point(10, 15);
            this.lblCurrentTab.Name = "lblCurrentTab";
            this.lblCurrentTab.Size = new System.Drawing.Size(300, 23);
            this.lblCurrentTab.TabIndex = 80;
            this.lblCurrentTab.Text = "Tab: [Chưa chọn]";
            // 
            // btnChangeSheet
            // 
            this.btnChangeSheet.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnChangeSheet.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(57)))), ((int)(((byte)(204)))), ((int)(((byte)(204)))));
            this.btnChangeSheet.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnChangeSheet.ForeColor = System.Drawing.Color.White;
            this.btnChangeSheet.Location = new System.Drawing.Point(1190, 15);
            this.btnChangeSheet.Name = "btnChangeSheet";
            this.btnChangeSheet.Size = new System.Drawing.Size(110, 32);
            this.btnChangeSheet.TabIndex = 66;
            this.btnChangeSheet.Text = "Đổi Tab";
            this.btnChangeSheet.IconChar = FontAwesome.Sharp.IconChar.Sync;
            this.btnChangeSheet.IconColor = System.Drawing.Color.White;
            this.btnChangeSheet.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnChangeSheet.IconSize = 18;
            this.btnChangeSheet.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnChangeSheet.UseVisualStyleBackColor = false;
            this.btnChangeSheet.Click += new System.EventHandler(this.btnChangeSheet_Click);
            // 
            // button10
            // 
            this.button10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(166)))), ((int)(((byte)(90)))));
            this.button10.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button10.ForeColor = System.Drawing.Color.White;
            this.button10.Location = new System.Drawing.Point(1070, 15);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(110, 32);
            this.button10.TabIndex = 66;
            this.button10.Text = "Xuất file";
            this.button10.IconChar = FontAwesome.Sharp.IconChar.FileExcel;
            this.button10.IconColor = System.Drawing.Color.White;
            this.button10.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.button10.IconSize = 18;
            this.button10.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.button10.UseVisualStyleBackColor = false;
            this.button10.Click += new System.EventHandler(this.btnExportFile_Click);
            // 
            // button4
            // 
            this.button4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(75)))), ((int)(((byte)(57)))));
            this.button4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button4.ForeColor = System.Drawing.Color.White;
            this.button4.Location = new System.Drawing.Point(940, 15);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(120, 32);
            this.button4.TabIndex = 64;
            this.button4.Text = "Xóa tất cả";
            this.button4.IconChar = FontAwesome.Sharp.IconChar.TrashAlt;
            this.button4.IconColor = System.Drawing.Color.White;
            this.button4.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.button4.IconSize = 18;
            this.button4.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.button4.UseVisualStyleBackColor = false;
            // 
            // lstSavedConfigs
            // 
            this.lstSavedConfigs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.lstSavedConfigs.FormattingEnabled = true;
            this.lstSavedConfigs.Location = new System.Drawing.Point(10, 45);
            this.lstSavedConfigs.Name = "lstSavedConfigs";
            this.lstSavedConfigs.Placeholder = "-- Chọn cấu hình --";
            this.lstSavedConfigs.Size = new System.Drawing.Size(203, 24);
            this.lstSavedConfigs.TabIndex = 62;
            // 
            // btnOpenSearchModalForQuote
            // 
            this.btnOpenSearchModalForQuote.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenSearchModalForQuote.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(141)))), ((int)(((byte)(188)))));
            this.btnOpenSearchModalForQuote.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenSearchModalForQuote.ForeColor = System.Drawing.Color.White;
            this.btnOpenSearchModalForQuote.Location = new System.Drawing.Point(1310, 15);
            this.btnOpenSearchModalForQuote.Name = "btnOpenSearchModalForQuote";
            this.btnOpenSearchModalForQuote.Size = new System.Drawing.Size(150, 32);
            this.btnOpenSearchModalForQuote.TabIndex = 81;
            this.btnOpenSearchModalForQuote.Text = "Thêm Sản Phẩm";
            this.btnOpenSearchModalForQuote.IconChar = FontAwesome.Sharp.IconChar.Plus;
            this.btnOpenSearchModalForQuote.IconColor = System.Drawing.Color.White;
            this.btnOpenSearchModalForQuote.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnOpenSearchModalForQuote.IconSize = 18;
            this.btnOpenSearchModalForQuote.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOpenSearchModalForQuote.UseVisualStyleBackColor = false;
            // 
            // btnAdvancedConfigForQuotation
            // 
            this.btnAdvancedConfigForQuotation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAdvancedConfigForQuotation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(102)))), ((int)(((byte)(102)))));
            this.btnAdvancedConfigForQuotation.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdvancedConfigForQuotation.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnAdvancedConfigForQuotation.ForeColor = System.Drawing.Color.White;
            this.btnAdvancedConfigForQuotation.Location = new System.Drawing.Point(605, 15);
            this.btnAdvancedConfigForQuotation.Name = "btnAdvancedConfigForQuotation";
            this.btnAdvancedConfigForQuotation.Size = new System.Drawing.Size(180, 32);
            this.btnAdvancedConfigForQuotation.TabIndex = 82;
            this.btnAdvancedConfigForQuotation.Text = "Cấu hình nâng cao";
            this.btnAdvancedConfigForQuotation.IconChar = FontAwesome.Sharp.IconChar.Cog;
            this.btnAdvancedConfigForQuotation.IconColor = System.Drawing.Color.White;
            this.btnAdvancedConfigForQuotation.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.btnAdvancedConfigForQuotation.IconSize = 18;
            this.btnAdvancedConfigForQuotation.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnAdvancedConfigForQuotation.UseVisualStyleBackColor = false;
            this.btnAdvancedConfigForQuotation.Click += new System.EventHandler(this.btnAdvancedConfigForQuotation_Click);
            // 
            // button5
            // 
            this.button5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(92)))), ((int)(((byte)(168)))));
            this.button5.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button5.ForeColor = System.Drawing.Color.White;
            this.button5.Location = new System.Drawing.Point(795, 15);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(135, 32);
            this.button5.TabIndex = 65;
            this.button5.Text = "Lưu báo giá";
            this.button5.IconChar = FontAwesome.Sharp.IconChar.Save;
            this.button5.IconColor = System.Drawing.Color.White;
            this.button5.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.button5.IconSize = 18;
            this.button5.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.button5.UseVisualStyleBackColor = false;
            // 
            // button6
            // 
            this.button6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.button6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(239)))));
            this.button6.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button6.ForeColor = System.Drawing.Color.White;
            this.button6.Location = new System.Drawing.Point(219, 41);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(130, 32);
            this.button6.TabIndex = 63;
            this.button6.Text = "Tải cấu hình";
            this.button6.IconChar = FontAwesome.Sharp.IconChar.FolderOpen;
            this.button6.IconColor = System.Drawing.Color.White;
            this.button6.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.button6.IconSize = 18;
            this.button6.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.button6.UseVisualStyleBackColor = false;
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 0);
            this.splitMain.Name = "splitMain";
            this.splitMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.groupBox1);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.groupBox2);
            this.splitMain.Size = new System.Drawing.Size(1480, 650);
            this.splitMain.SplitterDistance = 325;
            this.splitMain.TabIndex = 100;
            // 
            // FrmConfig
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.button8);
            this.Name = "FrmConfig";
            this.Size = new System.Drawing.Size(1480, 650);
            this.Font = new System.Drawing.Font("Times New Roman", 9F);
            this.Load += new System.EventHandler(this.FrmConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvParentProducts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.ResumeLayout(true);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvParentProducts;
        private System.Windows.Forms.DataGridView dataGridView1;
        private FontAwesome.Sharp.IconButton button3;
        private FontAwesome.Sharp.IconButton button7;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox1;
        private FontAwesome.Sharp.IconButton btnOpenSearchModal;
        private FontAwesome.Sharp.IconButton btn_baogia;
        private FontAwesome.Sharp.IconButton btnAdvancedConfigBuild;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label lblCurrentTab;
        private FontAwesome.Sharp.IconButton btnChangeSheet;
        private FontAwesome.Sharp.IconButton button10;
        private FontAwesome.Sharp.IconButton button4;
        private ECQ_Soft.Helper.CheckedComboBox lstSavedConfigs;
        private FontAwesome.Sharp.IconButton btnOpenSearchModalForQuote;
        private FontAwesome.Sharp.IconButton btnAdvancedConfigForQuotation;
        private FontAwesome.Sharp.IconButton button5;
        private FontAwesome.Sharp.IconButton button6;
        private System.Windows.Forms.SplitContainer splitMain;
    }
}
