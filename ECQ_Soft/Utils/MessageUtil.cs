using System;
using System.Windows.Forms;

namespace ECQ_Soft.Utils
{
    public static class MessageUtil
    {
        public static void Info(string message, string caption = null, IWin32Window owner = null)
        {
            Show(owner, message, caption ?? AppConstant.DefaultInfoCaption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void Warning(string message, string caption = null, IWin32Window owner = null)
        {
            Show(owner, message, caption ?? AppConstant.DefaultWarningCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void Error(string message, string caption = null, IWin32Window owner = null)
        {
            Show(owner, message, caption ?? AppConstant.DefaultErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void Error(Exception ex, string prefix = null, string caption = null, IWin32Window owner = null)
        {
            string message = string.IsNullOrWhiteSpace(prefix) ? ex.Message : prefix + ex.Message;
            Error(message, caption, owner);
        }

        public static bool Confirm(string message, string caption = null, IWin32Window owner = null)
        {
            DialogResult result;
            if (owner == null)
            {
                result = MessageBox.Show(message, caption ?? AppConstant.DefaultConfirmCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            }
            else
            {
                result = MessageBox.Show(owner, message, caption ?? AppConstant.DefaultConfirmCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            }

            return result == DialogResult.Yes;
        }

        private static void Show(IWin32Window owner, string message, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (owner == null)
            {
                MessageBox.Show(message, caption, buttons, icon);
                return;
            }

            MessageBox.Show(owner, message, caption, buttons, icon);
        }
    }
}
