using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ECQ_Soft.Forms
{
    public class FrmSpinner : Form
    {
        private float _startAngle = 0;
        private readonly System.Windows.Forms.Timer _timer;
        private string _message;

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                this.Invalidate();
            }
        }

        public FrmSpinner(Rectangle bounds, string message = "Đang tải dữ liệu...")
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = bounds;
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.Opacity = 0.75;
            this.DoubleBuffered = true;
            this.Message = message;

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 30; // ~33 FPS
            _timer.Tick += Timer_Tick;
            _timer.Start();

            this.FormClosing += FrmSpinner_FormClosing;
        }

        private void FrmSpinner_FormClosing(object sender, FormClosingEventArgs e)
        {
            _timer.Stop();
            _timer.Dispose();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _startAngle = (_startAngle + 10) % 360;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Kích thước vòng xoay
            int size = 50;
            int x = (this.ClientSize.Width - size) / 2;
            int y = (this.ClientSize.Height - size) / 2 - 20;

            // Vẽ vòng tròn nền mờ (track)
            using (var trackPen = new Pen(Color.FromArgb(40, 255, 255, 255), 5))
            {
                e.Graphics.DrawEllipse(trackPen, x, y, size, size);
            }

            // Vẽ vòng cung chạy (active spinner)
            using (var spinnerPen = new Pen(Color.FromArgb(0, 120, 215), 5))
            {
                spinnerPen.StartCap = LineCap.Round;
                spinnerPen.EndCap = LineCap.Round;
                e.Graphics.DrawArc(spinnerPen, x, y, size, size, _startAngle, 100);
            }

            // Vẽ text thông điệp
            if (!string.IsNullOrEmpty(Message))
            {
                using (var font = new Font("Segoe UI", 11.5F, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                {
                    var textSize = e.Graphics.MeasureString(Message, font);
                    float tx = (this.ClientSize.Width - textSize.Width) / 2;
                    float ty = y + size + 25;

                    // Vẽ bóng mờ phía dưới text để tăng tương phản
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                    {
                        e.Graphics.DrawString(Message, font, shadowBrush, tx + 1, ty + 1);
                    }

                    e.Graphics.DrawString(Message, font, brush, tx, ty);
                }
            }
        }

        // Ngăn không cho Form này nhận focus
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }
    }
}
