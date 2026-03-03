using ECQ_Soft.Model;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace ECQ_Soft.Services
{
    /// <summary>
    /// Đóng gói toàn bộ tương tác với Google Sheets API.
    /// </summary>
    public class GoogleSheetsService
    {
        private SheetsService _sheetsService;
        private readonly string _spreadsheetId;
        private readonly string _sheetName;

        public GoogleSheetsService(string spreadsheetId, string sheetName)
        {
            _spreadsheetId = spreadsheetId;
            _sheetName     = sheetName;
            Init();
        }

        // ── Khởi tạo ────────────────────────────────────────────────────────
        private void Init()
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
                    ApplicationName       = "GSheetUpdater",
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

        // ── Đọc dữ liệu ─────────────────────────────────────────────────────

        public List<Material> LoadMaterials()
        {
            var result = new List<Material>();
            try
            {
                string range   = $"{_sheetName}!B2:H";
                var    request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
                var    rows    = request.Execute().Values;

                int stt = 0;
                for (int i = 0; i < rows.Count; i++)
                {
                    stt++;
                    var row = rows[i];
                    if (row.Count < 7) continue;

                    string type     = row[0].ToString();
                    string thickStr = row[1].ToString().Split(' ')[0];
                    string qStr     = row[2].ToString().Trim();
                    string priceStr = row[3].ToString().Trim().Replace(".", "");
                    string pcStr    = row[4].ToString().Trim().Replace(".", "");
                    string hdgStr   = row[5].ToString().Trim().Replace(".", "");
                    string otherStr = row[6].ToString().Trim().Replace(".", "");

                    if (float.TryParse(qStr,     out float q)
                        && int.TryParse(priceStr, out int price)
                        && int.TryParse(pcStr,    out int pcFee)
                        && int.TryParse(hdgStr,   out int hdgFee)
                        && int.TryParse(otherStr, out int otherFee)
                        && float.TryParse(thickStr, out float t))
                    {
                        result.Add(new Material
                        {
                            Id          = stt,
                            Name        = type + ", dày " + thickStr + " mm",
                            DisplayName = " dày " + thickStr + " mm, " + type,
                            Q           = q,
                            Thick       = t,
                            Price       = price,
                            PCFee       = pcFee,
                            HDGFee      = hdgFee,
                            OrtherFee   = otherFee,
                        });
                    }
                    else
                    {
                        MessageBox.Show($"Giá trị không hợp lệ tại dòng {i + 2}",
                            "Lỗi dữ liệu bảng Vật liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Google.GoogleApiException ex) when (ex.Message.Contains("Unable to parse range"))
            {
                MessageBox.Show($"Không tìm thấy sheet '{_sheetName}'.\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Lỗi khi truy cập Google Sheets:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi không xác định:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return result;
        }

        public List<CabinetType> LoadCabinetTypes()
        {
            var result = new List<CabinetType>();
            try
            {
                string range   = $"{_sheetName}!K2:M";
                var    request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
                var    rows    = request.Execute().Values;

                int stt = 0;
                for (int i = 0; i < rows.Count; i++)
                {
                    stt++;
                    var row = rows[i];
                    if (row.Count < 2) continue;

                    result.Add(new CabinetType
                    {
                        Id          = stt,
                        Name        = row[0].ToString(),
                        Formula     = row[1].ToString().Trim(),
                        FormulaType = row[2].ToString().Trim(),
                    });
                }
            }
            catch (Google.GoogleApiException ex) when (ex.Message.Contains("Unable to parse range"))
            {
                MessageBox.Show($"Không tìm thấy sheet '{_sheetName}'.\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Lỗi khi truy cập Google Sheets:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi không xác định:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return result;
        }

        public List<MarketPrice> LoadMarketPrices()
        {
            var result = new List<MarketPrice>();
            try
            {
                string range   = $"{_sheetName}!W2:AA";
                var    request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
                var    rows    = request.Execute().Values;

                int stt = 0;
                for (int i = 0; i < rows.Count; i++)
                {
                    stt++;
                    var row = rows[i];

                    string matStr  = row[0].ToString().Trim();
                    string cabStr  = row[1].ToString().Trim();
                    string coatStr = row[2].ToString().Trim();
                    string mktStr  = row[3].ToString().Trim().Replace(".", "");
                    string vpaStr  = row[4].ToString().Trim().Replace(".", "");

                    if (int.TryParse(matStr,  out int materialId)
                        && int.TryParse(cabStr,  out int cabinetId)
                        && int.TryParse(coatStr, out int coatingId)
                        && int.TryParse(mktStr,  out int mktPrice)
                        && int.TryParse(vpaStr,  out int vpaPrice))
                    {
                        result.Add(new MarketPrice
                        {
                            Stt           = stt,
                            MaterialId    = materialId,
                            CabinetId     = cabinetId,
                            CoatingTypeId = coatingId,
                            CommonPrice   = mktPrice,
                            VPAPrice      = vpaPrice,
                        });
                    }
                    else
                    {
                        MessageBox.Show($"Giá trị không hợp lệ tại dòng {i + 2}",
                            "Lỗi dữ liệu Bảng giá bán", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Google.GoogleApiException ex) when (ex.Message.Contains("Unable to parse range"))
            {
                MessageBox.Show($"Không tìm thấy sheet '{_sheetName}'.\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Lỗi khi truy cập Google Sheets:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi không xác định:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return result;
        }

        // ── Cập nhật ────────────────────────────────────────────────────────

        public void UpdatePrices(int rowIndex1, int rowIndex2, string col,
                                  int value1, int value2, int value3, int value4)
        {
            Init(); // Làm mới credential
            try
            {
                int sheetRow1 = rowIndex1 + 1;
                int sheetRow2 = rowIndex2 + 1;

                var data = new List<ValueRange>
                {
                    new ValueRange
                    {
                        Range  = $"{_sheetName}!H{sheetRow1}",
                        Values = new List<IList<object>> { new List<object> { value2 } }
                    },
                    new ValueRange
                    {
                        Range  = $"{_sheetName}!Z{sheetRow2}",
                        Values = new List<IList<object>> { new List<object> { value3 } }
                    },
                    new ValueRange
                    {
                        Range  = $"{_sheetName}!AA{sheetRow2}",
                        Values = new List<IList<object>> { new List<object> { value4 } }
                    },
                };

                if (col != "")
                {
                    data.Add(new ValueRange
                    {
                        Range  = $"{_sheetName}!{col}{sheetRow1}",
                        Values = new List<IList<object>> { new List<object> { value1 } }
                    });
                }

                _sheetsService.Spreadsheets.Values
                    .BatchUpdate(new BatchUpdateValuesRequest { ValueInputOption = "RAW", Data = data },
                                 _spreadsheetId)
                    .Execute();
            }
            catch (Google.GoogleApiException ex) when (ex.Message.Contains("Unable to parse range"))
            {
                MessageBox.Show($"Không tìm thấy sheet '{_sheetName}'.\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Google.GoogleApiException ex)
            {
                MessageBox.Show($"Lỗi truy cập Google Sheets:\n\n{ex.Message}",
                    "Lỗi Google Sheets", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi không xác định:\n\n{ex.Message}",
                    "Lỗi không xác định", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
