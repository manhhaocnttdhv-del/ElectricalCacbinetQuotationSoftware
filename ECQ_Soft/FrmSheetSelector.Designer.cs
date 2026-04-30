namespace ECQ_Soft
{
    partial class FrmSheetSelector
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
            this.panelTop = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.panelContent = new System.Windows.Forms.Panel();
            this.rdoNew = new System.Windows.Forms.RadioButton();
            this.rdoExisting = new System.Windows.Forms.RadioButton();
            this.rdoRename = new System.Windows.Forms.RadioButton();
            this.lblNewName = new System.Windows.Forms.Label();
            this.txtNewName = new System.Windows.Forms.TextBox();
            this.lblExisting = new System.Windows.Forms.Label();
            this.cboExisting = new System.Windows.Forms.ComboBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnConfirm = new System.Windows.Forms.Button();
            this.panelTop.SuspendLayout();
            this.panelContent.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.Color.FromArgb(34, 139, 34);
            this.panelTop.Controls.Add(this.lblTitle);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(440, 56);
            this.panelTop.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("Times New Roman", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(440, 56);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "🗂️  Chọn Tab Google Sheet";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelContent
            // 
            this.panelContent.Controls.Add(this.rdoExisting);
            this.panelContent.Controls.Add(this.rdoNew);
            this.panelContent.Controls.Add(this.rdoRename);
            this.panelContent.Controls.Add(this.lblExisting);
            this.panelContent.Controls.Add(this.cboExisting);
            this.panelContent.Controls.Add(this.lblNewName);
            this.panelContent.Controls.Add(this.txtNewName);
            this.panelContent.Controls.Add(this.lblStatus);
            this.panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelContent.Location = new System.Drawing.Point(0, 56);
            this.panelContent.Name = "panelContent";
            this.panelContent.Padding = new System.Windows.Forms.Padding(20, 16, 20, 8);
            this.panelContent.Size = new System.Drawing.Size(440, 190);
            this.panelContent.TabIndex = 1;
            // 
            // rdoExisting
            // 
            this.rdoExisting.AutoSize = true;
            this.rdoExisting.Checked = true;
            this.rdoExisting.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.rdoExisting.Location = new System.Drawing.Point(20, 16);
            this.rdoExisting.Name = "rdoExisting";
            this.rdoExisting.Size = new System.Drawing.Size(137, 23);
            this.rdoExisting.TabIndex = 0;
            this.rdoExisting.TabStop = true;
            this.rdoExisting.Text = "Dùng tab cũ";
            this.rdoExisting.UseVisualStyleBackColor = true;
            this.rdoExisting.CheckedChanged += new System.EventHandler(this.rdoExisting_CheckedChanged);
            // 
            // rdoNew
            // 
            this.rdoNew.AutoSize = true;
            this.rdoNew.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.rdoNew.Location = new System.Drawing.Point(140, 16);
            this.rdoNew.Name = "rdoNew";
            this.rdoNew.Size = new System.Drawing.Size(140, 23);
            this.rdoNew.TabIndex = 1;
            this.rdoNew.Text = "Tạo tab mới";
            this.rdoNew.UseVisualStyleBackColor = true;
            this.rdoNew.CheckedChanged += new System.EventHandler(this.rdoNew_CheckedChanged);
            // 
            // rdoRename
            // 
            this.rdoRename.AutoSize = true;
            this.rdoRename.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.rdoRename.Location = new System.Drawing.Point(260, 16);
            this.rdoRename.Name = "rdoRename";
            this.rdoRename.Size = new System.Drawing.Size(100, 23);
            this.rdoRename.TabIndex = 7;
            this.rdoRename.Text = "Đổi tên tab";
            this.rdoRename.UseVisualStyleBackColor = true;
            this.rdoRename.CheckedChanged += new System.EventHandler(this.rdoRename_CheckedChanged);
            // 
            // lblExisting
            // 
            this.lblExisting.AutoSize = true;
            this.lblExisting.Font = new System.Drawing.Font("Times New Roman", 9.5F);
            this.lblExisting.ForeColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.lblExisting.Location = new System.Drawing.Point(20, 56);
            this.lblExisting.Name = "lblExisting";
            this.lblExisting.Size = new System.Drawing.Size(131, 17);
            this.lblExisting.TabIndex = 2;
            this.lblExisting.Text = "Chọn tab hiện có:";
            // 
            // cboExisting
            // 
            this.cboExisting.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.cboExisting.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cboExisting.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cboExisting.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.cboExisting.Location = new System.Drawing.Point(20, 78);
            this.cboExisting.Name = "cboExisting";
            this.cboExisting.Size = new System.Drawing.Size(390, 25);
            this.cboExisting.TabIndex = 3;
            // 
            // lblNewName
            // 
            this.lblNewName.AutoSize = true;
            this.lblNewName.Font = new System.Drawing.Font("Times New Roman", 9.5F);
            this.lblNewName.ForeColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.lblNewName.Location = new System.Drawing.Point(20, 56);
            this.lblNewName.Name = "lblNewName";
            this.lblNewName.Size = new System.Drawing.Size(109, 17);
            this.lblNewName.TabIndex = 4;
            this.lblNewName.Text = "Tên tab mới:";
            this.lblNewName.Visible = false;
            // 
            // txtNewName
            // 
            this.txtNewName.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.txtNewName.Location = new System.Drawing.Point(20, 78);
            this.txtNewName.Name = "txtNewName";
            this.txtNewName.Size = new System.Drawing.Size(390, 25);
            this.txtNewName.TabIndex = 5;
            this.txtNewName.Text = "Nhập tên tab mới...";
            this.txtNewName.ForeColor = System.Drawing.Color.Gray;
            this.txtNewName.Visible = false;
            // 
            // lblStatus
            // 
            this.lblStatus.Font = new System.Drawing.Font("Times New Roman", 8.5F, System.Drawing.FontStyle.Italic);
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(20, 148);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(390, 20);
            this.lblStatus.TabIndex = 6;
            this.lblStatus.Text = "Đang kết nối...";
            // 
            // panelBottom
            // 
            this.panelBottom.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.panelBottom.Controls.Add(this.btnConfirm);
            this.panelBottom.Controls.Add(this.btnCancel);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 246);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(440, 54);
            this.panelBottom.TabIndex = 2;
            // 
            // btnConfirm
            // 
            this.btnConfirm.BackColor = System.Drawing.Color.FromArgb(34, 139, 34);
            this.btnConfirm.FlatAppearance.BorderSize = 0;
            this.btnConfirm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfirm.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Bold);
            this.btnConfirm.ForeColor = System.Drawing.Color.White;
            this.btnConfirm.Location = new System.Drawing.Point(217, 12);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Size = new System.Drawing.Size(125, 32);
            this.btnConfirm.TabIndex = 0;
            this.btnConfirm.Text = "✔ Xác nhận";
            this.btnConfirm.UseVisualStyleBackColor = false;
            this.btnConfirm.Click += new System.EventHandler(this.btnConfirm_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Times New Roman", 10F);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.btnCancel.Location = new System.Drawing.Point(348, 12);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(82, 32);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "✖ Hủy";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // FrmSheetSelector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(440, 300);
            this.Controls.Add(this.panelContent);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.panelBottom);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmSheetSelector";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Chọn Tab Google Sheet";
            this.Load += new System.EventHandler(this.FrmSheetSelector_Load);
            this.panelTop.ResumeLayout(false);
            this.panelContent.ResumeLayout(false);
            this.panelContent.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel panelContent;
        private System.Windows.Forms.RadioButton rdoExisting;
        private System.Windows.Forms.RadioButton rdoNew;
        private System.Windows.Forms.RadioButton rdoRename;
        private System.Windows.Forms.Label lblExisting;
        private System.Windows.Forms.ComboBox cboExisting;
        private System.Windows.Forms.Label lblNewName;
        private System.Windows.Forms.TextBox txtNewName;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Button btnConfirm;
        private System.Windows.Forms.Button btnCancel;
    }
}
