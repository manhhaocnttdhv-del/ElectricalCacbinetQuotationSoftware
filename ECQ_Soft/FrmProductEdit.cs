using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ECQ_Soft.Model;
using ECQ_Soft.Helper;

namespace ECQ_Soft
{
    public class FrmProductEdit : Form
    {
        public Products ProductData { get; private set; }
        public bool IsAddMode { get; private set; }

        private TextBox txtID, txtName, txtModel, txtSKU, txtPrice, txtPriceCost, txtType, txtHang;
        private TextBox txtWeight, txtWidth, txtHeight, txtLength;
        private TextBox txtTrangThai, txtPole, txtIr, txtIcu, txtPriceList;
        private CategoryTreeDropdown cboCategory;
        private Button btnSave, btnCancel;

        public FrmProductEdit(Products product = null, List<CategoryTreeNode> categoryRoots = null)
        {
            IsAddMode = (product == null);
            ProductData = product ?? new Products();

            InitializeUI();

            if (categoryRoots != null)
            {
                cboCategory.LoadTree(categoryRoots);
            }

            if (!IsAddMode)
            {
                txtID.Text = ProductData.Id.ToString();
                txtName.Text = ProductData.Name;
                txtModel.Text = ProductData.Model;
                txtSKU.Text = ProductData.SKU;
                txtPrice.Text = ProductData.Price;
                txtPriceCost.Text = ProductData.PriceCost;
                txtWeight.Text = ProductData.Weight;
                txtWidth.Text = ProductData.Width;
                txtHeight.Text = ProductData.Height;
                txtLength.Text = ProductData.Length;
                cboCategory.Text = ProductData.Category;
                txtType.Text = ProductData.Type;
                txtHang.Text = ProductData.HÃNG;
                txtTrangThai.Text = ProductData.TrangThai;
                txtPole.Text = ProductData.Pole;
                txtIr.Text = ProductData.Ir;
                txtIcu.Text = ProductData.Icu;
                txtPriceList.Text = ProductData.PriceList;
            }
        }

        private void InitializeUI()
        {
            this.Text = IsAddMode ? "Thêm Sản Phẩm Mới" : "Sửa Sản Phẩm";
            this.Size = new Size(950, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None; // Custom border
            this.Region = System.Drawing.Region.FromHrgn(Helper.GdiHelper.CreateRoundRectRgn(0, 0, this.Width, this.Height, 20, 20));
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f);

            // --- HEADER ---
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(26, 58, 92) };
            Label lblTitle = new Label { 
                Text = IsAddMode ? "THÊM SẢN PHẨM MỚI" : "CHỈNH SỬA SẢN PHẨM", 
                ForeColor = Color.White, 
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Location = new Point(25, 15),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // --- CONTENT PANEL ---
            Panel pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30, 20, 30, 20), AutoScroll = true };
            this.Controls.Add(pnlContent);

            int y1 = 20, y2 = 20;
            int x1 = 20, x2 = 160; // Col 1
            int x3 = 460, x4 = 600; // Col 2
            int width = 250, height = 30;
            int spacing = 45;

            // -------- CỘT 1 (Trái) --------
            
