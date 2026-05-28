using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ECQ_Soft.Utils
{
    public static class FunctionUtils
    {
        public static void SetDoubleBuffered(Control control, bool enabled = true)
        {
            if (control == null) return;

            typeof(Control).InvokeMember(
                "DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                control,
                new object[] { enabled });
        }

        public static void SetDoubleBufferedRecursive(Control control, bool enabled = true)
        {
            if (control == null) return;

            SetDoubleBuffered(control, enabled);

            foreach (Control child in control.Controls)
            {
                SetDoubleBufferedRecursive(child, enabled);
            }
        }

        public static int ToInt32(object value, int defaultValue = 0)
        {
            if (value == null || value == DBNull.Value) return defaultValue;
            int parsed;
            return int.TryParse(value.ToString(), out parsed) ? parsed : defaultValue;
        }

        public static string SafeString(object value)
        {
            return value == null || value == DBNull.Value ? string.Empty : value.ToString();
        }

        public static string EscapeDataViewFilterValue(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public static void RunOnUi(Control control, Action action)
        {
            if (control == null || action == null) return;

            if (control.InvokeRequired)
            {
                control.Invoke(action);
                return;
            }

            action();
        }

        public static async Task WithWaitCursorAsync(Control control, Func<Task> action)
        {
            if (control == null || action == null) return;

            Cursor oldCursor = control.Cursor;
            control.Cursor = Cursors.WaitCursor;
            try
            {
                await action();
            }
            finally
            {
                control.Cursor = oldCursor;
            }
        }

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);

        private const int WM_SETREDRAW = 11;

        public static void SuspendDrawing(Control control)
        {
            if (control != null && control.IsHandleCreated)
            {
                SendMessage(control.Handle, WM_SETREDRAW, false, 0);
            }
        }

        public static void ResumeDrawing(Control control)
        {
            if (control != null && control.IsHandleCreated)
            {
                SendMessage(control.Handle, WM_SETREDRAW, true, 0);
                control.Refresh();
            }
        }
    }
}
