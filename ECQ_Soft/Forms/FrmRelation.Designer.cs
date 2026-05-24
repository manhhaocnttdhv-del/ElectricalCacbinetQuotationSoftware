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
            this.pnlTop = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBox1 = new ECQ_Soft.Helper.CategoryTreeDropdown();
            this.btnSearch = new System.Windows.Forms.Button();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.dgvAllProducts = new System.Windows.Forms.DataGridView();
            this.pnlAllProductsHeader = new System.Windows.Forms.Panel();
            this.lblAllProducts = new System.Windows.Forms.Label();
            this.btnAddParent = new System.Windows.Forms.Button();
            this.btnAddChild = new System.Windows.Forms.Button();
            this.splitRight = new System.Windows.Forms.SplitContainer();
            this.dgvParentProducts = new System.Windows.Forms.DataGridView();
            this.pnlParentHeader = new System.Windows.Forms.Panel();
            this.lblParentList = new System.Windows.Forms.Label();
            this.category_relationship = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.btnSaveRelation = new System.Windows.Forms.Button();
            this.btnRemoveParent = new System.Windows.Forms.Button();
            this.dgvChildProducts = new System.Windows.Forms.DataGridView();
            this.pnlChildHeader = new System.Windows.Forms.Panel();
            this.lblChildList = new System.Windows.Forms.Label();
            this.btnRemoveChild = new System.Windows.Forms.Button();
            this.pnlTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAllProducts)).BeginInit();
            this.pnlAllProductsHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitRight)).BeginInit();
            this.splitRight.Panel1.SuspendLayout();
            this.splitRight.Panel2.SuspendLayout();
            this.splitRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvParentProducts)).BeginInit();
            this.pnlParentHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvChildProducts)).BeginInit();
            this.pnlChildHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlTop
            // 
            this.pnlTop.Controls.Add(this.label1);
            this.pnlTop.Controls.Add(this.textBox1);
            this.pnlTop.Controls.Add(this.label2);
            this.pnlTop.Controls.Add(this.comboBox2);
            this.pnlTop.Controls.Add(this.label3);
            this.pnlTop.Controls.Add(this.comboBox1);
            this.pnlTop.Controls.Add(this.btnSearch);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(1913, 55);
            this.pnlTop.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point(10, 15);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Tên:";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(50, 15);
            this.textBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(150, 22);
            this.textBox1.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Bold);
            this.label2.Location = new System.Drawing.Point(212, 18);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "Hãng:";
            // 
            // comboBox2
            // 
            this.comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(263, 15);
            this.comboBox2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(150, 24);
            this.comboBox2.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Bold);
            this.label3.Location = new System.Drawing.Point(425, 18);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(95, 20);
            this.label3.TabIndex = 4;
            this.label3.Text = "Danh mục:";
            // 
            // comboBox1
            // 
            this.comboBox1.AllowTyping = false;
            this.comboBox1.DropDownHeight = 1;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.IntegralHeight = false;
            this.comboBox1.Location = new System.Drawing.Point(503, 15);
            this.comboBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.ReadOnly = false;
            this.comboBox1.Size = new System.Drawing.Size(150, 24);
            this.comboBox1.TabIndex = 5;
            // 
            // btnSearch
            // 
            this.btnSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(141)))), ((int)(((byte)(188)))));
            this.btnSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSearch.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Bold);
            this.btnSearch.ForeColor = System.Drawing.Color.White;
            this.btnSearch.Location = new System.Drawing.Point(663, 11);
            this.btnSearch.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(100, 32);
            this.btnSearch.TabIndex = 6;
            this.btnSearch.Text = "Tìm kiếm";
            this.btnSearch.UseVisualStyleBackColor = false;
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 55);
            this.splitMain.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.dgvAllProducts);
            this.splitMain.Panel1.Controls.Add(this.pnlAllProductsHeader);
            this.splitMain.Panel1.Padding = new System.Windows.Forms.Padding(13, 0, 7, 12);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.splitRight);
            this.splitMain.Panel2.Padding = new System.Windows.Forms.Padding(7, 0, 13, 12);
            this.splitMain.Size = new System.Drawing.Size(1913, 887);
            this.splitMain.SplitterDistance = 866;
            this.splitMain.SplitterWidth = 5;
            this.splitMain.TabIndex = 1;
            // 
            // dgvAllProducts
            // 
            this.dgvAllProducts.AllowUserToAddRows = false;
            this.dgvAllProducts.AllowUserToDeleteRows = false;
            this.dgvAllProducts.BackgroundColor = System.Drawing.Color.White;
            this.dgvAllProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAllProducts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvAllProducts.Location = new System.Drawing.Point(13, 49);
            this.dgvAllProducts.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dgvAllProducts.Name = "dgvAllProducts";
            this.dgvAllProducts.ReadOnly = true;
            this.dgvAllProducts.RowHeadersWidth = 51;
            this.dgvAllProducts.Size = new System.Drawing.Size(846, 801);
            this.dgvAllProducts.TabIndex = 0;
            // 
            // pnlAllProductsHeader
            // 
            this.pnlAllProductsHeader.Controls.Add(this.lblAllProducts);
            this.pnlAllProductsHeader.Controls.Add(this.btnAddParent);
            this.pnlAllProductsHeader.Controls.Add(this.btnAddChild);
            this.pnlAllProductsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlAllProductsHeader.Location = new System.Drawing.Point(13, 0);
            this.pnlAllProductsHeader.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlAllProductsHeader.Name = "pnlAllProductsHeader";
            this.pnlAllProductsHeader.Size = new System.Drawing.Size(846, 40);
            this.pnlAllProductsHeader.TabIndex = 1;
            // 
            // lblAllProducts
            // 
            this.lblAllProducts.AutoSize = false;
            this.lblAllProducts.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Bold);
            this.lblAllProducts.Location = new System.Drawing.Point(0, 11);
            this.lblAllProducts.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAllProducts.Name = "lblAllProducts";
            this.lblAllProducts.Size = new System.Drawing.Size(220, 22);
            this.lblAllProducts.TabIndex = 0;
            this.lblAllProducts.Text = "Tất cả sản phẩm";
            // 
            // btnAddParent
            // 
            this.btnAddParent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddParent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(239)))));
            this.btnAddParent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddParent.Font = new System.Drawing.Font("Times New Roman", 9F, System.Drawing.FontStyle.Bold);
            this.btnAddParent.ForeColor = System.Drawing.Color.White;
            this.btnAddParent.Location = new System.Drawing.Point(620, 5);
            this.btnAddParent.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnAddParent.Name = "btnAddParent";
            this.btnAddParent.Size = new System.Drawing.Size(100, 30);
            this.btnAddParent.TabIndex = 1;
            this.btnAddParent.Text = "+ Thêm Cha";
            this.btnAddParent.UseVisualStyleBackColor = false;
            // 
            // btnAddChild
            // 
            this.btnAddChild.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddChild.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(166)))), ((int)(((byte)(90)))));
            this.btnAddChild.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddChild.Font = new System.Drawing.Font("Times New Roman", 9F, System.Drawing.FontStyle.Bold);
            this.btnAddChild.ForeColor = System.Drawing.Color.White;
            this.btnAddChild.Location = new System.Drawing.Point(730, 5);
            this.btnAddChild.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnAddChild.Name = "btnAddChild";
            this.btnAddChild.Size = new System.Drawing.Size(100, 30);
            this.btnAddChild.TabIndex = 2;
            this.btnAddChild.Text = "+ Thêm Con";
            this.btnAddChild.UseVisualStyleBackColor = false;
            // 
            // splitRight
            // 
            this.splitRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitRight.Location = new System.Drawing.Point(7, 0);
            this.splitRight.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.splitRight.Name = "splitRight";
            this.splitRight.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitRight.Panel1
            // 
            this.splitRight.Panel1.Controls.Add(this.dgvParentProducts);
            this.splitRight.Panel1.Controls.Add(this.pnlParentHeader);
            // 
            // splitRight.Panel2
            // 
            this.splitRight.Panel2.Controls.Add(this.dgvChildProducts);
            this.splitRight.Panel2.Controls.Add(this.pnlChildHeader);
            this.splitRight.Size = new System.Drawing.Size(1022, 850);
            this.splitRight.SplitterDistance = 418;
            this.splitRight.SplitterWidth = 5;
            this.splitRight.TabIndex = 0;
            // 
            // dgvParentProducts
            // 
            this.dgvParentProducts.BackgroundColor = System.Drawing.Color.White;
            this.dgvParentProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvParentProducts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvParentProducts.Location = new System.Drawing.Point(0, 80);
            this.dgvParentProducts.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dgvParentProducts.Name = "dgvParentProducts";
            this.dgvParentProducts.RowHeadersWidth = 51;
            this.dgvParentProducts.Size = new System.Drawing.Size(1022, 369);
            this.dgvParentProducts.TabIndex = 0;
            // 
            // pnlParentHeader
            // 
            this.pnlParentHeader.Controls.Add(this.btnRemoveParent);
            this.pnlParentHeader.Controls.Add(this.btnSaveRelation);
            this.pnlParentHeader.Controls.Add(this.textBox2);
            this.pnlParentHeader.Controls.Add(this.category_relationship);
            this.pnlParentHeader.Controls.Add(this.lblParentList);
            this.pnlParentHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlParentHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlParentHeader.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlParentHeader.Name = "pnlParentHeader";
            this.pnlParentHeader.Size = new System.Drawing.Size(1022, 80);
            this.pnlParentHeader.TabIndex = 1;
            // 
            // lblParentList
            // 
            this.lblParentList.AutoSize = false;
            this.lblParentList.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Bold);
            this.lblParentList.Location = new System.Drawing.Point(10, 10);
            this.lblParentList.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblParentList.Name = "lblParentList";
            this.lblParentList.Size = new System.Drawing.Size(220, 22);
            this.lblParentList.TabIndex = 0;
            this.lblParentList.Text = "Sản phẩm Cha";
            // 
            // category_relationship
            // 
            this.category_relationship.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.category_relationship.AutoSize = false;
            this.category_relationship.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Bold);
            this.category_relationship.Location = new System.Drawing.Point(10, 50);
            this.category_relationship.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.category_relationship.Name = "category_relationship";
            this.category_relationship.Size = new System.Drawing.Size(125, 22);
            this.category_relationship.TabIndex = 2;
            this.category_relationship.Text = "Danh mục PR: ";
            // 
            // textBox2
            // 
            this.textBox2.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.textBox2.Location = new System.Drawing.Point(140, 47);
            this.textBox2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(150, 22);
            this.textBox2.TabIndex = 3;
            // 
            // btnSaveRelation
            // 
            this.btnSaveRelation.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.btnSaveRelation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnSaveRelation.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveRelation.Font = new System.Drawing.Font("Times New Roman", 9F, System.Drawing.FontStyle.Bold);
            this.btnSaveRelation.ForeColor = System.Drawing.Color.White;
            this.btnSaveRelation.Location = new System.Drawing.Point(298, 44);
            this.btnSaveRelation.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnSaveRelation.Name = "btnSaveRelation";
            this.btnSaveRelation.Size = new System.Drawing.Size(100, 30);
            this.btnSaveRelation.TabIndex = 4;
            this.btnSaveRelation.Text = "Lưu quan hệ";
            this.btnSaveRelation.UseVisualStyleBackColor = false;
            // 
            // btnRemoveParent
            // 
            this.btnRemoveParent.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Bottom;
            this.btnRemoveParent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(75)))), ((int)(((byte)(57)))));
            this.btnRemoveParent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRemoveParent.Font = new System.Drawing.Font("Times New Roman", 9F, System.Drawing.FontStyle.Bold);
            this.btnRemoveParent.ForeColor = System.Drawing.Color.White;
            this.btnRemoveParent.Location = new System.Drawing.Point(405, 44);
            this.btnRemoveParent.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnRemoveParent.Name = "btnRemoveParent";
            this.btnRemoveParent.Size = new System.Drawing.Size(65, 30);
            this.btnRemoveParent.TabIndex = 1;
            this.btnRemoveParent.Text = "Xóa";
            this.btnRemoveParent.UseVisualStyleBackColor = false;
            // 
            // dgvChildProducts
            // 
            this.dgvChildProducts.BackgroundColor = System.Drawing.Color.White;
            this.dgvChildProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvChildProducts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvChildProducts.Location = new System.Drawing.Point(0, 40);
            this.dgvChildProducts.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dgvChildProducts.Name = "dgvChildProducts";
            this.dgvChildProducts.RowHeadersWidth = 51;
            this.dgvChildProducts.Size = new System.Drawing.Size(1022, 378);
            this.dgvChildProducts.TabIndex = 0;
            // 
            // pnlChildHeader
            // 
            this.pnlChildHeader.Controls.Add(this.lblChildList);
            this.pnlChildHeader.Controls.Add(this.btnRemoveChild);
            this.pnlChildHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlChildHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlChildHeader.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlChildHeader.Name = "pnlChildHeader";
            this.pnlChildHeader.Size = new System.Drawing.Size(1022, 35);
            this.pnlChildHeader.TabIndex = 1;
            // 
            // lblChildList
            // 
            this.lblChildList.AutoSize = false;
            this.lblChildList.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Bold);
            this.lblChildList.Location = new System.Drawing.Point(10, 8);
            this.lblChildList.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblChildList.Name = "lblChildList";
            this.lblChildList.Size = new System.Drawing.Size(300, 22);
            this.lblChildList.TabIndex = 0;
            this.lblChildList.Text = "Danh sách sản phẩm Con";
            // 
            // btnRemoveChild
            // 
            this.btnRemoveChild.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemoveChild.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(75)))), ((int)(((byte)(57)))));
            this.btnRemoveChild.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRemoveChild.Font = new System.Drawing.Font("Times New Roman", 9F, System.Drawing.FontStyle.Bold);
            this.btnRemoveChild.ForeColor = System.Drawing.Color.White;
            this.btnRemoveChild.Location = new System.Drawing.Point(948, 3);
            this.btnRemoveChild.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnRemoveChild.Name = "btnRemoveChild";
            this.btnRemoveChild.Size = new System.Drawing.Size(60, 30);
            this.btnRemoveChild.TabIndex = 1;
            this.btnRemoveChild.Text = "Xóa";
            this.btnRemoveChild.UseVisualStyleBackColor = false;
            // 
            // FrmRelation
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(247)))));
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.pnlTop);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "FrmRelation";
            this.Size = new System.Drawing.Size(1913, 942);
            this.Font = new System.Drawing.Font("Times New Roman", 9F);
            this.Load += new System.EventHandler(this.FrmRelation_Load);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvAllProducts)).EndInit();
            this.pnlAllProductsHeader.ResumeLayout(false);
            this.pnlAllProductsHeader.PerformLayout();
            this.splitRight.Panel1.ResumeLayout(false);
            this.splitRight.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitRight)).EndInit();
            this.splitRight.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvParentProducts)).EndInit();
            this.pnlParentHeader.ResumeLayout(false);
            this.pnlParentHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvChildProducts)).EndInit();
            this.pnlChildHeader.ResumeLayout(false);
            this.pnlChildHeader.PerformLayout();
            this.ResumeLayout(true);

        }

        #endregion

        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.Label label3;
        private ECQ_Soft.Helper.CategoryTreeDropdown comboBox1;
        private System.Windows.Forms.Button btnSearch;
        
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.DataGridView dgvAllProducts;
        private System.Windows.Forms.Panel pnlAllProductsHeader;
        private System.Windows.Forms.Label lblAllProducts;
        private System.Windows.Forms.Button btnAddParent;
        private System.Windows.Forms.Button btnAddChild;

        private System.Windows.Forms.SplitContainer splitRight;
        private System.Windows.Forms.DataGridView dgvParentProducts;
        private System.Windows.Forms.Panel pnlParentHeader;
        private System.Windows.Forms.Label lblParentList;
        private System.Windows.Forms.Button btnRemoveParent;

        private System.Windows.Forms.DataGridView dgvChildProducts;
        private System.Windows.Forms.Panel pnlChildHeader;
        private System.Windows.Forms.Label lblChildList;
        private System.Windows.Forms.Button btnRemoveChild;

        private System.Windows.Forms.Label category_relationship;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button btnSaveRelation;
    }
}
