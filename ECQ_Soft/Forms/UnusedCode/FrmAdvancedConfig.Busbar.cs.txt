using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ECQ_Soft.Helpers;
using ECQ_Soft.Model;
using Color = System.Drawing.Color;

namespace ECQ_Soft
{
    // Tach rieng: Logic tinh toan va hien thi thong so He thong dong thanh cai (Busbar)
    public partial class FrmAdvancedConfig : Form
    {
        private class BusbarCalcDetail
        {
            public string DeviceName { get; set; }
            public double IR { get; set; }
            public string Dimension { get; set; }
            public bool IsTong { get; set; }
            public double TotalMeters { get; set; }
            public string FormulaText { get; set; }
        }

        private async Task CalculateAndApplyBusbarSpec(DataGridViewRow gridRow, string tenHang, List<IList<object>> rawData, bool showModal = false)
        {
            if (rawData == null) return;
            IList<IList<object>> busbarSheetData = null;
            try
            {
                var resp = await _service.Spreadsheets.Values.Get(_spreadsheetId, "Tinh toan dong thanh cai!A:Z").ExecuteAsync();
                busbarSheetData = resp.Values;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Khong the tai sheet 'Tinh toan dong thanh cai'.\nLoi: " + ex.Message);
                return;
            }

            int irColB1 = -1, lenColB1 = -1, lenColCongthucB1 = -1, typeColB1 = -1;
            int irColB2 = -1, mccbColB2 = -1, mcbColB2 = -1, acbColB2 = -1;
            int dimColB3 = -1, buyPriceColB3 = -1, sellPriceColB3 = -1;

            if (busbarSheetData != null && busbarSheetData.Count > 1)
            {
                var headerRow = busbarSheetData[1];
                for (int i = 0; i < headerRow.Count; i++)
                {
                    string val = headerRow[i]?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrEmpty(val)) continue;
                    if (val == "Loai") typeColB1 = i;
                    if (val == "Dong CB (A)") irColB1 = i;
                    if (val == "Chieu dai (mm)") lenColB1 = i;
                    if (val == "Cong thuc") lenColCongthucB1 = i;
                    if (val.IndexOf("Dong dien", StringComparison.OrdinalIgnoreCase) >= 0) irColB2 = i;
                    if (val.Equals("MCCB", StringComparison.OrdinalIgnoreCase)) mccbColB2 = i;
                    if (val.Equals("MCB", StringComparison.OrdinalIgnoreCase)) mcbColB2 = i;
                    if (val.Equals("ACB", StringComparison.OrdinalIgnoreCase)) acbColB2 = i;
                    if (val == "Tiet dien") dimColB3 = i;
                    if (val == "Mua vao") buyPriceColB3 = i;
                    if (val == "Ban ra") sellPriceColB3 = i;
                }
            }

            double cabinetWidth = 0, cabinetQty = 1;
            foreach (DataGridViewRow row in dgvSelectedItems.Rows)
            {
                if (row.IsNewRow) continue;
                string tenItem = row.Cells["colTen"].Value?.ToString() ?? "";
                if (tenItem.StartsWith("Vo tu", StringComparison.OrdinalIgnoreCase) || tenItem.Contains("Kich thuoc H"))
                {
                    if (row.Cells.Count > 6 && double.TryParse(row.Cells[6].Value?.ToString(), out double q)) cabinetQty = q;
                    var wMatch = Regex.Match(tenItem, @"W(\d+)", RegexOptions.IgnoreCase);
                    if (wMatch.Success && double.TryParse(wMatch.Groups[1].Value, out double wVal) && wVal > 0) cabinetWidth = wVal;
                }
            }

            if (dimColB3 == -1 || buyPriceColB3 == -1 || sellPriceColB3 == -1 || irColB2 == -1 ||
                (mccbColB2 == -1 && mcbColB2 == -1 && acbColB2 == -1) || irColB1 == -1 || lenColB1 == -1)
            {
                MessageBox.Show("Khong tim thay cac tieu de cot trong Sheet 'Tinh toan dong thanh cai'. Vui long kiem tra lai Sheet.");
                return;
            }

            bool isGanDung = false;

