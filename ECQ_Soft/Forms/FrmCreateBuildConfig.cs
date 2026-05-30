using ECQ_Soft.Model;
using ECQ_Soft.Services;
using FontAwesome.Sharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ECQ_Soft.Forms
{
    /// <summary>
    /// Popup "Đóng gói cấu hình mới":
    /// - Hiển thị fullscreen với layout 3 vùng: header banner / toolbar / grid + footer.
    /// - Toolbar có ô nhập "Tên cấu hình", nút "Thêm sản phẩm" mở FrmProductSearch, counter.
    /// - Grid 10 cột giống grid chính (header vàng).
    /// - Nút "Lưu cấu hình" lưu thẳng vào SQL Server.
    /// </summary>
    public class FrmCreateBuildConfig : Form
    {
        public string SavedConfigName { get; private set; }
        public bool Saved { get; private set; }

        private readonly BindingList<Products> _items = new BindingList<Products>();
        private readonly TextBox _txtConfigName;
        private readonly DataGridView _grid;
        private readonly Label _lblCount;
        private readonly List<Products> _allProducts;

        private int _editingConfigId;
        private string _editingOriginalName;

        // ── Palette ────────────────────────────────────────────────────
        private static readonly Color HeaderColor = Color.FromArgb(34, 139, 34);
        private static readonly Color PageBg = Color.FromArgb(247, 248, 250);
        private static readonly Color CardBg = Color.White;
        private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);
        private static readonly Color MutedText = Color.FromArgb(100, 116, 139);
        private static readonly Color AccentBlue = Color.FromArgb(37, 99, 235);

        public FrmCreateBuildConfig(List<Products> allProducts, string defaultConfigName = null)
        {
            _allProducts = allProducts ?? new List<Products>();

            Text = "Đóng gói cấu hình mới";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1280, 760);
            MinimumSize = new Size(1000, 540);
            WindowState = FormWindowState.Maximized;
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            ShowInTaskbar = true;
            DoubleBuffered = true;

            // ── Header banner ─────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = HeaderColor
            };
            var lblTitle = new Label
            {
                Text = "  ĐÓNG GÓI CẤU HÌNH MỚI",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };
            header.Controls.Add(lblTitle);

            // ── Footer (fixed, ngay trên status bar) ───────────────────
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = CardBg,
                Padding = new Padding(20, 14, 20, 14)
            };
            // Đường viền trên footer
            footer.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderColor, 1))
                    e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };

            var btnSave = new IconButton
            {
                Text = "  Lưu cấu hình",
                IconChar = IconChar.Save,
                IconColor = Color.White,
                IconSize = 18,
                ForeColor = Color.White,
                BackColor = HeaderColor,
                FlatStyle = FlatStyle.Flat,
                Width = 170,
                Height = 36,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += async (s, e) => await SaveAsync(btnSave);

            var btnCancel = new Button
            {
                Text = "Hủy",
                Width = 110,
                Height = 36,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.FlatAppearance.BorderColor = BorderColor;
            btnCancel.Click += (s, e) => Close();

            void RepositionFooterButtons()
            {
                btnSave.Left = footer.ClientSize.Width - 20 - btnSave.Width;
                btnSave.Top = (footer.ClientSize.Height - btnSave.Height) / 2;
                btnCancel.Left = btnSave.Left - 10 - btnCancel.Width;
                btnCancel.Top = btnSave.Top;
            }
            footer.Resize += (s, e) => RepositionFooterButtons();
            footer.Controls.Add(btnSave);
            footer.Controls.Add(btnCancel);

            // ── Card chính (chứa toolbar + grid) ───────────────────────
            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBg,
                Padding = new Padding(20, 16, 20, 12)
            };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBg,
                Padding = new Padding(0)
            };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderColor, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            // ── Toolbar trong card ────────────────────────────────────
            var toolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 64,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = CardBg,
                Padding = new Padding(16, 14, 16, 14),
                Margin = Padding.Empty
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // label
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380)); // textbox
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220)); // btn add
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // counter (right)
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var lblConfigName = new Label
            {
                Text = "Tên cấu hình:",
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 12, 0)
            };

            _txtConfigName = new TextBox
            {
                Width = 360,
                Height = 28,
                Font = new Font("Segoe UI", 10F),
                Text = defaultConfigName ?? string.Empty,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 4, 16, 0),
                BorderStyle = BorderStyle.FixedSingle
            };

            var btnAddProduct = new IconButton
            {
                Text = "Thêm sản phẩm",
                IconChar = IconChar.Plus,
                IconColor = Color.White,
                IconSize = 16,
                ForeColor = Color.White,
                BackColor = AccentBlue,
                FlatStyle = FlatStyle.Flat,
                Width = 200,
                Height = 34,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(10, 0, 10, 0),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Left,
                AutoEllipsis = false,
                AutoSize = false,
                Margin = new Padding(0, 1, 0, 0)
            };
            btnAddProduct.MinimumSize = new Size(200, 34);
            btnAddProduct.FlatAppearance.BorderSize = 0;
            btnAddProduct.Click += (s, e) => OpenProductSearchPopup();

            _lblCount = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = AccentBlue,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 10, 0, 0),
                Text = "0 mã  •  0 sản phẩm"
            };

            toolbar.Controls.Add(lblConfigName, 0, 0);
            toolbar.Controls.Add(_txtConfigName, 1, 0);
            toolbar.Controls.Add(btnAddProduct, 2, 0);
            toolbar.Controls.Add(_lblCount, 3, 0);

            // Separator dưới toolbar
            var toolbarSep = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = BorderColor
            };

            // ── Grid sản phẩm ─────────────────────────────────────────
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = CardBg,
                BorderStyle = BorderStyle.None,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 8.75F),
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 32 },
                MultiSelect = true,
                GridColor = Color.FromArgb(189, 215, 238),
                CellBorderStyle = DataGridViewCellBorderStyle.Single
            };
            _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.Yellow,
                ForeColor = Color.FromArgb(31, 73, 125),
                Font = new Font("Segoe UI", 8.75F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                WrapMode = DataGridViewTriState.True,
                Padding = new Padding(4, 0, 4, 0)
            };

            _grid.Columns.Clear();
            _grid.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "STT", HeaderText = "STT", Width = 50,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Tên sản phẩm", DataPropertyName = "Name",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 240,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 6, 0) } },
                new DataGridViewTextBoxColumn { Name = "Model", HeaderText = "Model", DataPropertyName = "Model", Width = 110,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft } },
                new DataGridViewTextBoxColumn { Name = "SKU", HeaderText = "Mã SKU", DataPropertyName = "SKU", Width = 110,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft } },
                new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "Giá bán", DataPropertyName = "Price", Width = 100,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "PriceCost", HeaderText = "Giá nhập", DataPropertyName = "PriceCost", Width = 100,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Danh mục", DataPropertyName = "Category", Width = 130,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft } },
                new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", DataPropertyName = "Type", Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft } },
                new DataGridViewTextBoxColumn { Name = "HÃNG", HeaderText = "Hãng", DataPropertyName = "HÃNG", Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft } },
                new DataGridViewTextBoxColumn { Name = "SoLuong", HeaderText = "Số lượng", DataPropertyName = "SoLuong", Width = 90,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9F, FontStyle.Bold) } }
            );

            _grid.DataBindingComplete += (s, ev) =>
            {
                for (int i = 0; i < _grid.Rows.Count; i++)
                    _grid.Rows[i].Cells["STT"].Value = (i + 1).ToString();
            };
            _grid.DataSource = _items;

            // Empty-state placeholder vẽ giữa grid khi chưa có dòng
            _grid.Paint += (s, e) =>
            {
                if (_items.Count > 0) return;
                var rect = _grid.ClientRectangle;
                int top = _grid.ColumnHeadersHeight + 40;
                using (var f1 = new Font("Segoe UI", 11F, FontStyle.Bold))
                using (var f2 = new Font("Segoe UI", 9F))
                using (var b1 = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (var b2 = new SolidBrush(MutedText))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center })
                {
                    e.Graphics.DrawString("Chưa có sản phẩm nào", f1, b1,
                        new RectangleF(0, top, rect.Width, 22), sf);
                    e.Graphics.DrawString("Bấm \"Thêm sản phẩm\" ở thanh trên để chọn sản phẩm cho cấu hình",
                        f2, b2, new RectangleF(0, top + 26, rect.Width, 18), sf);
                }
            };

            // Context menu xóa dòng
            var ctx = new ContextMenuStrip();
            var miDelete = new ToolStripMenuItem("Xóa dòng đã chọn") { Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            miDelete.Click += (s, e) => RemoveSelectedRows();
            ctx.Items.Add(miDelete);
            _grid.ContextMenuStrip = ctx;
            _grid.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete) RemoveSelectedRows();
            };
            _items.ListChanged += (s, e) => { UpdateCountLabel(); _grid.Invalidate(); };

            // ── Compose layout (THỨ TỰ THÊM QUAN TRỌNG cho DockStyle) ──
            // Dock fill được thêm cuối cùng để chiếm phần còn lại.
            card.Controls.Add(_grid);          // Fill
            card.Controls.Add(toolbarSep);     // Top
            card.Controls.Add(toolbar);        // Top (đẩy separator xuống)

            contentArea.Controls.Add(card);

            Controls.Add(contentArea);
            Controls.Add(footer);
            Controls.Add(header);

            Load += (s, e) => RepositionFooterButtons();
            UpdateCountLabel();
        }

        // ══════════════════════════════════════════════════════════════
        // PUBLIC API
        // ══════════════════════════════════════════════════════════════

        public void LoadForEdit(int configId, string configName, IEnumerable<Products> existingItems)
        {
            _editingConfigId = configId;
            _editingOriginalName = configName;
            _txtConfigName.Text = configName ?? string.Empty;
            Text = "Chỉnh sửa cấu hình - " + configName;

            _items.RaiseListChangedEvents = false;
            _items.Clear();
            if (existingItems != null)
            {
                foreach (var p in existingItems)
                    _items.Add(CloneProduct(p));
            }
            _items.RaiseListChangedEvents = true;
            _items.ResetBindings();
            UpdateCountLabel();
        }

        public void AddProducts(IEnumerable<Products> products)
        {
            if (products == null) return;
            foreach (var p in products)
            {
                if (p == null) continue;

                Products existing = null;
                if (p.Id > 0)
                    existing = _items.FirstOrDefault(x => x.Id == p.Id);
                if (existing == null && !string.IsNullOrWhiteSpace(p.SKU))
                    existing = _items.FirstOrDefault(x =>
                        string.Equals(x.SKU, p.SKU, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                    existing.SoLuong += Math.Max(1, p.SoLuong);
                else
                    _items.Add(CloneProduct(p));
            }
            _grid.Refresh();
        }

        // ══════════════════════════════════════════════════════════════
        // PRIVATE
        // ══════════════════════════════════════════════════════════════

        private static Products CloneProduct(Products p)
        {
            return new Products
            {
                Id = p.Id, SheetRowIndex = p.SheetRowIndex,
                Name = p.Name, Model = p.Model, SKU = p.SKU,
                Price = p.Price, PriceCost = p.PriceCost,
                Weight = p.Weight, Length = p.Length, Width = p.Width, Height = p.Height,
                Category = p.Category, Type = p.Type, HÃNG = p.HÃNG,
                TrangThai = p.TrangThai, Pole = p.Pole, Ir = p.Ir, Icu = p.Icu,
                PriceList = p.PriceList,
                SoLuong = p.SoLuong > 0 ? p.SoLuong : 1,
                ExtraAttributes = p.ExtraAttributes != null
                    ? new Dictionary<string, string>(p.ExtraAttributes, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
        }

        private void OpenProductSearchPopup()
        {
            if (_allProducts.Count == 0)
            {
                MessageBox.Show("Danh sách sản phẩm đang trống.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var frmSearch = new FrmProductSearch(_allProducts, isForQuote: false);
            frmSearch.StartPosition = FormStartPosition.CenterScreen;
            frmSearch.OnProductsSelected += (selectedList, _targetHeader) =>
            {
                if (this.IsDisposed) return;
                AddProducts(selectedList.Select(CloneProduct));
                this.BringToFront();
            };
            frmSearch.Show(this);
        }

        private void RemoveSelectedRows()
        {
            var rows = _grid.SelectedRows.Cast<DataGridViewRow>().ToList();
            if (rows.Count == 0) return;
            foreach (var row in rows)
            {
                if (row.DataBoundItem is Products p)
                    _items.Remove(p);
            }
        }

        private void UpdateCountLabel()
        {
            int total = _items.Sum(p => Math.Max(1, p.SoLuong));
            _lblCount.Text = $"{_items.Count} mã  •  {total} sản phẩm";
        }

        private async Task SaveAsync(Button trigger)
        {
            string configName = (_txtConfigName.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(configName))
            {
                MessageBox.Show("Vui lòng nhập tên cấu hình!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtConfigName.Focus();
                return;
            }
            if (_items.Count == 0)
            {
                MessageBox.Show("Cấu hình chưa có sản phẩm nào.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            trigger.Enabled = false;
            string originalText = trigger.Text;
            trigger.Text = "Đang lưu...";
            try
            {
                var snapshot = _items.ToList();

                bool nameChanged = _editingConfigId > 0
                    && !string.IsNullOrWhiteSpace(_editingOriginalName)
                    && !string.Equals(_editingOriginalName.Trim(), configName, StringComparison.OrdinalIgnoreCase);

                if (nameChanged)
                {
                    int oldId = _editingConfigId;
                    await Task.Run(() => DatabaseService.DeleteBuildConfig(oldId));
                }

                int id = await Task.Run(() =>
                    DatabaseService.SaveBuildConfigFromProducts(
                        configName: configName,
                        googleSheetName: null,
                        googleSpreadsheetId: null,
                        products: snapshot,
                        overwriteMode: true));

                SavedConfigName = configName;
                Saved = id > 0;

                MessageBox.Show($"Đã lưu cấu hình \"{configName}\" với {_items.Count} sản phẩm vào SQL Server.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu cấu hình:\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                trigger.Text = originalText;
                trigger.Enabled = true;
            }
        }
    }
}
