namespace ECQ_Soft
{
    partial class FrmRelation
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBox1 = new ECQ_Soft.Helper.CategoryTreeDropdown();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.btnSearch = new System.Windows.Forms.Button();
            this.btnAddParent = new System.Windows.Forms.Button();
            this.btnAddChild = new System.Windows.Forms.Button();
            this.dgvParentProducts = new System.Windows.Forms.DataGridView();
            this.dgvChildProducts = new System.Windows.Forms.DataGridView();
            this.lblParentList = new System.Windows.Forms.Label();
            this.lblChildList = new System.Windows.Forms.Label();
            this.btnSaveRelation = new System.Windows.Forms.Button();
            this.dgvAllProducts = new System.Windows.Forms.DataGridView();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.category_relationship = new System.Windows.Forms.Label();
            this.btnRemoveParent = new System.Windows.Forms.Button();
            this.btnRemoveChild = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvParentProducts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvChildProducts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAllProducts)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Tìm sản phẩm";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.label2.Location = new System.Drawing.Point(237, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(76, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Hãng sản xuất";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.label3.Location = new System.Drawing.Point(460, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Danh mục";
            // 
            // comboBox1 (CategoryTreeDropdown - Danh mục đa cấp)
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(463, 30);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(200, 21);
            this.comboBox1.TabIndex = 3;
            this.comboBox1.DropDownHeight = 1;
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(240, 30);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(200, 21);
            this.comboBox2.TabIndex = 4;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(15, 30);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(200, 20);
            this.textBox1.TabIndex = 5;
            // 
            // btnSearch
            // 
            this.btnSearch.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.btnSearch.Location = new System.Drawing.Point(683, 31);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(75, 23);
            this.btnSearch.TabIndex = 6;
            this.btnSearch.Text = "Tìm kiếm";
            this.btnSearch.UseVisualStyleBackColor = true;
            // 
            // btnAddParent
            // 
            this.btnAddParent.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.btnAddParent.Location = new System.Drawing.Point(602, 71);
            this.btnAddParent.Name = "btnAddParent";
            this.btnAddParent.Size = new System.Drawing.Size(75, 23);
            this.btnAddParent.TabIndex = 8;
            this.btnAddParent.Text = "+ Cha";
            this.btnAddParent.UseVisualStyleBackColor = true;
            // 
            // btnAddChild
            // 
            this.btnAddChild.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.btnAddChild.Location = new System.Drawing.Point(683, 71);
            this.btnAddChild.Name = "btnAddChild";
            this.btnAddChild.Size = new System.Drawing.Size(75, 23);
            this.btnAddChild.TabIndex = 9;
            this.btnAddChild.Text = "+ Con";
            this.btnAddChild.UseVisualStyleBackColor = true;
            // 
            // dgvParentProducts
            // 
            this.dgvParentProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvParentProducts.Location = new System.Drawing.Point(777, 101);
            this.dgvParentProducts.Name = "dgvParentProducts";
            this.dgvParentProducts.RowHeadersWidth = 51;
            this.dgvParentProducts.RowTemplate.Height = 24;
            this.dgvParentProducts.Size = new System.Drawing.Size(743, 300);
            this.dgvParentProducts.TabIndex = 10;
            // 
            // dgvChildProducts
            // 
            this.dgvChildProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvChildProducts.Location = new System.Drawing.Point(777, 436);
            this.dgvChildProducts.Name = "dgvChildProducts";
            this.dgvChildProducts.RowHeadersWidth = 51;
            this.dgvChildProducts.RowTemplate.Height = 24;
            this.dgvChildProducts.Size = new System.Drawing.Size(743, 300);
            this.dgvChildProducts.TabIndex = 11;
            // 
            // lblParentList
            // 
            this.lblParentList.AutoSize = true;
            this.lblParentList.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblParentList.Location = new System.Drawing.Point(780, 78);
            this.lblParentList.Name = "lblParentList";
            this.lblParentList.Size = new System.Drawing.Size(175, 15);
            this.lblParentList.TabIndex = 12;
            this.lblParentList.Text = "Danh sách sản phẩm Cha:";
            // 
            // lblChildList
            // 
            this.lblChildList.AutoSize = true;
            this.lblChildList.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblChildList.Location = new System.Drawing.Point(774, 418);
            this.lblChildList.Name = "lblChildList";
            this.lblChildList.Size = new System.Drawing.Size(175, 15);
            this.lblChildList.TabIndex = 13;
            this.lblChildList.Text = "Danh sách sản phẩm Con:";
            // 
            // btnSaveRelation
            // 
            this.btnSaveRelation.Location = new System.Drawing.Point(1367, 75);
            this.btnSaveRelation.Name = "btnSaveRelation";
            this.btnSaveRelation.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.btnSaveRelation.Size = new System.Drawing.Size(75, 23);
            this.btnSaveRelation.TabIndex = 14;
            this.btnSaveRelation.Text = "Lưu";
            this.btnSaveRelation.UseVisualStyleBackColor = true;
            // 
            // dgvAllProducts
            // 
            this.dgvAllProducts.AllowUserToAddRows = false;
            this.dgvAllProducts.AllowUserToDeleteRows = false;
            this.dgvAllProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAllProducts.Location = new System.Drawing.Point(15, 100);
            this.dgvAllProducts.Name = "dgvAllProducts";
            this.dgvAllProducts.ReadOnly = true;
            this.dgvAllProducts.RowHeadersWidth = 51;
            this.dgvAllProducts.RowTemplate.Height = 24;
            this.dgvAllProducts.Size = new System.Drawing.Size(743, 636);
            this.dgvAllProducts.TabIndex = 15;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(1161, 78);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(200, 20);
            this.textBox2.TabIndex = 17;
            // 
            // category_relationship
            // 
            this.category_relationship.AutoSize = true;
            this.category_relationship.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.category_relationship.Location = new System.Drawing.Point(1158, 57);
            this.category_relationship.Name = "category_relationship";
            this.category_relationship.Size = new System.Drawing.Size(102, 13);
            this.category_relationship.TabIndex = 16;
            this.category_relationship.Text = "Danh mục PR";
            // 
            // btnRemoveParent
            // 
            this.btnRemoveParent.Location = new System.Drawing.Point(1448, 75);
            this.btnRemoveParent.Name = "btnRemoveParent";
            this.btnRemoveParent.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.btnRemoveParent.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveParent.TabIndex = 18;
            this.btnRemoveParent.Text = "Xóa";
            this.btnRemoveParent.UseVisualStyleBackColor = true;
            // 
            // btnRemoveChild
            // 
            this.btnRemoveChild.Location = new System.Drawing.Point(1445, 407);
            this.btnRemoveChild.Name = "btnRemoveChild";
            this.btnRemoveChild.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Bold);
            this.btnRemoveChild.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveChild.TabIndex = 19;
            this.btnRemoveChild.Text = "Xóa";
            this.btnRemoveChild.UseVisualStyleBackColor = true;
            // 
            // FrmRelation
            // 
            this.AutoScroll = true;
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.category_relationship);
            this.Controls.Add(this.dgvAllProducts);
            this.Controls.Add(this.btnSaveRelation);
            this.Controls.Add(this.lblChildList);
            this.Controls.Add(this.lblParentList);
            this.Controls.Add(this.dgvChildProducts);
            this.Controls.Add(this.dgvParentProducts);
            this.Controls.Add(this.btnAddChild);
            this.Controls.Add(this.btnAddParent);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.comboBox2);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnRemoveChild);
            this.Controls.Add(this.btnRemoveParent);
            this.Controls.Add(this.label1);
            this.Name = "FrmRelation";
            this.Size = new System.Drawing.Size(1435, 765);
            this.Load += new System.EventHandler(this.FrmRelation_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvParentProducts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvChildProducts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAllProducts)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private ECQ_Soft.Helper.CategoryTreeDropdown comboBox1;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button btnSearch;

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
        private System.Windows.Forms.Button btnApply;

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
        private System.Windows.Forms.Button btnAddParent;
        private System.Windows.Forms.Button btnAddChild;
        private System.Windows.Forms.DataGridView dgvParentProducts;
        private System.Windows.Forms.DataGridView dgvChildProducts;
        private System.Windows.Forms.Label lblParentList;
        private System.Windows.Forms.Label lblChildList;
        private System.Windows.Forms.Button btnSaveRelation;
        private System.Windows.Forms.DataGridView dgvAllProducts;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label category_relationship;
        private System.Windows.Forms.Button btnRemoveParent;
        private System.Windows.Forms.Button btnRemoveChild;
    }
}
