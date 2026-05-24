namespace ECQ_Soft
{
    partial class FrmProductSearch
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.groupBox1  = new System.Windows.Forms.GroupBox();
            this.tlpSearch  = new System.Windows.Forms.TableLayoutPanel();
            this.lblSearch  = new System.Windows.Forms.Label();
            this.txtSearch  = new System.Windows.Forms.TextBox();
            this.lblCategory = new System.Windows.Forms.Label();
            this.cboCategory = new ECQ_Soft.Helper.CategoryTreeDropdown();
            this.btnAddTo   = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.dgvProducts = new System.Windows.Forms.DataGridView();
            this.pnlFooter  = new System.Windows.Forms.Panel();
            this.btnCancel  = new System.Windows.Forms.Button();
            this.lblAddHeader = new System.Windows.Forms.Label();
            this.txtHeaderSTT = new System.Windows.Forms.TextBox();
            this.cboHeaderName = new System.Windows.Forms.ComboBox();
            this.btnAddHeaderToQuote = new System.Windows.Forms.Button();
            this.lblTargetHeader = new System.Windows.Forms.Label();
            this.cboTargetHeader = new System.Windows.Forms.ComboBox();
            this.btnAddNewProduct = new System.Windows.Forms.Button();
            this.btnEditSelectedProduct = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.tlpSearch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvProducts)).BeginInit();
            this.pnlFooter.SuspendLayout();
            this.SuspendLayout();

            // groupBox1
            this.groupBox1.Controls.Add(this.tlpSearch);
            this.groupBox1.Anchor   = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.groupBox1.Location = new System.Drawing.Point(5, 5);
            this.groupBox1.Name     = "groupBox1";
            this.groupBox1.Size     = new System.Drawing.Size(1190, 80);
            this.groupBox1.TabStop  = false;
            this.groupBox1.Text     = "Bảng tìm kiếm lựa chọn sản phẩm";

            // tlpSearch
            this.tlpSearch.ColumnCount = 6;
            this.tlpSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.tlpSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.tlpSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tlpSearch.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tlpSearch.Controls.Add(this.lblSearch, 0, 0);
            this.tlpSearch.Controls.Add(this.lblCategory, 1, 0);
            this.tlpSearch.Controls.Add(this.txtSearch, 0, 1);
            this.tlpSearch.Controls.Add(this.cboCategory, 1, 1);
            this.tlpSearch.Controls.Add(this.btnAddNewProduct, 2, 1);
            this.tlpSearch.Controls.Add(this.btnEditSelectedProduct, 3, 1);
            this.tlpSearch.Controls.Add(this.btnAddTo, 4, 1);
            this.tlpSearch.Controls.Add(this.btnRefresh, 5, 1);
            this.tlpSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpSearch.Location = new System.Drawing.Point(3, 18);
            this.tlpSearch.Name = "tlpSearch";
            this.tlpSearch.Padding = new System.Windows.Forms.Padding(5, 2, 5, 2);
            this.tlpSearch.RowCount = 2;
            this.tlpSearch.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlpSearch.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tlpSearch.Size = new System.Drawing.Size(1184, 59);

            // lblSearch
            this.lblSearch.AutoSize = true;
            this.lblSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSearch.Location = new System.Drawing.Point(5, 2);
            this.lblSearch.Text     = "Sản phẩm";
            this.lblSearch.TextAlign = System.Drawing.ContentAlignment.BottomLeft;

            // txtSearch
            this.txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSearch.Margin = new System.Windows.Forms.Padding(3, 3, 10, 3);
            this.txtSearch.Name     = "txtSearch";
            this.txtSearch.Size     = new System.Drawing.Size(250, 26);

            // lblCategory
            this.lblCategory.AutoSize = true;
            this.lblCategory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCategory.Location = new System.Drawing.Point(280, 2);
            this.lblCategory.Text     = "Danh mục";
            this.lblCategory.TextAlign = System.Drawing.ContentAlignment.BottomLeft;

            // cboCategory
            this.cboCategory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboCategory.Margin = new System.Windows.Forms.Padding(3, 3, 10, 3);
            this.cboCategory.Name     = "cboCategory";
            this.cboCategory.Size     = new System.Drawing.Size(250, 26);

            // btnAddTo – nút xanh lá "Thêm vào"
            this.btnAddTo.BackColor          = System.Drawing.Color.FromArgb(40, 167, 69);
            this.btnAddTo.Dock               = System.Windows.Forms.DockStyle.Fill;
            this.btnAddTo.FlatStyle          = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddTo.FlatAppearance.BorderSize = 0;
            this.btnAddTo.ForeColor          = System.Drawing.Color.White;
            this.btnAddTo.Font               = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
            this.btnAddTo.Margin             = new System.Windows.Forms.Padding(3, 0, 5, 3);
            this.btnAddTo.Name               = "btnAddTo";
            this.btnAddTo.Text               = "Thêm vào";
            this.btnAddTo.UseVisualStyleBackColor = false;

            // btnRefresh – nút reload vector nhỏ
            this.btnRefresh.BackColor          = System.Drawing.Color.FromArgb(23, 162, 184);
            this.btnRefresh.Dock               = System.Windows.Forms.DockStyle.Fill;
            this.btnRefresh.FlatStyle          = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.ForeColor          = System.Drawing.Color.White;
            this.btnRefresh.Font               = new System.Drawing.Font("Segoe UI", 12f);
            this.btnRefresh.Margin             = new System.Windows.Forms.Padding(2, 0, 2, 3);
            this.btnRefresh.Text               = "⟳";
            this.btnRefresh.Name               = "btnRefresh";

            // dgvProducts
            this.dgvProducts.BackgroundColor          = System.Drawing.Color.White;
            this.dgvProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvProducts.Anchor                   = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.dgvProducts.Location                 = new System.Drawing.Point(5, 145);
            this.dgvProducts.Size                     = new System.Drawing.Size(1190, 550);
            this.dgvProducts.Name                     = "dgvProducts";
            this.dgvProducts.TabIndex                 = 1;

            // pnlFooter (Top Tool Panel)
            this.pnlFooter.Controls.Add(this.lblAddHeader);
            this.pnlFooter.Controls.Add(this.txtHeaderSTT);
            this.pnlFooter.Controls.Add(this.cboHeaderName);
            this.pnlFooter.Controls.Add(this.btnAddHeaderToQuote);
            this.pnlFooter.Controls.Add(this.lblTargetHeader);
            this.pnlFooter.Controls.Add(this.cboTargetHeader);
            this.pnlFooter.Anchor   = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.pnlFooter.Location = new System.Drawing.Point(5, 90);
            this.pnlFooter.Name     = "pnlFooter";
            this.pnlFooter.Size     = new System.Drawing.Size(1190, 50);
            this.pnlFooter.BackColor = System.Drawing.Color.FromArgb(244, 246, 249);
            this.pnlFooter.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);

            // btnCancel
            this.btnCancel.Anchor   = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnCancel.Location = new System.Drawing.Point(1090, 705);
            this.btnCancel.Name     = "btnCancel";
            this.btnCancel.Size     = new System.Drawing.Size(100, 30);
            this.btnCancel.Text     = "Đóng";



            // btnAddNewProduct
            this.btnAddNewProduct.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAddNewProduct.Margin = new System.Windows.Forms.Padding(3, 0, 5, 3);
            this.btnAddNewProduct.Name = "btnAddNewProduct";
            this.btnAddNewProduct.Text = "✚ Thêm Sản Phẩm";
            this.btnAddNewProduct.BackColor = System.Drawing.Color.FromArgb(40, 167, 69);
            this.btnAddNewProduct.ForeColor = System.Drawing.Color.White;
            this.btnAddNewProduct.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddNewProduct.FlatAppearance.BorderSize = 0;
            this.btnAddNewProduct.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);

            // btnEditSelectedProduct
            this.btnEditSelectedProduct.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnEditSelectedProduct.Margin = new System.Windows.Forms.Padding(3, 0, 5, 3);
            this.btnEditSelectedProduct.Name = "btnEditSelectedProduct";
            this.btnEditSelectedProduct.Text = "✎ Sửa Sản Phẩm";
            this.btnEditSelectedProduct.BackColor = System.Drawing.Color.FromArgb(255, 193, 7);
            this.btnEditSelectedProduct.ForeColor = System.Drawing.Color.Black;
            this.btnEditSelectedProduct.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEditSelectedProduct.FlatAppearance.BorderSize = 0;
            this.btnEditSelectedProduct.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);

            // lblAddHeader
            this.lblAddHeader.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblAddHeader.AutoSize = true;
            this.lblAddHeader.Location = new System.Drawing.Point(10, 17);
            this.lblAddHeader.Name = "lblAddHeader";
            this.lblAddHeader.Text = "Tạo mục (Màu Xanh):";
            this.lblAddHeader.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            this.lblAddHeader.ForeColor = System.Drawing.Color.FromArgb(0, 51, 102);

            // txtHeaderSTT
            this.txtHeaderSTT.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtHeaderSTT.Location = new System.Drawing.Point(190, 13);
            this.txtHeaderSTT.Name = "txtHeaderSTT";
            this.txtHeaderSTT.Size = new System.Drawing.Size(50, 27);

            // cboHeaderName
            this.cboHeaderName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cboHeaderName.FormattingEnabled = true;
            this.cboHeaderName.Location = new System.Drawing.Point(250, 13);
            this.cboHeaderName.Name = "cboHeaderName";
            this.cboHeaderName.Size = new System.Drawing.Size(250, 28);

            // btnAddHeaderToQuote
            this.btnAddHeaderToQuote.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnAddHeaderToQuote.Location = new System.Drawing.Point(510, 10);
            this.btnAddHeaderToQuote.Name = "btnAddHeaderToQuote";
            this.btnAddHeaderToQuote.Size = new System.Drawing.Size(150, 32);
            this.btnAddHeaderToQuote.Text = "✚ Thêm Tiêu Đề";
            this.btnAddHeaderToQuote.BackColor = System.Drawing.Color.FromArgb(0, 192, 192);
            this.btnAddHeaderToQuote.ForeColor = System.Drawing.Color.White;
            this.btnAddHeaderToQuote.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddHeaderToQuote.FlatAppearance.BorderSize = 0;
            this.btnAddHeaderToQuote.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);

            // lblTargetHeader
            this.lblTargetHeader.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTargetHeader.AutoSize = true;
            this.lblTargetHeader.Location = new System.Drawing.Point(680, 17);
            this.lblTargetHeader.Name = "lblTargetHeader";
            this.lblTargetHeader.Text = "Thêm vào cấu hình:";
            this.lblTargetHeader.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);

            // cboTargetHeader
            this.cboTargetHeader.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cboTargetHeader.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTargetHeader.FormattingEnabled = true;
            this.cboTargetHeader.Location = new System.Drawing.Point(860, 13);
            this.cboTargetHeader.Name = "cboTargetHeader";
            this.cboTargetHeader.Size = new System.Drawing.Size(280, 28);
            // FrmProductSearch
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(1200, 750);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.pnlFooter);
            this.Controls.Add(this.dgvProducts);
            this.Controls.Add(this.btnCancel);
            this.Padding             = new System.Windows.Forms.Padding(5);
            this.Name                = "FrmProductSearch";
            this.Text                = "Tìm kiếm & Lựa chọn Sản phẩm";
            this.tlpSearch.ResumeLayout(false);
            this.tlpSearch.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvProducts)).EndInit();
            this.pnlFooter.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.GroupBox          groupBox1;
        private System.Windows.Forms.TableLayoutPanel  tlpSearch;
        private System.Windows.Forms.Label             lblSearch;
        private System.Windows.Forms.TextBox           txtSearch;
        private System.Windows.Forms.Label             lblCategory;
        private ECQ_Soft.Helper.CategoryTreeDropdown   cboCategory;
        private System.Windows.Forms.Button            btnAddTo;
        private System.Windows.Forms.Button            btnRefresh;
        private System.Windows.Forms.DataGridView      dgvProducts;
        private System.Windows.Forms.Panel             pnlFooter;
        private System.Windows.Forms.Button            btnCancel;

        private System.Windows.Forms.Button            btnAddNewProduct;
        private System.Windows.Forms.Button            btnEditSelectedProduct;
        private System.Windows.Forms.Label             lblAddHeader;
        private System.Windows.Forms.TextBox           txtHeaderSTT;
        private System.Windows.Forms.ComboBox          cboHeaderName;
        private System.Windows.Forms.Button            btnAddHeaderToQuote;
        private System.Windows.Forms.Label             lblTargetHeader;
        private System.Windows.Forms.ComboBox          cboTargetHeader;
    }
}
