
using ECQ_Soft.Model;
using ECQ_Soft.Services;
using ECQ_Soft.Utils;
using FontAwesome.Sharp;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Color = System.Drawing.Color;
using Button = System.Windows.Forms.Button;
using Padding = System.Windows.Forms.Padding;


namespace ECQ_Soft
{
    public partial class FrmMain : Form
    {
        private FrmQuotation _frmQuotation;
        private FrmRelation  _frmRelation;
        private FrmConfig    _frmConfig;
        private FrmUserManagement _frmUserManagement;
        private FlowLayoutPanel _navigationFlowPanel;
        private Panel _headerActionsPanel;
        private IconButton _sidebarToggleButton;
        private Timer _sidebarAnimationTimer;
        private ToolTip _headerToolTip;
        private bool _headerConfigured;
        private bool _sidebarCollapsed;
        private bool _sidebarTargetCollapsed;
        private bool _isSidebarAnimating;
        private int _sidebarAnimationTargetWidth;
        private const int SidebarWidth = 292;
        private const int SidebarCollapsedWidth = 72;
        private const int SidebarAnimationStep = 80;
        private const int SidebarIconSize = 22;
        private const int SidebarActiveIconSize = 24;
        private const int HeaderIconSize = 18;

        // Tab index của tab "Cấu hình" (tabPage3)

        // Tab index của tab "Cấu hình" (tabPage3)
        private const int CONFIG_TAB_INDEX = 2;
        // Lưu tab trước đó để rollback nếu người dùng bấm Cancel trong modal
        private int _previousTabIndex = 0;
        // Cờ để tránh xử lý sự kiện SelectedIndexChanged đệ quy
        private bool _isHandlingTabChange = false;

        public FrmMain()
        {
            InitializeComponent();
            
            // Kích hoạt DoubleBuffered đệ quy cho toàn bộ form và các control con để loại bỏ hoàn toàn hiện tượng nhấp nháy (flicker)
            FunctionUtils.SetDoubleBufferedRecursive(this);

            _sidebarAnimationTimer = new Timer { Interval = 8 };
            _sidebarAnimationTimer.Tick += SidebarAnimationTimer_Tick;
        }

        public async Task LoadDataAsync()
        {
            // Buộc tạo handle để đảm bảo các control được khởi tạo
            var h1 = tabPage1.Handle;
            var h2 = tabPage2.Handle;
            var h3 = tabPage3.Handle;
            var h4 = tabPage4.Handle;

            _frmQuotation = new FrmQuotation();
            _frmQuotation.Dock = DockStyle.Fill;
            tabPage1.Controls.Add(_frmQuotation);

            _frmRelation = new FrmRelation();
            _frmRelation.Dock = DockStyle.Fill;
            tabPage2.Controls.Add(_frmRelation);

            _frmConfig = new FrmConfig();
            _frmConfig.Dock = DockStyle.Fill;
            tabPage3.Controls.Add(_frmConfig);

            _frmUserManagement = new FrmUserManagement();
            _frmUserManagement.TopLevel = false;
            _frmUserManagement.AutoScaleMode = AutoScaleMode.Inherit;
            _frmUserManagement.Dock = DockStyle.Fill;
            tabPage4.Controls.Add(_frmUserManagement);

            // Chạy tất cả các tác vụ tải dữ liệu song song
            var loadTasks = new List<Task>
            {
                _frmQuotation.LoadDataAsync(),
                _frmRelation.LoadDataAsync(),
                _frmConfig.LoadDataAsync()
            };

            // Nếu user có quyền quản lý, mới chạy tác vụ load dữ liệu user
            btnTabQuotation.Text = "Vỏ tủ và Thang máng";
            btnTabRelation.Text = "Liên kết sản phẩm";
            btnTabConfig.Text = "Báo giá và Tính toán";
            btnTabUser.Text = "Quản trị nhân viên";

            bool canManageUsers = Helper.UserSession.HasPermission("user:manage");
            if (canManageUsers)
            {
                loadTasks.Add(_frmUserManagement.LoadDataAsync());
            }

            await Task.WhenAll(loadTasks);

            _frmQuotation.Show();
            _frmRelation.Show();
            _frmConfig.Show();
            
            if (canManageUsers)
            {
                _frmUserManagement.Show();
            }

            // Gán event sau khi mọi thứ đã tải xong
            tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string userName = Settings.Default.Name;
            lbUserName.Text = "Xin chào, " + userName;

            tabPage1.Text = "Vỏ tủ & Thang máng";
            tabPage2.Text = "Liên kết sản phẩm";
            tabPage3.Text = "Báo giá & Tính toán";
            tabPage4.Text = "Quản trị nhân viên";
            tabControl1.SelectedTab = tabPage1;

            // Tự động maximize theo màn hình
            this.WindowState = FormWindowState.Maximized;
            this.AutoScroll = false;

            lbUserName.Text = "Xin chào, " + userName;
            tabPage1.Text = "Vỏ tủ & Thang máng";
            tabPage2.Text = "Liên kết sản phẩm";
            tabPage3.Text = "Báo giá & Tính toán";
            tabPage4.Text = "Quản trị nhân viên";

            panel1.Height = 72;
            panel1.BackColor = Color.White;
            panel1.Padding = new Padding(24, 0, 8, 0);
            pictureBox1.Width = 212;
            pictureBox1.Padding = new Padding(0, 8, 28, 8);

            lbUserName.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular);
            lbUserName.ForeColor = Color.FromArgb(51, 65, 85);

