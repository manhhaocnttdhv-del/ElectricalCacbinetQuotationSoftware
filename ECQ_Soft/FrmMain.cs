
using ECQ_Soft.Helpers;
using ECQ_Soft.Model;
using ECQ_Soft.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace ECQ_Soft
{
    public partial class FrmMain : Form
    {


        #region Khai báo biến

        // ── Service ────────────────────────────────────────────────────────
        private GoogleSheetsService _googleSheetsService;
        private const string SpreadsheetId = "1swdiFIwhoZaXf4c5R_Lzp2pgZng5RcdOKii2DYkN_Uc";
        private const string SheetName     = "Sheet1";

        // ── Trạng thái lựa chọn ───────────────────────────────────────────
        private int _selectedMaterialId;
        private int _selectedCabinetTypeId;
        private int _selectedMarketPriceId;
        private int _selectedRecordRowIndex;
        private int _selectedCoatingTypeId;

        // ── Dữ liệu master ────────────────────────────────────────────────
        private List<Material>    materials    = new List<Material>();
        private List<CabinetType> cabinetTypes = new List<CabinetType>();
        private List<Record>      records      = new List<Record>();
        private List<MarketPrice> marketPrices = new List<MarketPrice>();

        // ── Kết quả tính toán ─────────────────────────────────────────────
        private string formula;
        private float  weight;
        private string unit;

        private int  GiaSonMa;
        private int  Chiphikhac;
        private uint DonGiaHME;
        private uint DonGiaThiTruong;
        private uint DonGiaVPA;

        #endregion

        #region Các hàm chức năng

        // ── Tải dữ liệu từ Google Sheets ─────────────────────────────────
        private void LoadAllDataFromSheets()
        {
            _googleSheetsService = new GoogleSheetsService(SpreadsheetId, SheetName);
            materials    = _googleSheetsService.LoadMaterials();
            cabinetTypes = _googleSheetsService.LoadCabinetTypes();
            marketPrices = _googleSheetsService.LoadMarketPrices();
        }

        // ── Bind combobox ─────────────────────────────────────────────────
        private void LoadMaterialtoCombobox()
        {
            materials.Insert(0, new Material
            {
                Id   = 0,
                Name = "-- Chọn vật liệu --"
            });

            cboMaterial.DataSource    = materials;
            cboMaterial.DisplayMember = "Name";
            cboMaterial.ValueMember   = "Id";
            cboMaterial.SelectedIndex = 0;
        }

        private void LoadCabinetTypetoCombobox()
        {
            cabinetTypes.Insert(0, new CabinetType
            {
                Id   = 0,
                Name = "-- Chọn loại tủ điện hoặc thang, máng cáp --"
            });

            cboCabinetType.DataSource    = cabinetTypes;
            cboCabinetType.DisplayMember = "Name";
            cboCabinetType.ValueMember   = "Id";
            cboCabinetType.SelectedIndex = 0;
        }

        // ── Hiển thị bảng ghi ────────────────────────────────────────────
        private void LoadRecord()
        {
            var list = records.OrderBy(t => t.Stt).ToList();

            // Tính tổng
            ulong totalNoVat    = list.Aggregate(0UL, (acc, it) => acc + it.MarketTotalPrice);
            ulong vat           = (ulong)(totalNoVat * 8 / 100.0);
            ulong total         = totalNoVat + vat;

            ulong totalNoVatHME = list.Aggregate(0UL, (acc, it) => acc + it.HMETotalPrice);
            ulong vatHME        = (ulong)(totalNoVatHME * 8 / 100.0);
            ulong totalHME      = totalNoVatHME + vatHME;

            ulong totalNoVatVPA = list.Aggregate(0UL, (acc, it) => acc + it.VPATotalPrice);
            ulong vatVPA        = (ulong)(totalNoVatVPA * 8 / 100.0);
            ulong totalVPA      = totalNoVatVPA + vatVPA;

            // Dòng tổng cộng
            list.Add(new Record { Name = "TỔNG CỘNG (Giá chưa bao gồm VAT)",  MarketTotalPrice = totalNoVat,    HMETotalPrice = totalNoVatHME, VPATotalPrice = totalNoVatVPA });
            list.Add(new Record { Name = "THUẾ VAT 8%",                         MarketTotalPrice = vat,           HMETotalPrice = vatHME,        VPATotalPrice = vatVPA        });
            list.Add(new Record { Name = "TỔNG CỘNG (Đã bao gồm VAT)",          MarketTotalPrice = total,         HMETotalPrice = totalHME,      VPATotalPrice = totalVPA      });

            dgvRecord.DataSource         = list;
            dgvRecord.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Ẩn cột không cần hiện
            dgvRecord.Columns["WeightperUnit"].Visible = false;

            // FillWeight
            dgvRecord.Columns["Stt"].FillWeight             = 20;
            dgvRecord.Columns["Name"].FillWeight             = 120;
            dgvRecord.Columns["Unit"].FillWeight             = 27;
            dgvRecord.Columns["Quantity"].FillWeight         = 32;
            dgvRecord.Columns["HMEUnitPrice"].FillWeight     = 35;
            dgvRecord.Columns["HMETotalPrice"].FillWeight    = 35;
            dgvRecord.Columns["MarketUnitPrice"].FillWeight  = 35;
            dgvRecord.Columns["MarketTotalPrice"].FillWeight = 35;
            dgvRecord.Columns["VPAUnitPrice"].FillWeight     = 35;
            dgvRecord.Columns["VPATotalPrice"].FillWeight    = 35;
            dgvRecord.Columns["Weight"].FillWeight           = 37;
            dgvRecord.Columns["Note"].FillWeight             = 32;

            // Header text
            dgvRecord.Columns["Stt"].HeaderText             = "STT";
            dgvRecord.Columns["Name"].HeaderText             = "Tên vật tư, hàng hóa";
            dgvRecord.Columns["Unit"].HeaderText             = "Đơn vị";
            dgvRecord.Columns["Quantity"].HeaderText         = "Số lượng";
            dgvRecord.Columns["HMEUnitPrice"].HeaderText     = "Đơn giá\nHME";
            dgvRecord.Columns["HMETotalPrice"].HeaderText    = "Thành tiền\nHME";
            dgvRecord.Columns["MarketUnitPrice"].HeaderText  = "Đơn giá\n(VNĐ)";
            dgvRecord.Columns["MarketTotalPrice"].HeaderText = "Thành tiền\n(VNĐ)";
            dgvRecord.Columns["VPAUnitPrice"].HeaderText     = "Đơn giá\nVPA";
            dgvRecord.Columns["VPATotalPrice"].HeaderText    = "Thành tiền\nVPA";
            dgvRecord.Columns["Weight"].HeaderText           = "Khối lượng\n(Kg)";
            dgvRecord.Columns["Note"].HeaderText             = "Ghi chú";

            // Phân quyền hiển thị
            if (!Settings.Default.isAdmin)
            {
                if (Settings.Default.Role.ToLower() == "vnecco")
                {
                    dgvRecord.Columns["VPAUnitPrice"].HeaderText  = "Giá nhập\nVPA";
                    dgvRecord.Columns["HMEUnitPrice"].Visible     = false;
                    dgvRecord.Columns["HMETotalPrice"].Visible    = false;
                }
            }

            // Wrap text
            dgvRecord.Columns["Name"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvRecord.Columns["Note"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvRecord.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // Font
            dgvRecord.DefaultCellStyle.Font             = new Font("Times New Roman", 12, FontStyle.Regular);
            dgvRecord.ColumnHeadersDefaultCellStyle.Font = new Font("Times New Roman", 12, FontStyle.Bold);

            // Căn phải giá tiền
            dgvRecord.Columns["HMEUnitPrice"].DefaultCellStyle.Alignment     = DataGridViewContentAlignment.MiddleRight;
            dgvRecord.Columns["HMETotalPrice"].DefaultCellStyle.Alignment    = DataGridViewContentAlignment.MiddleRight;
            dgvRecord.Columns["MarketUnitPrice"].DefaultCellStyle.Alignment  = DataGridViewContentAlignment.MiddleRight;
            dgvRecord.Columns["MarketTotalPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvRecord.Columns["VPAUnitPrice"].DefaultCellStyle.Alignment     = DataGridViewContentAlignment.MiddleRight;
            dgvRecord.Columns["VPATotalPrice"].DefaultCellStyle.Alignment    = DataGridViewContentAlignment.MiddleRight;

            // Căn giữa header + cột số đơn giản
            dgvRecord.ColumnHeadersDefaultCellStyle.Alignment             = DataGridViewContentAlignment.MiddleCenter;
            dgvRecord.Columns["Stt"].DefaultCellStyle.Alignment           = DataGridViewContentAlignment.MiddleCenter;
            dgvRecord.Columns["Unit"].DefaultCellStyle.Alignment           = DataGridViewContentAlignment.MiddleCenter;
            dgvRecord.Columns["Quantity"].DefaultCellStyle.Alignment       = DataGridViewContentAlignment.MiddleCenter;
            dgvRecord.Columns["Weight"].DefaultCellStyle.Alignment         = DataGridViewContentAlignment.MiddleCenter;

            // Format số
            dgvRecord.Columns["HMEUnitPrice"].DefaultCellStyle.Format     = "N0";
            dgvRecord.Columns["HMETotalPrice"].DefaultCellStyle.Format     = "N0";
            dgvRecord.Columns["MarketUnitPrice"].DefaultCellStyle.Format   = "N0";
            dgvRecord.Columns["MarketTotalPrice"].DefaultCellStyle.Format  = "N0";
            dgvRecord.Columns["VPAUnitPrice"].DefaultCellStyle.Format      = "N0";
            dgvRecord.Columns["VPATotalPrice"].DefaultCellStyle.Format     = "N0";
            dgvRecord.Columns["Quantity"].DefaultCellStyle.Format          = "N0";
            dgvRecord.Columns["Weight"].DefaultCellStyle.Format            = "N2";

            // Màu header
            dgvRecord.EnableHeadersVisualStyles = false;
            dgvRecord.Columns["Stt"].HeaderCell.Style.BackColor             = Color.Yellow;
            dgvRecord.Columns["Name"].HeaderCell.Style.BackColor            = Color.Yellow;
            dgvRecord.Columns["Unit"].HeaderCell.Style.BackColor            = Color.Yellow;
            dgvRecord.Columns["Quantity"].HeaderCell.Style.BackColor        = Color.Yellow;
            dgvRecord.Columns["HMEUnitPrice"].HeaderCell.Style.BackColor    = Color.LightBlue;
            dgvRecord.Columns["HMETotalPrice"].HeaderCell.Style.BackColor   = Color.LightBlue;
            dgvRecord.Columns["MarketUnitPrice"].HeaderCell.Style.BackColor = Color.Yellow;
            dgvRecord.Columns["MarketTotalPrice"].HeaderCell.Style.BackColor = Color.Yellow;
            dgvRecord.Columns["Note"].HeaderCell.Style.BackColor            = Color.Yellow;
            dgvRecord.Columns["VPAUnitPrice"].HeaderCell.Style.BackColor    = Color.Lime;
            dgvRecord.Columns["VPATotalPrice"].HeaderCell.Style.BackColor   = Color.Lime;
            dgvRecord.Columns["Weight"].HeaderCell.Style.BackColor          = Color.Lime;

            // Màu nền dòng tổng
            for (int i = dgvRecord.Rows.Count - 3; i < dgvRecord.Rows.Count; i++)
            {
                dgvRecord.Rows[i].DefaultCellStyle.BackColor = Color.Yellow;
                dgvRecord.Rows[i].DefaultCellStyle.Font      = new Font("Times New Roman", 12, FontStyle.Bold);
            }
        }

        // ── Tính phí sơn/mạ và giá thị trường ───────────────────────────
        private void GetAdditionFee(int type)
        {
            if (_selectedMaterialId != 0)
            {
                var m = materials.FirstOrDefault(t => t.Id == _selectedMaterialId);
                if (m == null)
                {
                    MessageBox.Show("Không tìm thấy vật liệu", "Lỗi dữ liệu. Bảng vật liệu",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (type == 1)
                {
                    txtSonMa.Text  = m.PCFee.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                    int von = m.PCFee + m.OrtherFee + m.Price;
                    txtGiaVon.Text = von.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                }
                if (type == 2)
                {
                    txtSonMa.Text  = m.HDGFee.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                    int von = m.HDGFee + m.OrtherFee + m.Price;
                    txtGiaVon.Text = von.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                }
                if (type == 3)
                {
                    txtSonMa.Text  = "0";
                    int von = m.OrtherFee + m.Price;
                    txtGiaVon.Text = von.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                }

                if (_selectedCabinetTypeId != 0)
                {
                    var cabinettype = cabinetTypes.FirstOrDefault(t => t.Id == _selectedCabinetTypeId);
                    if (cabinettype == null)
                    {
                        MessageBox.Show("Không tìm thấy Loại tủ điện, thang, máng cáp",
                            "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var marketPrice = marketPrices
                        .Where(t => t.MaterialId == _selectedMaterialId || t.MaterialId == 0)
                        .Where(t => t.CabinetId  == _selectedCabinetTypeId)
                        .Where(t => t.CoatingTypeId == _selectedCoatingTypeId || t.CoatingTypeId == 4)
                        .FirstOrDefault();

                    if (marketPrice == null)
                    {
                        MessageBox.Show("Chọn lại nguyên vật liệu và loại tủ điện, thang máng cáp",
                            "Lỗi Google Sheet. Bảng giá thị trường", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    _selectedMarketPriceId = marketPrice.Stt;
                    txtGiaBan.Text    = marketPrice.CommonPrice.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                    txtGiaBanVPA.Text = marketPrice.VPAPrice.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                }
            }
            else
            {
                MessageBox.Show("Hãy chọn vật liệu", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                rbSon.Checked  = false;
                rbMa.Checked   = false;
                rbNone.Checked = false;
            }
        }

        #endregion

        #region Các hàm sự kiện

        public FrmMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Visible = false;
            FrmSplashScreen frmSplash = new FrmSplashScreen();
            frmSplash.Show();

            LoadAllDataFromSheets();
            LoadMaterialtoCombobox();
            LoadCabinetTypetoCombobox();

            // Tên người đăng nhập
            string userName    = Settings.Default.Name;
            lbUserName.Text = "Xin chào, " + userName;

            if (Settings.Default.isAdmin == false)
            {
                gbGiaVon.Visible   = false;
                btnUpdate.Visible  = false;

                if (Settings.Default.Role.ToLower() == "vnecco")
                {
                    lbDonGiaHME.Visible      = false;
                    lbHME.Visible            = false;
                    lbGiabanTTText.Text      = "Giá bán thị trường (VNĐ):";
                    lbGiabanVPAText.Text     = "Giá nhập VPA (VNĐ):";
                }
            }

            frmSplash.Close();
            this.Visible = true;
        }

        private void cboMaterial_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboMaterial.SelectedValue is int ID)
            {
                if (ID > 0)
                {
                    _selectedMaterialId = ID;

                    var m = materials.FirstOrDefault(t => t.Id == ID);
                    Chiphikhac         = m.OrtherFee;
                    txtChiphikhac.Text = Chiphikhac.ToString("N0", CultureInfo.GetCultureInfo("en-US"));

                    if (rbMa.Checked)
                    {
                        txtSonMa.Text  = m.HDGFee.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                        int von = m.HDGFee + m.OrtherFee + m.Price;
                        txtGiaVon.Text = von.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                    }
                    if (rbSon.Checked)
                    {
                        txtSonMa.Text  = m.PCFee.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                        int von = m.PCFee + m.OrtherFee + m.Price;
                        txtGiaVon.Text = von.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                    }
                    if (rbNone.Checked)
                    {
                        txtSonMa.Text  = "0";
                        int von = m.OrtherFee + m.Price;
                        txtGiaVon.Text = von.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                    }

                    if (_selectedCabinetTypeId != 0 && _selectedCoatingTypeId != 0)
                    {
                        var marketPrice = marketPrices
                            .Where(t => t.MaterialId    == _selectedMaterialId || t.MaterialId == 0)
                            .Where(t => t.CabinetId     == _selectedCabinetTypeId)
                            .Where(t => t.CoatingTypeId == _selectedCoatingTypeId || t.CoatingTypeId == 4)
                            .FirstOrDefault();

                        if (marketPrice == null)
                        {
                            MessageBox.Show("Chọn lại nguyên vật liệu và loại tủ điện, thang máng cáp",
                                "Lỗi Google Sheet. Bảng giá thị trường", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        _selectedMarketPriceId = marketPrice.Stt;
                        txtGiaBan.Text         = marketPrice.CommonPrice.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                        txtGiaBanVPA.Text       = marketPrice.VPAPrice.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                    }
                }
            }
        }

        private void cboCabinetType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboCabinetType.SelectedValue is int ID)
            {
                if (ID > 0)
                {
                    _selectedCabinetTypeId = ID;

                    if (_selectedMaterialId != 0 && _selectedCoatingTypeId != 0)
                    {
                        var marketPrice = marketPrices
                            .Where(t => t.MaterialId    == _selectedMaterialId || t.MaterialId == 0)
                            .Where(t => t.CabinetId     == _selectedCabinetTypeId)
                            .Where(t => t.CoatingTypeId == _selectedCoatingTypeId || t.CoatingTypeId == 4)
                            .FirstOrDefault();

                        if (marketPrice == null)
                        {
                            MessageBox.Show("Chọn lại nguyên vật liệu và loại tủ điện, thang máng cáp",
                                "Lỗi Google Sheet. Bảng giá thị trường", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        _selectedMarketPriceId = marketPrice.Stt;
                        txtGiaBan.Text         = marketPrice.CommonPrice.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                        txtGiaBanVPA.Text       = marketPrice.VPAPrice.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                    }
                }
            }
        }

        private void btnTinhGia_Click(object sender, EventArgs e)
        {
            var type = cabinetTypes.FirstOrDefault(t => t.Id == _selectedCabinetTypeId);
            if (type == null || type.Id == 0)
            {
                MessageBox.Show("Chọn lại kiểu tủ điện", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            formula = type.Formula;

            var m = materials.FirstOrDefault(t => t.Id == _selectedMaterialId);
            if (m == null || m.Id == 0)
            {
                MessageBox.Show("Chọn lại vật liệu tủ điện", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            float thick = m.Thick;

            // Tính khối lượng vật liệu
            if (type.FormulaType == "1")
            {
                if (int.TryParse(txtHeight.Text, out int H)
                    && int.TryParse(txtWidth.Text, out int W)
                    && int.TryParse(txtDepth.Text, out int D))
                {
                    float s = FormulaHelper.EvaluateFormula(formula, H, W, D, thick); // Diện tích (m2)
                    weight  = (float)(s * m.Q) / 1000000;                             // Khối lượng (kg)
                }
                else
                {
                    MessageBox.Show("Nhập lại kích thước tủ điện", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (type.FormulaType == "2")
            {
                if (int.TryParse(txtHeight.Text, out int H)
                    && int.TryParse(txtWidth.Text, out int W))
                {
                    weight = FormulaHelper.EvaluateFormula(formula, H, W, 0, thick); // Khối lượng (kg)
                }
                else
                {
                    MessageBox.Show("Nhập lại kích thước tủ điện", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                MessageBox.Show("Lỗi giá trị tại cột kiểu công thức, bảng loại tủ điện, thang, máng cáp.",
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Tính giá tiền sơn/mạ
            if (int.TryParse(txtSonMa.Text.Replace(",", ""), out int temp))
            {
                if      (rbSon.Checked)  GiaSonMa = temp;
                else if (rbMa.Checked)   GiaSonMa = temp;
                else if (rbNone.Checked) GiaSonMa = 0;
                else
                {
                    MessageBox.Show("Hãy chọn kiểu bề mặt", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                MessageBox.Show("Hãy nhập số nguyên cho chi phí sơn mạ", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (int.TryParse(txtChiphikhac.Text.Replace(",", ""), out int temp1))
                Chiphikhac = temp1;
            else
            {
                MessageBox.Show("Hãy nhập số nguyên cho chi phí khác", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int GiaVon = m.Price + GiaSonMa + Chiphikhac;

            // Đơn giá HME
            DonGiaHME = (uint)Math.Round((GiaVon * weight) / 1000) * 1000;

            // Đơn giá thị trường
            var marketPrice = marketPrices
                .Where(t => t.MaterialId    == _selectedMaterialId || t.MaterialId == 0)
                .Where(t => t.CabinetId     == _selectedCabinetTypeId)
                .Where(t => t.CoatingTypeId == _selectedCoatingTypeId || t.CoatingTypeId == 4)
                .FirstOrDefault();

            if (marketPrice == null)
            {
                MessageBox.Show("Không tìm được giá thị trường tương ứng.", "Lỗi Google Sheet",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(txtGiaBan.Text.Replace(",", ""), out int giaBanTT))
            {
                MessageBox.Show("Hãy nhập số nguyên cho giá bán thị trường", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!int.TryParse(txtGiaBanVPA.Text.Replace(",", ""), out int giaBanVPA))
            {
                MessageBox.Show("Hãy nhập số nguyên cho giá bán VPA", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DonGiaThiTruong = (uint)Math.Round(weight * giaBanTT  / 1000) * 1000;
            DonGiaVPA       = (uint)Math.Round(weight * giaBanVPA  / 1000) * 1000;

            // Hiển thị kết quả
            if (type.FormulaType == "1")
            {
                lbName.Text = $"{type.Name}, kích thước H{txtHeight.Text} x W{txtWidth.Text} x D{txtDepth.Text}, {m.DisplayName}";
                unit = "Cái";
            }
            if (type.FormulaType == "2")
            {
                lbName.Text = $"{type.Name}, kích thước H{txtHeight.Text} x W{txtWidth.Text}, {m.DisplayName}";
                unit = "Mét";
            }

            lbKhoiLuong.Text       = weight.ToString("N2", CultureInfo.GetCultureInfo("en-US"));
            lbDonGiaHME.Text       = DonGiaHME.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
            lbDonGiaThiTruong.Text = DonGiaThiTruong.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
            lbDonGiaVPA.Text       = DonGiaVPA.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            int index;
            if (_selectedRecordRowIndex == 0 || _selectedRecordRowIndex >= records.Count)
            {
                index = records.Count + 1;
            }
            else
            {
                index = _selectedRecordRowIndex + 1;
                foreach (var item in records.Where(t => t.Stt >= index).ToList())
                    item.Stt++;
            }

            records.Add(new Record
            {
                Stt             = index,
                Name            = lbName.Text,
                Unit            = unit,
                Quantity        = 1,
                HMEUnitPrice    = DonGiaHME,
                HMETotalPrice   = DonGiaHME,
                MarketUnitPrice = DonGiaThiTruong,
                MarketTotalPrice = DonGiaThiTruong,
                VPAUnitPrice    = DonGiaVPA,
                VPATotalPrice   = DonGiaVPA,
                WeightperUnit   = (float)Math.Round(weight, 2, MidpointRounding.AwayFromZero),
                Weight          = (float)Math.Round(weight, 2, MidpointRounding.AwayFromZero),
            });

            LoadRecord();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (_selectedRecordRowIndex != 0 && _selectedRecordRowIndex <= records.Count)
                {
                    records.RemoveAt(_selectedRecordRowIndex - 1);
                    foreach (var item in records.Where(t => t.Stt >= _selectedRecordRowIndex).ToList())
                        item.Stt--;
                    LoadRecord();
                }
                else
                {
                    MessageBox.Show("Chọn bản ghi để xóa", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void dgvRecord_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= records.Count) return;

            var cell   = dgvRecord.Rows[e.RowIndex].Cells[e.ColumnIndex];
            string col = dgvRecord.Columns[e.ColumnIndex].Name;

            if (col == "Quantity")
            {
                if (uint.TryParse(cell.Value?.ToString(), out uint qty))
                {
                    var rec = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (rec != null)
                    {
                        rec.Quantity         = qty;
                        rec.HMETotalPrice    = (ulong)(rec.Quantity * rec.HMEUnitPrice);
                        rec.MarketTotalPrice = (ulong)(rec.Quantity * rec.MarketUnitPrice);
                        rec.VPATotalPrice    = (ulong)(rec.Quantity * rec.VPAUnitPrice);
                        rec.Weight           = rec.Quantity * rec.WeightperUnit;
                        BeginInvoke((MethodInvoker)LoadRecord);
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng nhập số nguyên cho cột Số lượng!", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    var rec = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (rec != null) cell.Value = rec.Quantity;
                }
            }
            else if (col == "HMEUnitPrice")
            {
                if (uint.TryParse(cell.Value?.ToString(), out uint p))
                {
                    var rec = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (rec != null)
                    {
                        rec.HMEUnitPrice  = p;
                        rec.HMETotalPrice = (ulong)(rec.Quantity * rec.HMEUnitPrice);
                        BeginInvoke((MethodInvoker)LoadRecord);
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng nhập số nguyên cho cột đơn giá HME!", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    var rec = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (rec != null) cell.Value = rec.HMEUnitPrice;
                }
            }
            else if (col == "MarketUnitPrice")
            {
                if (uint.TryParse(cell.Value?.ToString(), out uint p))
                {
                    var rec = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (rec != null)
                    {
                        rec.MarketUnitPrice  = p;
                        rec.MarketTotalPrice = (ulong)(rec.Quantity * rec.MarketUnitPrice);
                        BeginInvoke((MethodInvoker)LoadRecord);
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng nhập số nguyên cho cột đơn giá thị trường!", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    var rec = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (rec != null) cell.Value = rec.MarketUnitPrice;
                }
            }
            else if (col == "VPAUnitPrice")
            {
                if (uint.TryParse(cell.Value?.ToString(), out uint p))
                {
                    var rec = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (rec != null)
                    {
                        rec.VPAUnitPrice  = p;
                        rec.VPATotalPrice = (ulong)(rec.Quantity * rec.VPAUnitPrice);
                        BeginInvoke((MethodInvoker)LoadRecord);
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng nhập số nguyên cho cột đơn giá VPA!", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    var rec = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (rec != null) cell.Value = rec.VPAUnitPrice;
                }
            }
        }

        private void dgvRecord_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            MessageBox.Show("Vui lòng nhập số nguyên!", "Cảnh báo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Xuất Excel — dùng ExcelHelper
            ExcelHelper.ExportToExcel(dgvRecord);
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

        private void rbSon_CheckedChanged(object sender, EventArgs e)
        {
            if (rbSon.Checked)
            {
                _selectedCoatingTypeId = 1;
                GetAdditionFee(1);
            }
        }

        private void rbMa_CheckedChanged(object sender, EventArgs e)
        {
            if (rbMa.Checked)
            {
                _selectedCoatingTypeId = 2;
                GetAdditionFee(2);
            }
        }

        private void rbNone_CheckedChanged(object sender, EventArgs e)
        {
            if (rbNone.Checked)
            {
                _selectedCoatingTypeId = 3;
                GetAdditionFee(3);
            }
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            FrmSplashScreen frm = new FrmSplashScreen();
            frm.Show();

            LoadAllDataFromSheets();
            LoadMaterialtoCombobox();
            LoadCabinetTypetoCombobox();

            _selectedCabinetTypeId = 0;
            _selectedMaterialId    = 0;
            _selectedCoatingTypeId = 0;
            txtChiphikhac.Text = "";
            txtGiaBan.Text     = "";
            txtGiaBanVPA.Text  = "";
            txtSonMa.Text      = "";
            txtGiaVon.Text     = "";
            rbMa.Checked       = false;
            rbSon.Checked      = false;
            rbNone.Checked     = false;

            frm.Close();
        }

        private void btnUpdate_Click_1(object sender, EventArgs e)
        {
            if (_selectedCabinetTypeId == 0 || _selectedMaterialId == 0)
            {
                MessageBox.Show("Chọn vật liệu hoặc loại tủ điện, thang, máng.", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var m      = materials.FirstOrDefault(t => t.Id == _selectedMaterialId);
            var market = marketPrices
                .Where(t => t.MaterialId    == _selectedMaterialId || t.MaterialId == 0)
                .Where(t => t.CabinetId     == _selectedCabinetTypeId)
                .Where(t => t.CoatingTypeId == _selectedCoatingTypeId || t.CoatingTypeId == 4)
                .FirstOrDefault();

            if (rbSon.Checked)
            {
                if (int.TryParse(txtSonMa.Text.Replace(",", ""),     out int p1)
                    && int.TryParse(txtChiphikhac.Text.Replace(",", ""), out int p2)
                    && int.TryParse(txtGiaBan.Text.Replace(",", ""),    out int p3)
                    && int.TryParse(txtGiaBanVPA.Text.Replace(",", ""), out int p4))
                {
                    _googleSheetsService.UpdatePrices(_selectedMaterialId, _selectedMarketPriceId, "F", p1, p2, p3, p4);
                    m.PCFee           = p1;
                    m.OrtherFee       = p2;
                    market.CommonPrice = p3;
                    market.VPAPrice   = p4;
                    MessageBox.Show("Cập nhật giá thành công", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Nhập lại Giá Sơn/ Mạ, Chi phí khác, và Giá thị trường", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (rbMa.Checked)
            {
                if (int.TryParse(txtSonMa.Text.Replace(",", ""),     out int p1)
                    && int.TryParse(txtChiphikhac.Text.Replace(",", ""), out int p2)
                    && int.TryParse(txtGiaBan.Text.Replace(",", ""),    out int p3)
                    && int.TryParse(txtGiaBanVPA.Text.Replace(",", ""), out int p4))
                {
                    _googleSheetsService.UpdatePrices(_selectedMaterialId, _selectedMarketPriceId, "G", p1, p2, p3, p4);
                    m.HDGFee          = p1;
                    m.OrtherFee       = p2;
                    market.CommonPrice = p3;
                    market.VPAPrice   = p4;
                    MessageBox.Show("Cập nhật giá thành công", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Nhập lại Giá Sơn/ Mạ, Chi phí khác, và Giá thị trường", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                if (int.TryParse(txtChiphikhac.Text.Replace(",", ""), out int p2)
                    && int.TryParse(txtGiaBan.Text.Replace(",", ""),    out int p3)
                    && int.TryParse(txtGiaBanVPA.Text.Replace(",", ""), out int p4))
                {
                    _googleSheetsService.UpdatePrices(_selectedMaterialId, _selectedMarketPriceId, "", 0, p2, p3, p4);
                    m.OrtherFee        = p2;
                    market.CommonPrice = p3;
                    market.VPAPrice    = p4;
                    MessageBox.Show("Cập nhật giá thành công", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Nhập lại Giá Sơn/ Mạ, Chi phí khác, và Giá thị trường", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void dgvRecord_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                _selectedRecordRowIndex = e.RowIndex + 1;
        }

        private void txtGiaVon_TextChanged(object sender, EventArgs e) { }

        // ── Navigation handlers ───────────────────────────────────────────

        /// <summary>Chuyển sang trang Báo giá.</summary>
        private void btnNavQuotation_Click(object sender, EventArgs e)
        {
            pnlQuotation.Visible = true;
            pnlObjects.Visible   = false;
            SetActiveNav(btnNavQuotation);
        }

        /// <summary>Chuyển sang trang Đối tượng.</summary>
        private void btnNavObjects_Click(object sender, EventArgs e)
        {
            pnlQuotation.Visible = false;
            pnlObjects.Visible   = true;
            SetActiveNav(btnNavObjects);
        }

        /// <summary>Làm nổi bật nav button đang active.</summary>
        private void SetActiveNav(Button active)
        {
            var activeColor   = Color.FromArgb(0, 120, 215);
            var inactiveColor = Color.FromArgb(30, 30, 60);
            var activeFG      = Color.White;
            var inactiveFG    = Color.FromArgb(200, 200, 200);
            var activeFont    = new Font("Segoe UI", 11F, FontStyle.Bold);
            var inactiveFont  = new Font("Segoe UI", 11F, FontStyle.Regular);

            foreach (Button btn in new[] { btnNavQuotation, btnNavObjects })
            {
                bool isActive  = btn == active;
                btn.BackColor  = isActive ? activeColor   : inactiveColor;
                btn.ForeColor  = isActive ? activeFG      : inactiveFG;
                btn.Font       = isActive ? activeFont    : inactiveFont;
            }
        }

        // ── Đối tượng panel handlers ──────────────────────────────────────

        /// <summary>Thêm một đối tượng mới vào bảng.</summary>
        private void btnAddObject_Click(object sender, EventArgs e)
        {
            // TODO: mở dialog nhập thông tin đối tượng mới
            MessageBox.Show("Chức năng Thêm đối tượng đang được phát triển.",
                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>Mở cấu hình hệ thống.</summary>
        private void btnCauHinh_Click(object sender, EventArgs e)
        {
            // TODO: mở form cấu hình
            MessageBox.Show("Chức năng Cấu hình đang được phát triển.",
                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion
    }
}
