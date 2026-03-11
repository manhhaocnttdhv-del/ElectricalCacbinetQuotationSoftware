using ECQ_Soft.Model;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
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
using Excel = Microsoft.Office.Interop.Excel;
namespace ECQ_Soft
{
    public partial class FrmQuotation : UserControl
    {

        private SheetsService _sheetsService;
        private int _selectedMaterialId;
        private int _selectedCabinetTypeId;
        private int _selectedMarketPriceId;
        private FrmQuotation _frmQuotation;
        private List<Material> materials = new List<Material>();
        private List<CabinetType> cabinetTypes = new List<CabinetType>();
        private List<Record> records = new List<Record>();
        private List<MarketPrice> marketPrices = new List<MarketPrice>();
        private int _selectedRecordRowIndex;
        string spreadsheetId = "1swdiFIwhoZaXf4c5R_Lzp2pgZng5RcdOKii2DYkN_Uc";
        string sheetName = "Sheet1";
        private int _selectedCoatingTypeId;
        string formula;
        float weight;
        string unit;

        int GiaVonThem;
        int GiaSonMa;
        int Chiphikhac;

        int GiaVon;
        int GiaThiTruong;
        int GiaVPA;

        uint DonGiaHME;
        uint DonGiaThiTruong;
        uint DonGiaVPA;
        public FrmQuotation()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private void InitGoogleSheetsService()
        {
            try
            {
                GoogleCredential credential;

                using (var stream = new FileStream("credential.json", FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(SheetsService.Scope.Spreadsheets);
                }


                _sheetsService = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "GSheetUpdater",
                });
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show("Không tìm thấy file 'credentials.json'.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show("Lỗi khi đọc file credentials.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show("Lỗi xác thực với Google API.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi không xác định khi kết nối Google Sheets.\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void GetMaterialInfor()
        {
            try
            {
                // Đọc dữ liệu từ Google Sheet
                string range = $"{sheetName}!B2:H"; // Bỏ dòng tiêu đề
                var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
                var response = request.Execute();
                IList<IList<object>> rows = response.Values;

                int Stt = 0;
                for (int i = 0; i < rows.Count; i++)
                {
                    Stt++;
                    // Lấy dữ liệu cần cập nhật
                    var row = rows[i];
                    if (row.Count < 7) continue; // tránh lỗi thiếu dữ liệu

                    string type = row[0].ToString();
                    string thickStr = row[1].ToString().Split(' ')[0];
                    string qStr = row[2].ToString().Trim();
                    string priceStr = row[3].ToString().Trim().Replace(".", "");
                    string PCFeeStr = row[4].ToString().Trim().Replace(".", "");
                    string HDGFeeStr = row[5].ToString().Trim().Replace(".", "");
                    string OtherFeeStr = row[6].ToString().Trim().Replace(".", "");

                    if (float.TryParse(qStr, out float Q)
                        && int.TryParse(priceStr, out int price)
                        && int.TryParse(PCFeeStr, out int PCFee)
                        && int.TryParse(HDGFeeStr, out int HDGFee)
                        && int.TryParse(OtherFeeStr, out int OtherFee)
                        && float.TryParse(thickStr, out float t))

                    {
                        var m = new Material
                        {
                            Id = Stt,
                            Name = type + ", dày " + thickStr + " mm",
                            DisplayName = " dày " + thickStr + " mm, " + type,
                            Q = Q,
                            Thick = t,
                            Price = price,
                            PCFee = PCFee,
                            HDGFee = HDGFee,
                            OrtherFee = OtherFee,
                        };
                        materials.Add(m);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Giá trị không hợp lệ tại dòng {i + 2}",
                            "Lỗi dữ liệu bảng Vật liệu",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                    }
                }
            }
            catch (Google.GoogleApiException ex) when (ex.Message.Contains("Unable to parse range"))
            {
                MessageBox.Show($"Không tìm thấy sheet có tên '{sheetName}'. Vui lòng kiểm tra lại.\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Lỗi khi truy cập Google Sheets:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi không xác định khi đọc Google Sheet:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void GetCabinetType()
        {
            try
            {
                // Đọc dữ liệu từ Google Sheet
                string range = $"{sheetName}!K2:M"; // Bỏ dòng tiêu đề
                var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
                var response = request.Execute();
                IList<IList<object>> rows = response.Values;

                int Stt = 0;
                for (int i = 0; i < rows.Count; i++)
                {
                    Stt++;
                    // Lấy dữ liệu cần cập nhật
                    var row = rows[i];
                    if (row.Count < 2) continue; // tránh lỗi thiếu dữ liệu

                    string name = row[0].ToString();
                    string formula = row[1].ToString().Trim();
                    string formulaType = row[2].ToString().Trim();

                    CabinetType f = new CabinetType
                    {
                        Id = Stt,
                        Name = name,
                        Formula = formula,
                        FormulaType = formulaType
                    };
                    cabinetTypes.Add(f);
                }
            }
            catch (Google.GoogleApiException ex) when (ex.Message.Contains("Unable to parse range"))
            {
                MessageBox.Show($"Không tìm thấy sheet có tên '{sheetName}'. Vui lòng kiểm tra lại.\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Lỗi khi truy cập Google Sheets:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi không xác định khi đọc Google Sheet:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GetMarketPrice()
        {
            try
            {
                // Đọc dữ liệu từ Google Sheet
                string range = $"{sheetName}!W2:AA"; // Bỏ dòng tiêu đề
                var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
                var response = request.Execute();
                IList<IList<object>> rows = response.Values;

                int Stt = 0;

                for (int i = 0; i < rows.Count; i++)
                {
                    Stt++;
                    var row = rows[i];
                    string materialStr = row[0].ToString().Trim();
                    string CabinetStr = row[1].ToString().Trim();
                    string CoatingTypeStr = row[2].ToString().Trim();
                    string marketPriceStr = row[3].ToString().Trim().Replace(".", "");
                    string VPAPriceStr = row[4].ToString().Trim().Replace(".", "");

                    if (int.TryParse(materialStr, out int materialId)
                        && int.TryParse(CabinetStr, out int cabinetId)
                        && int.TryParse(CoatingTypeStr, out int coatingTypeId)
                        && int.TryParse(marketPriceStr, out int marketprice)
                        && int.TryParse(VPAPriceStr, out int VPAprice)
                        )
                    {
                        MarketPrice mPrice = new MarketPrice
                        {
                            Stt = Stt,
                            MaterialId = materialId,
                            CabinetId = cabinetId,
                            CoatingTypeId = coatingTypeId,
                            CommonPrice = marketprice,
                            VPAPrice = VPAprice,
                        };
                        marketPrices.Add(mPrice);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Giá trị không hợp lệ tại dòng {i + 2}",
                            "Lỗi dữ liệu Bảng giá bán",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                    }
                }
            }
            catch (Google.GoogleApiException ex) when (ex.Message.Contains("Unable to parse range"))
            {
                MessageBox.Show($"Không tìm thấy sheet có tên '{sheetName}'. Vui lòng kiểm tra lại.\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Lỗi khi truy cập Google Sheets:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi không xác định khi đọc Google Sheet:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadMaterialtoCombobox()
        {
            materials.Insert(0, new Material
            {
                Id = 0,
                Name = "-- Chọn vật liệu --"
            });

            cboMaterial.DataSource = materials;
            cboMaterial.DisplayMember = "Name";
            cboMaterial.ValueMember = "Id";
            cboMaterial.SelectedIndex = 0;
        }

        private void LoadCabinetTypetoCombobox()
        {
            cabinetTypes.Insert(0, new CabinetType
            {
                Id = 0,
                Name = "-- Chọn loại tủ điện hoặc thang, máng cáp --"
            });

            cboCabinetType.DataSource = cabinetTypes;
            cboCabinetType.DisplayMember = "Name";
            cboCabinetType.ValueMember = "Id";

            cboCabinetType.SelectedIndex = 0;
        }

        private void LoadRecord()
        {
            var list = records.OrderBy(t => t.Stt).ToList();
            // Tính tổng
            ulong TotalNotVat = list.Aggregate(0UL, (acc, it) => acc + it.MarketTotalPrice);
            ulong VAT = (ulong)(TotalNotVat * 8 / 100.0);
            ulong Total = TotalNotVat + VAT;

            ulong TotalNotVatHME = list.Aggregate(0UL, (acc, it) => acc + it.HMETotalPrice);
            ulong VatHME = (ulong)(TotalNotVatHME * 8 / 100.0);
            ulong TotalHME = TotalNotVatHME + VatHME;

            ulong TotalNotVatVPA = list.Aggregate(0UL, (acc, it) => acc + it.VPATotalPrice);
            ulong VatVPA = (ulong)(TotalNotVatVPA * 8 / 100.0);
            ulong TotalVPA = TotalNotVatVPA + VatVPA;

            // Thêm 3 dòng tổng
            list.Add(new Record { Name = "TỔNG CỘNG (Giá chưa bao gồm VAT)", MarketTotalPrice = TotalNotVat, HMETotalPrice = TotalNotVatHME, VPATotalPrice = TotalNotVatVPA });
            list.Add(new Record { Name = "THUẾ VAT 8%", MarketTotalPrice = VAT, HMETotalPrice = VatHME, VPATotalPrice = VatVPA });
            list.Add(new Record { Name = "TỔNG CỘNG (Đã bao gồm VAT)", MarketTotalPrice = Total, HMETotalPrice = TotalHME, VPATotalPrice = TotalVPA });

            dgvRecord.DataSource = list;
            dgvRecord.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvRecord.Columns["WeightperUnit"].Visible = false;

            dgvRecord.Columns["Stt"].FillWeight = 20;
            dgvRecord.Columns["Name"].FillWeight = 120;
            dgvRecord.Columns["Unit"].FillWeight = 27;
            dgvRecord.Columns["Quantity"].FillWeight = 32;
            dgvRecord.Columns["HMEUnitPrice"].FillWeight = 35;
            dgvRecord.Columns["HMETotalPrice"].FillWeight = 35;
            dgvRecord.Columns["MarketUnitPrice"].FillWeight = 35;
            dgvRecord.Columns["MarketTotalPrice"].FillWeight = 35;
            dgvRecord.Columns["VPAUnitPrice"].FillWeight = 35;
            dgvRecord.Columns["VPATotalPrice"].FillWeight = 35;
            dgvRecord.Columns["Weight"].FillWeight = 37;
            dgvRecord.Columns["Note"].FillWeight = 32;

            dgvRecord.Columns["Stt"].HeaderText = "STT";
            dgvRecord.Columns["Name"].HeaderText = "Tên vật tư, hàng hóa";
            dgvRecord.Columns["Unit"].HeaderText = "Đơn vị";
            dgvRecord.Columns["Quantity"].HeaderText = "Số lượng";
            dgvRecord.Columns["HMEUnitPrice"].HeaderText = "Đơn giá\nHME";
            dgvRecord.Columns["HMETotalPrice"].HeaderText = "Thành tiền\nHME";
            dgvRecord.Columns["MarketUnitPrice"].HeaderText = "Đơn giá\n(VNĐ)";
            dgvRecord.Columns["MarketTotalPrice"].HeaderText = "Thành tiền\n(VNĐ)";
            dgvRecord.Columns["VPAUnitPrice"].HeaderText = "Đơn giá\nVPA";
            dgvRecord.Columns["VPATotalPrice"].HeaderText = "Thành tiền\nVPA";
            dgvRecord.Columns["Weight"].HeaderText = "Khối lượng\n(Kg)";
            dgvRecord.Columns["Note"].HeaderText = "Ghi chú";

            if (!Settings.Default.isAdmin)
            {
                if (Settings.Default.Role.ToLower() == "vnecco")
                {
                    dgvRecord.Columns["VPAUnitPrice"].HeaderText = "Giá nhập\nVPA";
                    dgvRecord.Columns["HMEUnitPrice"].Visible = false;
                    dgvRecord.Columns["HMETotalPrice"].Visible = false;
                }
            }


            // Cho phép xuống dòng
            dgvRecord.Columns["Name"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvRecord.Columns["Note"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            // Hàng tự động tăng chiều cao theo nội dung
            dgvRecord.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // Font cho cell (nội dung)
            dgvRecord.DefaultCellStyle.Font = new Font("Times New Roman", 12, FontStyle.Regular);

            // Font cho header
            dgvRecord.ColumnHeadersDefaultCellStyle.Font = new Font("Times New Roman", 12, FontStyle.Bold);
            // Căn phải cho giá tiền
            dgvRecord.Columns["HMEUnitPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvRecord.Columns["HMETotalPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvRecord.Columns["MarketUnitPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvRecord.Columns["MarketTotalPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvRecord.Columns["VPAUnitPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvRecord.Columns["VPATotalPrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Căn giữa text trong header
            dgvRecord.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Căn giữa cột 
            dgvRecord.Columns["Stt"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvRecord.Columns["Unit"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvRecord.Columns["Quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvRecord.Columns["Weight"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // hiển thị có dấu phẩy
            dgvRecord.Columns["HMEUnitPrice"].DefaultCellStyle.Format = "N0";
            dgvRecord.Columns["HMETotalPrice"].DefaultCellStyle.Format = "N0";
            dgvRecord.Columns["MarketUnitPrice"].DefaultCellStyle.Format = "N0";
            dgvRecord.Columns["MarketTotalPrice"].DefaultCellStyle.Format = "N0";
            dgvRecord.Columns["VPAUnitPrice"].DefaultCellStyle.Format = "N0";
            dgvRecord.Columns["VPATotalPrice"].DefaultCellStyle.Format = "N0";
            dgvRecord.Columns["Quantity"].DefaultCellStyle.Format = "N0";
            dgvRecord.Columns["Weight"].DefaultCellStyle.Format = "N2";

            // màu nền xám
            //dgvRecord.Columns["HMEUnitPrice"].DefaultCellStyle.BackColor = SystemColors.Control;
            //dgvRecord.Columns["MarketUnitPrice"].DefaultCellStyle.BackColor = SystemColors.Control;
            //dgvRecord.Columns["VPAUnitPrice"].DefaultCellStyle.BackColor = SystemColors.Control;

            //

            dgvRecord.EnableHeadersVisualStyles = false;
            dgvRecord.Columns["Stt"].HeaderCell.Style.BackColor = System.Drawing.Color.Yellow;
            dgvRecord.Columns["Name"].HeaderCell.Style.BackColor = System.Drawing.Color.Yellow;
            dgvRecord.Columns["Unit"].HeaderCell.Style.BackColor = System.Drawing.Color.Yellow;
            dgvRecord.Columns["Quantity"].HeaderCell.Style.BackColor = System.Drawing.Color.Yellow;
            dgvRecord.Columns["HMEUnitPrice"].HeaderCell.Style.BackColor = System.Drawing.Color.LightBlue;
            dgvRecord.Columns["HMETotalPrice"].HeaderCell.Style.BackColor = System.Drawing.Color.LightBlue;
            dgvRecord.Columns["MarketUnitPrice"].HeaderCell.Style.BackColor = System.Drawing.Color.Yellow;
            dgvRecord.Columns["MarketTotalPrice"].HeaderCell.Style.BackColor = System.Drawing.Color.Yellow;
            dgvRecord.Columns["Note"].HeaderCell.Style.BackColor = System.Drawing.Color.Yellow;
            dgvRecord.Columns["VPAUnitPrice"].HeaderCell.Style.BackColor = System.Drawing.Color.Lime;
            dgvRecord.Columns["VPATotalPrice"].HeaderCell.Style.BackColor = System.Drawing.Color.Lime;
            dgvRecord.Columns["Weight"].HeaderCell.Style.BackColor = System.Drawing.Color.Lime;


            for (int i = dgvRecord.Rows.Count - 3; i < dgvRecord.Rows.Count; i++)
            {
                dgvRecord.Rows[i].DefaultCellStyle.BackColor = System.Drawing.Color.Yellow;
                dgvRecord.Rows[i].DefaultCellStyle.Font = new Font("Times New Roman", 12, FontStyle.Bold);
            }
        }

        public float EvaluateFormula(string formula, int H, int W, int D, float T)
        {
            try
            {
                if (formula == null)
                {
                    MessageBox.Show(
                        "Hãy chọn loại tủ điện",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    return 0;
                }
                // Dùng CultureInfo.InvariantCulture để đảm bảo dấu chấm thập phân
                var ci = CultureInfo.InvariantCulture;

                formula = formula.Replace("a", H.ToString(ci))
                                 .Replace("b", W.ToString(ci))
                                 .Replace("c", D.ToString(ci))
                                 .Replace("d", T.ToString(ci));


                DataTable dt = new DataTable();
                var result = dt.Compute(formula, "");            // tính toán chuỗi công thức
                float w = Convert.ToSingle(result);
                return w;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tính công thức: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }

        private void ExportToExcel(DataGridView dgv)
        {
            try
            {
                // Khởi tạo Excel
                Excel.Application excelApp = new Excel.Application();
                excelApp.Visible = true; // mở Excel luôn để xem
                excelApp.DisplayAlerts = false;

                Excel.Workbook workbook = excelApp.Workbooks.Add(Type.Missing);
                Excel.Worksheet worksheet = workbook.ActiveSheet;
                worksheet.Name = "ExportData";

                // ---- 1. Xuất Header ----
                for (int i = 0; i < dgv.Columns.Count; i++)
                {
                    worksheet.Cells[1, i + 1] = dgv.Columns[i].HeaderText;
                }

                // Format hàng header
                Excel.Range headerRange = worksheet.Range[
                    worksheet.Cells[1, 1],
                    worksheet.Cells[1, dgv.Columns.Count]
                ];
                headerRange.Font.Bold = true;
                //headerRange.Interior.Color = System.Drawing.ColorTranslator.ToOle(Color.LightGray); // nền xám
                headerRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter; // căn giữa
                headerRange.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;

                // ---- 2. Xuất Dữ Liệu ----
                for (int i = 0; i < dgv.Rows.Count; i++)
                {
                    for (int j = 0; j < dgv.Columns.Count; j++)
                    {
                        if (dgv.Rows[i].Cells[j].Value != null)
                        {
                            worksheet.Cells[i + 2, j + 1] = dgv.Rows[i].Cells[j].Value.ToString();
                        }
                    }
                }

                // ---- 3. Format toàn bộ bảng ----
                Excel.Range usedRange = worksheet.Range[
                    worksheet.Cells[1, 1],
                    worksheet.Cells[dgv.Rows.Count + 1, dgv.Columns.Count]
                ];

                usedRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous; // kẻ viền
                usedRange.Columns.AutoFit(); // tự chỉnh độ rộng cột
                usedRange.Rows.AutoFit();    // tự chỉnh chiều cao hàng
                usedRange.WrapText = true;   // cho phép xuống dòng trong ô
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private void GetAdditionFee(int type)
        {
            if (_selectedMaterialId != 0)
            {
                var m = materials.Where(t => t.Id == _selectedMaterialId).FirstOrDefault();
                if (m == null)
                {
                    MessageBox.Show(
                        "Không tìm thấy vật liệu",
                        "Lỗi dữ liệu. Bảng vật liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );

                    return;
                }

                if (type == 1)
                {
                    txtSonMa.Text = m.PCFee.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                    int Von = m.PCFee + m.OrtherFee + m.Price;
                    txtGiaVon.Text = Von.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                }
                if (type == 2)
                {
                    txtSonMa.Text = m.HDGFee.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                    int Von = m.HDGFee + m.OrtherFee + m.Price;
                    txtGiaVon.Text = Von.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                }
                if (type == 3)
                {
                    txtSonMa.Text = "0";
                    int Von = m.OrtherFee + m.Price;
                    txtGiaVon.Text = Von.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                }

                if (_selectedCabinetTypeId != 0)
                {
                    var cabinettype = cabinetTypes.Where(t => t.Id == _selectedCabinetTypeId).FirstOrDefault();
                    if (cabinettype == null)
                    {
                        MessageBox.Show(
                           "Không tìm thấy Loại tủ điện, thang, máng cáp",
                           "Lỗi dữ liệu. Bảng Loại tủ điện, thang, máng cáp",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Error
                        );
                        return;
                    }

                    var marketPrice = marketPrices
                                .Where(t => t.MaterialId == _selectedMaterialId || t.MaterialId == 0)
                                .Where(t => t.CabinetId == _selectedCabinetTypeId)
                                .Where(t => t.CoatingTypeId == _selectedCoatingTypeId || t.CoatingTypeId == 4)
                                .FirstOrDefault();
                    if (marketPrice == null)
                    {
                        MessageBox.Show(
                            "Chọn lại nguyên vật liệu và loại tủ điện, thang máng cáp",
                            "Lỗi Google Sheet. Bảng giá thị trường",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        return;
                    }

                    _selectedMarketPriceId = marketPrice.Stt;
                    txtGiaBan.Text = marketPrice.CommonPrice.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                    txtGiaBanVPA.Text = marketPrice.VPAPrice.ToString("N0", CultureInfo.GetCultureInfo("en-US"));

                }
            }
            else
            {
                MessageBox.Show(
                    "Hãy chọn vật liệu",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                rbSon.Checked = false;
                rbMa.Checked = false;
                rbNone.Checked = false;
                return;
            }
        }

        public void GoogleSheetUpdate(int rowIndex1, int rowIndex2, string col, int value1, int value2, int value3, int value4)
        {
            InitGoogleSheetsService();
            try
            {
                int sheetRow1 = rowIndex1 + 1;
                int sheetRow2 = rowIndex2 + 1;
                string time = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                var data = new List<ValueRange>
                {

                     new ValueRange
                    {
                        Range = $"{sheetName}!H{sheetRow1}",
                        Values = new List<IList<object>> { new List<object> { value2 } }
                    },
                      new ValueRange
                    {
                        Range = $"{sheetName}!Z{sheetRow2}",
                        Values = new List<IList<object>> { new List<object> { value3 } }
                    },
                        new ValueRange
                    {
                        Range = $"{sheetName}!AA{sheetRow2}",
                        Values = new List<IList<object>> { new List<object> { value4 } }
                    },
                };

                if (col != "")
                {
                    var newValue = new ValueRange
                    {
                        Range = $"{sheetName}!{col}{sheetRow1}",
                        Values = new List<IList<object>> { new List<object> { value1 } }
                    };

                    data.Add(newValue);
                }

                var batchUpdateRequest = new BatchUpdateValuesRequest
                {
                    ValueInputOption = "RAW",
                    Data = data
                };

                var request = _sheetsService.Spreadsheets.Values.BatchUpdate(batchUpdateRequest, spreadsheetId);
                request.Execute();
            }
            catch (Google.GoogleApiException ex) when (ex.Message.Contains("Unable to parse range"))
            {
                MessageBox.Show(
                    $"Không tìm thấy sheet có tên '{sheetName}'. Vui lòng kiểm tra lại.\n\nChi tiết: {ex.Message}",
                    "Lỗi Google Sheets",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show(
                    $"Lỗi truy cập Google Sheets:\n\n{ex.Message}",
                    "Lỗi Google Sheets",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi không xác định khi cập nhật Google Sheet:\n\n{ex.Message}",
                    "Lỗi không xác định",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                    );
            }

        }

        private void cboMaterial_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboMaterial.SelectedValue is int ID)
            {
                if (ID > 0)
                {
                    _selectedMaterialId = ID;

                    var m = materials.Where(t => t.Id == ID).FirstOrDefault();
                    Chiphikhac = m.OrtherFee;
                    txtChiphikhac.Text = Chiphikhac.ToString("N0", CultureInfo.GetCultureInfo("en-US"));

                    if (rbMa.Checked)
                    {
                        txtSonMa.Text = m.HDGFee.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                        int Von = m.HDGFee + m.OrtherFee + m.Price;
                        txtGiaVon.Text = Von.ToString("N0", CultureInfo.GetCultureInfo("en-US"));

                    }
                    if (rbSon.Checked)
                    {
                        txtSonMa.Text = m.PCFee.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                        int Von = m.PCFee + m.OrtherFee + m.Price;
                        txtGiaVon.Text = Von.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                    }
                    if (rbNone.Checked)
                    {
                        txtSonMa.Text = "0";
                        int Von = m.OrtherFee + m.Price;
                        txtGiaVon.Text = Von.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                    }

                    if (_selectedCabinetTypeId != 0)
                    {
                        if (_selectedCoatingTypeId != 0)
                        {
                            var marketPrice = marketPrices
                                .Where(t => t.MaterialId == _selectedMaterialId || t.MaterialId == 0)
                                .Where(t => t.CabinetId == _selectedCabinetTypeId)
                                .Where(t => t.CoatingTypeId == _selectedCoatingTypeId || t.CoatingTypeId == 4)
                                .FirstOrDefault();
                            if (marketPrice == null)
                            {
                                MessageBox.Show(
                                    "Chọn lại nguyên vật liệu và loại tủ điện, thang máng cáp",
                                    "Lỗi Google Sheet. Bảng giá thị trường",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error
                                );
                                return;
                            }

                            _selectedMarketPriceId = marketPrice.Stt;
                            txtGiaBan.Text = marketPrice.CommonPrice.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                            txtGiaBanVPA.Text = marketPrice.VPAPrice.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                        }
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

                    if (_selectedMaterialId != 0)
                    {
                        if (_selectedCoatingTypeId != 0)
                        {
                            var marketPrice = marketPrices
                                .Where(t => t.MaterialId == _selectedMaterialId || t.MaterialId == 0)
                                .Where(t => t.CabinetId == _selectedCabinetTypeId)
                                .Where(t => t.CoatingTypeId == _selectedCoatingTypeId || t.CoatingTypeId == 4)
                                .FirstOrDefault();
                            if (marketPrice == null)
                            {
                                MessageBox.Show(
                                    "Chọn lại nguyên vật liệu và loại tủ điện, thang máng cáp",
                                    "Lỗi Google Sheet. Bảng giá thị trường",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error
                                );
                                return;
                            }

                            _selectedMarketPriceId = marketPrice.Stt;
                            txtGiaBan.Text = marketPrice.CommonPrice.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                            txtGiaBanVPA.Text = marketPrice.VPAPrice.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
                        }
                    }
                }
            }
        }

        private void btnTinhGia_Click(object sender, EventArgs e)
        {
            var type = cabinetTypes.Where(t => t.Id == _selectedCabinetTypeId).First();
            if (type.Id == 0 || type == null)
            {
                MessageBox.Show(
                    "Chọn lại kiểu tủ điện",
                    "Cảnh báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }
            formula = type.Formula;

            var m = materials.FirstOrDefault(t => t.Id == _selectedMaterialId);
            if (m.Id == 0 || m == null)
            {
                MessageBox.Show(
                    "Chọn lại vật liệu tủ điện",
                    "Cảnh báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            float thick = m.Thick;
            // Tính khối lượng vật liệu
            if (type.FormulaType == "1")
            {
                if (int.TryParse(txtHeight.Text, out int H) && int.TryParse(txtWidth.Text, out int W) && int.TryParse(txtDepth.Text, out int D))
                {
                    float s = EvaluateFormula(formula, H, W, D, thick);        // Diện tích vật liệu (m2)
                    weight = (float)(s * m.Q) / 1000000;                         // Khối lượng vật liệu (kg)
                }
                else
                {
                    MessageBox.Show(
                        "Nhập lại kích thước tủ điện",
                        "Cảnh báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    return;
                }
            }
            else if (type.FormulaType == "2")
            {
                if (int.TryParse(txtHeight.Text, out int H) && int.TryParse(txtWidth.Text, out int W))
                {
                    weight = EvaluateFormula(formula, H, W, 0, thick);         // Khối lượng vật liệu (kg)
                }
                else
                {
                    MessageBox.Show(
                        "Nhập lại kích thước tủ điện",
                        "Cảnh báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    return;
                }
            }
            else
            {
                MessageBox.Show(
                       "Lỗi giá trị tại cột kiểu công thức, bảng loại tủ điện, thang, máng cáp.",
                       "Cảnh báo",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Warning
                   );

                return;
            }


            // Tính Giá tiền của 1 kg vật liệu
            if (int.TryParse(txtSonMa.Text.Replace(",", ""), out int temp))
            {
                if (rbSon.Checked)
                {
                    GiaSonMa = temp;
                }
                else if (rbMa.Checked)
                {
                    GiaSonMa = temp;
                }
                else if (rbNone.Checked)
                {
                    GiaSonMa = 0;
                }
                else
                {
                    MessageBox.Show(
                        "Hãy chọn kiểu bề mặt",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    return;
                }
            }
            else
            {
                MessageBox.Show(
                       "Hãy nhập số nguyên cho chi phí sơn mạ",
                       "Lỗi",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Error
                   );
                return;
            }

            if (int.TryParse(txtChiphikhac.Text.Replace(",", ""), out int temp1))
            {
                Chiphikhac = temp1;
            }
            else
            {
                MessageBox.Show(
                       "Hãy nhập số nguyên cho chi phí khác",
                       "Lỗi",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Error
                   );
                return;
            }





            GiaVon = m.Price + GiaSonMa + Chiphikhac;

            // Tính đơn giá 1 tủ điện của HME
            DonGiaHME = (uint)Math.Round((GiaVon * weight) / 1000) * 1000;             // làm tròn đến hàng nghìn

            // Tính đơn giá
            var marketPrice = marketPrices
                .Where(t => t.MaterialId == _selectedMaterialId || t.MaterialId == 0)
                .Where(t => t.CabinetId == _selectedCabinetTypeId)
                .Where(t => t.CoatingTypeId == _selectedCoatingTypeId || t.CoatingTypeId == 4)
                .FirstOrDefault();
            if (marketPrice == null)
            {
                MessageBox.Show(
                    "Không tìm được giá thị trường tương ứng với vật liệu và loại tủ điện, thang, máng cáp.",
                    "Lỗi Google Sheet",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }
            int GiaBanThiTruong;
            int GiaBanVPA;

            if (int.TryParse(txtGiaBan.Text.Replace(",", ""), out GiaBanThiTruong))
            {

            }
            else
            {
                MessageBox.Show(
                       "Hãy nhập số nguyên cho giá bán thị trường",
                       "Lỗi",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Error
                   );
                return;
            }

            if (int.TryParse(txtGiaBanVPA.Text.Replace(",", ""), out GiaBanVPA))
            {

            }
            else
            {
                MessageBox.Show(
                       "Hãy nhập số nguyên cho chi phí khác",
                       "Lỗi",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Error
                   );
                return;
            }


            DonGiaThiTruong = (uint)Math.Round(weight * GiaBanThiTruong / 1000) * 1000;
            DonGiaVPA = (uint)Math.Round(weight * GiaBanVPA / 1000) * 1000;


            // Text
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
            lbKhoiLuong.Text = weight.ToString("N2", CultureInfo.GetCultureInfo("en-US"));
            lbDonGiaHME.Text = DonGiaHME.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
            lbDonGiaThiTruong.Text = DonGiaThiTruong.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
            lbDonGiaVPA.Text = DonGiaVPA.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            int index = 0;
            if (_selectedRecordRowIndex == 0 || _selectedRecordRowIndex >= records.Count)
            {
                int count = records.Count;
                index = count + 1;
            }
            else
            {
                index = _selectedRecordRowIndex + 1;
                foreach (var item in records.Where(t => t.Stt >= index).ToList())
                {
                    item.Stt++;
                }
            }

            // Tính giá bán        
            Record r = new Record
            {
                Stt = index,
                Name = lbName.Text,
                Unit = unit,
                Quantity = 1,
                HMEUnitPrice = DonGiaHME,
                HMETotalPrice = DonGiaHME,
                MarketUnitPrice = DonGiaThiTruong,
                MarketTotalPrice = DonGiaThiTruong,
                VPAUnitPrice = DonGiaVPA,
                VPATotalPrice = DonGiaVPA,
                WeightperUnit = (float)Math.Round(weight, 2, MidpointRounding.AwayFromZero),
                Weight = (float)Math.Round(weight, 2, MidpointRounding.AwayFromZero),
            };

            records.Add(r);
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
                    {
                        item.Stt--;
                    }
                    LoadRecord();
                }
                else
                {
                    MessageBox.Show(
                        "Chọn bản ghi để xóa",
                        "Cảnh báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }
        private void dgvRecord_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= records.Count)
            {
                return;
            }
            // Kiểm tra có phải cột Quantity không
            if (dgvRecord.Columns[e.ColumnIndex].Name == "Quantity")
            {
                var cell = dgvRecord.Rows[e.RowIndex].Cells[e.ColumnIndex];
                string input = cell.Value?.ToString();

                // Nếu nhập đúng số
                if (uint.TryParse(input, out uint newQuantity))
                {
                    var record = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (record != null)
                    {
                        record.Quantity = newQuantity;
                        record.HMETotalPrice = (ulong)(record.Quantity * record.HMEUnitPrice);
                        record.MarketTotalPrice = (ulong)(record.Quantity * record.MarketUnitPrice);
                        record.VPATotalPrice = (ulong)(record.Quantity * record.VPAUnitPrice);
                        record.Weight = record.Quantity * record.WeightperUnit;
                        //dgvRecord.Refresh(); // cập nhật lại hiển thị
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            LoadRecord();
                        });
                        //LoadRecord();
                    }
                }
                else
                {
                    // Nhập sai → cảnh báo + khôi phục giá trị cũ
                    MessageBox.Show("Vui lòng nhập số nguyên cho cột Số lượng!",
                        "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    var record = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (record != null)
                    {
                        cell.Value = record.Quantity; // giữ lại số cũ
                    }
                }
            }
            if (dgvRecord.Columns[e.ColumnIndex].Name == "HMEUnitPrice")
            {
                var cell = dgvRecord.Rows[e.RowIndex].Cells[e.ColumnIndex];
                string input = cell.Value?.ToString();

                // Nếu nhập đúng số
                if (uint.TryParse(input, out uint newHMEUnitPrice))
                {
                    var record = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (record != null)
                    {
                        record.HMEUnitPrice = newHMEUnitPrice;
                        record.HMETotalPrice = (ulong)(record.Quantity * record.HMEUnitPrice);
                        //dgvRecord.Refresh(); // cập nhật lại hiển thị
                        //LoadRecord();
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            LoadRecord();
                        });
                    }
                }
                else
                {
                    // Nhập sai → cảnh báo + khôi phục giá trị cũ
                    MessageBox.Show("Vui lòng nhập số nguyên cho cột đơn giá HME!",
                        "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    var record = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (record != null)
                    {
                        cell.Value = record.HMEUnitPrice; // giữ lại số cũ
                    }
                }
            }
            if (dgvRecord.Columns[e.ColumnIndex].Name == "MarketUnitPrice")
            {
                var cell = dgvRecord.Rows[e.RowIndex].Cells[e.ColumnIndex];
                string input = cell.Value?.ToString();

                // Nếu nhập đúng số
                if (uint.TryParse(input, out uint newMarketUnitPrice))
                {
                    var record = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (record != null)
                    {
                        record.MarketUnitPrice = newMarketUnitPrice;
                        record.MarketTotalPrice = (ulong)(record.Quantity * record.MarketUnitPrice);
                        //dgvRecord.Refresh(); // cập nhật lại hiển thị
                        //LoadRecord();
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            LoadRecord();
                        });
                    }
                }
                else
                {
                    // Nhập sai → cảnh báo + khôi phục giá trị cũ
                    MessageBox.Show("Vui lòng nhập số nguyên cho cột đơn giá thị trường!",
                        "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    var record = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (record != null)
                    {
                        cell.Value = record.MarketUnitPrice; // giữ lại số cũ
                    }
                }
            }
            if (dgvRecord.Columns[e.ColumnIndex].Name == "VPAUnitPrice")
            {
                var cell = dgvRecord.Rows[e.RowIndex].Cells[e.ColumnIndex];
                string input = cell.Value?.ToString();

                // Nếu nhập đúng số
                if (uint.TryParse(input, out uint newVPAUnitPrice))
                {
                    var record = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (record != null)
                    {
                        record.VPAUnitPrice = newVPAUnitPrice;
                        record.VPATotalPrice = (ulong)(record.Quantity * record.VPAUnitPrice);
                        //dgvRecord.Refresh(); // cập nhật lại hiển thị
                        //LoadRecord();
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            LoadRecord();
                        });
                    }
                }
                else
                {
                    // Nhập sai → cảnh báo + khôi phục giá trị cũ
                    MessageBox.Show("Vui lòng nhập số nguyên cho cột đơn giá VPA!",
                        "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    var record = dgvRecord.Rows[e.RowIndex].DataBoundItem as Record;
                    if (record != null)
                    {
                        cell.Value = record.VPAUnitPrice; // giữ lại số cũ
                    }
                }
            }
        }

        private void dgvRecord_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            MessageBox.Show("Vui lòng nhập số nguyên!",
                 "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            e.Cancel = false; // Ngăn không cho ném exception hệ thống
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ExportToExcel(dgvRecord);
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

            materials.Clear();
            cabinetTypes.Clear();
            marketPrices.Clear();
            GetMaterialInfor();
            LoadMaterialtoCombobox();
            GetCabinetType();
            LoadCabinetTypetoCombobox();
            GetMarketPrice();
            _selectedCabinetTypeId = 0;
            _selectedMaterialId = 0;
            _selectedCoatingTypeId = 0;
            txtChiphikhac.Text = "";
            txtGiaBan.Text = "";
            txtGiaBanVPA.Text = "";
            txtSonMa.Text = "";
            txtGiaVon.Text = "";
            rbMa.Checked = false;
            rbSon.Checked = false;
            rbNone.Checked = false;

            frm.Close();
        }

        private void btnUpdate_Click_1(object sender, EventArgs e)
        {
            // Tính giá bán
            if (_selectedCabinetTypeId != 0 && _selectedMaterialId != 0)
            {
                var m = materials.Where(t => t.Id == _selectedMaterialId).FirstOrDefault();
                var market = marketPrices
                    .Where(t => t.MaterialId == _selectedMaterialId || t.MaterialId == 0)
                    .Where(t => t.CabinetId == _selectedCabinetTypeId)
                    .Where(t => t.CoatingTypeId == _selectedCoatingTypeId || t.CoatingTypeId == 4)
                    .FirstOrDefault();

                if (rbSon.Checked)
                {
                    if (int.TryParse(txtSonMa.Text.Replace(",", ""), out int P1)
                        && int.TryParse(txtChiphikhac.Text.Replace(",", ""), out int P2)
                        && int.TryParse(txtGiaBan.Text.Replace(",", ""), out int P3)
                        && int.TryParse(txtGiaBanVPA.Text.Replace(",", ""), out int P4)
                        )
                    {
                        GoogleSheetUpdate(_selectedMaterialId, _selectedMarketPriceId, "F", P1, P2, P3, P4);
                        m.PCFee = P1;
                        m.OrtherFee = P2;
                        market.CommonPrice = P3;
                        market.VPAPrice = P4;

                        MessageBox.Show(
                            "Cập nhật giá thành công",
                            "Thông báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                    }
                    else
                    {
                        MessageBox.Show(
                            "Nhập lại Giá Sơn/ Mạ, Chi phí khác, và Giá thị trường",
                            "Cảnh báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        return;
                    }


                }
                else if (rbMa.Checked)
                {
                    if (int.TryParse(txtSonMa.Text.Replace(",", ""), out int P1)
                        && int.TryParse(txtChiphikhac.Text.Replace(",", ""), out int P2)
                         && int.TryParse(txtGiaBan.Text.Replace(",", ""), out int P3)
                         && int.TryParse(txtGiaBanVPA.Text.Replace(",", ""), out int P4)
                        )
                    {
                        GoogleSheetUpdate(_selectedMaterialId, _selectedMarketPriceId, "G", P1, P2, P3, P4);
                        m.HDGFee = P1;
                        m.OrtherFee = P2;
                        market.CommonPrice = P3;
                        market.VPAPrice = P4;
                        MessageBox.Show(
                            "Cập nhật giá thành công",
                            "Thông báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else
                    {
                        MessageBox.Show(
                           "Nhập lại Giá Sơn/ Mạ, Chi phí khác, và Giá thị trường",
                            "Cảnh báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        return;
                    }
                }
                else
                {

                    if (int.TryParse(txtChiphikhac.Text.Replace(",", ""), out int P2)
                         && int.TryParse(txtGiaBan.Text.Replace(",", ""), out int P3)
                         && int.TryParse(txtGiaBanVPA.Text.Replace(",", ""), out int P4)
                        )
                    {
                        GoogleSheetUpdate(_selectedMaterialId, _selectedMarketPriceId, "", 0, P2, P3, P4);
                        m.OrtherFee = P2;
                        market.CommonPrice = P3;
                        market.VPAPrice = P4;
                        MessageBox.Show(
                            "Cập nhật giá thành công",
                            "Thông báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else
                    {
                        MessageBox.Show(
                           "Nhập lại Giá Sơn/ Mạ, Chi phí khác, và Giá thị trường",
                            "Cảnh báo",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        return;
                    }
                }

            }
            else
            {
                MessageBox.Show(
                          "Chọn vật liệu hoặc loại tủ điện, thang, máng.",
                           "Cảnh báo",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Warning
                       );
                return;
            }


        }

        private void dgvRecord_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                //var row = dgvRecord.Rows[e.RowIndex];
                _selectedRecordRowIndex = e.RowIndex + 1;
            }
        }

        public async Task LoadDataAsync()
        {
            await Task.Run(() =>
            {
                InitGoogleSheetsService();
                GetMaterialInfor();
                GetCabinetType();
                GetMarketPrice();
            });

            // Sau khi Thread background chạy xong, ta cập nhật lại Control (UI Thread)
            LoadMaterialtoCombobox();
            LoadCabinetTypetoCombobox();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                if (Settings.Default.isAdmin == false)
                {
                    gbGiaVon.Visible = false;
                    btnUpdate.Visible = false;

                    if (!string.IsNullOrEmpty(Settings.Default.Role) && Settings.Default.Role.ToLower() == "vnecco")
                    {
                        lbDonGiaHME.Visible = false;
                        lbHME.Visible = false;
                        lbGiabanTTText.Text = "Giá bán thị trường (VNĐ):";
                        lbGiabanVPAText.Text = "Giá nhập VPA (VNĐ):";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải FrmQuotation: " + ex.Message, "Lỗi UI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
