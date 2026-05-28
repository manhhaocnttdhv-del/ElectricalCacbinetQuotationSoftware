using System.Drawing;

namespace ECQ_Soft.Utils
{
    public static class AppConstant
    {
        public const string AppName = "ECQ Soft";
        public const string DefaultInfoCaption = "Thong bao";
        public const string DefaultWarningCaption = "Canh bao";
        public const string DefaultErrorCaption = "Loi";
        public const string DefaultConfirmCaption = "Xac nhan";

        public static class Network
        {
            public const string DefaultHost = "8.8.8.8";
            public const int DefaultPort = 53;
            public const int DefaultTimeoutMs = 2000;
        }

        public static class Ui
        {
            public static readonly Color PrimaryColor = Color.FromArgb(26, 115, 232);
            public static readonly Color PrimaryDarkColor = Color.FromArgb(29, 78, 216);
            public static readonly Color DangerColor = Color.FromArgb(217, 83, 79);
            public static readonly Color BorderColor = Color.FromArgb(226, 232, 240);
            public static readonly Color TextColor = Color.FromArgb(45, 55, 72);
            public static readonly Color MutedTextColor = Color.FromArgb(74, 85, 104);
            public static readonly Color DisabledColor = Color.Gray;
            public static readonly Color HoverColor = Color.FromArgb(241, 243, 244);
            public static readonly Color PressedColor = Color.FromArgb(215, 230, 252);
            public static readonly Color SurfaceColor = Color.White;
            public static readonly Color SidebarBackColor = Color.FromArgb(241, 245, 249);
            public static readonly Color SidebarHoverColor = Color.FromArgb(226, 232, 240);
            public static readonly Color SidebarPressedColor = Color.FromArgb(203, 213, 225);
            public static readonly Color SidebarActiveBackColor = Color.FromArgb(219, 234, 254);
            public static readonly Color SidebarTextColor = Color.FromArgb(51, 65, 85);
            public static readonly Color SidebarIconColor = Color.FromArgb(71, 85, 105);
        }
    }
}