            // ID
            pnlContent.Controls.Add(new Label { Text = "ID sản phẩm:", Location = new Point(x1, y1 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtID = new TextBox { Location = new Point(x2, y1), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle, ReadOnly = !IsAddMode, BackColor = IsAddMode ? Color.White : Color.FromArgb(245, 245, 245) };
            pnlContent.Controls.Add(txtID); y1 += spacing;

            // Name
            pnlContent.Controls.Add(new Label { Text = "Tên sản phẩm:", Location = new Point(x1, y1 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtName = new TextBox { Location = new Point(x2, y1), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtName); y1 += spacing;

            // Model
            pnlContent.Controls.Add(new Label { Text = "Model:", Location = new Point(x1, y1 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtModel = new TextBox { Location = new Point(x2, y1), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtModel); y1 += spacing;

            // SKU
            pnlContent.Controls.Add(new Label { Text = "Mã SKU:", Location = new Point(x1, y1 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtSKU = new TextBox { Location = new Point(x2, y1), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtSKU); y1 += spacing;

            // Price
            pnlContent.Controls.Add(new Label { Text = "Giá bán:", Location = new Point(x1, y1 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtPrice = new TextBox { Location = new Point(x2, y1), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtPrice); y1 += spacing;

            // PriceCost
            pnlContent.Controls.Add(new Label { Text = "Giá nhập:", Location = new Point(x1, y1 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtPriceCost = new TextBox { Location = new Point(x2, y1), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtPriceCost); y1 += spacing;

            // Category
            pnlContent.Controls.Add(new Label { Text = "Danh mục:", Location = new Point(x1, y1 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            cboCategory = new CategoryTreeDropdown { Location = new Point(x2, y1), Size = new Size(width, height), AllowTyping = true };
            pnlContent.Controls.Add(cboCategory); y1 += spacing;

            // Type
            pnlContent.Controls.Add(new Label { Text = "Type:", Location = new Point(x1, y1 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtType = new TextBox { Location = new Point(x2, y1), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtType); y1 += spacing;

            // Hãng
            pnlContent.Controls.Add(new Label { Text = "Hãng:", Location = new Point(x1, y1 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtHang = new TextBox { Location = new Point(x2, y1), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtHang); y1 += spacing;

            // PriceList
            pnlContent.Controls.Add(new Label { Text = "PriceList:", Location = new Point(x1, y1 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtPriceList = new TextBox { Location = new Point(x2, y1), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtPriceList); y1 += spacing;


            // -------- CỘT 2 (Phải) --------

            // Weight
            pnlContent.Controls.Add(new Label { Text = "Trọng lượng (Kg):", Location = new Point(x3, y2 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtWeight = new TextBox { Location = new Point(x4, y2), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtWeight); y2 += spacing;

            // Width
            pnlContent.Controls.Add(new Label { Text = "Chiều rộng (mm):", Location = new Point(x3, y2 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtWidth = new TextBox { Location = new Point(x4, y2), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtWidth); y2 += spacing;

            // Height
            pnlContent.Controls.Add(new Label { Text = "Chiều cao (mm):", Location = new Point(x3, y2 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtHeight = new TextBox { Location = new Point(x4, y2), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtHeight); y2 += spacing;

            // Length
            pnlContent.Controls.Add(new Label { Text = "Chiều sâu (mm):", Location = new Point(x3, y2 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtLength = new TextBox { Location = new Point(x4, y2), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtLength); y2 += spacing;

            // TrangThai
            pnlContent.Controls.Add(new Label { Text = "Trạng Thái:", Location = new Point(x3, y2 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtTrangThai = new TextBox { Location = new Point(x4, y2), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtTrangThai); y2 += spacing;

            // Pole
            pnlContent.Controls.Add(new Label { Text = "Pole (số Cực):", Location = new Point(x3, y2 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtPole = new TextBox { Location = new Point(x4, y2), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtPole); y2 += spacing;

            // Ir
            pnlContent.Controls.Add(new Label { Text = "Ir (I Rate):", Location = new Point(x3, y2 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtIr = new TextBox { Location = new Point(x4, y2), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtIr); y2 += spacing;

            // Icu
            pnlContent.Controls.Add(new Label { Text = "Icu:", Location = new Point(x3, y2 + 5), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(64, 64, 64) });
            txtIcu = new TextBox { Location = new Point(x4, y2), Size = new Size(width, height), BorderStyle = BorderStyle.FixedSingle };
            pnlContent.Controls.Add(txtIcu); y2 += spacing;

            // --- FOOTER ---
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.FromArgb(245, 245, 245) };
            this.Controls.Add(pnlFooter);

            btnSave = new Button { 
                Text = "LƯU THAY ĐỔI", 
                Size = new Size(180, 45), 
                Location = new Point(this.Width / 2 - 190, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button { 
                Text = "HỦY BỎ", 
                Size = new Size(150, 45), 
                Location = new Point(this.Width / 2 + 10, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(btnCancel);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Tên sản phẩm không được để trống!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (int.TryParse(txtID.Text, out int id)) ProductData.Id = id;
            ProductData.Name = txtName.Text.Trim();
            ProductData.Model = txtModel.Text.Trim();
            ProductData.SKU = txtSKU.Text.Trim();
            ProductData.Price = txtPrice.Text.Trim();
            ProductData.PriceCost = txtPriceCost.Text.Trim();
            ProductData.Weight = txtWeight.Text.Trim();
            ProductData.Width = txtWidth.Text.Trim();
            ProductData.Height = txtHeight.Text.Trim();
            ProductData.Length = txtLength.Text.Trim();
            ProductData.Category = cboCategory.Text.Trim();
            ProductData.Type = txtType.Text.Trim();
            ProductData.HÃNG = txtHang.Text.Trim();
            ProductData.TrangThai = txtTrangThai.Text.Trim();
            ProductData.Pole = txtPole.Text.Trim();
            ProductData.Ir = txtIr.Text.Trim();
            ProductData.Icu = txtIcu.Text.Trim();
            ProductData.PriceList = txtPriceList.Text.Trim();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
