using ECQ_Soft.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ECQ_Soft
{
    public static class GridHelper
    {
        private static readonly char[] LineSeparators = new[] { '\n', '\r' };

        public static void SetDoubleBuffered(this DataGridView dgv, bool setting)
        {
            Type dgvType = dgv.GetType();
            System.Reflection.PropertyInfo pi = dgvType.GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (pi != null)
            {
                pi.SetValue(dgv, setting, null);
            }
        }

        public static void EnsureMoveColumns(DataGridView dgv)
        {
            if (dgv == null || dgv.Columns.Contains("ColMove")) return;

            var colMove = new DataGridViewButtonColumn
            {
                Name = "ColMove",
                HeaderText = "Di chuyển",
                Width = 60,
                FlatStyle = FlatStyle.Flat
            };
            dgv.Columns.Insert(0, colMove);
        }

        public static void FormatConfigGrid(DataGridView dgv)
        {
            if (dgv == null || dgv.Columns.Count == 0) return;

            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(255, 255, 0); // Yellow
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 35;

            // Common headers
            if (dgv.Columns.Contains("STT")) dgv.Columns["STT"].Width = 40;
            if (dgv.Columns.Contains("TenHang")) { dgv.Columns["TenHang"].HeaderText = "Tên hàng"; dgv.Columns["TenHang"].Width = 300; }
            if (dgv.Columns.Contains("MaHang")) dgv.Columns["MaHang"].HeaderText = "Mã hàng";
            if (dgv.Columns.Contains("XuatXu")) dgv.Columns["XuatXu"].HeaderText = "Xuất xứ";
            if (dgv.Columns.Contains("DonVi")) dgv.Columns["DonVi"].HeaderText = "Đơn vị";
            if (dgv.Columns.Contains("SoLuong")) dgv.Columns["SoLuong"].HeaderText = "Số lượng";

            string[] currencyCols = { "DonGiaVND", "ThanhTienVND", "GiaNhap", "ThanhTien", "LoiNhuan", "BangGia" };
            foreach (var colName in currencyCols)
            {
                if (dgv.Columns.Contains(colName))
                {
                    dgv.Columns[colName].DefaultCellStyle.Format = "N0";
                    dgv.Columns[colName].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            if (dgv.Columns.Contains("IsHeader")) dgv.Columns["IsHeader"].Visible = false;
            if (dgv.Columns.Contains("IsSummary")) dgv.Columns["IsSummary"].Visible = false;
            if (dgv.Columns.Contains("SheetRowIndex")) dgv.Columns["SheetRowIndex"].Visible = false;

            dgv.RowHeadersVisible = false;
            dgv.BackgroundColor = Color.White;
        }

        public static void DrawRichCabinetCell(Graphics g, Rectangle bounds, string text, Font font, bool isSelected, bool isHeader)
        {
            if (isHeader)
            {
                using (var headerBrush = new SolidBrush(Color.FromArgb(146, 208, 80))) // Green header
                using (var headerFont = new Font(font, FontStyle.Bold))
                {
                    g.FillRectangle(headerBrush, bounds);
                    TextRenderer.DrawText(g, text, headerFont, bounds, Color.Black, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }
                return;
            }

            // Regular item with specs
            string[] lines = text.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return;

            // Draw main name
            using (var mainFont = new Font(font, FontStyle.Bold))
            {
                Size mainSize = TextRenderer.MeasureText(lines[0], mainFont);
                Rectangle mainRect = new Rectangle(bounds.Left + 5, bounds.Top + 5, bounds.Width - 10, mainSize.Height);
                TextRenderer.DrawText(g, lines[0], mainFont, mainRect, Color.Black, TextFormatFlags.Top | TextFormatFlags.Left);

                // Draw sub lines (specs) in red
                int subFontSize = Math.Max(1, (int)Math.Round(font.Size - 1));
                using (var subFont = new Font(font.FontFamily, subFontSize, FontStyle.Regular))
                {
                    int currentY = mainRect.Bottom + 2;
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        Rectangle subRect = new Rectangle(bounds.Left + 15, currentY, bounds.Width - 20, 15);
                        TextRenderer.DrawText(g, line, subFont, subRect, Color.Red, TextFormatFlags.Top | TextFormatFlags.Left);
                        currentY += 14;
                    }
                }
            }
        }
    }
}
