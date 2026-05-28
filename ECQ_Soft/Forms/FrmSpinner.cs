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

        private Pen _trackPen;
        private Pen _spinnerPen;
        private Font _messageFont;
        private SolidBrush _textBrush;
        private SolidBrush _shadowBrush;

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                this.Invalidate();
            }
        }

        private Rectangle _bounds;

        public FrmSpinner(Rectangle bounds, string message = "Đang tải dữ liệu...")
        {
            _bounds = bounds;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = GetContentBounds(bounds);
            this.BackColor = Color.FromArgb(38, 38, 38);
            this.Opacity = 1;
            this.TopMost = true;
            this.DoubleBuffered = true;

            _trackPen = new Pen(Color.FromArgb(40, 255, 255, 255), 5);
            _spinnerPen = new Pen(Color.FromArgb(0, 120, 215), 5)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            _messageFont = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            _textBrush = new SolidBrush(Color.White);
            _shadowBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
            this.Message = message;

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 30; // ~33 FPS
            _timer.Tick += Timer_Tick;
            _timer.Start();

            this.FormClosing += FrmSpinner_FormClosing;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Bounds = GetContentBounds(_bounds);
        }

        private static Rectangle GetContentBounds(Rectangle screenBounds)
        {
            int width = Math.Min(560, Math.Max(380, screenBounds.Width / 3));
            int height = 150;
            int x = screenBounds.Left + (screenBounds.Width - width) / 2;
            int y = screenBounds.Top + (screenBounds.Height - height) / 2;
            return new Rectangle(x, y, width, height);
        }

        private void FrmSpinner_FormClosing(object sender, FormClosingEventArgs e)
        {
            _timer.Stop();
            _timer.Dispose();
            _trackPen?.Dispose();
            _spinnerPen?.Dispose();
            _messageFont?.Dispose();
            _textBrush?.Dispose();
            _shadowBrush?.Dispose();
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
            e.Graphics.DrawEllipse(_trackPen, x, y, size, size);

            // Vẽ vòng cung chạy (active spinner)
            e.Graphics.DrawArc(_spinnerPen, x, y, size, size, _startAngle, 100);

            // Vẽ text thông điệp
            if (!string.IsNullOrEmpty(Message))
            {
                var textSize = e.Graphics.MeasureString(Message, _messageFont);
                float tx = (this.ClientSize.Width - textSize.Width) / 2;
                float ty = y + size + 25;

                // Vẽ bóng mờ phía dưới text để tăng tương phản
                e.Graphics.DrawString(Message, _messageFont, _shadowBrush, tx + 1, ty + 1);

                e.Graphics.DrawString(Message, _messageFont, _textBrush, tx, ty);
            }
        }

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

    public class FrmLoadingBackdrop : Form
    {
        private readonly Rectangle _bounds;

        public FrmLoadingBackdrop(Rectangle bounds)
        {
            _bounds = bounds;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = bounds;
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.Opacity = 0.75;
            this.TopMost = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Bounds = _bounds;
        }

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