            async Task<string> GetDongKichThuoc(double ir, string productName)
            {
                int targetDimCol = productName.IndexOf("MCCB", StringComparison.OrdinalIgnoreCase) >= 0 ? mccbColB2
                    : productName.IndexOf("MCB", StringComparison.OrdinalIgnoreCase) >= 0 ? mcbColB2
                    : productName.IndexOf("ACB", StringComparison.OrdinalIgnoreCase) >= 0 ? acbColB2
                    : (mccbColB2 != -1 ? mccbColB2 : mcbColB2);

                string foundDim = "", bestDimForApprox = "";
                double minDiff = double.MaxValue;

                if (busbarSheetData != null && irColB2 != -1 && targetDimCol != -1)
                {
                    foreach (var row in busbarSheetData.Skip(3))
                    {
                        if (row.Count > irColB2 && double.TryParse(row[irColB2]?.ToString()?.Trim(), out double rowIr))
                        {
                            if (Math.Abs(rowIr - ir) < 0.1) { if (row.Count > targetDimCol) { foundDim = row[targetDimCol]?.ToString()?.Trim() ?? ""; break; } }
                            else if (isGanDung && rowIr > ir) { double diff = rowIr - ir; if (diff < minDiff) { minDiff = diff; if (row.Count > targetDimCol) bestDimForApprox = row[targetDimCol]?.ToString()?.Trim() ?? ""; } }
                        }
                    }
                }
                if (string.IsNullOrEmpty(foundDim) && isGanDung) foundDim = bestDimForApprox;
                if (productName.IndexOf("MCB", StringComparison.OrdinalIgnoreCase) >= 0 && (string.IsNullOrEmpty(foundDim) || foundDim.Equals("x", StringComparison.OrdinalIgnoreCase))) foundDim = "8x3";
                if (!string.IsNullOrEmpty(foundDim) && !foundDim.Equals("x", StringComparison.OrdinalIgnoreCase))
                {
                    if (foundDim.Contains("hoac") || foundDim.Contains("/"))
                    {
                        var options = foundDim.Split(new[] { "hoac", "/" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                        return await ShowSelectionDialog(productName, ir, options);
                    }
                    return foundDim;
                }
                return "Khong tim thay trong Sheet";
            }

            double GetLengthSumFromIR(double ir, bool isTong)
            {
                if (isTong) return cabinetWidth - 100;
                double foundSum = 0, bestSumForApprox = 0, minDiff = double.MaxValue;
                if (busbarSheetData != null && irColB1 != -1 && lenColB1 != -1)
                {
                    foreach (var row in busbarSheetData.Skip(3))
                    {
                        if (row.Count > irColB1 && double.TryParse(row[irColB1]?.ToString(), out double rowIr))
                        {
                            double currentSum = 0;
                            if (row.Count > lenColB1) { var parts = row[lenColB1]?.ToString()?.Split(new[] { '-', ' ', '+' }, StringSplitOptions.RemoveEmptyEntries); if (parts != null) foreach (var p in parts) if (double.TryParse(p, out double v)) currentSum += v; }
                            if (Math.Abs(rowIr - ir) < 0.1) { foundSum = currentSum; break; }
                            else if (isGanDung && rowIr > ir) { double diff = rowIr - ir; if (diff < minDiff) { minDiff = diff; bestSumForApprox = currentSum; } }
                        }
                    }
                }
                if (foundSum == 0 && isGanDung) foundSum = bestSumForApprox;
                return foundSum;
            }

            string GetLengthDescription(double ir, bool isTong)
            {
                if (isTong) return $"(W:{cabinetWidth} - 100) = {cabinetWidth - 100}mm";
                if (busbarSheetData != null && irColB1 != -1 && lenColB1 != -1)
                {
                    string bestLenStr = ""; double minDiff = double.MaxValue;
                    foreach (var row in busbarSheetData.Skip(3))
                    {
                        if (row.Count > irColB1 && double.TryParse(row[irColB1]?.ToString(), out double rowIr))
                        {
                            if (Math.Abs(rowIr - ir) < 0.1) { string lenStr = row[lenColB1]?.ToString() ?? ""; return $"({lenStr.Replace(" ", "")}) = {GetLengthSumFromIR(ir, false)}mm"; }
                            else if (isGanDung && rowIr > ir) { double diff = rowIr - ir; if (diff < minDiff) { minDiff = diff; bestLenStr = row[lenColB1]?.ToString() ?? ""; } }
                        }
                    }
                    if (isGanDung && !string.IsNullOrEmpty(bestLenStr)) return $"({bestLenStr.Replace(" ", "")} [Gan dung]) = {GetLengthSumFromIR(ir, false)}mm";
                }
                return "Can tra cuu trong Sheet";
            }

            var seenKeys = new HashSet<string>();
            var deduplicatedRawData = new List<IList<object>>();
            foreach (var draftRow in rawData)
            {
                string p0 = draftRow.Count > 0 ? draftRow[0]?.ToString() ?? "" : "";
                string p1 = draftRow.Count > 1 ? draftRow[1]?.ToString() ?? "" : "";
                string p6 = draftRow.Count > 6 ? draftRow[6]?.ToString() ?? "" : "";
                string dedupKey = p0 + "|" + p1 + "|" + p6;
                if (!string.IsNullOrEmpty(p1) && seenKeys.Contains(dedupKey)) continue;
                if (!string.IsNullOrEmpty(p1)) seenKeys.Add(dedupKey);
                deduplicatedRawData.Add(draftRow);
            }

            var busbarDetails = new List<BusbarCalcDetail>();
            foreach (var draftRow in deduplicatedRawData)
            {
                string rowPath = draftRow.Count > 0 ? draftRow[0]?.ToString() ?? "" : "";
                bool isCheckAt = rowPath.IndexOf("Attomat TONG", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 rowPath.IndexOf("Attomat NHANH", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!isCheckAt) continue;
                string productName = draftRow.Count > 1 ? draftRow[1]?.ToString() ?? "" : "";
                string attrStr = draftRow.Count > 14 ? draftRow[14]?.ToString() ?? "" : "";
                var irMatch = Regex.Match(attrStr, @"\bir\s*:\s*([\d.]+)", RegexOptions.IgnoreCase);
                if (irMatch.Success && double.TryParse(irMatch.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double irVal))
                {
                    if (draftRow.Count > 6 && double.TryParse(draftRow[6]?.ToString(), out double qty))
                    {
                        bool isTong = rowPath.IndexOf("tong", StringComparison.OrdinalIgnoreCase) >= 0;
                        string kichThuoc = await GetDongKichThuoc(irVal, productName);
                        double lenPerItem = GetLengthSumFromIR(irVal, isTong);

                        int phases = 3;
                        if (productName.IndexOf("4P", StringComparison.OrdinalIgnoreCase) >= 0) phases = 4;
                        else if (productName.IndexOf("1P", StringComparison.OrdinalIgnoreCase) >= 0) phases = 1;
                        else if (productName.IndexOf("2P", StringComparison.OrdinalIgnoreCase) >= 0) phases = 2;

                        double totalM;
                        string formulas;
                        if (isTong)
                        {
                            if (cabinetWidth <= 0) { MessageBox.Show("Vui long thuc hien tinh toan Vo tu truoc."); return; }
                            totalM = (lenPerItem * 4) / 1000.0;
                            formulas = $"{GetLengthDescription(irVal, isTong)} * 4 thanh / 1000 = {totalM:N3}m";
                        }
                        else
                        {
                            totalM = (lenPerItem * qty * phases) / 1000.0;
                            formulas = $"{GetLengthDescription(irVal, isTong)} * {qty} (at) * {phases} (pha) / 1000 = {totalM:N3}m";
                        }

                        busbarDetails.Add(new BusbarCalcDetail { DeviceName = productName, IR = irVal, Dimension = kichThuoc, IsTong = isTong, TotalMeters = totalM, FormulaText = formulas });
                    }
                }
            }

            decimal grandTotalBuy = 0, grandTotalSell = 0;
            var displayRows = new List<object[]>();
            var attrBreakdown = new StringBuilder();
            attrBreakdown.AppendLine("CHI TIET TINH TOAN DONG THANH CAI:");

            foreach (var detail in busbarDetails)
            {
                decimal priceBuy = 0, priceSell = 0;
                if (busbarSheetData != null && dimColB3 != -1)
                {
                    string searchDim = detail.Dimension.Replace(" chap doi", "").Trim();
                    foreach (var row in busbarSheetData.Skip(3))
                    {
                        if (row.Count > dimColB3)
                        {
                            string dimVal = row[dimColB3]?.ToString()?.Replace(" ", "").ToLower() ?? "";
                            if (!string.IsNullOrEmpty(dimVal) && !string.IsNullOrEmpty(searchDim) &&
                                (dimVal == searchDim.ToLower() || dimVal.StartsWith(searchDim.ToLower() + "x") || searchDim.ToLower().StartsWith(dimVal)))
                            {
                                if (buyPriceColB3 != -1 && row.Count > buyPriceColB3) priceBuy = ParseCurrencyValue(row[buyPriceColB3]?.ToString());
                                if (sellPriceColB3 != -1 && row.Count > sellPriceColB3) priceSell = ParseCurrencyValue(row[sellPriceColB3]?.ToString());
                                if (priceBuy > 0 && priceBuy < 50000) priceBuy *= 1000;
                                if (priceSell > 0 && priceSell < 50000) priceSell *= 1000;
                                break;
                            }
                        }
                    }
                }
                decimal rowBuy = priceBuy * (decimal)detail.TotalMeters;
                decimal rowSell = priceSell * (decimal)detail.TotalMeters;
                grandTotalBuy += rowBuy; grandTotalSell += rowSell;
                string desc = $"{detail.DeviceName} ({detail.IR}A)";
                string dongType = $"Dong {detail.Dimension}";
                displayRows.Add(new object[] { desc, dongType, detail.TotalMeters.ToString("N3"), detail.FormulaText, FormatCurrencyVnd(priceSell), FormatCurrencyVnd(rowSell) });
                attrBreakdown.AppendLine($"- {desc} - {dongType}: {detail.FormulaText} | Tien: {FormatCurrencyVnd(rowSell)}");
            }

            gridRow.Cells["colTen"].Value = "He thong dong thanh cai (Da tinh toan chi tiet)";
            gridRow.Cells["colSoLuong"].Value = "1";
            gridRow.Cells["colDonGia"].Value = FormatCurrencyVnd(grandTotalSell);
            gridRow.Cells["colGiaTien"].Value = FormatCurrencyVnd(grandTotalSell);
            gridRow.Cells["colGiaNhap"].Value = FormatCurrencyVnd(grandTotalBuy);
            gridRow.Cells["colAttributes"].Value = attrBreakdown.ToString();
            gridRow.Cells["colTen"].ToolTipText = "He thong dong thanh cai (Chuot phai de xem chi tiet tinh toan)";

            if (showModal || true) ShowDetailedResultModal(displayRows, grandTotalSell, grandTotalBuy);
        }

        private async Task<string> ShowSelectionDialog(string productName, double ir, string[] options)
        {
            string selected = options[0];
            using (var frm = new Form { Text = $"LUA CHON THANH CAI - {ir}A", Size = new Size(500, 300), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Color.White })
            {
                var lblTitle = new Label { Text = "XAC NHAN TIET DIEN DONG", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 51, 153), Location = new Point(20, 20), AutoSize = true };
                var lblInfo = new Label { Text = $"San pham: {productName}\nDong dien dinh muc: {ir}A\n\nVui long chon quy cach:", Location = new Point(20, 60), Size = new Size(440, 80), Font = new Font("Segoe UI", 10f) };
                var cmb = new ComboBox { Location = new Point(20, 150), Size = new Size(440, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11f, FontStyle.Bold) };
                cmb.Items.AddRange(options); cmb.SelectedIndex = 0;
                var btn = new Button { Text = "XAC NHAN", Location = new Point(170, 200), Size = new Size(150, 45), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Cursor = Cursors.Hand };
                btn.Click += (s, e) => { selected = cmb.SelectedItem.ToString(); frm.DialogResult = DialogResult.OK; };
                frm.Controls.AddRange(new Control[] { lblTitle, lblInfo, cmb, btn });
                frm.ShowDialog();
            }
            return selected;
        }

        private void ShowDetailedResultModal(List<object[]> rows, decimal totalSell, decimal totalBuy)
        {
            using (var frm = new Form { Text = "BANG CHI TIET TINH TOAN DONG THANH CAI", Size = new Size(1100, 600), StartPosition = FormStartPosition.CenterParent, ShowIcon = false })
            {
                var dgv = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, BackgroundColor = Color.White, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, ColumnHeadersHeight = 40, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing };
                dgv.RowTemplate.Height = 30;
                dgv.SetDoubleBuffered(true);
                dgv.Columns.Add("col1", "Thiet bi");
                dgv.Columns.Add("colDungDong", "Dung dong");
                dgv.Columns.Add("col2", "So met (m)");
                dgv.Columns.Add("col3", "Dien giai cong thuc");
                dgv.Columns.Add("col4", "Don gia");
                dgv.Columns.Add("col5", "Thanh tien");
                dgv.Columns[0].FillWeight = 150; dgv.Columns[1].FillWeight = 100; dgv.Columns[3].FillWeight = 250;
                dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
                dgv.EnableHeadersVisualStyles = false;
                foreach (DataGridViewColumn col in dgv.Columns) col.SortMode = DataGridViewColumnSortMode.NotSortable;
                foreach (var r in rows) dgv.Rows.Add(r);

                var ctxMenu = new ContextMenuStrip();
                ctxMenu.Items.Add("Mo Google Sheets", null, (s, ev) => {
                    if (!string.IsNullOrEmpty(_spreadsheetId))
                        using (var webFrm = new FrmBusbarWebView(_spreadsheetId, _service)) webFrm.ShowDialog(frm);
                });
                dgv.ContextMenuStrip = ctxMenu;

                var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.AliceBlue };
                var lblSummary = new Label { Text = $"TONG GIA BAN: {FormatCurrencyVnd(totalSell)}   |   TONG GIA MUA: {FormatCurrencyVnd(totalBuy)}   |   LOI NHUAN: {FormatCurrencyVnd(totalSell - totalBuy)}", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.DarkBlue, AutoSize = true, Location = new Point(20, 25) };
                pnlBottom.Controls.Add(lblSummary);
                frm.Controls.Add(dgv);
                frm.Controls.Add(pnlBottom);
                frm.ShowDialog();
            }
        }
    }
}
