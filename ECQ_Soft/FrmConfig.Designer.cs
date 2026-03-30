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
            this.dgvAllProducts = new System.Windows.Forms.DataGridView();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.button9 = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.button5 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.button10 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.gbTimkiemSanPham = new System.Windows.Forms.GroupBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.button3 = new System.Windows.Forms.Button();
            this.button7 = new System.Windows.Forms.Button();
            this.btnSearch = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button8 = new System.Windows.Forms.Button();
            this.cboCategory = new ECQ_Soft.Helper.CategoryTreeDropdown();
            this.lstSavedConfigs = new ECQ_Soft.Helper.CheckedComboBox();
            this.button11 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvParentProducts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAllProducts)).BeginInit();
            this.gbTimkiemSanPham.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvParentProducts
            // 
            this.dgvParentProducts.AllowUserToAddRows = false;
            this.dgvParentProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvParentProducts.Location = new System.Drawing.Point(28, 387);
            this.dgvParentProducts.Name = "dgvParentProducts";
            this.dgvParentProducts.RowHeadersWidth = 51;
            this.dgvParentProducts.RowTemplate.Height = 24;
            this.dgvParentProducts.Size = new System.Drawing.Size(1446, 327);
            this.dgvParentProducts.TabIndex = 10;
            // 
            // dgvAllProducts
            // 
            this.dgvAllProducts.AllowUserToAddRows = false;
            this.dgvAllProducts.AllowUserToDeleteRows = false;
            this.dgvAllProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAllProducts.Location = new System.Drawing.Point(7, 63);
            this.dgvAllProducts.Name = "dgvAllProducts";
            this.dgvAllProducts.ReadOnly = true;
            this.dgvAllProducts.RowHeadersWidth = 51;
            this.dgvAllProducts.RowTemplate.Height = 24;
            this.dgvAllProducts.Size = new System.Drawing.Size(700, 254);
            this.dgvAllProducts.TabIndex = 15;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(25, 371);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(88, 16);
            this.label5.TabIndex = 35;
            this.label5.Text = "Bảng báo giá";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(117, 33);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(156, 22);
            this.textBox2.TabIndex = 54;
            // 
            // button9
            // 
            this.button9.Location = new System.Drawing.Point(553, 34);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(75, 23);
            this.button9.TabIndex = 52;
            this.button9.Text = "Cập nhật";
            this.button9.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(114, 17);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(68, 16);
            this.label9.TabIndex = 53;
            this.label9.Text = "Sản phẩm";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(465, 34);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 50;
            this.button2.Text = "Tìm kiếm";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(634, 34);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 51;
            this.button1.Text = "Thêm vào cấu hình";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.label6.Location = new System.Drawing.Point(926, 335);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(179, 16);
            this.label6.TabIndex = 61;
            this.label6.Text = "Tìm kiếm danh sách cấu hình";
            // 
            // button5
            // 
            this.button5.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.button5.Location = new System.Drawing.Point(1216, 358);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(78, 23);
            this.button5.TabIndex = 65;
            this.button5.Text = "Lưu";
            this.button5.UseVisualStyleBackColor = true;
            // 
            // button6
            // 
            this.button6.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.button6.Location = new System.Drawing.Point(1135, 358);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(78, 23);
            this.button6.TabIndex = 63;
            this.button6.Text = "Tìm kiếm";
            this.button6.UseVisualStyleBackColor = true;
            // 
            // button4
            // 
            this.button4.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.button4.Location = new System.Drawing.Point(1297, 358);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(78, 23);
            this.button4.TabIndex = 64;
            this.button4.Text = "Xóa";
            this.button4.UseVisualStyleBackColor = true;
            // 
            // button10
            // 
            this.button10.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.button10.Location = new System.Drawing.Point(1381, 358);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(78, 23);
            this.button10.TabIndex = 66;
            this.button10.Text = "Xuất Excel";
            this.button10.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(276, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 16);
            this.label1.TabIndex = 71;
            this.label1.Text = "Danh mục";
            // 
            // gbTimkiemSanPham
            // 
            this.gbTimkiemSanPham.Controls.Add(this.label1);
            this.gbTimkiemSanPham.Controls.Add(this.cboCategory);
            this.gbTimkiemSanPham.Controls.Add(this.textBox2);
            this.gbTimkiemSanPham.Controls.Add(this.button9);
            this.gbTimkiemSanPham.Controls.Add(this.label9);
            this.gbTimkiemSanPham.Controls.Add(this.button2);
            this.gbTimkiemSanPham.Controls.Add(this.button1);
            this.gbTimkiemSanPham.Controls.Add(this.dgvAllProducts);
            this.gbTimkiemSanPham.Location = new System.Drawing.Point(21, 8);
            this.gbTimkiemSanPham.Name = "gbTimkiemSanPham";
            this.gbTimkiemSanPham.Size = new System.Drawing.Size(711, 327);
            this.gbTimkiemSanPham.TabIndex = 72;
            this.gbTimkiemSanPham.TabStop = false;
            this.gbTimkiemSanPham.Text = "Bảng tìm kiếm lựa chọn sản phẩm";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(13, 64);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.RowTemplate.Height = 24;
            this.dataGridView1.Size = new System.Drawing.Size(700, 253);
            this.dataGridView1.TabIndex = 33;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(557, 36);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 7;
            this.button3.Text = "Thêm";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // button7
            // 
            this.button7.Location = new System.Drawing.Point(638, 36);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(75, 23);
            this.button7.TabIndex = 49;
            this.button7.Text = "Xóa tất cả";
            this.button7.UseVisualStyleBackColor = true;
            // 
            // btnSearch
            // 
            this.btnSearch.Location = new System.Drawing.Point(476, 36);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(75, 23);
            this.btnSearch.TabIndex = 59;
            this.btnSearch.Text = "Tìm kiếm";
            this.btnSearch.UseVisualStyleBackColor = true;
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(270, 36);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(200, 24);
            this.comboBox1.TabIndex = 57;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(267, 13);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(89, 16);
            this.label3.TabIndex = 56;
            this.label3.Text = "Danh mục PR";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(61, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 16);
            this.label2.TabIndex = 55;
            this.label2.Text = "Sản phẩm chính";
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(64, 36);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(200, 24);
            this.comboBox2.TabIndex = 58;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.button11);
            this.groupBox1.Controls.Add(this.comboBox2);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.comboBox1);
            this.groupBox1.Controls.Add(this.btnSearch);
            this.groupBox1.Controls.Add(this.button7);
            this.groupBox1.Controls.Add(this.button3);
            this.groupBox1.Controls.Add(this.dataGridView1);
            this.groupBox1.Location = new System.Drawing.Point(775, 8);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(709, 327);
            this.groupBox1.TabIndex = 73;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Bảng cấu hình (Đóng gói sản phẩm)";
            // 
            // button8
            // 
            this.button8.Location = new System.Drawing.Point(739, 158);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(30, 23);
            this.button8.TabIndex = 74;
            this.button8.Text = ">";
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.button8_Click);
            // 
            // cboCategory
            // 
            this.cboCategory.DropDownHeight = 1;
            this.cboCategory.FormattingEnabled = true;
            this.cboCategory.IntegralHeight = false;
            this.cboCategory.Location = new System.Drawing.Point(279, 33);
            this.cboCategory.Name = "cboCategory";
            this.cboCategory.ReadOnly = false;
            this.cboCategory.Size = new System.Drawing.Size(180, 24);
            this.cboCategory.TabIndex = 70;
            // 
            // lstSavedConfigs
            // 
            this.lstSavedConfigs.DropDownHeight = 1;
            this.lstSavedConfigs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.lstSavedConfigs.FormattingEnabled = true;
            this.lstSavedConfigs.IntegralHeight = false;
            this.lstSavedConfigs.Location = new System.Drawing.Point(929, 358);
            this.lstSavedConfigs.Name = "lstSavedConfigs";
            this.lstSavedConfigs.Placeholder = "-- Chọn cấu hình --";
            this.lstSavedConfigs.Size = new System.Drawing.Size(203, 24);
            this.lstSavedConfigs.TabIndex = 62;
            // 
            // button11
            // 
            this.button11.Location = new System.Drawing.Point(552, 14);
            this.button11.Name = "button11";
            this.button11.Size = new System.Drawing.Size(151, 23);
            this.button11.TabIndex = 60;
            this.button11.Text = "Cấu hình nâng cao";
            this.button11.UseVisualStyleBackColor = true;
            this.button11.Click += new System.EventHandler(this.button11_Click);
            // 
            // FrmConfig
            // 
            this.AutoScroll = true;
            this.Controls.Add(this.button8);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.gbTimkiemSanPham);
            this.Controls.Add(this.button10);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.lstSavedConfigs);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.dgvParentProducts);
            this.Name = "FrmConfig";
            this.Size = new System.Drawing.Size(1038, 351);
            this.Load += new System.EventHandler(this.FrmConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvParentProducts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAllProducts)).EndInit();
            this.gbTimkiemSanPham.ResumeLayout(false);
            this.gbTimkiemSanPham.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        // LEFT SIDE
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblHang;
        private System.Windows.Forms.TextBox txtHang;
        private System.Windows.Forms.Label lblCategory;
        private System.Windows.Forms.TextBox txtCategory;
        private System.Windows.Forms.DataGridView dgvProducts;
        // private System.Windows.Forms.Button btnAddMain;
        // private System.Windows.Forms.Button btnAddChild;

        // RIGHT SIDE - TOP
        private System.Windows.Forms.Label lblParentSelected;
        private System.Windows.Forms.ListBox lstParent;
        private System.Windows.Forms.Label lblChildSelected;
        private System.Windows.Forms.ListBox lstChild;

        // BOTTOM SECTION
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Label lblCauHinh;
        private System.Windows.Forms.Label lblChonSanPham;
        private System.Windows.Forms.ComboBox cboSanPham;
        private System.Windows.Forms.Label lblLinhVuc;
        private System.Windows.Forms.CheckedListBox lstLinhVuc;
        private System.Windows.Forms.Label lblPhanLoai;
        private System.Windows.Forms.DataGridView dgvPhanLoai;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.DataGridView dgvParentProducts;
        private System.Windows.Forms.DataGridView dgvAllProducts;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button button5;
        private Helper.CheckedComboBox lstSavedConfigs;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button10;
        private ECQ_Soft.Helper.CategoryTreeDropdown cboCategory;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox gbTimkiemSanPham;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.Button button11;
    }
}
