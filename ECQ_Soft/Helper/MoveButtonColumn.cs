using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ECQ_Soft.Model;

namespace ECQ_Soft.Helper
{
    /// <summary>
    /// Cột DataGridView tùy chỉnh – hiển thị 2 button ▲▼ trong 1 ô,
    /// với gradient, hover highlight, rounded corners và arrow đẹp.
    /// </summary>
    public class MoveButtonColumn : DataGridViewColumn
    {
        public MoveButtonColumn() : base(new MoveButtonCell()) { }

        public override DataGridViewCell CellTemplate
        {
            get => base.CellTemplate;
            set
            {
                if (value != null && !(value is MoveButtonCell))
                    throw new InvalidCastException("CellTemplate phải là MoveButtonCell.");
                base.CellTemplate = value;
            }
        }
    }

    public class MoveButtonCell : DataGridViewCell
    {
        // ── Hover state (static: dùng chung toàn cột) ──
        // -1 = không hover; 0 = hover nửa trên; 1 = hover nửa dưới
        private static int _hoverRow = -1;
        private static int _hoverHalf = -1; // 0=up, 1=down

        // ── Màu sắc hiện đại (Modern UI) ──
        private static readonly Color ColNormalBg    = Color.White;
        private static readonly Color ColHoverBg     = Color.FromArgb(241, 243, 244);
        private static readonly Color ColPressedBg   = Color.FromArgb(232, 234, 237);
        private static readonly Color ColArrow       = Color.FromArgb(95, 99, 104);   // Xám đậm hiện đại
        private static readonly Color ColArrowHover  = Color.FromArgb(26, 115, 232);  // Xanh Blue khi hover
        private static readonly Color ColBorder      = Color.FromArgb(218, 220, 224); // Viền nhạt hiện đại

        public override Type FormattedValueType => typeof(string);
        public override Type ValueType         => typeof(string);
        public override object DefaultNewRowValue => "";

        // ── Khi attach vào DataGridView, đăng ký mouse events ──
        protected override void OnDataGridViewChanged()
        {
            base.OnDataGridViewChanged();
            var dgv = this.DataGridView;
            if (dgv == null) return;

            dgv.CellMouseMove  += Dgv_CellMouseMove;
            dgv.CellMouseLeave += Dgv_CellMouseLeave;
        }

        private void Dgv_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
            var dgv = sender as DataGridView;
            if (dgv == null || !dgv.Columns.Contains("ColMove")) return;
            if (e.ColumnIndex != dgv.Columns["ColMove"].Index) return;

            var bounds = dgv.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            // Chia đôi ô để bắt hover nửa trên/dưới
            int newHalf = e.Y < bounds.Height / 2 ? 0 : 1;

            if (_hoverRow != e.RowIndex || _hoverHalf != newHalf)
            {
                int oldRow = _hoverRow;
                _hoverRow  = e.RowIndex;
                _hoverHalf = newHalf;
                if (oldRow >= 0 && oldRow < dgv.RowCount)
                    dgv.InvalidateCell(e.ColumnIndex, oldRow);
                dgv.InvalidateCell(e.ColumnIndex, e.RowIndex);
            }
        }

