namespace ECQ_Soft
{
    partial class FrmAdvancedConfig
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlStepsContainer = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlControls = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnAddToGrid = new System.Windows.Forms.Button();
            this.dgvSelectedItems = new System.Windows.Forms.DataGridView();
            this.colTen = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSoLuong = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colGhiChu = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colXoa = new System.Windows.Forms.DataGridViewButtonColumn();
            this.splitterMain = new System.Windows.Forms.Splitter();
            this.lblDivider = new System.Windows.Forms.Label();
            this.lblGridTitle = new System.Windows.Forms.Label();
            this.pnlControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSelectedItems)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(100)))));
            this.lblTitle.Location = new System.Drawing.Point(15, 12);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(170, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Cấu hình nâng cao";
            // 
            // pnlStepsContainer
            // 
            this.pnlStepsContainer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlStepsContainer.AutoScroll = true;
            this.pnlStepsContainer.BackColor = System.Drawing.Color.White;
            this.pnlStepsContainer.Location = new System.Drawing.Point(15, 45);
            this.pnlStepsContainer.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.pnlStepsContainer.Name = "pnlStepsContainer";
            this.pnlStepsContainer.Size = new System.Drawing.Size(1184, 276);
            this.pnlStepsContainer.TabIndex = 1;
            this.pnlStepsContainer.WrapContents = false;
            // 
            // pnlControls
            // 
            this.pnlControls.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlControls.Controls.Add(this.btnCancel);
            this.pnlControls.Controls.Add(this.btnApply);
            this.pnlControls.Controls.Add(this.btnReload);
            this.pnlControls.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlControls.Location = new System.Drawing.Point(0, 712);
            this.pnlControls.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.Size = new System.Drawing.Size(1034, 49);
            this.pnlControls.TabIndex = 9;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnCancel.Location = new System.Drawing.Point(738, 8);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 32);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Đóng";
            // 
            // btnApply
            // 
            this.btnApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnApply.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnApply.Enabled = false;
            this.btnApply.FlatAppearance.BorderSize = 0;
            this.btnApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApply.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnApply.ForeColor = System.Drawing.Color.White;
            this.btnApply.Location = new System.Drawing.Point(607, 8);
            this.btnApply.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(120, 32);
            this.btnApply.TabIndex = 7;
            this.btnApply.Text = "✔ Xác nhận (Apply)";
            this.btnApply.UseVisualStyleBackColor = false;
            // 
            // btnReload
            // 
            this.btnReload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(140)))), ((int)(((byte)(0)))));
            this.btnReload.FlatAppearance.BorderSize = 0;
            this.btnReload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReload.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnReload.ForeColor = System.Drawing.Color.White;
            this.btnReload.Location = new System.Drawing.Point(840, 8);
            this.btnReload.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(110, 32);
            this.btnReload.TabIndex = 9;
            this.btnReload.Text = "⟳ Tải lại";
            this.btnReload.UseVisualStyleBackColor = false;
            this.btnReload.Click += new System.EventHandler(this.BtnReload_Click);
            // 
            // btnAddToGrid
            // 
            this.btnAddToGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddToGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(139)))), ((int)(((byte)(34)))));
            this.btnAddToGrid.Enabled = false;
            this.btnAddToGrid.FlatAppearance.BorderSize = 0;
            this.btnAddToGrid.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddToGrid.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnAddToGrid.ForeColor = System.Drawing.Color.White;
            this.btnAddToGrid.Location = new System.Drawing.Point(1101, 332);
            this.btnAddToGrid.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnAddToGrid.Name = "btnAddToGrid";
            this.btnAddToGrid.Size = new System.Drawing.Size(98, 26);
            this.btnAddToGrid.TabIndex = 12;
            this.btnAddToGrid.Text = "+ Thêm vào danh sách";
            this.btnAddToGrid.UseVisualStyleBackColor = false;
            // 
            // dgvSelectedItems
            // 
            this.dgvSelectedItems.AllowUserToAddRows = false;
            this.dgvSelectedItems.AllowUserToDeleteRows = false;
            this.dgvSelectedItems.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvSelectedItems.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvSelectedItems.BackgroundColor = System.Drawing.Color.White;
            this.dgvSelectedItems.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(245)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvSelectedItems.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvSelectedItems.ColumnHeadersHeight = 36;
            this.dgvSelectedItems.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colTen,
            this.colSoLuong,
            this.colGhiChu,
            this.colXoa});
            this.dgvSelectedItems.EnableHeadersVisualStyles = false;
            this.dgvSelectedItems.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.dgvSelectedItems.Location = new System.Drawing.Point(15, 364);
            this.dgvSelectedItems.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.dgvSelectedItems.Name = "dgvSelectedItems";
            this.dgvSelectedItems.RowHeadersVisible = false;
            this.dgvSelectedItems.RowTemplate.Height = 32;
            this.dgvSelectedItems.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSelectedItems.Size = new System.Drawing.Size(1184, 388);
            this.dgvSelectedItems.TabIndex = 13;
            // 
            // colTen
            // 
            this.colTen.FillWeight = 40F;
            this.colTen.HeaderText = "Tên cấu hình";
            this.colTen.Name = "colTen";
            this.colTen.ReadOnly = true;
            // 
            // colSoLuong
            // 
            this.colSoLuong.FillWeight = 15F;
            this.colSoLuong.HeaderText = "Số lượng";
            this.colSoLuong.Name = "colSoLuong";
            // 
            // colGhiChu
            // 
            this.colGhiChu.FillWeight = 35F;
            this.colGhiChu.HeaderText = "Ghi chú";
            this.colGhiChu.Name = "colGhiChu";
            // 
            // colXoa
            // 
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(50)))), ((int)(((byte)(47)))));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            this.colXoa.DefaultCellStyle = dataGridViewCellStyle2;
            this.colXoa.FillWeight = 10F;
            this.colXoa.HeaderText = "";
            this.colXoa.Name = "colXoa";
            this.colXoa.Text = "Xóa";
            this.colXoa.UseColumnTextForButtonValue = true;
            // 
            // splitterMain
            // 
            this.splitterMain.Location = new System.Drawing.Point(0, 0);
            this.splitterMain.Name = "splitterMain";
            this.splitterMain.Size = new System.Drawing.Size(3, 3);
            this.splitterMain.TabIndex = 0;
            this.splitterMain.TabStop = false;
            // 
            // lblDivider
            // 
            this.lblDivider.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblDivider.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblDivider.Location = new System.Drawing.Point(15, 329);
            this.lblDivider.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblDivider.Name = "lblDivider";
            this.lblDivider.Size = new System.Drawing.Size(1184, 1);
            this.lblDivider.TabIndex = 10;
            // 
            // lblGridTitle
            // 
            this.lblGridTitle.AutoSize = true;
            this.lblGridTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblGridTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(100)))));
            this.lblGridTitle.Location = new System.Drawing.Point(15, 337);
            this.lblGridTitle.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblGridTitle.Name = "lblGridTitle";
            this.lblGridTitle.Size = new System.Drawing.Size(132, 19);
            this.lblGridTitle.TabIndex = 11;
            this.lblGridTitle.Text = "Sản phẩm đã chọn";
            // 
            // FrmAdvancedConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1034, 761);
            this.Controls.Add(this.btnAddToGrid);
            this.Controls.Add(this.lblGridTitle);
            this.Controls.Add(this.lblDivider);
            this.Controls.Add(this.dgvSelectedItems);
            this.Controls.Add(this.pnlStepsContainer);
            this.Controls.Add(this.pnlControls);
            this.Controls.Add(this.lblTitle);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(604, 414);
            this.Name = "FrmAdvancedConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Cấu hình nâng cao";
            this.pnlControls.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSelectedItems)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblDivider;
        private System.Windows.Forms.Label lblGridTitle;
        private System.Windows.Forms.FlowLayoutPanel pnlStepsContainer;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnAddToGrid;
        private System.Windows.Forms.Panel pnlControls;
        private System.Windows.Forms.DataGridView dgvSelectedItems;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTen;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSoLuong;
        private System.Windows.Forms.DataGridViewTextBoxColumn colGhiChu;
        private System.Windows.Forms.DataGridViewButtonColumn colXoa;
        private System.Windows.Forms.Splitter splitterMain;
        private System.Windows.Forms.Button btnReload;
    }
}
