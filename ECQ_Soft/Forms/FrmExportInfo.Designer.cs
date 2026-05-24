namespace ECQ_Soft
{
    partial class FrmExportInfo
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblOldCustomer = new System.Windows.Forms.Label();
            this.cboOldCustomer = new System.Windows.Forms.ComboBox();
            this.lblKinhGui = new System.Windows.Forms.Label();
            this.txtKinhGui = new System.Windows.Forms.TextBox();
            this.lblDiaChi = new System.Windows.Forms.Label();
            this.txtDiaChi = new System.Windows.Forms.TextBox();
            this.lblNguoiNhan = new System.Windows.Forms.Label();
            this.txtNguoiNhan = new System.Windows.Forms.TextBox();
            this.lblMaSoThue = new System.Windows.Forms.Label();
            this.txtMaSoThue = new System.Windows.Forms.TextBox();
            this.lblNoiDung = new System.Windows.Forms.Label();
            this.txtNoiDung = new System.Windows.Forms.TextBox();
            this.lblFormat = new System.Windows.Forms.Label();
            this.cboFormat = new System.Windows.Forms.ComboBox();
            this.btnConfirm = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.panelTop = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.panelTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // 
            // lblOldCustomer
            // 
            this.lblOldCustomer.AutoSize = true;
            this.lblOldCustomer.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Bold);
            this.lblOldCustomer.Location = new System.Drawing.Point(30, 80);
            this.lblOldCustomer.Name = "lblOldCustomer";
            this.lblOldCustomer.Size = new System.Drawing.Size(115, 17);
            this.lblOldCustomer.TabIndex = 15;
            this.lblOldCustomer.Text = "Khách hàng:";
            // 
            // cboOldCustomer
            // 
            this.cboOldCustomer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboOldCustomer.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.cboOldCustomer.FormattingEnabled = true;
            this.cboOldCustomer.Location = new System.Drawing.Point(160, 77);
            this.cboOldCustomer.Name = "cboOldCustomer";
            this.cboOldCustomer.Size = new System.Drawing.Size(280, 23);
            this.cboOldCustomer.TabIndex = 16;
            this.cboOldCustomer.SelectedIndexChanged += new System.EventHandler(this.cboOldCustomer_SelectedIndexChanged);
            // 
            // lblKinhGui
            // 
            this.lblKinhGui.AutoSize = true;
            this.lblKinhGui.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Bold);
            this.lblKinhGui.Location = new System.Drawing.Point(30, 120);
            this.lblKinhGui.Name = "lblKinhGui";
            this.lblKinhGui.Size = new System.Drawing.Size(91, 17);
            this.lblKinhGui.TabIndex = 0;
            this.lblKinhGui.Text = "Kính gửi (*):";
            // 
            // txtKinhGui
            // 
            this.txtKinhGui.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.txtKinhGui.Location = new System.Drawing.Point(160, 117);
            this.txtKinhGui.Name = "txtKinhGui";
            this.txtKinhGui.Size = new System.Drawing.Size(280, 23);
            this.txtKinhGui.TabIndex = 1;
            // 
            // lblDiaChi
            // 
            this.lblDiaChi.AutoSize = true;
            this.lblDiaChi.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Bold);
            this.lblDiaChi.Location = new System.Drawing.Point(30, 160);
            this.lblDiaChi.Name = "lblDiaChi";
            this.lblDiaChi.Size = new System.Drawing.Size(81, 17);
            this.lblDiaChi.TabIndex = 2;
            this.lblDiaChi.Text = "Địa chỉ (*):";
            // 
            // txtDiaChi
            // 
            this.txtDiaChi.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.txtDiaChi.Location = new System.Drawing.Point(160, 157);
            this.txtDiaChi.Name = "txtDiaChi";
            this.txtDiaChi.Size = new System.Drawing.Size(280, 23);
            this.txtDiaChi.TabIndex = 3;
            // 
            // lblNguoiNhan
            // 
            this.lblNguoiNhan.AutoSize = true;
            this.lblNguoiNhan.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Bold);
            this.lblNguoiNhan.Location = new System.Drawing.Point(30, 200);
            this.lblNguoiNhan.Name = "lblNguoiNhan";
            this.lblNguoiNhan.Size = new System.Drawing.Size(109, 17);
            this.lblNguoiNhan.TabIndex = 4;
            this.lblNguoiNhan.Text = "Người nhận (*):";
            // 
            // txtNguoiNhan
            // 
            this.txtNguoiNhan.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.txtNguoiNhan.Location = new System.Drawing.Point(160, 197);
            this.txtNguoiNhan.Name = "txtNguoiNhan";
            this.txtNguoiNhan.Size = new System.Drawing.Size(280, 23);
            this.txtNguoiNhan.TabIndex = 5;
            // 
            // lblMaSoThue
            // 
            this.lblMaSoThue.AutoSize = true;
            this.lblMaSoThue.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.lblMaSoThue.Location = new System.Drawing.Point(30, 240);
            this.lblMaSoThue.Name = "lblMaSoThue";
            this.lblMaSoThue.Size = new System.Drawing.Size(76, 16);
            this.lblMaSoThue.TabIndex = 6;
            this.lblMaSoThue.Text = "Mã số thuế:";
            // 
            // txtMaSoThue
            // 
            this.txtMaSoThue.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.txtMaSoThue.Location = new System.Drawing.Point(160, 237);
            this.txtMaSoThue.Name = "txtMaSoThue";
            this.txtMaSoThue.Size = new System.Drawing.Size(280, 23);
            this.txtMaSoThue.TabIndex = 7;
            // 
            // lblNoiDung
            // 
            this.lblNoiDung.AutoSize = true;
            this.lblNoiDung.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.lblNoiDung.Location = new System.Drawing.Point(30, 280);
            this.lblNoiDung.Name = "lblNoiDung";
            this.lblNoiDung.Size = new System.Drawing.Size(111, 16);
            this.lblNoiDung.TabIndex = 8;
            this.lblNoiDung.Text = "Nội dung báo giá:";
            // 
            // txtNoiDung
            // 
            this.txtNoiDung.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.txtNoiDung.Location = new System.Drawing.Point(160, 277);
            this.txtNoiDung.Name = "txtNoiDung";
            this.txtNoiDung.Size = new System.Drawing.Size(280, 23);
            this.txtNoiDung.TabIndex = 9;
            // 
            // lblFormat
            // 
            this.lblFormat.AutoSize = true;
            this.lblFormat.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Bold);
            this.lblFormat.Location = new System.Drawing.Point(30, 320);
            this.lblFormat.Name = "lblFormat";
            this.lblFormat.Size = new System.Drawing.Size(103, 17);
            this.lblFormat.TabIndex = 10;
            this.lblFormat.Text = "Định dạng xuất:";
            // 
            // cboFormat
            // 
            this.cboFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFormat.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.cboFormat.FormattingEnabled = true;
            this.cboFormat.Items.AddRange(new object[] {
            "PDF",
            "Excel"});
            this.cboFormat.Location = new System.Drawing.Point(160, 317);
            this.cboFormat.Name = "cboFormat";
            this.cboFormat.Size = new System.Drawing.Size(280, 23);
            this.cboFormat.TabIndex = 11;
            // 
            // btnConfirm
            // 
            this.btnConfirm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(139)))), ((int)(((byte)(34)))));
            this.btnConfirm.FlatAppearance.BorderSize = 0;
            this.btnConfirm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfirm.Font = new System.Drawing.Font("Times New Roman", 11F, System.Drawing.FontStyle.Bold);
            this.btnConfirm.ForeColor = System.Drawing.Color.White;
            this.btnConfirm.Location = new System.Drawing.Point(160, 370);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Size = new System.Drawing.Size(130, 35);
            this.btnConfirm.TabIndex = 12;
            this.btnConfirm.Text = "✔ Xuất file";
            this.btnConfirm.UseVisualStyleBackColor = false;
            this.btnConfirm.Click += new System.EventHandler(this.btnConfirm_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Times New Roman", 11F);
            this.btnCancel.Location = new System.Drawing.Point(310, 370);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(130, 35);
            this.btnCancel.TabIndex = 13;
            this.btnCancel.Text = "✖ Hủy";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(130)))), ((int)(((byte)(180)))));
            this.panelTop.Controls.Add(this.lblTitle);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(480, 50);
            this.panelTop.TabIndex = 14;
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("Times New Roman", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(480, 50);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "THÔNG TIN KHÁCH HÀNG & XUẤT BÁO GIÁ";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FrmExportInfo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(480, 430);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.cboOldCustomer);
            this.Controls.Add(this.lblOldCustomer);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnConfirm);
            this.Controls.Add(this.cboFormat);
            this.Controls.Add(this.lblFormat);
            this.Controls.Add(this.txtNoiDung);
            this.Controls.Add(this.lblNoiDung);
            this.Controls.Add(this.txtMaSoThue);
            this.Controls.Add(this.lblMaSoThue);
            this.Controls.Add(this.txtNguoiNhan);
            this.Controls.Add(this.lblNguoiNhan);
            this.Controls.Add(this.txtDiaChi);
            this.Controls.Add(this.lblDiaChi);
            this.Controls.Add(this.txtKinhGui);
            this.Controls.Add(this.lblKinhGui);
            this.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmExportInfo";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Nhập thông tin báo giá";
            this.Load += new System.EventHandler(this.FrmExportInfo_Load);
            this.panelTop.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblOldCustomer;
        private System.Windows.Forms.ComboBox cboOldCustomer;
        private System.Windows.Forms.Label lblKinhGui;
        private System.Windows.Forms.TextBox txtKinhGui;
        private System.Windows.Forms.Label lblDiaChi;
        private System.Windows.Forms.TextBox txtDiaChi;
        private System.Windows.Forms.Label lblNguoiNhan;
        private System.Windows.Forms.TextBox txtNguoiNhan;
        private System.Windows.Forms.Label lblMaSoThue;
        private System.Windows.Forms.TextBox txtMaSoThue;
        private System.Windows.Forms.Label lblNoiDung;
        private System.Windows.Forms.TextBox txtNoiDung;
        private System.Windows.Forms.Label lblFormat;
        private System.Windows.Forms.ComboBox cboFormat;
        private System.Windows.Forms.Button btnConfirm;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblTitle;
    }
}