            UIService.StyleHeaderButton(btnRefresh, Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255));
            UIService.StyleHeaderButton(button2, Color.FromArgb(220, 38, 38), Color.FromArgb(254, 242, 242));
            btnRefresh.Text = "Tải lại dữ liệu";
            button2.Text = "Đăng xuất";

            panelNavigation.Height = 50;
            panelNavigation.BackColor = AppConstant.Ui.SidebarBackColor;
            panelNavigation.Padding = new Padding(24, 0, 24, 3);

            // Đặt padding dưới cho panelNavigation để chừa khoảng trống 3px vẽ vạch chỉ định
            panelNavigation.Padding = new Padding(24, 0, 24, 3);

            // Cấu hình các nút tab: tăng chiều rộng lên 290px tránh tràn chữ và set hiệu ứng hover/click
            var buttons = new List<Button> { btnTabQuotation, btnTabRelation, btnTabConfig, btnTabUser };
            btnTabQuotation.Text = "Vỏ tủ và Thang máng";
            btnTabRelation.Text = "Liên kết sản phẩm";
            btnTabConfig.Text = "Báo giá và Tính toán";
            btnTabUser.Text = "Quản trị nhân viên";
            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                btn.Width = btn == btnTabQuotation ? 260 : 240;
                btn.Height = 47;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = panelNavigation.BackColor;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 243, 244); // Màu hover xám nhạt
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(215, 230, 252); // Màu click xanh nhạt
            }

            // Kiểm tra quyền hiển thị Tab Quản trị Nhân viên
            bool canManageUsers = Helper.UserSession.HasPermission("user:manage");
            if (!canManageUsers)
            {
                btnTabUser.Visible = false;
                tabControl1.TabPages.Remove(tabPage4);
            }

            ConfigureHeaderShell(canManageUsers);
            UpdateTabButtonStyles();
        }

        private async void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isHandlingTabChange) return;

            // Cập nhật giao diện tab khi chuyển đổi
            UpdateTabButtonStyles();

            if (tabControl1.SelectedIndex != CONFIG_TAB_INDEX) 
            {
                _previousTabIndex = tabControl1.SelectedIndex;
                return;
            }



            _isHandlingTabChange = true;
            try
            {
                // Chỉ hiển thị modal lần ĐẦU TIÊN (chưa chọn sheet)
                var service = _frmConfig.GetSheetsService();
                var spreadsheetId = _frmConfig.GetSpreadsheetId();

                string selectedSheet = null;
                bool cancelled = false;

                using (var selector = new FrmSheetSelector(spreadsheetId, service))
                {
                    var result = selector.ShowDialog(this);
                    if (result == DialogResult.OK && !string.IsNullOrEmpty(selector.SelectedSheetName))
                        selectedSheet = selector.SelectedSheetName;
                    else
                        cancelled = true;
                }

                // Thả cờ NGAY SAU KHI dialog đóng để click nhanh không bị chặn
                _isHandlingTabChange = false;




                if (cancelled)
                    tabControl1.SelectedIndex = _previousTabIndex;
                else
                    await _frmConfig.SetConfigSheet(selectedSheet);
            }
            finally
            {
                // Đảm bảo flag luôn được thả dù có exception
                _isHandlingTabChange = false;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            this.Hide();
            FrmLogin frmLogin = new FrmLogin();
            frmLogin.ShowDialog();
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                btnRefresh.Enabled = false;
                if (btnRefresh is IconButton loadingButton)
                {
                    loadingButton.IconChar = IconChar.Spinner;
                }
                
                // Reload lại toàn bộ dữ liệu lấy từ Google Sheets.
                var loadTasks = new List<Task>
                {
                    _frmQuotation.LoadDataAsync(),
                    _frmRelation.LoadDataAsync(),
                    _frmConfig.LoadDataAsync()
                };
                await Task.WhenAll(loadTasks);
                
                MessageBox.Show("Đã tải lại toàn bộ dữ liệu thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi khi tải lại dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Text = string.Empty;
                if (btnRefresh is IconButton refreshButton)
                {
                    refreshButton.IconChar = IconChar.ArrowsRotate;
                }
                btnRefresh.Enabled = true;
                this.Cursor = Cursors.Default;
            }
        }

        private void btnTab_Click(object sender, EventArgs e)
        {
            Button clickedBtn = sender as Button;
            if (clickedBtn == null) return;

            int targetIndex = 0;
            if (clickedBtn == btnTabQuotation) targetIndex = 0;
            else if (clickedBtn == btnTabRelation) targetIndex = 1;
            else if (clickedBtn == btnTabConfig) targetIndex = 2;
            else if (clickedBtn == btnTabUser) targetIndex = 3;

            // Đổi tab trong TabControl
            tabControl1.SelectedIndex = targetIndex;
            
            // Cập nhật giao diện menu
            UpdateTabButtonStyles();
        }

        private void ConfigureHeaderShell(bool canManageUsers)
        {
            if (_headerConfigured) return;
            _headerConfigured = true;

            _headerToolTip = new ToolTip
            {
                InitialDelay = 400,
                ReshowDelay = 100,
                AutoPopDelay = 5000
            };

            panel1.SuspendLayout();
            panelNavigation.SuspendLayout();

            panel1.Height = 64;
            panel1.BackColor = Color.White;
            panel1.Padding = new Padding(24, 0, 24, 0);

            pictureBox1.Dock = DockStyle.Left;
            pictureBox1.Width = 220;
            pictureBox1.Padding = new Padding(0, 10, 36, 10);
            pictureBox1.Margin = Padding.Empty;

            lbUserName.Dock = DockStyle.None;
            lbUserName.Width = 170;
            lbUserName.Height = 42;
            lbUserName.Margin = new Padding(0, 0, 14, 0);
            lbUserName.TextAlign = ContentAlignment.MiddleRight;
            lbUserName.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular);
            lbUserName.ForeColor = Color.FromArgb(51, 65, 85);
            lbUserName.Padding = Padding.Empty;

            btnRefresh = CreateHeaderActionButton("Tải lại dữ liệu", IconChar.ArrowsRotate, Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255));
            MakeRefreshButtonIconOnly();
            btnRefresh.Click += btnRefresh_Click;
            button2 = CreateHeaderActionButton("Đăng xuất", IconChar.RightFromBracket, Color.FromArgb(220, 38, 38), Color.FromArgb(254, 242, 242));
            button2.Click += button2_Click_1;
            _sidebarToggleButton = CreateSidebarToggleButton();

            _headerActionsPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 440,
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            _headerActionsPanel.Controls.Add(lbUserName);
            _headerActionsPanel.Controls.Add(button2);
            _headerActionsPanel.Controls.Add(btnRefresh);
            _headerActionsPanel.Resize += (s, e) => LayoutHeaderActions();

            panel1.Controls.Clear();
            panel1.Controls.Add(_headerActionsPanel);
            panel1.Controls.Add(_sidebarToggleButton);
            panel1.Controls.Add(pictureBox1);
            LayoutHeaderActions();

            panelNavigation.Dock = DockStyle.Left;
            panelNavigation.Width = SidebarWidth;
            panelNavigation.BackColor = AppConstant.Ui.SidebarBackColor;
            panelNavigation.Padding = Padding.Empty;
            panelNavigation.Controls.Clear();

            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Margin = Padding.Empty;
            tabControl1.BringToFront();

            panelNavigation.Height = 56;
            panelNavigation.BackColor = AppConstant.Ui.SidebarBackColor;
            panelNavigation.Padding = Padding.Empty;
            panelNavigation.Controls.Clear();

            btnTabQuotation = CreateNavigationButton("Vỏ tủ / Thang máng", IconChar.Industry, "Vỏ tủ và Thang máng");
            btnTabRelation = CreateNavigationButton("Liên kết sản phẩm", IconChar.Link);
            btnTabConfig = CreateNavigationButton("Báo giá / Tính toán", IconChar.Calculator, "Báo giá và Tính toán");
            btnTabUser = CreateNavigationButton("Quản trị nhân viên", IconChar.UsersGear);
            btnTabUser.Visible = canManageUsers;

            _navigationFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = panelNavigation.BackColor,
                Padding = new Padding(8, 14, 8, 0),
                Margin = Padding.Empty
            };
            _navigationFlowPanel.Controls.Add(btnTabQuotation);
            _navigationFlowPanel.Controls.Add(btnTabRelation);
            _navigationFlowPanel.Controls.Add(btnTabConfig);
            if (canManageUsers)
            {
                _navigationFlowPanel.Controls.Add(btnTabUser);
            }

            panelNavigation.Controls.Add(_navigationFlowPanel);
            panelNavigation.Resize += (s, e) =>
            {
                if (!_isSidebarAnimating)
                {
                    UpdateSidebarButtonWidths();
                }
            };
            UpdateSidebarButtonWidths();

            panelNavigation.ResumeLayout();
            panel1.ResumeLayout();
        }

        private Button CreateNavigationButton(string text, IconChar icon, string tooltipText = null)
        {
            var button = new Button
            {
                Width = SidebarWidth - 24,
                AccessibleName = text
            };
            UIService.StyleNavigationButton(button, text, icon, SidebarWidth - 24, SidebarIconSize);
            button.Click += btnTab_Click;
            _headerToolTip?.SetToolTip(button, tooltipText ?? text);
            return button;
        }

        private void UpdateSidebarButtonWidths()
        {
            if (_navigationFlowPanel == null) return;

            _navigationFlowPanel.Padding = _sidebarCollapsed
                ? new Padding(10, 14, 10, 0)
                : new Padding(8, 14, 8, 0);

            int minWidth = _sidebarCollapsed ? 48 : 180;
            int width = Math.Max(minWidth, panelNavigation.ClientSize.Width - _navigationFlowPanel.Padding.Left - _navigationFlowPanel.Padding.Right);
            foreach (Control control in _navigationFlowPanel.Controls)
            {
                if (control is Button button)
                {
                    ApplySidebarButtonLayout(button, width);
                }
            }
        }

        private IconButton CreateSidebarToggleButton()
        {
            var button = new IconButton
            {
                Dock = DockStyle.Left,
                Width = 42,
                Height = 42,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(37, 99, 235),
                IconChar = IconChar.Bars,
                IconColor = Color.FromArgb(37, 99, 235),
                IconSize = 20,
                Text = string.Empty,
                TextImageRelation = TextImageRelation.Overlay,
                ImageAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 246, 255);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(219, 234, 254);
            button.Click += (s, e) => ToggleSidebarMenuInstant();
            _headerToolTip?.SetToolTip(button, "Thu gọn / mở rộng menu");
            return button;
        }

        private void ToggleSidebarMenu()
        {
            _sidebarCollapsed = !_sidebarCollapsed;
            panelNavigation.Width = _sidebarCollapsed ? SidebarCollapsedWidth : SidebarWidth;

            if (_sidebarToggleButton != null)
            {
                _sidebarToggleButton.IconChar = _sidebarCollapsed ? IconChar.AngleRight : IconChar.Bars;
                _headerToolTip?.SetToolTip(_sidebarToggleButton, _sidebarCollapsed ? "Mở rộng menu" : "Thu gọn menu");
            }

            UpdateSidebarButtonWidths();
            UpdateTabButtonStyles();
        }

        private void ToggleSidebarMenuInstant()
        {
            if (_sidebarAnimationTimer != null && _sidebarAnimationTimer.Enabled)
            {
                _sidebarAnimationTimer.Stop();
                _isSidebarAnimating = false;
            }

            _sidebarCollapsed = !_sidebarCollapsed;

            // Tạm ngưng vẽ giao diện chính để ngăn giật hình trong quá trình co giãn layout
            Utils.FunctionUtils.SuspendDrawing(this);

            SuspendLayout();
            panel1.SuspendLayout();
            panelNavigation.SuspendLayout();
            tabControl1.SuspendLayout();

            try
            {
                panelNavigation.Width = _sidebarCollapsed ? SidebarCollapsedWidth : SidebarWidth;
                UpdateSidebarToggleState(_sidebarCollapsed);
                UpdateSidebarButtonWidths();
                UpdateTabButtonStyles();
            }
            finally
            {
                tabControl1.ResumeLayout(true);
                panelNavigation.ResumeLayout(true);
                panel1.ResumeLayout(true);
                ResumeLayout(true);

                // Khôi phục vẽ giao diện và làm mới màn hình một lần duy nhất
                Utils.FunctionUtils.ResumeDrawing(this);
            }
        }

        private void ToggleSidebarMenuSmooth()
        {
            if (_sidebarAnimationTimer != null && _sidebarAnimationTimer.Enabled) return;

            _sidebarTargetCollapsed = !_sidebarCollapsed;
            _sidebarAnimationTargetWidth = _sidebarTargetCollapsed ? SidebarCollapsedWidth : SidebarWidth;
            _isSidebarAnimating = true;

            if (_sidebarTargetCollapsed)
            {
                _sidebarCollapsed = true;
                UpdateSidebarButtonWidths();
            }

            UpdateSidebarToggleState(_sidebarTargetCollapsed);
            if (_sidebarToggleButton != null) _sidebarToggleButton.Enabled = false;
            _sidebarAnimationTimer.Start();
        }

        private void SidebarAnimationTimer_Tick(object sender, EventArgs e)
        {
            int currentWidth = panelNavigation.Width;
            int remaining = _sidebarAnimationTargetWidth - currentWidth;

            if (Math.Abs(remaining) <= SidebarAnimationStep)
            {
                _sidebarAnimationTimer.Stop();
                panelNavigation.Width = _sidebarAnimationTargetWidth;
                _sidebarCollapsed = _sidebarTargetCollapsed;
                _isSidebarAnimating = false;

                UpdateSidebarToggleState(_sidebarCollapsed);
                UpdateTabButtonStyles();

                if (_sidebarToggleButton != null) _sidebarToggleButton.Enabled = true;
                return;
            }

            panelNavigation.Width = currentWidth + (remaining > 0 ? SidebarAnimationStep : -SidebarAnimationStep);
        }

        private void UpdateSidebarToggleState(bool collapsed)
        {
            if (_sidebarToggleButton == null) return;

            _sidebarToggleButton.IconChar = collapsed ? IconChar.AngleRight : IconChar.Bars;
            _headerToolTip?.SetToolTip(_sidebarToggleButton, collapsed ? "Mở rộng menu" : "Thu gọn menu");
        }

        private void ApplySidebarButtonLayout(Button button, int width)
        {
            button.Width = width;

            if (_sidebarCollapsed)
            {
                button.Text = string.Empty;
                button.Padding = Padding.Empty;
                button.TextAlign = ContentAlignment.MiddleCenter;
                button.TextImageRelation = TextImageRelation.Overlay;
                button.ImageAlign = ContentAlignment.MiddleCenter;
                return;
            }

            button.Text = button.AccessibleName ?? button.Text;
            button.Padding = new Padding(20, 0, 12, 0);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            button.ImageAlign = ContentAlignment.MiddleLeft;
        }

        private Button CreateHeaderActionButton(string text, IconChar icon, Color textColor, Color hoverColor)
        {
            var button = new IconButton
            {
                Text = text,
                Width = text.Length > 8 ? 180 : 150,
                Height = 42,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(10, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = textColor,
                Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                TextAlign = ContentAlignment.MiddleCenter,
                ImageAlign = ContentAlignment.MiddleCenter,
                RightToLeft = RightToLeft.No,
                AutoEllipsis = true,
                IconChar = icon,
                IconSize = HeaderIconSize,
                IconColor = textColor,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = hoverColor;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            _headerToolTip?.SetToolTip(button, text);
            return button;
        }

        private void MakeRefreshButtonIconOnly()
        {
            if (btnRefresh is IconButton refreshIconButton)
            {
                refreshIconButton.Text = string.Empty;
                refreshIconButton.Width = 42;
                refreshIconButton.Height = 42;
                refreshIconButton.Margin = new Padding(0, 0, 8, 0);
                refreshIconButton.Padding = Padding.Empty;
                refreshIconButton.BackColor = Color.White;
                refreshIconButton.TextImageRelation = TextImageRelation.Overlay;
                refreshIconButton.ImageAlign = ContentAlignment.MiddleCenter;
                refreshIconButton.IconSize = 20;
                refreshIconButton.IconChar = IconChar.ArrowsRotate;
                refreshIconButton.IconColor = Color.FromArgb(37, 99, 235);
                refreshIconButton.FlatAppearance.BorderSize = 1;
                refreshIconButton.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
                refreshIconButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 246, 255);
                refreshIconButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(219, 234, 254);
                _headerToolTip?.SetToolTip(refreshIconButton, "Tải lại dữ liệu Google Sheets");
            }
        }

        private void LayoutHeaderActions()
        {
            if (_headerActionsPanel == null || lbUserName == null || button2 == null || btnRefresh == null)
                return;

            int top = Math.Max(0, (_headerActionsPanel.ClientSize.Height - 42) / 2);
            int right = 16;
            int gap = 8;

            const int logoutWidth = 150;

            btnRefresh.SetBounds(_headerActionsPanel.ClientSize.Width - right - 42, top, 42, 42);
            button2.SetBounds(btnRefresh.Left - gap - logoutWidth, top, logoutWidth, 42);
            lbUserName.SetBounds(button2.Left - 14 - 190, top, 190, 42);
        }

        private void StyleHeaderButton(Button button, Color textColor, Color hoverColor)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = hoverColor;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            button.BackColor = Color.White;
            button.ForeColor = textColor;
            button.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            button.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1))
            {
                e.Graphics.DrawLine(pen, 0, panel1.Height - 1, panel1.Width, panel1.Height - 1);
            }
        }

        private void UpdateTabButtonStyles()
        {
            var buttons = new List<Button> { btnTabQuotation, btnTabRelation, btnTabConfig, btnTabUser };
            for (int i = 0; i < buttons.Count; i++)
            {
                var btn = buttons[i];
                if (btn == null) continue;

                if (tabControl1.SelectedIndex == i)
                {
                    UIService.SetNavigationButtonState(btn, true, SidebarIconSize, SidebarActiveIconSize);
                }
                else
                {
                    UIService.SetNavigationButtonState(btn, false, SidebarIconSize, SidebarActiveIconSize);
                }
            }
            UpdateSidebarButtonWidths();
            panelNavigation.Invalidate(); // Vẽ lại panel để cập nhật vạch chỉ định chân tab nếu cần
        }

        private void panelNavigation_Paint(object sender, PaintEventArgs e)
        {
            // Vẽ đường phân cách giữa sidebar và vùng nội dung.
            using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1))
            {
                e.Graphics.DrawLine(pen, panelNavigation.Width - 1, 0, panelNavigation.Width - 1, panelNavigation.Height);
            }

            // Tìm nút đang active
            Button activeBtn = null;
            if (tabControl1.SelectedIndex == 0) activeBtn = btnTabQuotation;
            else if (tabControl1.SelectedIndex == 1) activeBtn = btnTabRelation;
            else if (tabControl1.SelectedIndex == 2) activeBtn = btnTabConfig;
            else if (tabControl1.SelectedIndex == 3 && btnTabUser.Visible) activeBtn = btnTabUser;

            // Vẽ vạch active theo menu dọc.
            if (activeBtn != null)
            {
                using (var brush = new SolidBrush(AppConstant.Ui.PrimaryDarkColor))
                {
                    var activeLocation = panelNavigation.PointToClient(activeBtn.PointToScreen(Point.Empty));
                    e.Graphics.FillRectangle(brush, activeLocation.X, activeLocation.Y + 8, 4, activeBtn.Height - 16);
                }
            }
        }
    }
}
