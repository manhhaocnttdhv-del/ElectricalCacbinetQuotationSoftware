using ECQ_Soft.Model;
using System;
using System.Windows.Forms;

namespace ECQ_Soft
{
    public partial class FrmExportInfo : Form
    {
        public ExportInfo ExportData { get; private set; }
        private System.Collections.Generic.List<ExportInfo> _oldCustomers;

        public FrmExportInfo()
        {
            InitializeComponent();
        }

        public FrmExportInfo(System.Collections.Generic.List<ExportInfo> oldCustomers) : this()
        {
            _oldCustomers = oldCustomers;
        }

        private void FrmExportInfo_Load(object sender, EventArgs e)
        {
            cboFormat.SelectedIndex = 0; // Mặc định chọn PDF

            if (_oldCustomers != null && _oldCustomers.Count > 0)
            {
                cboOldCustomer.Items.Add("-- Nhập khách hàng mới --");
                foreach (var cus in _oldCustomers)
                {
                    cboOldCustomer.Items.Add(cus.KinhGui);
                }
                cboOldCustomer.SelectedIndex = 0;
            }
            else
            {
                cboOldCustomer.Items.Add("-- Không có dữ liệu --");
                cboOldCustomer.SelectedIndex = 0;
                cboOldCustomer.Enabled = false;
            }
        }

        private void cboOldCustomer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboOldCustomer.SelectedIndex > 0 && _oldCustomers != null)
            {
                int cusIndex = cboOldCustomer.SelectedIndex - 1; // bù trừ cho item "-- Nhập mới --"
                if (cusIndex >= 0 && cusIndex < _oldCustomers.Count)
                {
                    var cus = _oldCustomers[cusIndex];
                    txtKinhGui.Text = cus.KinhGui;
                    txtDiaChi.Text = cus.DiaChi;
                    txtNguoiNhan.Text = cus.NguoiNhan;
                    txtMaSoThue.Text = cus.MaSoThue;
                    // Nội dung báo giá không tự động điền (hoặc nếu cần thì điền)
                    // txtNoiDung.Text = cus.NoiDung;
                }
            }
            else
            {
                // Nhập mới: Xóa trắng
                txtKinhGui.Text = "";
                txtDiaChi.Text = "";
                txtNguoiNhan.Text = "";
                txtMaSoThue.Text = "";
                txtNoiDung.Text = "";
            }
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            string kinhGui = txtKinhGui.Text.Trim();
            string diaChi = txtDiaChi.Text.Trim();
            string nguoiNhan = txtNguoiNhan.Text.Trim();
            string maSoThue = txtMaSoThue.Text.Trim();
            string noiDung = txtNoiDung.Text.Trim();
            string format = cboFormat.SelectedItem.ToString();

            if (string.IsNullOrEmpty(kinhGui) || string.IsNullOrEmpty(diaChi) || string.IsNullOrEmpty(nguoiNhan))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ 3 thông tin bắt buộc: Kính gửi, Địa chỉ và Người nhận!", 
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ExportData = new ExportInfo
            {
                KinhGui = kinhGui,
                DiaChi = diaChi,
                NguoiNhan = nguoiNhan,
                MaSoThue = maSoThue,
                NoiDung = noiDung,
                Format = format
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