        private void Dgv_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null || !dgv.Columns.Contains("ColMove")) return;
            if (e.ColumnIndex != dgv.Columns["ColMove"].Index) return;

            int oldRow = _hoverRow;
            _hoverRow  = -1;
            _hoverHalf = -1;
            if (oldRow >= 0 && oldRow < dgv.RowCount)
                dgv.InvalidateCell(e.ColumnIndex, oldRow);
        }

        // ── Paint ──
        protected override void Paint(
            Graphics g,
            Rectangle clipBounds,
            Rectangle cell,
            int rowIndex,
            DataGridViewElementStates state,
            object value,
            object formattedValue,
            string errorText,
            DataGridViewCellStyle style,
            DataGridViewAdvancedBorderStyle borderStyle,
            DataGridViewPaintParts parts)
        {
            var dgv = this.DataGridView;
            bool isSpecialRow = false;

            if (dgv != null && rowIndex >= 0 && rowIndex < dgv.Rows.Count)
            {
                var item = dgv.Rows[rowIndex].DataBoundItem as ConfigProductItem;
                if (item != null && (item.IsHeader || item.IsSummary))
                {
                    isSpecialRow = true;
                }
            }

            // 1. Vẽ nền (Trừ đi 1px ở cạnh phải và dưới để không đè lên Grid line)
            Rectangle bgRect = new Rectangle(cell.X, cell.Y, cell.Width - 1, cell.Height - 1);
            using (var bgBrush = new SolidBrush(style.BackColor.A == 0 ? Color.White : style.BackColor))
            {
                g.FillRectangle(bgBrush, bgRect);
            }

            // 2. Gọi base để vẽ viền lưới (Border) của ô
            base.Paint(g, clipBounds, cell, rowIndex, state, value, formattedValue,
                errorText, style, borderStyle,
                DataGridViewPaintParts.Border);

            if (isSpecialRow) return;

            g.SmoothingMode      = SmoothingMode.AntiAlias;
            g.InterpolationMode  = InterpolationMode.HighQualityBicubic;

            // ── CĂN GIỮA TUYỆT ĐỐI ──
            // Nút bấm: Rộng 18px, Cao 11px. Khoảng cách: 2px. Tổng: 24px.
            int btnW = 18;
            int btnH = 11;
            int gap  = 2;
            int totalH = btnH * 2 + gap;

            // Tính toán tọa độ X, Y để căn giữa khối 2 nút vào giữa ô
            // Dùng float để tính toán chính xác hơn sau đó cast về int
            float startX = cell.X + (cell.Width - btnW) / 2f;
            float startY = cell.Y + (cell.Height - totalH) / 2f;

            // ── Nửa trên (▲) ──
            var topRect = new Rectangle((int)startX, (int)startY, btnW, btnH);
            bool hoverUp = (_hoverRow == rowIndex && _hoverHalf == 0);
            DrawModernButton(g, topRect, isUp: true, hover: hoverUp);

            // ── Nửa dưới (▼) ──
            var botRect = new Rectangle((int)startX, (int)startY + btnH + gap, btnW, btnH);
            bool hoverDn = (_hoverRow == rowIndex && _hoverHalf == 1);
            DrawModernButton(g, botRect, isUp: false, hover: hoverDn);
        }

        private static void DrawModernButton(Graphics g, Rectangle r, bool isUp, bool hover)
        {
            if (r.Width <= 0 || r.Height <= 0) return;

            // Vẽ Shape nút (Bo tròn nhẹ 3px)
            int radius = 3;
            using (var path = RoundedRect(r, radius))
            {
                using (var brush = new SolidBrush(hover ? ColHoverBg : ColNormalBg))
                    g.FillPath(brush, path);

                using (var pen = new Pen(ColBorder, 1))
                    g.DrawPath(pen, path);
            }

            // Vẽ Mũi tên (▲ ▼)
            // Tính toán tọa độ mũi tên lệch 1 xíu để trông cân mắt nhất (Optical alignment)
            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;
            float arrowSize = 3.5f;

            PointF[] pts;
            if (isUp)
            {
                pts = new PointF[] {
                    new PointF(cx, cy - arrowSize/2f - 0.5f),
                    new PointF(cx + arrowSize, cy + arrowSize/2f - 0.5f),
                    new PointF(cx - arrowSize, cy + arrowSize/2f - 0.5f)
                };
            }
            else
            {
                pts = new PointF[] {
                    new PointF(cx, cy + arrowSize/2f + 0.5f),
                    new PointF(cx + arrowSize, cy - arrowSize/2f + 0.5f),
                    new PointF(cx - arrowSize, cy - arrowSize/2f + 0.5f)
                };
            }

            using (var arrowBrush = new SolidBrush(hover ? ColArrowHover : ColArrow))
                g.FillPolygon(arrowBrush, pts);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
