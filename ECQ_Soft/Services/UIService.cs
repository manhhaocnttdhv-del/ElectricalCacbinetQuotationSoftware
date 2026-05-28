using System.Drawing;
using System.Windows.Forms;
using ECQ_Soft.Utils;
using FontAwesome.Sharp;

namespace ECQ_Soft.Services
{
    public static class UIService
    {
        private const int NavigationIconCanvasSize = 28;

        public static void StyleFlatButton(Button button, bool primary = false)
        {
            if (button == null) return;

            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = AppConstant.Ui.BorderColor;
            button.FlatAppearance.MouseOverBackColor = AppConstant.Ui.HoverColor;
            button.FlatAppearance.MouseDownBackColor = AppConstant.Ui.PressedColor;
            button.BackColor = primary ? AppConstant.Ui.PrimaryColor : Color.FromArgb(248, 249, 250);
            button.ForeColor = primary ? Color.White : AppConstant.Ui.MutedTextColor;
            button.Cursor = Cursors.Hand;
        }

        public static void StyleHeaderButton(Button button, Color textColor, Color hoverColor)
        {
            if (button == null) return;

            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = hoverColor;
            button.FlatAppearance.MouseDownBackColor = AppConstant.Ui.PressedColor;
            button.BackColor = Color.White;
            button.ForeColor = textColor;
            button.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            button.TextAlign = ContentAlignment.MiddleCenter;
        }

        public static void StyleNavigationButton(
            Button button,
            string text,
            IconChar icon,
            int width,
            int iconSize)
        {
            if (button == null) return;

            button.Text = text;
            button.Tag = icon;
            button.Width = width;
            button.Height = 52;
            button.Margin = new Padding(0, 0, 0, 8);
            button.Padding = new Padding(20, 0, 12, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = AppConstant.Ui.SidebarHoverColor;
            button.FlatAppearance.MouseDownBackColor = AppConstant.Ui.SidebarPressedColor;
            button.BackColor = AppConstant.Ui.SidebarBackColor;
            button.ForeColor = AppConstant.Ui.SidebarTextColor;
            button.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.Image = CreateNavigationImage(icon, AppConstant.Ui.SidebarIconColor, iconSize);
            button.RightToLeft = RightToLeft.No;
            button.AutoEllipsis = false;
            button.UseCompatibleTextRendering = false;
            button.Cursor = Cursors.Hand;
        }

        public static void SetNavigationButtonState(
            Button button,
            bool active,
            int normalIconSize,
            int activeIconSize)
        {
            if (button == null) return;

            button.BackColor = active ? AppConstant.Ui.SidebarActiveBackColor : AppConstant.Ui.SidebarBackColor;
            button.ForeColor = active ? AppConstant.Ui.PrimaryDarkColor : AppConstant.Ui.SidebarTextColor;
            button.Font = new Font("Segoe UI", 10F, active ? FontStyle.Bold : FontStyle.Regular);

            if (button.Tag is IconChar icon)
            {
                Image oldImage = button.Image;
                button.Image = CreateNavigationImage(
                    icon,
                    active ? AppConstant.Ui.PrimaryDarkColor : AppConstant.Ui.SidebarIconColor,
                    active ? activeIconSize : normalIconSize);
                oldImage?.Dispose();
            }
        }

        private static Bitmap CreateNavigationImage(IconChar icon, Color color, int iconSize)
        {
            Bitmap iconBitmap = FormsIconHelper.ToBitmap(icon, color, iconSize);
            Bitmap canvas = new Bitmap(NavigationIconCanvasSize, NavigationIconCanvasSize);

            using (Graphics graphics = Graphics.FromImage(canvas))
            {
                graphics.Clear(Color.Transparent);
                int x = (NavigationIconCanvasSize - iconBitmap.Width) / 2;
                int y = (NavigationIconCanvasSize - iconBitmap.Height) / 2;
                graphics.DrawImage(iconBitmap, x, y, iconBitmap.Width, iconBitmap.Height);
            }

            iconBitmap.Dispose();
            return canvas;
        }

        public static void ApplyButtonPermissionState(Button button, bool enabled, Color? enabledBackColor = null)
        {
            if (button == null) return;

            button.Enabled = enabled;
            if (!enabled)
            {
                button.BackColor = AppConstant.Ui.DisabledColor;
                button.ForeColor = Color.White;
                return;
            }

            if (enabledBackColor.HasValue)
            {
                button.BackColor = enabledBackColor.Value;
            }
        }

        public static string ShowInputDialog(IWin32Window owner, string title, string promptText, string defaultValue = "")
        {
            using (Form form = new Form())
            using (Label label = new Label())
            using (TextBox textBox = new TextBox())
            using (Button buttonOk = new Button())
            using (Button buttonCancel = new Button())
            {
                form.Text = title;
                form.ClientSize = new Size(396, 115);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.BackColor = Color.White;

                label.Text = promptText;
                label.SetBounds(12, 15, 372, 18);
                label.AutoSize = true;
                label.Font = new Font("Segoe UI", 9.5F);

                textBox.Text = defaultValue ?? string.Empty;
                textBox.SetBounds(12, 36, 372, 25);
                textBox.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
                textBox.Font = new Font("Segoe UI", 10F);

                buttonOk.Text = "OK";
                buttonOk.SetBounds(228, 75, 75, 28);
                buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                buttonOk.DialogResult = DialogResult.OK;
                buttonOk.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                StyleFlatButton(buttonOk, true);

                buttonCancel.Text = "Huy";
                buttonCancel.SetBounds(309, 75, 75, 28);
                buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                buttonCancel.DialogResult = DialogResult.Cancel;
                buttonCancel.Font = new Font("Segoe UI", 9.5F);
                StyleFlatButton(buttonCancel);

                form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
                form.AcceptButton = buttonOk;
                form.CancelButton = buttonCancel;

                DialogResult result = owner == null ? form.ShowDialog() : form.ShowDialog(owner);
                return result == DialogResult.OK ? textBox.Text : null;
            }
        }
    }
}
