using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ECQ_Soft
{
    public partial class FrmSplashScreen : Form
    {
        public FrmSplashScreen()
        {
            InitializeComponent();
            this.Load += FrmSplashScreen_Load;
        }

        private async void FrmSplashScreen_Load(object sender, EventArgs e)
        {
            // Ép hệ thống vẽ các thành phần giao diện của FrmSplashScreen trước.
            Application.DoEvents();

            // Khởi tạo FrmMain trên cùng luồng UI để tránh lỗi Thread.
            FrmMain frmMain = new FrmMain();

            // Chờ FrmMain nạp toàn bộ cấu hình Google Sheet ở dưới nền
            await frmMain.LoadDataAsync();

            this.Hide(); // Ẩn Splash đi
            frmMain.ShowDialog(); // Mở Form chính lên (chạy theo dạng hộp thoại để giữ process)

            this.Close(); // Đóng hoàn toàn chương trình sau khi Form Main cũng đóng
        }
    }
}
