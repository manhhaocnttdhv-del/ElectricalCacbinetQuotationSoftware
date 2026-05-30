using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ECQ_Soft.Helpers;
using ECQ_Soft.Model;
using Color = System.Drawing.Color;

namespace ECQ_Soft
{
    // Tách riêng: Logic tính toán và hiển thị thông số Vỏ tủ điện
    public partial class FrmAdvancedConfig : Form
    {
        /// <summary>
        /// Tính toán kích thước vỏ tủ dựa trên công thức trong Workflow Node,
        /// sau đó mở dialog để người dùng xác nhận thông số sơn và hiển thị kết quả lên Grid.
        /// </summary>
        private void CalculateAndApplyCabinetDimensions(DataGridViewRow row, string tenHang, List<IList<object>> rawData, HierarchyNode workflowNode)
        {
            var varMap = GetCalculationVariables(rawData, workflowNode);

            if (workflowNode != null && !string.IsNullOrEmpty(workflowNode.Formula))
            {
                try
                {
                    double finalW = 0, finalH = 0, finalD = 0;

                    // Khởi tạo local dictionary để lưu kết quả tạm & các biến gán theo Case (H1, W1...)
                    var localVars = new Dictionary<string, double>(varMap, StringComparer.OrdinalIgnoreCase);
                    if (!localVars.ContainsKey("W")) localVars["W"] = 0;
                    if (!localVars.ContainsKey("H")) localVars["H"] = 0;
                    if (!localVars.ContainsKey("D")) localVars["D"] = 0;

                    string formula = workflowNode.Formula;
                    var matches = Regex.Matches(formula, @"(?i)'?Case\s*(\d+)").Cast<Match>().ToList();

                    string ExtractFormula(string text, string varName)
                    {
                        var match = Regex.Match(text, $@"\b{varName}\s*=\s*(.*?)(?=\s*\b[HWD]\s*=|$)");
                        return match.Success ? match.Groups[1].Value.Trim() : "";
                    }

                    if (matches.Count == 0)
                    {
                        // Không có Case, xử lý toàn bộ biểu thức như 1 khối duy nhất
                        var assignedVars = Regex.Matches(formula, @"\b([HWD])\s*=")
                            .Cast<Match>()
                            .Select(m => m.Groups[1].Value)
                            .Distinct()
                            .ToList();

                        foreach (string vName in assignedVars)
                        {
                            string fExp = ExtractFormula(formula, vName);
                            if (!string.IsNullOrEmpty(fExp))
                                localVars[vName] = CalculationEngine.Evaluate(fExp, localVars);
                        }
                    }
                    else
                    {
                        // Tính toán phần công thức gốc (nằm trước chữ Case đầu tiên)
                        if (matches[0].Index > 0)
                        {
                            string baseFormula = formula.Substring(0, matches[0].Index);
                            var baseVars = Regex.Matches(baseFormula, @"\b([HWD])\s*=")
                                .Cast<Match>()
                                .Select(mx => mx.Groups[1].Value)
                                .Distinct()
                                .ToList();

                            foreach (string vName in baseVars)
                            {
                                string fExp = ExtractFormula(baseFormula, vName);
                                if (!string.IsNullOrEmpty(fExp))
                                    localVars[vName] = CalculationEngine.Evaluate(fExp, localVars);
                            }
                        }

                        for (int i = 0; i < matches.Count; i++)
                        {
                            Match m = matches[i];
                            int startIndex = m.Index + m.Length;
                            int endIndex = (i + 1 < matches.Count) ? matches[i + 1].Index : formula.Length;
                            string blockContent = formula.Substring(startIndex, endIndex - startIndex);
                            string caseLabel = m.Groups[1].Value;

                            // A. Tách Header và Nội dung Formulas
                            var firstAssignMatch = Regex.Match(blockContent, @"\b[HWD]\s*=");
                            string headerText = blockContent;
                            string formulasPart = "";
                            if (firstAssignMatch.Success)
                            {
                                headerText = blockContent.Substring(0, firstAssignMatch.Index);
                                formulasPart = blockContent.Substring(firstAssignMatch.Index);
                            }

                            // B. Kiểm tra điều kiện (...) ở Header
                            string conditionStr = "";
                            var parenMatch = Regex.Match(headerText, @"\(([^()]+)\)");
                            if (parenMatch.Success) conditionStr = parenMatch.Groups[1].Value.Trim();

                            bool isConditionMet = true;
                            if (!string.IsNullOrEmpty(conditionStr))
                            {
                                string normalizedCond = conditionStr
                                    .Replace("&&", " AND ")
                                    .Replace("||", " OR ")
                                    .Replace("==", "=");
                                double condVal = CalculationEngine.Evaluate(normalizedCond, localVars);
                                isConditionMet = (condVal > 0);
                            }

                            // C. Thực thi tính toán nếu thỏa mãn điều kiện
                            if (isConditionMet)
                            {
                                var assignedVars = Regex.Matches(formulasPart, @"\b([HWD])\s*=")
                                    .Cast<Match>()
                                    .Select(mx => mx.Groups[1].Value)
                                    .Distinct()
                                    .ToList();

                                var caseDebugInfo = new StringBuilder();
                                caseDebugInfo.AppendLine($"[THÔNG TIN CASE {caseLabel}]");

                                foreach (string vName in assignedVars)
                                {
                                    string fExp = ExtractFormula(formulasPart, vName);
                                    if (!string.IsNullOrEmpty(fExp))
                                    {
                                        string substitutedExp = CalculationEngine.GetDebugExpression(fExp, localVars);
                                        double val = CalculationEngine.Evaluate(fExp, localVars);
                                        localVars[vName] = val;
                                        localVars[vName + caseLabel] = val;

                                        caseDebugInfo.AppendLine($"- Tính {vName}: {fExp}");
                                        caseDebugInfo.AppendLine($"  => Thế số: {substitutedExp}");
                                        caseDebugInfo.AppendLine($"  => KẾT QUẢ: {val}\n");
                                    }
                                }

                                double currentW = localVars.ContainsKey("W") ? localVars["W"] : 0;
                                double currentH = localVars.ContainsKey("H") ? localVars["H"] : 0;
                                string finalShowMsg = caseDebugInfo.ToString();

                                if (i == matches.Count - 1)
                                {
                                    MessageBox.Show($"Tên tủ: {tenHang}\n\n{finalShowMsg}\n=> CHỐT Ở CASE CUỐI CÙNG: {caseLabel} (W={currentW}, H={currentH})", "Quá trình tính toán Case");
                                    break;
                                }

                                if (currentW >= 1000 || currentH >= 2000)
                                {
                                    MessageBox.Show($"Tên tủ: {tenHang}\n\n{finalShowMsg}\n=> Kích thước W={currentW}, H={currentH} vượt quá hoặc bằng giới hạn W < 1000, H < 2000 -> TỰ ĐỘNG CHUYỂN CASE TIẾP THEO!", "Quá trình tính toán Case");
                                    continue;
                                }

                                MessageBox.Show($"Tên tủ: {tenHang}\n\n{finalShowMsg}\n=> Kích thước chuẩn -> CHỐT Ở CASE NÀY: {caseLabel} (W={currentW}, H={currentH})", "Quá trình tính toán Case");
                                break;
                            }
                        }
                    }

                    finalH = localVars["H"];
                    finalW = localVars["W"];
                    finalD = localVars["D"];

                    // Làm tròn lên theo bước 50 (1599 -> 1600, 327 -> 350...)
                    int RoundUpTo(double value, int step = 50) => value <= 0 ? 0 : (int)(Math.Ceiling(value / step) * step);

                    int finalHmm = RoundUpTo(finalH);
                    int finalWmm = RoundUpTo(finalW);
                    int finalDmm = RoundUpTo(finalD);
                    string kichThuoc = $"H{finalHmm}xW{finalWmm}xD{finalDmm}mm";

                    ShowCabinetSpecForm(row, tenHang, kichThuoc);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi tính toán công thức: " + ex.Message, "Lỗi tính toán", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Hiển thị dialog cho phép người dùng chọn thông số sơn, lớp cánh, môi trường lắp đặt...
        /// Sau khi xác nhận, ghi kết quả chi tiết vào ô "Tên hàng" trong Grid.
        /// </summary>
        private void ShowCabinetSpecForm(DataGridViewRow row, string tenHang, string kichThuoc)
        {
            string viTri = "trong nhà";
            string lopCanh = "2 lớp cánh";
            string doDay = "2";
            string loaiSon = "sơn sần";
            string mauSon = "RAL 7035";
            string moLung = "không mở lưng";
            string vatLieu = "tấm Panel";

            // Thử đọc giá trị cũ từ tên hiện tại (nếu đã có)
            string existingName = tenHang;
            if (existingName.Contains("ngoài trời")) viTri = "ngoài trời";
            if (existingName.Contains("1 lớp cánh")) lopCanh = "1 lớp cánh";
            if (existingName.Contains("sơn bóng")) loaiSon = "sơn bóng";
            if (existingName.Contains("mở lưng")) moLung = "mở lưng";
            if (existingName.Contains("thanh gá")) vatLieu = "thanh gá";

            using (var frmCabSpec = new Form
            {
                Text = "Thông số vỏ tủ điện",
                Size = new Size(480, 410),
                StartPosition = FormStartPosition.CenterParent,
                ShowIcon = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            })
            {
                int labelX = 20, controlX = 200, rowH = 44, startY = 20;
                Font fntLabel = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                Font fntCtrl = new Font("Segoe UI", 9.5f);

                // ── Header kết quả kích thước ──
                var lblDimResult = new Label
                {
                    Text = $"✅ Kích thước tính được: {kichThuoc}",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 120, 60),
                    Location = new Point(labelX, startY),
                    AutoSize = true
                };
                frmCabSpec.Controls.Add(lblDimResult);
                int y = startY + 36;

                // ── 1. Trong nhà / Ngoài trời ──
                var lblViTri = new Label { Text = "Môi trường lắp đặt:", Font = fntLabel, Location = new Point(labelX, y + 4), AutoSize = true };
                var cmbViTri = new ComboBox { Font = fntCtrl, Location = new Point(controlX, y), Size = new Size(230, 28), DropDownStyle = ComboBoxStyle.DropDownList };
                cmbViTri.Items.AddRange(new[] { "trong nhà", "ngoài trời" });
                cmbViTri.SelectedItem = viTri;
                if (cmbViTri.SelectedIndex < 0) cmbViTri.SelectedIndex = 0;
                frmCabSpec.Controls.Add(lblViTri); frmCabSpec.Controls.Add(cmbViTri);
                y += rowH;

                // ── 2. Số lớp cánh ──
                var lblLopCanh = new Label { Text = "Số lớp cánh:", Font = fntLabel, Location = new Point(labelX, y + 4), AutoSize = true };
                var cmbLopCanh = new ComboBox { Font = fntCtrl, Location = new Point(controlX, y), Size = new Size(230, 28), DropDownStyle = ComboBoxStyle.DropDownList };
                cmbLopCanh.Items.AddRange(new[] { "1 lớp cánh", "2 lớp cánh" });
                cmbLopCanh.SelectedItem = lopCanh;
                if (cmbLopCanh.SelectedIndex < 0) cmbLopCanh.SelectedIndex = 1;
                frmCabSpec.Controls.Add(lblLopCanh); frmCabSpec.Controls.Add(cmbLopCanh);
                y += rowH;

                // ── 3. Độ dày tôn (mm) ──
                var lblDoDay = new Label { Text = "Độ dày tôn (mm):", Font = fntLabel, Location = new Point(labelX, y + 4), AutoSize = true };
                var cmbDoDay = new ComboBox { Font = fntCtrl, Location = new Point(controlX, y), Size = new Size(120, 28), DropDownStyle = ComboBoxStyle.DropDownList };
                cmbDoDay.Items.AddRange(new[] { "1", "1.2", "1.5", "2", "2.5", "3" });
                cmbDoDay.SelectedItem = doDay;
                if (cmbDoDay.SelectedIndex < 0) cmbDoDay.SelectedIndex = 3;
                frmCabSpec.Controls.Add(lblDoDay); frmCabSpec.Controls.Add(cmbDoDay);
                y += rowH;

                // ── 4. Loại sơn ──
                var lblLoaiSon = new Label { Text = "Loại sơn:", Font = fntLabel, Location = new Point(labelX, y + 4), AutoSize = true };
                var cmbLoaiSon = new ComboBox { Font = fntCtrl, Location = new Point(controlX, y), Size = new Size(230, 28), DropDownStyle = ComboBoxStyle.DropDownList };
                cmbLoaiSon.Items.AddRange(new[] { "sơn sần", "sơn bóng" });
                cmbLoaiSon.SelectedItem = loaiSon;
                if (cmbLoaiSon.SelectedIndex < 0) cmbLoaiSon.SelectedIndex = 0;
                frmCabSpec.Controls.Add(lblLoaiSon); frmCabSpec.Controls.Add(cmbLoaiSon);
                y += rowH;

                // ── 5. Màu sơn ──
                var lblMauSon = new Label { Text = "Màu sơn:", Font = fntLabel, Location = new Point(labelX, y + 4), AutoSize = true };
                var cmbMauSon = new ComboBox { Font = fntCtrl, Location = new Point(controlX, y), Size = new Size(230, 28), DropDownStyle = ComboBoxStyle.DropDownList };
                cmbMauSon.Items.AddRange(new[] { "RAL 7035 (ghi sáng)", "RAL 7032 (ghi đậm)", "Trắng", "Đen", "Xám", "Đỏ", "Vàng", "Xanh dương", "Xanh lá" });
                bool matchedColor = false;
                foreach (var item in cmbMauSon.Items) { if (item.ToString().StartsWith(mauSon, StringComparison.OrdinalIgnoreCase)) { cmbMauSon.SelectedItem = item; matchedColor = true; break; } }
                if (!matchedColor) cmbMauSon.SelectedIndex = 0;
                cmbMauSon.DropDownStyle = ComboBoxStyle.DropDown;
                frmCabSpec.Controls.Add(lblMauSon); frmCabSpec.Controls.Add(cmbMauSon);
                y += rowH;

                // ── 6. Mở lưng ──
                var lblMoLung = new Label { Text = "Mở lưng tủ:", Font = fntLabel, Location = new Point(labelX, y + 4), AutoSize = true };
                var cmbMoLung = new ComboBox { Font = fntCtrl, Location = new Point(controlX, y), Size = new Size(230, 28), DropDownStyle = ComboBoxStyle.DropDownList };
                cmbMoLung.Items.AddRange(new[] { "không mở lưng", "mở lưng" });
                cmbMoLung.SelectedItem = moLung;
                if (cmbMoLung.SelectedIndex < 0) cmbMoLung.SelectedIndex = 0;
                frmCabSpec.Controls.Add(lblMoLung); frmCabSpec.Controls.Add(cmbMoLung);
                y += rowH;

                // ── 7. Vật liệu lưng ──
                var lblVatLieu = new Label { Text = "Vật liệu lưng tủ:", Font = fntLabel, Location = new Point(labelX, y + 4), AutoSize = true };
                var cmbVatLieu = new ComboBox { Font = fntCtrl, Location = new Point(controlX, y), Size = new Size(230, 28), DropDownStyle = ComboBoxStyle.DropDownList };
                cmbVatLieu.Items.AddRange(new[] { "tấm Panel", "thanh gá" });
                cmbVatLieu.SelectedItem = vatLieu;
                if (cmbVatLieu.SelectedIndex < 0) cmbVatLieu.SelectedIndex = 0;
                lblVatLieu.Enabled = cmbVatLieu.Enabled = (moLung == "mở lưng");
                lblVatLieu.ForeColor = lblVatLieu.Enabled ? Color.Black : Color.Silver;
                cmbMoLung.SelectedIndexChanged += (sv, ev) =>
                {
                    bool isMoLung = cmbMoLung.SelectedItem?.ToString() == "mở lưng";
                    lblVatLieu.Enabled = cmbVatLieu.Enabled = isMoLung;
                    lblVatLieu.ForeColor = isMoLung ? Color.Black : Color.Silver;
                };
                frmCabSpec.Controls.Add(lblVatLieu); frmCabSpec.Controls.Add(cmbVatLieu);
                y += rowH + 8;

                // ── OK / Cancel ──
                var btnOk = new Button { Text = "✔ Xác nhận", Size = new Size(140, 36), Location = new Point(controlX, y), BackColor = Color.FromArgb(0, 150, 70), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "✖ Hủy", Size = new Size(80, 36), Location = new Point(controlX + 150, y), BackColor = Color.FromArgb(200, 60, 50), ForeColor = Color.White, Font = new Font("Segoe UI", 9f), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
                frmCabSpec.Controls.Add(btnOk);
                frmCabSpec.Controls.Add(btnCancel);
                frmCabSpec.AcceptButton = btnOk;
                frmCabSpec.CancelButton = btnCancel;
                frmCabSpec.ClientSize = new Size(450, y + 56);

                if (frmCabSpec.ShowDialog(this) == DialogResult.OK)
                {
                    string selViTri = cmbViTri.SelectedItem?.ToString() ?? viTri;
                    string selLopCanh = cmbLopCanh.SelectedItem?.ToString() ?? lopCanh;
                    string selDoDay = cmbDoDay.SelectedItem?.ToString() ?? doDay;
                    string selLoaiSon = cmbLoaiSon.SelectedItem?.ToString() ?? loaiSon;
                    string selMauSon = cmbMauSon.Text.Trim();
                    if (string.IsNullOrEmpty(selMauSon)) selMauSon = "RAL 7035 (ghi sáng)";
                    string selMoLung = cmbMoLung.SelectedItem?.ToString() ?? moLung;
                    string selVatLieu = cmbVatLieu.SelectedItem?.ToString() ?? vatLieu;

                    var lines = new StringBuilder();
                    lines.AppendLine($"Vỏ tủ điện {selViTri} loại {selLopCanh}:");
                    lines.AppendLine($"- Kích thước {kichThuoc}");
                    lines.AppendLine($"- Tôn dày {selDoDay}mm");
                    lines.AppendLine($"- Sơn tĩnh điện, {selLoaiSon}");
                    lines.Append($"- Sơn màu {selMauSon}");
                    if (selMoLung == "mở lưng")
                    {
                        lines.AppendLine();
                        lines.Append($"- Mở lưng, dùng {selVatLieu}");
                    }

                    row.Cells["colTen"].Value = lines.ToString().TrimEnd();
                    AdjustCabinetRowHeight(row);
                }
            }
        }

        /// <summary>
        /// Tự động điều chỉnh chiều cao row theo số dòng text mô tả vỏ tủ và invalidate để vẽ lại màu.
        /// </summary>
        private void AdjustCabinetRowHeight(DataGridViewRow row)
        {
            if (row == null) return;
            string val = row.Cells["colTen"].Value?.ToString() ?? "";
            if (!val.StartsWith("Vỏ tủ điện")) return;

            int lineCount = val.Split('\n').Length;
            int baseFont = dgvSelectedItems.Font?.Height ?? 15;
            int newHeight = lineCount * (baseFont + 3) + 10;
            if (newHeight < 28) newHeight = 28;
            if (row.Height != newHeight) row.Height = newHeight;
            dgvSelectedItems.InvalidateRow(row.Index);
        }
    }
}
