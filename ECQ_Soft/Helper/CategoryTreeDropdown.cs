using ECQ_Soft.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ECQ_Soft.Helper
{
    /// <summary>
    /// Custom dropdown control hiển thị danh mục đa cấp dạng cây (TreeView-like).
    /// Vẽ đệ quy – click ▶/▼ để expand/collapse, click node lá để chọn giá trị.
    /// </summary>
    public class CategoryTreeDropdown : ComboBox
    {
        // ── State ────────────────────────────────────────────────────────────
        private List<CategoryTreeNode> _roots = new List<CategoryTreeNode>();
        private ToolStripDropDown      _dropDown;
        private ToolStripControlHost   _host;
        private TreePanel              _treePanel;
        private bool                   _suppressDropDown = false;

        // ── Public API ───────────────────────────────────────────────────────
        /// <summary>FullPath của node đang được chọn. "" = chưa chọn / tất cả.</summary>
        public string SelectedFullPath { get; private set; } = "";

        /// <summary>Label hiển thị của node hiện tại (ngắn gọn).</summary>
        public string SelectedLabel { get; private set; } = "";

        /// <summary>Fire khi người dùng chọn một node lá.</summary>
        public event EventHandler<string> SelectionChanged;

        // ── Constructor ──────────────────────────────────────────────────────
        public CategoryTreeDropdown()
        {
            this.DropDownStyle  = ComboBoxStyle.DropDown;
            this.DropDownHeight = 1;      // tắt dropdown gốc
            this.KeyPress      += (s, e) => e.Handled = true;
        }

        // ── Lazy init popup (tránh crash trong VS Designer) ──────────────────
        private bool _initialized = false;

        private void EnsureInitialized()
        {
            if (_initialized) return;
            if (this.DesignMode) return;   // Không khởi tạo trong VS Designer

            _initialized = true;

            _treePanel = new TreePanel();
            _treePanel.NodeSelected += OnNodeSelected;
            _treePanel.NeedResize   += OnNeedResize;

            _host = new ToolStripControlHost(_treePanel)
            {
                AutoSize = false,
                Padding  = Padding.Empty,
                Margin   = Padding.Empty,
            };

            _dropDown = new ToolStripDropDown
            {
                AutoClose = true,
                Padding   = Padding.Empty
            };
            _dropDown.Items.Add(_host);
        }

        // Chặn dropdown gốc của ComboBox
        protected override void OnDropDown(EventArgs e) { }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            EnsureInitialized();
            if (!_suppressDropDown) ShowTreeDropDown();
        }

        // ── Public methods ───────────────────────────────────────────────────
        /// <summary>Nạp cây vào control. Gọi sau khi ParseToTreeNodes.</summary>
        public void LoadTree(List<CategoryTreeNode> roots)
        {
            EnsureInitialized();
            _roots = roots ?? new List<CategoryTreeNode>();
            if (_treePanel != null) _treePanel.SetRoots(_roots);
            SelectedFullPath  = "";
            SelectedLabel     = "";
            _suppressDropDown = true;
            this.Text         = "";
            _suppressDropDown = false;
        }

        /// <summary>Đặt lại về "(Tất cả)" mà không kích hoạt SelectionChanged.</summary>
        public void ResetSelection()
        {
            SelectedFullPath  = "";
            SelectedLabel     = "";
            _suppressDropDown = true;
            this.Text         = "";
            _suppressDropDown = false;
        }

        // ── Private helpers ──────────────────────────────────────────────────
        private void ShowTreeDropDown()
        {
            if (_treePanel == null || _dropDown == null) return;
            if (_roots == null || _roots.Count == 0) return;

            int width = Math.Max(this.Width, 280);
            ResizePanel(width);
            _dropDown.Show(this, 0, this.Height);
        }

        private void ResizePanel(int width)
        {
            if (_treePanel == null || _host == null) return;
            int height       = Math.Min(_treePanel.PreferredHeight + 4, 400);
            _treePanel.Size  = new Size(width, height);
            _host.Size       = new Size(width, height);
        }

        private void OnNodeSelected(object sender, CategoryTreeNode node)
        {
            SelectedFullPath  = node.FullPath;
            SelectedLabel     = node.Label;

            _suppressDropDown = true;
            this.Text         = node.FullPath;
            _suppressDropDown = false;

            _dropDown?.Close();
            SelectionChanged?.Invoke(this, node.FullPath);
        }

        private void OnNeedResize(object sender, EventArgs e)
        {
            ResizePanel(_treePanel?.Width > 0 ? _treePanel.Width : Math.Max(this.Width, 280));
        }

        // ── ReadOnly-like behavior ────────────────────────────────────────────
        private bool _readOnly;
        public new bool ReadOnly
        {
            get => _readOnly;
            set
            {
                _readOnly    = value;
                this.TabStop = !value;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // INNER CLASS: TreePanel – panel vẽ đệ quy cây node
        // ════════════════════════════════════════════════════════════════════
        private class TreePanel : Panel
        {
            // Hằng số giao diện
            private const int ROW_HEIGHT   = 24;
            private const int INDENT       = 18;  // pixel thụt lề mỗi cấp
            private const int TOGGLE_SIZE  = 18;  // kích thước ô ▶/▼
            private const int TEXT_OFFSET  = 4;   // khoảng cách giữa toggle và text

            private List<CategoryTreeNode> _roots = new List<CategoryTreeNode>();

            /// <summary>Danh sách node đang hiển thị (flat, theo thứ tự vẽ), gồm cả các node bị ẩn theo collapse.</summary>
            private List<(CategoryTreeNode Node, Rectangle Bounds, Rectangle ToggleBounds)> _visibleRows
                = new List<(CategoryTreeNode, Rectangle, Rectangle)>();

            public event EventHandler<CategoryTreeNode> NodeSelected;
            public event EventHandler NeedResize;

            public int PreferredHeight => _visibleRows.Count * ROW_HEIGHT + 4;

            public TreePanel()
            {
                this.DoubleBuffered  = true;
                this.BackColor       = Color.White;
                this.BorderStyle     = BorderStyle.FixedSingle;
                this.AutoScroll      = true;
                this.Cursor          = Cursors.Hand;
            }

            public void SetRoots(List<CategoryTreeNode> roots)
            {
                _roots = roots;
                // Expand level 0 mặc định
                foreach (var r in _roots) r.IsExpanded = true;
                RebuildVisible();
                Invalidate();
            }

            // ── Build danh sách visible (đệ quy) ────────────────────────────
            private void RebuildVisible()
            {
                _visibleRows.Clear();
                int y = 2;
                BuildRows(_roots, ref y);

                // Cập nhật ClientSize để scroll hoạt động đúng
                this.AutoScrollMinSize = new Size(0, y + 2);
            }

            /// <summary>Đệ quy duyệt cây và thêm các node vào _visibleRows.</summary>
            private void BuildRows(List<CategoryTreeNode> nodes, ref int y)
            {
                foreach (var node in nodes)
                {
                    int x       = node.Level * INDENT + 4;
                    var bounds  = new Rectangle(x, y, this.ClientSize.Width - x - 4, ROW_HEIGHT);

                    // Vùng click toggle (chỉ có nếu có con)
                    Rectangle toggleBounds = Rectangle.Empty;
                    if (!node.IsLeaf)
                    {
                        toggleBounds = new Rectangle(x, y + (ROW_HEIGHT - TOGGLE_SIZE) / 2, TOGGLE_SIZE, TOGGLE_SIZE);
                    }

                    _visibleRows.Add((node, bounds, toggleBounds));
                    y += ROW_HEIGHT;

                    // Đệ quy vào con nếu đang mở
                    if (node.IsExpanded && node.Children.Count > 0)
                    {
                        BuildRows(node.Children, ref y);
                    }
                }
            }

            // ── Paint đệ quy ─────────────────────────────────────────────────
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                int scrollY = this.AutoScrollPosition.Y;

                foreach (var (node, bounds, toggleBounds) in _visibleRows)
                {
                    // Dịch theo scroll
                    var drawBounds  = new Rectangle(bounds.X,  bounds.Y  + scrollY, bounds.Width,  bounds.Height);
                    var drawToggle  = toggleBounds.IsEmpty
                        ? Rectangle.Empty
                        : new Rectangle(toggleBounds.X, toggleBounds.Y + scrollY, toggleBounds.Width, toggleBounds.Height);

                    DrawNode(g, node, drawBounds, drawToggle);
                }
            }

            /// <summary>Vẽ 1 node: background hover, toggle ▶/▼, icon folder/leaf, label.</summary>
            private void DrawNode(Graphics g, CategoryTreeNode node, Rectangle bounds, Rectangle toggleBounds)
            {
                // Highlight hover (TODO: theo _hoverNode)
                if (node == _hoverNode)
                {
                    using (var br = new SolidBrush(Color.FromArgb(229, 243, 255)))
                        g.FillRectangle(br, bounds);
                }

                // ── Vẽ toggle ▶ / ▼ ──────────────────────────────────────────
                if (!toggleBounds.IsEmpty)
                {
                    string glyph = node.IsExpanded ? "▼" : "▶";
                    using (var f  = new Font("Segoe UI", 10f))
                    using (var br = new SolidBrush(Color.FromArgb(90, 90, 90)))
                    {
                        var sf = new StringFormat
                        {
                            Alignment     = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };
                        g.DrawString(glyph, f, br, toggleBounds, sf);
                    }
                }

                // ── Vẽ icon thư mục / lá ─────────────────────────────────────
                int iconX = (toggleBounds.IsEmpty ? bounds.X : toggleBounds.Right) + TEXT_OFFSET;
                string icon = node.IsLeaf ? "📄" : (node.IsExpanded ? "📂" : "📁");
                using (var f = new Font("Segoe UI Emoji", 8.5f))
                using (var br = new SolidBrush(Color.Black))
                {
                    g.DrawString(icon, f, br, iconX, bounds.Y + (ROW_HEIGHT - 14) / 2);
                }
                int textX = iconX + 20;

                // ── Vẽ label ─────────────────────────────────────────────────
                Color textColor = node.IsLeaf
                    ? Color.FromArgb(30, 30, 30)
                    : Color.FromArgb(0, 90, 160);

                FontStyle style = node.IsLeaf ? FontStyle.Regular : FontStyle.Bold;
                using (var f  = new Font("Segoe UI", 8.5f, style))
                using (var br = new SolidBrush(textColor))
                {
                    var textRect = new Rectangle(textX, bounds.Y, bounds.Right - textX - 4, ROW_HEIGHT);
                    var sf = new StringFormat
                    {
                        Alignment     = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        Trimming      = StringTrimming.EllipsisCharacter,
                        FormatFlags   = StringFormatFlags.NoWrap   // Không xuống dòng
                    };
                    g.DrawString(node.Label, f, br, textRect, sf);
                }
            }

            // ── Hover tracking ───────────────────────────────────────────────
            private CategoryTreeNode _hoverNode;

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                var node = HitTestNode(e.Location);
                if (node != _hoverNode)
                {
                    _hoverNode = node;
                    Invalidate();
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hoverNode = null;
                Invalidate();
            }

            // ── Click: toggle expand hoặc select node ────────────────────────
            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);

                int scrollY = this.AutoScrollPosition.Y;
                var hitPt   = new Point(e.X, e.Y - scrollY);

                for (int i = 0; i < _visibleRows.Count; i++)
                {
                    var (node, bounds, toggleBounds) = _visibleRows[i];

                    // Click vào toggle ▶/▼? → chỉ expand/collapse, KHÔNG chọn
                    if (!toggleBounds.IsEmpty && toggleBounds.Contains(hitPt))
                    {
                        node.IsExpanded = !node.IsExpanded;
                        RebuildVisible();
                        Invalidate();
                        NeedResize?.Invoke(this, EventArgs.Empty);
                        return;
                    }

                    // Click vào bounds của node (dù là cha hay lá)?
                    if (bounds.Contains(hitPt))
                    {
                        // Luôn fire NodeSelected để filter sản phẩm (cả node cha lẫn lá)
                        NodeSelected?.Invoke(this, node);

                        // Nếu là node cha → toggle expand/collapse thêm
                        if (!node.IsLeaf)
                        {
                            node.IsExpanded = !node.IsExpanded;
                            RebuildVisible();
                            Invalidate();
                            NeedResize?.Invoke(this, EventArgs.Empty);
                        }
                        return;
                    }
                }
            }

            // ── Hit test ─────────────────────────────────────────────────────
            private CategoryTreeNode HitTestNode(Point pt)
            {
                int scrollY = this.AutoScrollPosition.Y;
                var hitPt   = new Point(pt.X, pt.Y - scrollY);

                foreach (var (node, bounds, _) in _visibleRows)
                {
                    if (bounds.Contains(hitPt)) return node;
                }
                return null;
            }

            // Khi resize panel → rebuild để bounds đúng
            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                RebuildVisible();
                Invalidate();
            }
        }
    }
}
