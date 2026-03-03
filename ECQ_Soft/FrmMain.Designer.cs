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
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));

            // ── Controls ────────────────────────────────────────────────────
            this.panelHeader       = new System.Windows.Forms.Panel();
            this.pictureBox1       = new System.Windows.Forms.PictureBox();
            this.btnNavQuotation   = new System.Windows.Forms.Button();
            this.btnNavObjects     = new System.Windows.Forms.Button();
            this.lbUserName        = new System.Windows.Forms.Label();
            this.button2           = new System.Windows.Forms.Button();   // Đăng xuất

            // ── Panel nội dung 1: Báo giá ───────────────────────────────────
            this.pnlQuotation      = new System.Windows.Forms.Panel();
            this.groupBox2         = new System.Windows.Forms.GroupBox();
            this.gbGiaVon          = new System.Windows.Forms.GroupBox();
            this.label9            = new System.Windows.Forms.Label();
            this.txtGiaVon         = new System.Windows.Forms.TextBox();
            this.gbGiaBanVPA       = new System.Windows.Forms.GroupBox();
            this.label8            = new System.Windows.Forms.Label();
            this.txtGiaBanVPA      = new System.Windows.Forms.TextBox();
            this.btnReload         = new System.Windows.Forms.Button();
            this.gpGiaBan          = new System.Windows.Forms.GroupBox();
            this.label6            = new System.Windows.Forms.Label();
            this.txtGiaBan         = new System.Windows.Forms.TextBox();
            this.gpSonMa           = new System.Windows.Forms.GroupBox();
            this.label13           = new System.Windows.Forms.Label();
            this.txtSonMa          = new System.Windows.Forms.TextBox();
            this.btnUpdate         = new System.Windows.Forms.Button();
            this.gpChiPhiKhac      = new System.Windows.Forms.GroupBox();
            this.label14           = new System.Windows.Forms.Label();
            this.txtChiphikhac     = new System.Windows.Forms.TextBox();
            this.groupBox3         = new System.Windows.Forms.GroupBox();
            this.rbNone            = new System.Windows.Forms.RadioButton();
            this.rbMa              = new System.Windows.Forms.RadioButton();
            this.rbSon             = new System.Windows.Forms.RadioButton();
            this.txtDepth          = new System.Windows.Forms.TextBox();
            this.txtWidth          = new System.Windows.Forms.TextBox();
            this.txtHeight         = new System.Windows.Forms.TextBox();
            this.label7            = new System.Windows.Forms.Label();
            this.label2            = new System.Windows.Forms.Label();
            this.label1            = new System.Windows.Forms.Label();
            this.cboCabinetType    = new System.Windows.Forms.ComboBox();
            this.label3            = new System.Windows.Forms.Label();
            this.cboMaterial       = new System.Windows.Forms.ComboBox();
            this.label4            = new System.Windows.Forms.Label();
            this.dgvRecord         = new System.Windows.Forms.DataGridView();
            this.btnTinhGia        = new System.Windows.Forms.Button();
            this.btnAdd            = new System.Windows.Forms.Button();
            this.btnExporttoExcel  = new System.Windows.Forms.Button();
            this.label5            = new System.Windows.Forms.Label();
            this.lbName            = new System.Windows.Forms.Label();
            this.lbKhoiLuong       = new System.Windows.Forms.Label();
            this.label10           = new System.Windows.Forms.Label();
            this.button1           = new System.Windows.Forms.Button();
            this.lbDonGiaHME       = new System.Windows.Forms.Label();
            this.lbHME             = new System.Windows.Forms.Label();
            this.lbDonGiaVPA       = new System.Windows.Forms.Label();
            this.lbGiabanVPAText   = new System.Windows.Forms.Label();
            this.lbDonGiaThiTruong = new System.Windows.Forms.Label();
            this.lbGiabanTTText    = new System.Windows.Forms.Label();

            // ── Panel nội dung 2: Đối tượng ────────────────────────────────
            this.pnlObjects        = new System.Windows.Forms.Panel();
            this.pnlObjectsToolbar = new System.Windows.Forms.Panel();
            this.btnCauHinh        = new System.Windows.Forms.Button();
            this.btnAddObject      = new System.Windows.Forms.Button();
            this.dgvObjects        = new System.Windows.Forms.DataGridView();

            // SuspendLayout
            this.panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.pnlQuotation.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.gbGiaVon.SuspendLayout();
            this.gbGiaBanVPA.SuspendLayout();
            this.gpGiaBan.SuspendLayout();
            this.gpSonMa.SuspendLayout();
            this.gpChiPhiKhac.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecord)).BeginInit();
            this.pnlObjects.SuspendLayout();
            this.pnlObjectsToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvObjects)).BeginInit();
            this.SuspendLayout();

            // ════════════════════════════════════════════════════════════════
            // panelHeader — thanh điều hướng trên cùng
            // ════════════════════════════════════════════════════════════════
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(30, 30, 60);
            this.panelHeader.Dock      = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height    = 56;
            this.panelHeader.Name      = "panelHeader";
            this.panelHeader.Controls.Add(this.button2);        // Đăng xuất (Dock Right)
            this.panelHeader.Controls.Add(this.lbUserName);     // Fill
            this.panelHeader.Controls.Add(this.btnNavObjects);  // sau logo
            this.panelHeader.Controls.Add(this.btnNavQuotation);
            this.panelHeader.Controls.Add(this.pictureBox1);    // Dock Left

            // pictureBox1 – logo
            this.pictureBox1.Dock      = System.Windows.Forms.DockStyle.Left;
            this.pictureBox1.Image     = global::ECQ_Soft.Properties.Resources.VneccoLogo;
            this.pictureBox1.SizeMode  = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.Size      = new System.Drawing.Size(160, 56);
            this.pictureBox1.TabStop   = false;
            this.pictureBox1.Name      = "pictureBox1";

            // btnNavQuotation – "Báo giá"
            this.btnNavQuotation.Text      = "📋  Báo giá";
            this.btnNavQuotation.Name      = "btnNavQuotation";
            this.btnNavQuotation.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavQuotation.FlatAppearance.BorderSize = 0;
            this.btnNavQuotation.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.btnNavQuotation.ForeColor = System.Drawing.Color.White;
            this.btnNavQuotation.Font      = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnNavQuotation.Size      = new System.Drawing.Size(140, 56);
            this.btnNavQuotation.Location  = new System.Drawing.Point(165, 0);
            this.btnNavQuotation.TabIndex  = 1;
            this.btnNavQuotation.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnNavQuotation.Click    += new System.EventHandler(this.btnNavQuotation_Click);

            // btnNavObjects – "Đối tượng"
            this.btnNavObjects.Text      = "🗂️  Đối tượng";
            this.btnNavObjects.Name      = "btnNavObjects";
            this.btnNavObjects.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavObjects.FlatAppearance.BorderSize = 0;
            this.btnNavObjects.BackColor = System.Drawing.Color.FromArgb(30, 30, 60);
            this.btnNavObjects.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.btnNavObjects.Font      = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular);
            this.btnNavObjects.Size      = new System.Drawing.Size(150, 56);
            this.btnNavObjects.Location  = new System.Drawing.Point(310, 0);
            this.btnNavObjects.TabIndex  = 2;
            this.btnNavObjects.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnNavObjects.Click    += new System.EventHandler(this.btnNavObjects_Click);

            // lbUserName
            this.lbUserName.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lbUserName.Font      = new System.Drawing.Font("Segoe UI", 10F);
            this.lbUserName.ForeColor = System.Drawing.Color.White;
            this.lbUserName.Name      = "lbUserName";
            this.lbUserName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lbUserName.Padding   = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.lbUserName.TabIndex  = 3;

            // button2 – Đăng xuất
            this.button2.Dock       = System.Windows.Forms.DockStyle.Right;
            this.button2.FlatStyle  = System.Windows.Forms.FlatStyle.Flat;
            this.button2.FlatAppearance.BorderSize = 0;
            this.button2.BackColor  = System.Drawing.Color.FromArgb(30, 30, 60);
            this.button2.ForeColor  = System.Drawing.Color.FromArgb(240, 80, 80);
            this.button2.Font       = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.button2.Name       = "button2";
            this.button2.Size       = new System.Drawing.Size(100, 56);
            this.button2.TabIndex   = 4;
            this.button2.Text       = "Đăng xuất";
            this.button2.Cursor     = System.Windows.Forms.Cursors.Hand;
            this.button2.Click     += new System.EventHandler(this.button2_Click_1);

            // ════════════════════════════════════════════════════════════════
            // pnlQuotation — nội dung trang Báo giá (panel 1)
            // ════════════════════════════════════════════════════════════════
            this.pnlQuotation.Dock     = System.Windows.Forms.DockStyle.Fill;
            this.pnlQuotation.Name     = "pnlQuotation";
            this.pnlQuotation.Visible  = true;
            this.pnlQuotation.AutoScroll = true;
            this.pnlQuotation.Controls.Add(this.lbDonGiaVPA);
            this.pnlQuotation.Controls.Add(this.lbGiabanVPAText);
            this.pnlQuotation.Controls.Add(this.lbDonGiaThiTruong);
            this.pnlQuotation.Controls.Add(this.lbGiabanTTText);
            this.pnlQuotation.Controls.Add(this.lbDonGiaHME);
            this.pnlQuotation.Controls.Add(this.lbHME);
            this.pnlQuotation.Controls.Add(this.button1);
            this.pnlQuotation.Controls.Add(this.lbKhoiLuong);
            this.pnlQuotation.Controls.Add(this.label10);
            this.pnlQuotation.Controls.Add(this.lbName);
            this.pnlQuotation.Controls.Add(this.btnExporttoExcel);
            this.pnlQuotation.Controls.Add(this.label5);
            this.pnlQuotation.Controls.Add(this.btnAdd);
            this.pnlQuotation.Controls.Add(this.btnTinhGia);
            this.pnlQuotation.Controls.Add(this.dgvRecord);
            this.pnlQuotation.Controls.Add(this.groupBox2);

            // ── groupBox2 – thông tin tủ điện ──────────────────────────────
            this.groupBox2.Controls.Add(this.gbGiaVon);
            this.groupBox2.Controls.Add(this.gbGiaBanVPA);
            this.groupBox2.Controls.Add(this.btnReload);
            this.groupBox2.Controls.Add(this.gpGiaBan);
            this.groupBox2.Controls.Add(this.gpSonMa);
            this.groupBox2.Controls.Add(this.btnUpdate);
            this.groupBox2.Controls.Add(this.gpChiPhiKhac);
            this.groupBox2.Controls.Add(this.groupBox3);
            this.groupBox2.Controls.Add(this.txtDepth);
            this.groupBox2.Controls.Add(this.txtWidth);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.cboCabinetType);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.cboMaterial);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.txtHeight);
            this.groupBox2.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.groupBox2.Location = new System.Drawing.Point(9, 8);
            this.groupBox2.Margin   = new System.Windows.Forms.Padding(2);
            this.groupBox2.Name     = "groupBox2";
            this.groupBox2.Padding  = new System.Windows.Forms.Padding(2);
            this.groupBox2.Size     = new System.Drawing.Size(1373, 233);
            this.groupBox2.TabStop  = false;
            this.groupBox2.Text     = "Thông tin tủ điện/ thang máng cáp";

            // gbGiaVon
            this.gbGiaVon.Controls.Add(this.label9);
            this.gbGiaVon.Controls.Add(this.txtGiaVon);
            this.gbGiaVon.Location = new System.Drawing.Point(684, 106);
            this.gbGiaVon.Name     = "gbGiaVon";
            this.gbGiaVon.Padding  = new System.Windows.Forms.Padding(2);
            this.gbGiaVon.Size     = new System.Drawing.Size(165, 88);
            this.gbGiaVon.TabStop  = false;
            this.gbGiaVon.Text     = "Giá vốn";

            this.label9.AutoSize = true;
            this.label9.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.label9.Location = new System.Drawing.Point(8, 24);
            this.label9.Name     = "label9";
            this.label9.Text     = "Giá vốn (VNĐ):";

            this.txtGiaVon.Font      = new System.Drawing.Font("Times New Roman", 12F);
            this.txtGiaVon.Location  = new System.Drawing.Point(11, 50);
            this.txtGiaVon.Name      = "txtGiaVon";
            this.txtGiaVon.ReadOnly  = true;
            this.txtGiaVon.Size      = new System.Drawing.Size(138, 26);
            this.txtGiaVon.TextChanged += new System.EventHandler(this.txtGiaVon_TextChanged);

            // gbGiaBanVPA
            this.gbGiaBanVPA.Controls.Add(this.label8);
            this.gbGiaBanVPA.Controls.Add(this.txtGiaBanVPA);
            this.gbGiaBanVPA.Location = new System.Drawing.Point(1023, 106);
            this.gbGiaBanVPA.Name     = "gbGiaBanVPA";
            this.gbGiaBanVPA.Padding  = new System.Windows.Forms.Padding(2);
            this.gbGiaBanVPA.Size     = new System.Drawing.Size(165, 88);
            this.gbGiaBanVPA.TabStop  = false;
            this.gbGiaBanVPA.Text     = "Giá bán VPA";

            this.label8.AutoSize = true;
            this.label8.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.label8.Location = new System.Drawing.Point(8, 24);
            this.label8.Name     = "label8";
            this.label8.Text     = "Giá bán (VNĐ):";

            this.txtGiaBanVPA.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.txtGiaBanVPA.Location = new System.Drawing.Point(11, 50);
            this.txtGiaBanVPA.Name     = "txtGiaBanVPA";
            this.txtGiaBanVPA.Size     = new System.Drawing.Size(138, 26);

            // btnReload
            this.btnReload.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.btnReload.Location = new System.Drawing.Point(1224, 37);
            this.btnReload.Name     = "btnReload";
            this.btnReload.Size     = new System.Drawing.Size(128, 54);
            this.btnReload.Text     = "Tải lại";
            this.btnReload.Click   += new System.EventHandler(this.btnReload_Click);

            // gpGiaBan
            this.gpGiaBan.Controls.Add(this.label6);
            this.gpGiaBan.Controls.Add(this.txtGiaBan);
            this.gpGiaBan.Location = new System.Drawing.Point(854, 106);
            this.gpGiaBan.Name     = "gpGiaBan";
            this.gpGiaBan.Padding  = new System.Windows.Forms.Padding(2);
            this.gpGiaBan.Size     = new System.Drawing.Size(165, 88);
            this.gpGiaBan.TabStop  = false;
            this.gpGiaBan.Text     = "Giá thị trường";

            this.label6.AutoSize = true;
            this.label6.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.label6.Location = new System.Drawing.Point(8, 24);
            this.label6.Name     = "label6";
            this.label6.Text     = "Giá bán (VNĐ):";

            this.txtGiaBan.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.txtGiaBan.Location = new System.Drawing.Point(11, 50);
            this.txtGiaBan.Name     = "txtGiaBan";
            this.txtGiaBan.Size     = new System.Drawing.Size(138, 26);

            // gpSonMa
            this.gpSonMa.Controls.Add(this.label13);
            this.gpSonMa.Controls.Add(this.txtSonMa);
            this.gpSonMa.Location = new System.Drawing.Point(345, 106);
            this.gpSonMa.Name     = "gpSonMa";
            this.gpSonMa.Padding  = new System.Windows.Forms.Padding(2);
            this.gpSonMa.Size     = new System.Drawing.Size(165, 81);
            this.gpSonMa.TabStop  = false;
            this.gpSonMa.Text     = "Chi phí Sơn/ Mạ";

            this.label13.AutoSize = true;
            this.label13.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.label13.Location = new System.Drawing.Point(8, 23);
            this.label13.Name     = "label13";
            this.label13.Text     = "Đơn giá Sơn/Mạ (VNĐ):";

            this.txtSonMa.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.txtSonMa.Location = new System.Drawing.Point(11, 48);
            this.txtSonMa.Name     = "txtSonMa";
            this.txtSonMa.Size     = new System.Drawing.Size(138, 26);

            // btnUpdate
            this.btnUpdate.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.btnUpdate.Location = new System.Drawing.Point(1224, 162);
            this.btnUpdate.Name     = "btnUpdate";
            this.btnUpdate.Size     = new System.Drawing.Size(128, 31);
            this.btnUpdate.Text     = "Cập nhật giá";
            this.btnUpdate.Click   += new System.EventHandler(this.btnUpdate_Click_1);

            // gpChiPhiKhac
            this.gpChiPhiKhac.Controls.Add(this.label14);
            this.gpChiPhiKhac.Controls.Add(this.txtChiphikhac);
            this.gpChiPhiKhac.Location = new System.Drawing.Point(514, 106);
            this.gpChiPhiKhac.Name     = "gpChiPhiKhac";
            this.gpChiPhiKhac.Padding  = new System.Windows.Forms.Padding(2);
            this.gpChiPhiKhac.Size     = new System.Drawing.Size(165, 81);
            this.gpChiPhiKhac.TabStop  = false;
            this.gpChiPhiKhac.Text     = "Chi phí khác";

            this.label14.AutoSize = true;
            this.label14.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.label14.Location = new System.Drawing.Point(8, 24);
            this.label14.Name     = "label14";
            this.label14.Text     = "Chi phí khác (VNĐ):";

            this.txtChiphikhac.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.txtChiphikhac.Location = new System.Drawing.Point(11, 50);
            this.txtChiphikhac.Name     = "txtChiphikhac";
            this.txtChiphikhac.Size     = new System.Drawing.Size(138, 26);

            // groupBox3 – Bề mặt Sơn/Mạ
            this.groupBox3.Controls.Add(this.rbNone);
            this.groupBox3.Controls.Add(this.rbMa);
            this.groupBox3.Controls.Add(this.rbSon);
            this.groupBox3.Location = new System.Drawing.Point(4, 106);
            this.groupBox3.Name     = "groupBox3";
            this.groupBox3.Padding  = new System.Windows.Forms.Padding(2);
            this.groupBox3.Size     = new System.Drawing.Size(336, 88);
            this.groupBox3.TabStop  = false;
            this.groupBox3.Text     = "Bề mặt Sơn/ Mạ";

            this.rbNone.AutoSize        = true;
            this.rbNone.Font            = new System.Drawing.Font("Times New Roman", 12F);
            this.rbNone.Location        = new System.Drawing.Point(7, 50);
            this.rbNone.Name            = "rbNone";
            this.rbNone.Text            = "Không sơn, không mạ";
            this.rbNone.CheckedChanged += new System.EventHandler(this.rbNone_CheckedChanged);

            this.rbMa.AutoSize        = true;
            this.rbMa.Font            = new System.Drawing.Font("Times New Roman", 12F);
            this.rbMa.Location        = new System.Drawing.Point(180, 24);
            this.rbMa.Name            = "rbMa";
            this.rbMa.Text            = "Mạ kẽm nhúng nóng";
            this.rbMa.CheckedChanged += new System.EventHandler(this.rbMa_CheckedChanged);

            this.rbSon.AutoSize        = true;
            this.rbSon.Font            = new System.Drawing.Font("Times New Roman", 12F);
            this.rbSon.Location        = new System.Drawing.Point(7, 24);
            this.rbSon.Name            = "rbSon";
            this.rbSon.Text            = "Sơn tĩnh điện";
            this.rbSon.CheckedChanged += new System.EventHandler(this.rbSon_CheckedChanged);

            // Dimensions
            this.txtDepth.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.txtDepth.Location = new System.Drawing.Point(656, 198);
            this.txtDepth.Name     = "txtDepth";
            this.txtDepth.Size     = new System.Drawing.Size(105, 26);

            this.txtWidth.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.txtWidth.Location = new System.Drawing.Point(382, 198);
            this.txtWidth.Name     = "txtWidth";
            this.txtWidth.Size     = new System.Drawing.Size(105, 26);

            this.txtHeight.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.txtHeight.Location = new System.Drawing.Point(123, 198);
            this.txtHeight.Name     = "txtHeight";
            this.txtHeight.Size     = new System.Drawing.Size(105, 26);

            this.label7.AutoSize = true;
            this.label7.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.label7.Location = new System.Drawing.Point(512, 204);
            this.label7.Name     = "label7";
            this.label7.Text     = "Chiều sâu/ dài (mm):";

            this.label2.AutoSize = true;
            this.label2.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.label2.Location = new System.Drawing.Point(258, 204);
            this.label2.Name     = "label2";
            this.label2.Text     = "Chiều rộng (mm):";

            this.label1.AutoSize = true;
            this.label1.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point(5, 204);
            this.label1.Name     = "label1";
            this.label1.Text     = "Chiều cao (mm):";

            // ComboBoxes
            this.cboCabinetType.Font                  = new System.Drawing.Font("Times New Roman", 12F);
            this.cboCabinetType.FormattingEnabled      = true;
            this.cboCabinetType.Location               = new System.Drawing.Point(246, 68);
            this.cboCabinetType.Name                   = "cboCabinetType";
            this.cboCabinetType.Size                   = new System.Drawing.Size(942, 27);
            this.cboCabinetType.SelectedIndexChanged  += new System.EventHandler(this.cboCabinetType_SelectedIndexChanged);

            this.label3.AutoSize = true;
            this.label3.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.label3.Location = new System.Drawing.Point(4, 72);
            this.label3.Name     = "label3";
            this.label3.Text     = "Loại tủ điện hoặc thang, máng cáp:";

            this.cboMaterial.Font                 = new System.Drawing.Font("Times New Roman", 12F);
            this.cboMaterial.FormattingEnabled     = true;
            this.cboMaterial.Location              = new System.Drawing.Point(247, 37);
            this.cboMaterial.Name                  = "cboMaterial";
            this.cboMaterial.Size                  = new System.Drawing.Size(942, 27);
            this.cboMaterial.SelectedIndexChanged += new System.EventHandler(this.cboMaterial_SelectedIndexChanged);

            this.label4.AutoSize = true;
            this.label4.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.label4.Location = new System.Drawing.Point(4, 35);
            this.label4.Name     = "label4";
            this.label4.Text     = "Vật liệu:";

            // dgvRecord
            this.dgvRecord.BackgroundColor            = System.Drawing.Color.White;
            this.dgvRecord.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvRecord.Location                   = new System.Drawing.Point(9, 474);
            this.dgvRecord.Name                       = "dgvRecord";
            this.dgvRecord.RowHeadersWidth            = 51;
            this.dgvRecord.RowTemplate.Height         = 24;
            this.dgvRecord.Size                       = new System.Drawing.Size(1495, 261);
            this.dgvRecord.CellClick                 += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvRecord_CellClick);
            this.dgvRecord.CellEndEdit               += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvRecord_CellEndEdit);
            this.dgvRecord.DataError                 += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dgvRecord_DataError);

            // btnTinhGia
            this.btnTinhGia.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.btnTinhGia.Location = new System.Drawing.Point(9, 295);
            this.btnTinhGia.Name     = "btnTinhGia";
            this.btnTinhGia.Size     = new System.Drawing.Size(128, 31);
            this.btnTinhGia.Text     = "Tính đơn giá";
            this.btnTinhGia.Click   += new System.EventHandler(this.btnTinhGia_Click);

            // btnAdd
            this.btnAdd.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.btnAdd.Location = new System.Drawing.Point(1033, 428);
            this.btnAdd.Name     = "btnAdd";
            this.btnAdd.Size     = new System.Drawing.Size(225, 31);
            this.btnAdd.Text     = "Thêm vào Danh mục Đơn hàng";
            this.btnAdd.Click   += new System.EventHandler(this.btnAdd_Click);

            // btnExporttoExcel
            this.btnExporttoExcel.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.btnExporttoExcel.Location = new System.Drawing.Point(1279, 748);
            this.btnExporttoExcel.Name     = "btnExporttoExcel";
            this.btnExporttoExcel.Size     = new System.Drawing.Size(225, 31);
            this.btnExporttoExcel.Text     = "Xuất Đơn hàng ra Excel";
            this.btnExporttoExcel.Click   += new System.EventHandler(this.button2_Click);

            // label5 – "Thông tin sản phẩm:"
            this.label5.AutoSize = true;
            this.label5.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.label5.Location = new System.Drawing.Point(14, 340);
            this.label5.Name     = "label5";
            this.label5.Text     = "Thông tin sản phẩm:";

            // lbName
            this.lbName.Font        = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.lbName.Location    = new System.Drawing.Point(144, 340);
            this.lbName.MaximumSize = new System.Drawing.Size(600, 55);
            this.lbName.Name        = "lbName";
            this.lbName.Size        = new System.Drawing.Size(600, 55);
            this.lbName.Text        = "Chưa có thông tin.";

            // lbKhoiLuong
            this.lbKhoiLuong.AutoSize = true;
            this.lbKhoiLuong.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.lbKhoiLuong.Location = new System.Drawing.Point(180, 397);
            this.lbKhoiLuong.Name     = "lbKhoiLuong";
            this.lbKhoiLuong.Text     = "0";

            // label10
            this.label10.AutoSize = true;
            this.label10.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.label10.Location = new System.Drawing.Point(14, 396);
            this.label10.Name     = "label10";
            this.label10.Text     = "Khối lượng (kg):";

            // button1 – Xóa khỏi danh mục
            this.button1.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.button1.Location = new System.Drawing.Point(1279, 428);
            this.button1.Name     = "button1";
            this.button1.Size     = new System.Drawing.Size(225, 31);
            this.button1.Text     = "Xóa khỏi Danh mục Đơn hàng";
            this.button1.Click   += new System.EventHandler(this.button1_Click);

            // Giá labels
            this.lbDonGiaHME.AutoSize = true;
            this.lbDonGiaHME.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.lbDonGiaHME.Location = new System.Drawing.Point(180, 433);
            this.lbDonGiaHME.Name     = "lbDonGiaHME";
            this.lbDonGiaHME.Text     = "0";

            this.lbHME.AutoSize = true;
            this.lbHME.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.lbHME.Location = new System.Drawing.Point(14, 436);
            this.lbHME.Name     = "lbHME";
            this.lbHME.Text     = "Đơn giá HME (VNĐ):";

            this.lbDonGiaVPA.AutoSize = true;
            this.lbDonGiaVPA.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.lbDonGiaVPA.Location = new System.Drawing.Point(622, 428);
            this.lbDonGiaVPA.Name     = "lbDonGiaVPA";
            this.lbDonGiaVPA.Text     = "0";

            this.lbGiabanVPAText.AutoSize = true;
            this.lbGiabanVPAText.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.lbGiabanVPAText.Location = new System.Drawing.Point(425, 428);
            this.lbGiabanVPAText.Name     = "lbGiabanVPAText";
            this.lbGiabanVPAText.Text     = "Đơn giá VPA (VNĐ):";

            this.lbDonGiaThiTruong.AutoSize = true;
            this.lbDonGiaThiTruong.Font     = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Bold);
            this.lbDonGiaThiTruong.Location = new System.Drawing.Point(622, 395);
            this.lbDonGiaThiTruong.Name     = "lbDonGiaThiTruong";
            this.lbDonGiaThiTruong.Text     = "0";

            this.lbGiabanTTText.AutoSize = true;
            this.lbGiabanTTText.Font     = new System.Drawing.Font("Times New Roman", 12F);
            this.lbGiabanTTText.Location = new System.Drawing.Point(425, 396);
            this.lbGiabanTTText.Name     = "lbGiabanTTText";
            this.lbGiabanTTText.Text     = "Đơn giá thị trường (VNĐ):";

            // ════════════════════════════════════════════════════════════════
            // pnlObjects — nội dung trang Đối tượng (panel 2)
            // ════════════════════════════════════════════════════════════════
            this.pnlObjects.Dock    = System.Windows.Forms.DockStyle.Fill;
            this.pnlObjects.Name    = "pnlObjects";
            this.pnlObjects.Visible = false;
            this.pnlObjects.Controls.Add(this.dgvObjects);
            this.pnlObjects.Controls.Add(this.pnlObjectsToolbar);

            // pnlObjectsToolbar – thanh công cụ phía trên bảng đối tượng
            this.pnlObjectsToolbar.Dock      = System.Windows.Forms.DockStyle.Top;
            this.pnlObjectsToolbar.Height    = 52;
            this.pnlObjectsToolbar.BackColor = System.Drawing.Color.FromArgb(245, 245, 250);
            this.pnlObjectsToolbar.Name      = "pnlObjectsToolbar";
            this.pnlObjectsToolbar.Controls.Add(this.btnCauHinh);
            this.pnlObjectsToolbar.Controls.Add(this.btnAddObject);

            // btnAddObject – "Thêm đối tượng"
            this.btnAddObject.Text      = "＋  Thêm đối tượng";
            this.btnAddObject.Name      = "btnAddObject";
            this.btnAddObject.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddObject.FlatAppearance.BorderSize = 0;
            this.btnAddObject.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.btnAddObject.ForeColor = System.Drawing.Color.White;
            this.btnAddObject.Font      = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAddObject.Size      = new System.Drawing.Size(160, 36);
            this.btnAddObject.Location  = new System.Drawing.Point(8, 8);
            this.btnAddObject.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnAddObject.Click    += new System.EventHandler(this.btnAddObject_Click);

            // btnCauHinh – "Cấu hình"
            this.btnCauHinh.Text      = "⚙  Cấu hình";
            this.btnCauHinh.Name      = "btnCauHinh";
            this.btnCauHinh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCauHinh.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.btnCauHinh.FlatAppearance.BorderSize  = 1;
            this.btnCauHinh.BackColor = System.Drawing.Color.White;
            this.btnCauHinh.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.btnCauHinh.Font      = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnCauHinh.Size      = new System.Drawing.Size(130, 36);
            this.btnCauHinh.Location  = new System.Drawing.Point(176, 8);
            this.btnCauHinh.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnCauHinh.Click    += new System.EventHandler(this.btnCauHinh_Click);

            // dgvObjects – bảng danh sách đối tượng
            this.dgvObjects.BackgroundColor            = System.Drawing.Color.White;
            this.dgvObjects.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvObjects.Dock                       = System.Windows.Forms.DockStyle.Fill;
            this.dgvObjects.Name                       = "dgvObjects";
            this.dgvObjects.RowTemplate.Height         = 28;
            this.dgvObjects.Font                       = new System.Drawing.Font("Segoe UI", 10F);

            // ════════════════════════════════════════════════════════════════
            // FrmMain
            // ════════════════════════════════════════════════════════════════
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(1443, 857);
            this.Controls.Add(this.pnlObjects);      // fill, hidden
            this.Controls.Add(this.pnlQuotation);    // fill, visible
            this.Controls.Add(this.panelHeader);     // top
            this.Icon        = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name        = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text        = "ECQ Soft";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load       += new System.EventHandler(this.Form1_Load);

            // ResumeLayout
            this.panelHeader.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.gbGiaVon.ResumeLayout(false);
            this.gbGiaVon.PerformLayout();
            this.gbGiaBanVPA.ResumeLayout(false);
            this.gbGiaBanVPA.PerformLayout();
            this.gpGiaBan.ResumeLayout(false);
            this.gpGiaBan.PerformLayout();
            this.gpSonMa.ResumeLayout(false);
            this.gpSonMa.PerformLayout();
            this.gpChiPhiKhac.ResumeLayout(false);
            this.gpChiPhiKhac.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecord)).EndInit();
            this.pnlQuotation.ResumeLayout(false);
            this.pnlQuotation.PerformLayout();
            this.pnlObjectsToolbar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvObjects)).EndInit();
            this.pnlObjects.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        // ── Header ──────────────────────────────────────────────────────────
        private System.Windows.Forms.Panel     panelHeader;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button    btnNavQuotation;
        private System.Windows.Forms.Button    btnNavObjects;
        private System.Windows.Forms.Label     lbUserName;
        private System.Windows.Forms.Button    button2;

        // ── Panel 1: Báo giá ────────────────────────────────────────────────
        private System.Windows.Forms.Panel     pnlQuotation;
        private System.Windows.Forms.GroupBox  groupBox2;
        private System.Windows.Forms.GroupBox  gbGiaVon;
        private System.Windows.Forms.Label     label9;
        private System.Windows.Forms.TextBox   txtGiaVon;
        private System.Windows.Forms.GroupBox  gbGiaBanVPA;
        private System.Windows.Forms.Label     label8;
        private System.Windows.Forms.TextBox   txtGiaBanVPA;
        private System.Windows.Forms.Button    btnReload;
        private System.Windows.Forms.GroupBox  gpGiaBan;
        private System.Windows.Forms.Label     label6;
        private System.Windows.Forms.TextBox   txtGiaBan;
        private System.Windows.Forms.GroupBox  gpSonMa;
        private System.Windows.Forms.Label     label13;
        private System.Windows.Forms.TextBox   txtSonMa;
        private System.Windows.Forms.Button    btnUpdate;
        private System.Windows.Forms.GroupBox  gpChiPhiKhac;
        private System.Windows.Forms.Label     label14;
        private System.Windows.Forms.TextBox   txtChiphikhac;
        private System.Windows.Forms.GroupBox  groupBox3;
        private System.Windows.Forms.RadioButton rbNone;
        private System.Windows.Forms.RadioButton rbMa;
        private System.Windows.Forms.RadioButton rbSon;
        private System.Windows.Forms.TextBox   txtDepth;
        private System.Windows.Forms.TextBox   txtWidth;
        private System.Windows.Forms.TextBox   txtHeight;
        private System.Windows.Forms.Label     label7;
        private System.Windows.Forms.Label     label2;
        private System.Windows.Forms.Label     label1;
        private System.Windows.Forms.ComboBox  cboCabinetType;
        private System.Windows.Forms.Label     label3;
        private System.Windows.Forms.ComboBox  cboMaterial;
        private System.Windows.Forms.Label     label4;
        private System.Windows.Forms.DataGridView dgvRecord;
        private System.Windows.Forms.Button    btnTinhGia;
        private System.Windows.Forms.Button    btnAdd;
        private System.Windows.Forms.Button    btnExporttoExcel;
        private System.Windows.Forms.Label     label5;
        private System.Windows.Forms.Label     lbName;
        private System.Windows.Forms.Label     lbKhoiLuong;
        private System.Windows.Forms.Label     label10;
        private System.Windows.Forms.Button    button1;
        private System.Windows.Forms.Label     lbDonGiaHME;
        private System.Windows.Forms.Label     lbHME;
        private System.Windows.Forms.Label     lbDonGiaVPA;
        private System.Windows.Forms.Label     lbGiabanVPAText;
        private System.Windows.Forms.Label     lbDonGiaThiTruong;
        private System.Windows.Forms.Label     lbGiabanTTText;

        // ── Panel 2: Đối tượng ──────────────────────────────────────────────
        private System.Windows.Forms.Panel     pnlObjects;
        private System.Windows.Forms.Panel     pnlObjectsToolbar;
        private System.Windows.Forms.Button    btnAddObject;
        private System.Windows.Forms.Button    btnCauHinh;
        private System.Windows.Forms.DataGridView dgvObjects;
    }
}
