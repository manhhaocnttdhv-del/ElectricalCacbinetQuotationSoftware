using System;
using System.Drawing;
using System.Windows.Forms;

namespace ECQ_Soft.Helper
{
    /// <summary>
    /// Bảng màu nhanh kiểu Excel — hiện popup với các màu preset.
    /// Dùng: var picker = new ColorPickerPopup(); picker.ShowDialog(); var c = picker.SelectedColor;
    /// </summary>
    public class ColorPickerPopup : Form
    {
        public Color? SelectedColor { get; private set; }

        private static readonly Color[] PresetColors =
        {
            // Row 1 – Trắng → Đen
            Color.White,      Color.FromArgb(242,242,242), Color.FromArgb(217,217,217),
            Color.FromArgb(166,166,166), Color.FromArgb(127,127,127), Color.FromArgb(89,89,89),
            Color.FromArgb(38,38,38),   Color.Black,

            // Row 2 – Đỏ nhạt → Đỏ đậm
            Color.FromArgb(255,199,206), Color.FromArgb(255,102,102), Color.Red,
            Color.FromArgb(192,0,0),     Color.FromArgb(128,0,0),
            Color.FromArgb(255,153,153), Color.FromArgb(204,0,0), Color.FromArgb(102,0,0),

            // Row 3 – Cam / Vàng
            Color.FromArgb(255,235,156), Color.FromArgb(255,214,102), Color.FromArgb(255,192,0),
            Color.FromArgb(255,153,0),   Color.FromArgb(204,102,0),
            Color.FromArgb(255,242,204), Color.FromArgb(255,229,153), Color.FromArgb(255,166,0),

            // Row 4 – Xanh lá
            Color.FromArgb(198,239,206), Color.FromArgb(0,255,0),    Color.FromArgb(0,176,80),
            Color.FromArgb(0,128,0),     Color.FromArgb(0,64,0),
            Color.LightGreen,             Color.FromArgb(146,208,80),  Color.FromArgb(0,100,0),

            // Row 5 – Xanh dương
            Color.FromArgb(189,215,238), Color.FromArgb(173,216,230), Color.FromArgb(0,176,240),
            Color.FromArgb(0,112,192),   Color.FromArgb(0,70,127),
            Color.FromArgb(0,0,255),     Color.FromArgb(0,0,128),     Color.FromArgb(0,32,96),

            // Row 6 – Tím / Hồng
            Color.FromArgb(255,204,255), Color.FromArgb(255,153,204), Color.FromArgb(255,0,255),
            Color.FromArgb(153,0,153),   Color.FromArgb(102,0,102),
            Color.FromArgb(204,153,255), Color.FromArgb(153,51,255),  Color.FromArgb(76,0,153),

            // Row 7 – Cyan / Teal
            Color.FromArgb(204,255,255), Color.FromArgb(153,255,255), Color.FromArgb(0,255,255),
            Color.FromArgb(0,176,176),   Color.FromArgb(0,128,128),
            Color.FromArgb(0,255,204),   Color.FromArgb(0,204,153),   Color.FromArgb(0,64,64),
        };

        private const int CellSize = 24;
        private const int Cols     = 8;
        private const int Padding  = 6;

        public ColorPickerPopup()
        {
            int rows = (int)Math.Ceiling(PresetColors.Length / (double)Cols);

            this.Text            = "Chọn màu";
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition   = FormStartPosition.Manual;
            this.Location        = Cursor.Position;
            this.BackColor       = Color.White;
            this.ShowInTaskbar   = false;
            this.KeyPreview      = true;
            this.KeyDown        += (s, e) => { if (e.KeyCode == Keys.Escape) this.Close(); };

            int panelW = Cols * CellSize + Padding * 2;
            int panelH = rows * CellSize + Padding * 2;

            // Vẽ các ô màu
            var panel = new Panel { Location = new Point(Padding, Padding), Size = new Size(panelW - Padding * 2, panelH - Padding * 2) };
            for (int i = 0; i < PresetColors.Length; i++)
            {
                int col = i % Cols;
                int row = i / Cols;
                var color = PresetColors[i];

                var btn = new Panel
                {
                    BackColor = color,
                    Size      = new Size(CellSize - 2, CellSize - 2),
                    Location  = new Point(col * CellSize, row * CellSize),
                    Cursor    = Cursors.Hand,
                    Tag       = color
                };
                btn.MouseEnter += (s, e) => ((Panel)s).Size = new Size(CellSize - 1, CellSize - 1);
                btn.MouseLeave += (s, e) => ((Panel)s).Size = new Size(CellSize - 2, CellSize - 2);
                btn.Click      += (s, e) =>
                {
                    SelectedColor = (Color)((Panel)s).Tag;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                panel.Controls.Add(btn);
            }

            // Nút "Chọn màu khác..."
            var btnMore = new Button
            {
                Text     = "🎨 Chọn màu khác...",
                Size     = new Size(panelW - Padding * 2, 26),
                Location = new Point(Padding, panelH),
                FlatStyle = FlatStyle.Flat,
                Cursor   = Cursors.Hand
            };
            btnMore.Click += (s, e) =>
            {
                using (var dlg = new ColorDialog { FullOpen = true })
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        SelectedColor = dlg.Color;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
            };

            this.Controls.Add(panel);
            this.Controls.Add(btnMore);
            this.ClientSize = new Size(panelW, panelH + 32);
        }
    }
}
