using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using ECQ_Soft.Model;
using Color = System.Drawing.Color;
using System.ComponentModel;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ECQ_Soft
{
    public partial class FrmAdvancedConfig : Form
    {
        private async Task RunAiCalculationAsync()
        {
            Form aiForm = new Form { Text = "BÁO CÁO PHÂN TÍCH TỦ ĐIỆN (AI)", Size = new Size(1000, 800), StartPosition = FormStartPosition.CenterScreen, ShowIcon = false };
            Newtonsoft.Json.Linq.JArray sketchOptions = null;
            WebBrowser webResult = new WebBrowser { Dock = DockStyle.Fill, ScriptErrorsSuppressed = true };
            aiForm.Controls.Add(webResult);

            webResult.Navigating += (webSender, navEv) =>
            {
                string urlStr = navEv.Url.ToString();
                if (urlStr.StartsWith("app://apply-option", StringComparison.OrdinalIgnoreCase))
                {
                    navEv.Cancel = true; // Chặn trình duyệt chuyển trang
                    
                    string query = navEv.Url.Query;
                    string w = "", h = "", d = "", bb = "", bb_len = "", mat = "", opt_idx = "";
                    if (query.StartsWith("?"))
                    {
                        var pairs = query.Substring(1).Split('&');
                        foreach (var pair in pairs)
                        {
                            var parts = pair.Split('=');
                            if (parts.Length == 2)
                            {
                                string key = parts[0];
                                string val = System.Uri.UnescapeDataString(parts[1]);
                                if (key == "w") w = val;
                                else if (key == "h") h = val;
                                else if (key == "d") d = val;
                                else if (key == "bb") bb = val;
                                else if (key == "bb_len") bb_len = val;
                                else if (key == "mat") mat = val;
                                else if (key == "opt_idx") opt_idx = val;
                            }
                        }
                    }
                    
                    // Lấy Option được chọn để trích xuất danh sách thanh cái nhánh
                    int optIndex = -1;
                    Newtonsoft.Json.Linq.JObject selectedOpt = null;
                    Newtonsoft.Json.Linq.JArray branches = null;
                    if (int.TryParse(opt_idx, out optIndex) && sketchOptions != null && optIndex >= 0 && optIndex < sketchOptions.Count)
                    {
                        selectedOpt = sketchOptions[optIndex] as Newtonsoft.Json.Linq.JObject;
                        var optBusbar = selectedOpt?["busbar"];
                        branches = optBusbar?["branches"] as Newtonsoft.Json.Linq.JArray;
                    }

                    // Nhóm các nhánh của AI trước khi đối chiếu
                    var groupedAiBranches = new System.Collections.Generic.Dictionary<string, (string size, double totalLen)>();
                    if (branches != null)
                    {
                        foreach (var b in branches)
                        {
                            string rawName = b["breaker_name"]?.ToString() ?? "Át nhánh";
                            // Loại bỏ các hậu tố chú thích phụ để nhóm
                            string normName = System.Text.RegularExpressions.Regex.Replace(rawName, @"\s*\(nhánh\s*\d+\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            normName = System.Text.RegularExpressions.Regex.Replace(normName, @"\s*\(\d+\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                            
                            string size = b["busbar_size"]?.ToString() ?? "---";
                            double length = 0;
                            double.TryParse(b["length_mm"]?.ToString(), out length);
                            
                            string key = normName.ToLower();
                            if (groupedAiBranches.ContainsKey(key))
                            {
                                var existing = groupedAiBranches[key];
                                groupedAiBranches[key] = (existing.size, existing.totalLen + length);
                            }
                            else
                            {
                                groupedAiBranches[key] = (size, length);
                            }
                        }
                    }

                    // Cập nhật Grid
                    foreach (DataGridViewRow r in dgvSelectedItems.Rows)
                    {
                        if (r.IsNewRow) continue;
                        string tHang = r.Cells["colTen"].Value?.ToString() ?? "";
                        string tHangLower = tHang.ToLower();
                        
                        if (tHang.StartsWith("Vỏ tủ", StringComparison.OrdinalIgnoreCase))
                        {
                            ShowCabinetSpecForm(r, tHang, $"H{h}xW{w}xD{d}mm");
                        }
                        else if (tHang.StartsWith("Hệ thống đồng thanh cái", StringComparison.OrdinalIgnoreCase))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(tHang, @"\d+x\d+mm", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                r.Cells["colTen"].Value = tHang.Replace(match.Value, $"{bb}");
                            }
                            else
                            {
                                r.Cells["colTen"].Value = $"Hệ thống đồng thanh cái {bb}";
                            }
                            r.Cells["colDungDong"].Value = string.IsNullOrEmpty(mat) ? "Cu" : mat;
                        }
                        else
                        {
                            // Đối chiếu thiết bị Attomat để lưu thuộc tính kích thước AI tính và tooltip
                            string path = r.Cells["colFormId"].Value?.ToString() ?? "";
                            if (path.Contains("Attomat"))
                            {
                                bool isTong = path.Contains("Attomat TỔNG");
                                if (isTong)
                                {
                                    double mainLen = 0;
                                    double.TryParse(bb_len, out mainLen);
                                    
                                    string curAttr = r.Cells["colAttributes"].Value?.ToString() ?? "";
                                    curAttr = System.Text.RegularExpressions.Regex.Replace(curAttr, @"\bai_dim\s*:\s*[^;]+;?", "");
                                    curAttr = System.Text.RegularExpressions.Regex.Replace(curAttr, @"\bai_len\s*:\s*[^;]+;?", "");
                                    if (!curAttr.EndsWith(";") && !string.IsNullOrEmpty(curAttr)) curAttr += ";";
                                    curAttr += $"ai_dim:{bb};ai_len:{mainLen};";
                                    r.Cells["colAttributes"].Value = curAttr;
                                    
                                    r.Cells["colTen"].ToolTipText = $"[AI Tính] Quy cách: {bb}, Dài: {mainLen}mm (Thanh cái chính)";
                                }
                                else
                                {
                                    string matchedKey = null;
                                    foreach (var k in groupedAiBranches.Keys)
                                    {
                                        if (tHangLower.Contains(k) || k.Contains(tHangLower))
                                        {
                                            matchedKey = k;
                                            break;
                                        }
                                        
                                        // Tìm kiếm tương đối bằng MCB/MCCB + Dòng điện định mức + Số cực
                                        var cleanK = k.Replace("susol", "").Replace("metasol", "").Trim();
                                        if ((tHangLower.Contains("mcb") && cleanK.Contains("mcb")) || (tHangLower.Contains("mccb") && cleanK.Contains("mccb")))
                                        {
                                            var m1 = System.Text.RegularExpressions.Regex.Match(cleanK, @"\b\d+a\b");
                                            var m2 = System.Text.RegularExpressions.Regex.Match(tHangLower, @"\b\d+a\b");
                                            if (m1.Success && m2.Success && m1.Value == m2.Value)
                                            {
                                                var p1 = System.Text.RegularExpressions.Regex.Match(cleanK, @"\b\d+p\b");
                                                var p2 = System.Text.RegularExpressions.Regex.Match(tHangLower, @"\b\d+p\b");
                                                if (p1.Success && p2.Success && p1.Value == p2.Value)
                                                {
                                                    matchedKey = k;
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    if (matchedKey != null)
                                    {
                                        var info = groupedAiBranches[matchedKey];
                                        
                                        string curAttr = r.Cells["colAttributes"].Value?.ToString() ?? "";
                                        curAttr = System.Text.RegularExpressions.Regex.Replace(curAttr, @"\bai_dim\s*:\s*[^;]+;?", "");
                                        curAttr = System.Text.RegularExpressions.Regex.Replace(curAttr, @"\bai_len\s*:\s*[^;]+;?", "");
                                        if (!curAttr.EndsWith(";") && !string.IsNullOrEmpty(curAttr)) curAttr += ";";
                                        curAttr += $"ai_dim:{info.size};ai_len:{info.totalLen};";
                                        r.Cells["colAttributes"].Value = curAttr;
                                        
                                        r.Cells["colTen"].ToolTipText = $"[AI Tính] Quy cách: {info.size}, Dài: {info.totalLen}mm";
                                    }
                                }
                            }
                        }
                    }

                    // Đồng bộ lại Cache Draft ngay lập tức để đồng bộ thuộc tính
                    SyncGridToDraftGroups();
                    
                    MessageBox.Show($"Đã áp dụng thành công thông số:\n- Vỏ tủ: H{h}xW{w}xD{d} mm\n- Thanh cái chính: {bb} ({mat})\nvào bảng tính!\n\n*Lưu ý: Mở menu 'Xem chi tiết tính toán' ở dòng thanh cái để tính toán tiền.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    aiForm.Close();
                }
            };
            aiForm.Show();

            string loadingHtml = @"
            <html>
            <head>
                <meta http-equiv='X-UA-Compatible' content='IE=edge' />
                <style>
                    body { margin: 0; padding: 0; display: flex; justify-content: center; align-items: center; height: 100vh; background-color: #f8f9fa; font-family: 'Segoe UI', sans-serif; text-align: center; }
                    .container { margin-top: 250px; width: 100%; }
                    h3 { color: #333; font-size: 20px; font-weight: 500; margin-bottom: 10px; }
                    p { color: #777; font-size: 14px; margin-bottom: 20px; }
                    .progress-wrap { width: 300px; height: 8px; background: #e0e0e0; margin: 0 auto; border-radius: 4px; overflow: hidden; }
                    #progress-bar { width: 0%; height: 100%; background: #3498db; }
                </style>
            </head>
            <body>
                <div class='container'>
                    <h3>Đang gửi dữ liệu và chờ AI phân tích...</h3>
                    <p>Quá trình này có thể mất từ 10 - 20 giây. Vui lòng không đóng cửa sổ.</p>
                    <div class='progress-wrap'>
                        <div id='progress-bar'></div>
                    </div>
                </div>
                <script>
                    var w = 0;
                    setInterval(function() {
                        w += 2;
                        if (w > 100) w = 0;
                        document.getElementById('progress-bar').style.width = w + '%';
                    }, 50);
                </script>
            </body>
            </html>";
            webResult.Navigate("about:blank");
            while (webResult.ReadyState != WebBrowserReadyState.Complete) { Application.DoEvents(); }
            webResult.Document.Write(loadingHtml);
            Application.DoEvents();


            try
            {
                var productsList = new Newtonsoft.Json.Linq.JArray();
                Newtonsoft.Json.Linq.JObject cabinetObj = null;

                foreach (DataGridViewRow r in dgvSelectedItems.Rows)
                {
                    if (r.IsNewRow) continue;
                    string tHang = r.Cells["colTen"].Value?.ToString() ?? "";

                    if (tHang.StartsWith("Vỏ tủ", StringComparison.OrdinalIgnoreCase))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(tHang, @"H(\d+)xW(\d+)xD(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            cabinetObj = new Newtonsoft.Json.Linq.JObject();
                            cabinetObj["height_mm"] = int.Parse(match.Groups[1].Value);
                            cabinetObj["width_mm"] = int.Parse(match.Groups[2].Value);
                            
                            int d = int.Parse(match.Groups[3].Value);
                            if (d < 150) d = 200; // Nếu Depth < 150 (hoặc bằng 0) thì ép thành 200mm
                            cabinetObj["depth_mm"] = d;
                            
                            cabinetObj["type"] = "wall-mount";
                        }
                        continue;
                    }

                    if (tHang.StartsWith("Hệ thống đồng thanh cái", StringComparison.OrdinalIgnoreCase) || 
                        tHang.StartsWith("Phụ", StringComparison.OrdinalIgnoreCase) || 
                        tHang.StartsWith("Nhân công", StringComparison.OrdinalIgnoreCase))
                        continue;

                    int qty = 1;
                    if (r.Cells["colSoLuong"].Value != null) int.TryParse(r.Cells["colSoLuong"].Value.ToString(), out qty);

                    var pObj = new Newtonsoft.Json.Linq.JObject();
                    pObj["name"] = tHang;
                    pObj["quantity"] = qty;

                    var p = r.Tag as ECQ_Soft.Model.Products;
                    if (p != null)
                    {
                        if (!string.IsNullOrEmpty(p.Model)) pObj["model"] = p.Model;
                        if (!string.IsNullOrEmpty(p.HÃNG)) pObj["brand"] = p.HÃNG;

                        if (decimal.TryParse(p.Width, out decimal w)) pObj["width_mm"] = w;
                        if (decimal.TryParse(p.Height, out decimal h)) pObj["height_mm"] = h;
                        if (decimal.TryParse(p.Length, out decimal d)) pObj["depth_mm"] = d;

                        string irStr = p.Ir?.Replace("A", "");
                        if (decimal.TryParse(irStr, out decimal cur)) pObj["current_a"] = cur;
                    }
                    productsList.Add(pObj);
                }

                var inputObj = new Newtonsoft.Json.Linq.JObject();
                inputObj["cabinet"] = cabinetObj;
                inputObj["total_current_a"] = 100;
                inputObj["products"] = productsList;
                inputObj["constraints"] = new Newtonsoft.Json.Linq.JArray();

                string inputJson = inputObj.ToString(Newtonsoft.Json.Formatting.Indented);

                string prompt = @"Bạn là kỹ sư thiết kế tủ điện công nghiệp M&E, chuyên thiết kế tủ điện hạ thế theo IEC 61439.

Nhiệm vụ:
1. Nhận danh sách sản phẩm, số lượng, kích thước rộng/cao/sâu mm và dòng điện A nếu có.
2. Nếu không có kích thước vỏ tủ, hãy tự đề xuất kích thước vỏ tủ điện W x H x D theo kích thước tiêu chuẩn phù hợp. (LƯU Ý BẮT BUỘC: Chiều rộng W phải NHỎ HƠN 1000mm (W < 1000), chiều cao H phải NHỎ HƠN 2000mm (H < 2000). Tuyệt đối không thiết kế vỏ tủ có W >= 1000mm hoặc H >= 2000mm).
3. Tự xác định dòng tải tổng từ thiết bị chính hoặc trường current_a/ampere. Nếu tên có ""150AT"", hiểu dòng tải là 150A.
4. Phân loại thiết bị:
   - Tập trung hoàn toàn vào các thiết bị lắp trên panel bên trong tủ (panel): MCCB, MCB, contactor, relay, biến dòng, cầu chì, đồng hồ, đèn báo (các thiết bị hiển thị đo lường nếu có thì xếp chung lên khoang đo lường/khoang tổng của panel).
   - KHÔNG CẦN phân tích hay vẽ phần mặt cánh tủ (door). Bỏ qua hoàn toàn thiết kế mặt cánh.
   - Bỏ qua các dòng không phải thiết bị layout như phụ kiện, nhân công, dây điện, vật tư phụ.
5. Kiểm tra hình học: thiết bị có vượt vùng lắp đặt không, có đủ khoảng hở đi dây, máng cáp và clearance/creepage sơ bộ không.
6. Tối ưu layout thành các hàng ray/máng cáp, ưu tiên thiết bị lớn và nguồn tổng ở phía trên.

7. Tính đồng thanh cái chính (Main Busbar): Chọn quy cách Dày x Rộng mm (selected_size) và tổng chiều dài (total_length_mm) cho thanh cái chính phù hợp theo kích thước vỏ tủ và dòng định mức của Át Tổng.
   * QUY TẮC CHỌN KÍCH THƯỚC ĐỒNG THANH CÁI THEO DÒNG ĐIỆN (BẮT BUỘC):
     - Với MCCB / ACB Tổng có dòng định mức:
       + Dưới hoặc bằng 100A: chọn 15x3 (tức là 15x3mm, không chọn 5x15 hay 5x20)
       + 125A: chọn 20x3 hoặc 20x4
       + 150A đến 175A: chọn 20x4
       + 180A đến 230A: chọn 20x5
       + 250A: chọn 25x5
       + 300A đến 320A: chọn 30x5
       + 350A đến 500A: chọn 30x6
       + 600A đến 630A: chọn 30x10
       + 700A: chọn 40x8
       + 800A: chọn 40x10 hoặc 50x8
       + 1000A: chọn 50x10
       + 1200A đến 1250A: chọn 60x10
       + 1500A đến 1800A: chọn 80x10
       + 2000A: chọn 100x10
       + 2500A: chọn 120x10 hoặc 80x8 chập đôi
       + 3000A đến 3200A: chọn 100x8 chập đôi hoặc 100x10 chập đôi
     - Với ACB:
       + 600A đến 700A: chọn 50x6
       + 800A: chọn 50x8
   * LƯU Ý BẮT BUỘC: total_length_mm phải là TỔNG CHIỀU DÀI CỦA CẢ 4 THANH (L1, L2, L3, N) chạy dọc hoặc chạy ngang rồi nhân 4. Không cần tính khối lượng đồng (điền total_weight_kg = 0).
8. TÍNH CHI TIẾT THANH ĐỒNG NHÁNH (Branch Busbars) cho từng con át nhánh: xác định tên thiết bị (breaker_name), dòng định mức (current_a), quy cách đồng nhánh (busbar_size) và tổng chiều dài đấu nối (length_mm) từ thanh cái chính sang át nhánh.
   * QUY TẮC CHỌN KÍCH THƯỚC ĐỒNG NHÁNH THEO DÒNG ĐIỆN VÀ LOẠI ÁT (BẮT BUỘC):
     - Với các át nhánh loại tép (MCB):
       + Dòng từ 5A đến 32A: chọn 8x3 (tức là 8x3mm, không được chọn 3x10 hay 5x10)
       + Dòng từ 40A đến 63A: chọn 8x3
       + Dòng từ 80A đến 100A: chọn 10x3
     - Với các át nhánh loại khối (MCCB):
       + Dòng định mức dưới hoặc bằng 100A: chọn 15x3 (không được chọn 5x20)
       + 125A: chọn 20x3 hoặc 20x4
       + 150A đến 175A: chọn 20x4
       + 180A đến 230A: chọn 20x5
       + 250A: chọn 25x5 (không được chọn 5x40)
       + 300A đến 320A: chọn 30x5
       + 350A đến 500A: chọn 30x6
   * LƯU Ý BẮT BUỘC: length_mm phải là TỔNG CHIỀU DÀI CHO CẢ SỐ PHA (ví dụ: MCB 3P có chiều dài 1 pha là 100mm thì length_mm = 300). Điền weight_kg = 0.
9. VẼ PHÁC THẢO ASCII CỰC KỲ CHI TIẾT và cho ra ít nhất **2 OPTION bố trí khác nhau** trong trường `sketch_options`:
   - Quy cách máng cáp dọc/ngang (ví dụ: 60x60mm, 80x60mm hoặc 100x60mm) PHẢI được xác định rõ và vẽ gọn dưới dạng [M 80] hoặc [M 60] ở hai cột dọc để tiết kiệm diện tích vẽ. Đồng thời, PHẢI tính toán động và vẽ thêm ký hiệu chỉ khoảng cách khe hở thực tế ở hai bên từ thiết bị ra máng (ví dụ: vẽ <-30->, <-50->, hoặc <-75-> tùy theo khoảng hở thực tế tính toán được của từng loại tủ/thiết bị) giúp người dùng thấy rõ khoảng hở lắp đặt thực tế.
   - Việc chọn chiều dọc hay chiều ngang của layout phải phụ thuộc trực tiếp vào cách đi thanh cái đồng chính và nhánh (main & branch busbars) nhằm tối ưu chiều dài uốn đồng và khả năng chịu tải:
     + **Option 1 (Bố trí dọc)**: Thích hợp khi dòng tải lớn hoặc nhiều át nhánh. Bố trí 2 cột MCCB phân phối đối xứng qua hệ Busbar dọc ở giữa giúp phân bổ pha đều, đấu nối đồng nhánh ngắn nhất trực tiếp vào thanh cái dọc trung tâm.
     + **Option 2 (Bố trí ngang hoặc xếp ngang)**: Thích hợp khi dòng tải nhỏ hoặc ít thiết bị. Xếp ngang các MCB/MCCB thành các hàng ngang để sử dụng thanh cái chạy ngang phía trên/dưới (hoặc dùng cầu liên kết ngang), giúp tiết kiệm khối lượng đồng thanh cái chính và giảm chiều rộng tủ điện.
   - AI phải phân tích kỹ sự ảnh hưởng này và thuyết minh rõ trong phần `notes` hoặc `calculation_note` lý do lựa chọn bố trí dọc hay ngang cho từng Option tương ứng.
   - VỚI MỖI OPTION TRONG `sketch_options`, BẮT BUỘC TÍNH TOÁN RIÊNG KÍCH THƯỚC VỎ TỦ KHÁC NHAU theo logic sau (KHÔNG ĐƯỢC để 2 option có cabinet giống hệt nhau):
     + **Option 1 (Bố trí dọc)**: Thanh cái chạy dọc giữa + 2 cột át nhánh hai bên => tủ có xu hướng CAO HƠN và HẸP HƠN. Chiều rộng W chỉ cần đủ cho 2 cột át + máng + busbar dọc giữa (thường W = 600~800mm). Chiều cao H lớn hơn do xếp dọc nhiều hàng (thường H = 1200~1800mm). Chiều dài busbar = H * 4 pha.
     + **Option 2 (Bố trí ngang)**: Thanh cái chạy ngang phía trên + át nhánh xếp 1 hàng ngang => tủ có xu hướng THẤP HƠN và RỘNG HƠN. Chiều rộng W lớn hơn do xếp ngang nhiều thiết bị (thường W = 800~1000mm). Chiều cao H nhỏ hơn do chỉ cần 1~2 hàng (thường H = 800~1200mm). Chiều dài busbar = W * 4 pha.
     + **Nếu input đã có kích thước vỏ tủ**: Hãy dùng làm tham chiếu nhưng PHẢI điều chỉnh riêng cho từng option theo logic dọc/ngang ở trên (ví dụ: Option 1 giữ W gốc nhưng tăng H, Option 2 tăng W nhưng giảm H). Hai option KHÔNG ĐƯỢC có cabinet giống nhau.
     + Điền vào trường `cabinet` của từng object option đó kích thước đã tính riêng, và ghi rõ lý do vào `calculation_note` của busbar.
10. TÍNH KHOẢNG HỞ (CLEARANCE) CHI TIẾT cho từng phương án:
   - Với mỗi thiết bị quan trọng (ít nhất: át tổng, át nhánh đầu tiên bên trái, át nhánh đầu tiên bên phải), tính:
     + `gap_to_left_wall_mm`: khoảng hở từ cạnh trái thiết bị đến vách trái tủ (sau khi trừ máng cáp ~50mm)
     + `gap_to_right_wall_mm`: khoảng hở từ cạnh phải thiết bị đến vách phải tủ (sau khi trừ máng cáp ~50mm)
     + `gap_to_top_mm`: khoảng hở từ đỉnh thiết bị đến trần vùng phía trên
     + `gap_to_bottom_mm`: khoảng hở từ đáy thiết bị đến sàn vùng phía dưới (máng cáp ra)
     + `depth_gap_mm`: chiều sâu tủ trừ chiều sâu thiết bị trừ khoảng cách tiếp điểm (thường trừ 80mm cho ray DIN + dây sau)
   - Đánh giá `status`: ""ok"" nếu gap >= 50mm, ""tight"" nếu gap 25-49mm, ""danger"" nếu gap < 25mm
   - Ghi `warning_note` nếu status là tight hoặc danger (tiếng Việt, ngắn gọn)

11. ĐỀ XUẤT TỐI ƯU HÓA VỎ TỦ & ĐỒNG THANH CÁI: Điền các khuyến nghị cụ thể vào mảng `notes` ở ngoài cùng của JSON. Đề xuất rõ nên dùng Option nào (1 hay 2) và loại kích thước vỏ tủ nào để giúp tối ưu hóa không gian lắp ráp thiết bị, đảm bảo khoảng cách an toàn điện đồng thời tiết kiệm chiều dài đồng thanh cái và chi phí làm vỏ tủ nhất. Giải thích lý do kỹ thuật ngắn gọn.
12. QUY TẮC CĂN GIỮA, ĐỐI XỨNG & HIỂN THỊ ĐỦ KẾT NỐI THANH CÁI (BẮT BUỘC):
   - CĂN GIỮA THIẾT BỊ TỔNG (Center Incomer): Cầu dao tổng/Át tổng (MCCB TONG) bắt buộc phải nằm ở CHÍNH GIỮA khoang tổng (theo chiều ngang). Số lượng khoảng trắng đệm ở bên trái và bên phải của hộp thiết bị tổng phải bằng nhau tuyệt đối. Không được lệch sang bên nào.
   - ĐỐI XỨNG PHỤ KIỆN & HIỂN THỊ KHOẢNG CÁCH: Bố trí trái-phải hoàn toàn cân xứng. Vẽ ký hiệu khe hở thực tế (<-30->, <-50->, v.v.) ở giữa máng cáp và thiết bị.
   - THANH CÁI ĐỒNG VẼ LIỀN MẠCH: Đường vẽ đồng nhánh KHÔNG được dừng ở biên vùng thanh cái, phải vẽ nối dài chạm vào đúng thanh pha tương ứng (L1, L2, L3, N).

Mỗi Option phác thảo phải vẽ theo ĐÚNG format mẫu sau (bắt buộc thay đổi theo thông số thực tế):

FORMAT NỘI THẤT (panel) MẪU OPTION 1 (Bố trí dọc - busbar chạy DỌC GIỮA, át nhánh 2 CỘT HAI BÊN):
*** ĐẶC TRƯNG BẮT BUỘC: tủ CAO HẸP (W nhỏ, H lớn), busbar [ L1 ][ L2 ][ L3 ][ N ] dọc ở CHÍNH GIỮA, đồng nhánh = chạy NGANG nối thiết bị vào busbar. ***
+-------------------------------------------------------------+ ---
|               +----------------------------+               |  |
|               |      MCCB TONG 4P 250A     |               |  |  Khoang Tong
|               +----------------------------+               |  |  150mm
|                ||||   ||||       ||||   ||||               | ---
|                [ L1 ] [ L2 ]   [ L3 ] [  N ]              |  |
| [M80]<-50->| +------+ || |  | | | || +------+ |<-50->|[M80]|  |  Khoang
| [M80]<-50->| |MCB3P |==|  |  | |  |==|MCB3P | |<-50->|[M80]|  |  Phan Phoi
| [M80]<-50->| |nhanh1|==|==|  | |  |  |nhanh5| |<-50->|[M80]|  |  (xep DOC
| [M80]<-50->| +------+  || |  | | | | +------+ |<-50->|[M80]|  |   2 COT)
|====[ Mang cap ngang ]=============================|  |
| [M80]<-50->| +------+  ||     |  | | +------+ |<-50->|[M80]|  |
| [M80]<-50->| |MCB3P |  ||  |  |===|==|MCB3P | |<-50->|[M80]|  |
| [M80]<-50->| |nhanh2|  ||  |  |   |  |nhanh6| |<-50->|[M80]|  |
| [M80]<-50->| +------+  ||  |  |   |  +------+ |<-50->|[M80]|  |
|              KHOANG DAY TRONG                          |  |  Cap ra
+========================================================+ ---
|                   CHAN DE (100mm)                      |
+-------------------------------------------------------------+
|<------------------- W = 650mm (HEP) ------------------>|

FORMAT NỘI THẤT (panel) MẪU OPTION 2 (Bố trí ngang - busbar chạy NGANG PHÍA TRÊN, át nhánh XẾP HÀNG NGANG BÊN DƯỚI):
*** ĐẶC TRƯNG BẮT BUỘC: tủ THẤP RỘNG (W lớn, H nhỏ), busbar ===[L1]===[L2]===[L3]===[N]=== chạy NGANG dưới khoang tổng, đồng nhánh ||| chạy DỌC XUỐNG vào thiết bị. ***
+-------------------------------------------------------------------------------------+ ---
|                    +----------------------------+                                   |  |
|                    |      MCCB TONG 4P 250A     |                                   |  |  Khoang Tong
|                    +----------------------------+                                   |  |  150mm
|                     ||||        ||||    ||||                                        | ---
|                     vvvv        vvvv    vvvv                                        | ---
|[M80]|====[ L1 ]==========[ L2 ]==========[ L3 ]==========[ N ]====|[M80]|          |  |  He Busbar
|[M80]|=================================================================|[M80]|          |  |  Ngang (4 day)
|===================================================================================| ---
|[M80]<-50->| +---------+   +---------+   +---------+   +---------+ |<-50->|[M80]| |  |
|[M80]<-50->| |  MCB 3P |   |  MCB 3P |   |  MCB 3P |   |  MCB 3P | |<-50->|[M80]| |  |  Khoang
|[M80]<-50->| |  nhanh1 |   |  nhanh2 |   |  nhanh3 |   |  nhanh4 | |<-50->|[M80]| |  |  Phan Phoi
|[M80]<-50->| +---------+   +---------+   +---------+   +---------+ |<-50->|[M80]| |  |  (xep NGANG
|[M80]       |||            |||            |||            |||          |[M80]|        |  |   1 HANG)
|[M80]   (dong nhanh chay DOC XUONG tu busbar ngang vao cac at nhanhh) |[M80]|        |  |
|                    KHOANG DAY TRONG (CAP DONG LUC RA TAI)                          |  |  Cap ra
+===================================================================================+ ---
|                           CHAN DE (100mm)                                          |
+-------------------------------------------------------------------------------------+
|<----------------------------- W = 900mm (RONG) ---------------------------------->|

Quy tac ASCII panel (TOAN BO bat buoc):
- Chu thich zone ben phai |  | voi ten vung + kich thuoc mm
- Dong cuoi: |<------- XXXmm ------->|
- CHI dung ASCII: + - | = [ ] ( ) : khong dung unicode
- OPTION 1 (DOC): do rong ban ve NGAN hon, busbar dung DUNG O GIUA (cot ky tu | chay doc), dong nhanh = chay NGANG
- OPTION 2 (NGANG): do rong ban ve RONG hon, busbar la CAC HANG NGANG ===, dong nhanh ||| chay DOC XUONG
- KHONG DUOC ve 2 option co cung cau truc busbar (vi du ca 2 deu dung busbar doc hoac ca 2 deu dung busbar ngang)
- HIEN THI DAY DU THIET BI (BAT BUOC - KHONG DUOC BO SOT BAT KY THIET BI NAO):
  + BUOC 1: TRUOC KHI VE, hay dem tong so thiet bi trong danh sach input. Ghi nho con so nay.
  + BUOC 2: Moi thiet bi trong ban ve PHAI ghi DAY DU: ten thiet bi + so cuc + dong dinh muc + so luong (xN). Vi du: |MCCB 3P 250A x1|, |MCB 2P 16A x1|, |MC-50a x8|, |Ro le nhiet MT-63 x1|
  + BUOC 3: KHONG DUOC ghi chung chung nhu ""nhanh1"", ""nhanh2"". Phai ghi TEN THAT cua thiet bi tu danh sach input.
  + BUOC 4: Neu nhieu thiet bi CUNG TEN VA CUNG DONG DIEN, co the gom thanh 1 hop voi so luong: |MCB 2P 16A x4|. Nhung neu khac dong dien (16A va 20A) thi PHAI la 2 hop rieng biet.
  + BUOC 5: SAU KHI VE XONG, dem lai so thiet bi da ve (tinh theo tong so luong). Neu khong du so voi input -> PHAI bo sung them vao ban ve cho du.
  + Phai ve DU TAT CA thiet bi trong danh sach input, ke ca: contactor, khoi dong tu, ro le nhiet, bien dong ha the, dong ho do, den bao, cau chi...
  + Khoi dong tu (contactor) va ro le nhiet thuong di CAP voi nhau. Nen ve chung 1 hang: |MC-50a + MT-63| hoac 2 hop lien ke.
  + Voi cac thiet bi do luong (bien dong, dong ho, den bao): xep vao KHOANG TONG hoac khoang do luong rieng phia tren cua tu.
  + Kich thuoc hop thiet bi trong ban ve phai tuong doi phu hop voi kich thuoc thuc te (MCCB lon hon MCB, contactor lon hon relay...)
  + LUU Y DAC BIET: Cac thiet bi co so luong lon (vi du MC-50a x8) phai ve DU 8 hop (hoac gom thanh |MC-50a x8| nhung phai the hien chiem nhieu khong gian tuong ung). KHONG duoc ve 1 hop nho roi ghi x8 ma bo tri khong gian chi nhu 1 thiet bi.
- CAN COT THANG HANG (BAT BUOC - QUAN TRONG NHAT):
  + TAT CA cac dong trong ban ve PHAI co CUNG DO RONG (cung so ky tu). Dong nao ngan hon phai them khoang trang (space) de bang dong dai nhat.
  + Cac dau | doc (vien trai, vien phai, cot busbar, cot mang cap) PHAI nam o CUNG VI TRI COT tren moi dong. Vi du: dau | vien trai luon o cot 1, dau | vien phai luon o cot 80 (hoac bat ky so nao nhung PHAI NHAT QUAN).
  + Cac hop thiet bi o CUNG COT TRAI hoac CUNG COT PHAI phai co CUNG DO RONG. Vi du: neu hop trai rong 16 ky tu (|MCCB 3P 100A x1|) thi tat ca hop trai khac cung phai rong 16 ky tu, them space de padding: |MCB 2P 16A x1  |
  + Cach lam: Chon do rong hop = do rong cua TEN THIET BI DAI NHAT + 2 ky tu padding. Tat ca hop khac padding bang space cho du.
  + Cot mang cap [M60] hoac [M80] va cot khoang ho <-50-> PHAI luon o cung vi tri tren moi dong.
  + Cac duong ngang phan cach (====, +---+) phai co CUNG DO RONG voi cac dong khac.
  + Vi du DUNG (thang hang):
    | [M60]<-45->| +----------------+ || |  | +----------------+ |<-45->|[M60] |
    | [M60]<-45->| |MCCB 3P 100A x1 | || |  | |MCB 2P 16A x1   | |<-45->|[M60] |
    | [M60]<-45->| +----------------+ || |  | +----------------+ |<-45->|[M60] |
  + Vi du SAI (khong thang hang):
    | [M60]<-45->| +----------+ || | +------+ |<-45->|[M60] |
    | [M60]<-45->| |MCCB 100A| || | |MCB16A| |<-45->|[M60] |

13. BÀI HỌC VÀ MẪU THỰC TẾ LẮP RÁP (AI BẮT BUỘC PHẢI HIỂU VÀ ÁP DỤNG):
   - MẪU TỦ BỐ TRÍ DỌC TRUNG TÂM (Phổ biến nhất - tương tự các hình thực tế 1, 2, 3, 5, 6): Thiết kế át tổng đặt phía trên ở giữa. Hệ thanh cái chính (L1, L2, L3, N) chạy dọc từ trên xuống ở chính giữa. Các át nhánh xếp dọc thành 2 cột đối xứng hai bên. Uốn các thanh đồng nhánh nằm ngang nối từ thanh cái dọc chính vào các pha của át nhánh (dùng kí tự = hoặc - để thể hiện). Hai biên ngoài cùng là máng cáp dọc [M 80] hoặc [M 60].
   - MẪU TỦ BỐ TRÍ NGANG (Phổ biến thứ hai - tương tự hình thực tế 4): Thiết kế át tổng đặt trên, hệ thanh cái ngang chạy suốt bên dưới át tổng. Các át nhánh xếp thành một hàng nằm ngang bên dưới thanh cái ngang. Uốn các nhánh đồng chạy dọc (dùng kí tự | hoặc |||) đi từ thanh cái ngang phía trên xuống các pha đầu vào của át nhánh.

Yeu cau output:
- Chi tra ve JSON hop le, khong markdown, khong giai thich ngoai JSON.
- JSON phai co cac key: cabinet, validation, layout, busbar, materials, sketch_options, notes.
- sketch_options la mang cac option phac thao. Moi option co:
  + option_name: Ten phuong an.
  + cabinet: Object chua kich thuoc vo tu rieng cho option do, vi du: {""width_mm"": 800, ""height_mm"": 1200, ""depth_mm"": 300}.
  + busbar: Object chua thong so dong thanh cai chinh va danh sach dong nhanh chi tiet cua option do, vi du: {""selected_size"": ""5x30mm"", ""total_length_mm"": 1200, ""total_weight_kg"": 8.5, ""calculation_note"": ""Ghi chú..."", ""branches"": [{""breaker_name"": ""MCCB 3P 100A"", ""breaker_size"": ""130x75x78 mm"", ""current_a"": 100, ""busbar_size"": ""5x20mm"", ""length_mm"": 150, ""weight_kg"": 0.1335}]}.
  + panel: Chuỗi vẽ phác thảo panel nhiều dòng dạng STRING, sử dụng ký tự xuống dòng \n để ngăn cách các dòng. TUYỆT ĐỐI KHÔNG TRẢ VỀ DẠNG MẢNG (ARRAY OF STRINGS) VÀ KHÔNG ĐỔI TÊN THÀNH ""sketch"".
- Cac ket luan ky thuat viet bang tieng Viet.
- panel PHAI co day du: khoang tong, thanh dong busbar, mang cap ngang, MCCB nhanh, khoang day, chan de.

Schema mong muon:
{
  ""cabinet"": {""width_mm"":0,""height_mm"":0,""depth_mm"":0,""type"":"""",""source"":""""},
  ""validation"": {""is_feasible"":true,""issues"":[],""warnings"":[],""depth_check"":{""required_depth_mm"":0,""available_depth_mm"":0}},
  ""layout"": {""panel"":[]},
  ""busbar"": {""total_current_a"":0,""selected_size"":"""",""total_length_mm"":0,""total_weight_kg"":0,""calculation_note"":"""",""branches"":[{""breaker_name"":""MCCB 3P 100A (Tên át nhánh)"",""breaker_size"":""130x75x78 mm"",""current_a"":100,""busbar_size"":""5x20mm"",""cross_section_mm2"":25,""material"":""Cu"",""length_mm"":150,""weight_kg"":0.35,""route_note"":""Từ thanh cái chính đến đầu vào MCCB nhánh""}]},
  ""materials"": [],
  ""sketch_options"": [
     {
       ""option_name"": ""Option 1: Bố trí dọc"",
       ""cabinet"": {""width_mm"": 800, ""height_mm"": 1200, ""depth_mm"": 300},
       ""busbar"": {
         ""selected_size"": ""5x30mm"", 
         ""total_length_mm"": 1200, 
         ""total_weight_kg"": 8.5,
         ""calculation_note"": ""Mô tả cách đi busbar dọc ở giữa và nối ngắn sang hai bên"",
         ""branches"": [
           {""breaker_name"": ""MCCB 3P 100A"", ""breaker_size"": ""130x75x78 mm"", ""current_a"": 100, ""busbar_size"": ""5x20mm"", ""length_mm"": 150, ""weight_kg"": 0.1335}
         ]
       },
       ""clearances"": [
         {
           ""device_name"": ""MCCB Tổng 3P 1000A"",
           ""device_width_mm"": 220,
           ""device_height_mm"": 300,
           ""device_depth_mm"": 105,
           ""gap_to_left_wall_mm"": 80,
           ""gap_to_right_wall_mm"": 80,
           ""gap_to_top_mm"": 50,
           ""gap_to_bottom_mm"": 150,
           ""depth_gap_mm"": 95,
           ""status"": ""ok"",
           ""warning_note"": """"
         },
         {
           ""device_name"": ""MCCB Nhánh 3P 125A (trái ngoài cùng)"",
           ""device_width_mm"": 100,
           ""device_height_mm"": 150,
           ""device_depth_mm"": 78,
           ""gap_to_left_wall_mm"": 35,
           ""gap_to_right_wall_mm"": 300,
           ""gap_to_top_mm"": 200,
           ""gap_to_bottom_mm"": 250,
           ""depth_gap_mm"": 142,
           ""status"": ""tight"",
           ""warning_note"": ""Khoảng hở trái chỉ 35mm, khó đi dây và xiết đầu cốt""
         }
       ],
       ""panel"": ""Bản vẽ panel Option 1""
     },
     {
       ""option_name"": ""Option 2: Bố trí ngang"",
       ""cabinet"": {""width_mm"": 600, ""height_mm"": 1000, ""depth_mm"": 250},
       ""busbar"": {
         ""selected_size"": ""5x30mm"", 
         ""total_length_mm"": 800, 
         ""total_weight_kg"": 5.7,
         ""calculation_note"": ""Mô tả cách đi busbar chạy ngang phía trên"",
         ""branches"": [
           {""breaker_name"": ""MCCB 3P 100A"", ""current_a"": 100, ""busbar_size"": ""5x20mm"", ""length_mm"": 100, ""weight_kg"": 0.089}
         ]
       },
       ""clearances"": [
         {
           ""device_name"": ""MCCB Tổng"",
           ""device_width_mm"": 220, ""device_height_mm"": 300, ""device_depth_mm"": 105,
           ""gap_to_left_wall_mm"": 60, ""gap_to_right_wall_mm"": 60,
           ""gap_to_top_mm"": 50, ""gap_to_bottom_mm"": 100, ""depth_gap_mm"": 65,
           ""status"": ""ok"", ""warning_note"": """"
         }
       ],
       ""panel"": ""Bản vẽ panel Option 2""
     }
  ],
  ""notes"": []
}

Du lieu dau vao:
{inputJson}

Yeu cau bo sung tu nguoi dung:
Vui long phan tich bo tri thiet bi va tinh toan chi tiet cho tu dien nay.".Replace("{inputJson}", inputJson);

                // Nạp danh sách các API Keys từ môi trường hoặc file cục bộ (hỗ trợ nhiều dòng/nhiều key)
                System.Collections.Generic.List<string> apiKeys = new System.Collections.Generic.List<string>();

                // 1. Kiểm tra biến môi trường
                string envKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (!string.IsNullOrEmpty(envKey))
                {
                    var keys = envKey.Split(new char[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var k in keys)
                    {
                        string tk = k.Trim();
                        if (!string.IsNullOrEmpty(tk)) apiKeys.Add(tk);
                    }
                }

                // 2. Kiểm tra file cấu hình cục bộ gemini_key.txt (cho phép nhiều dòng, mỗi dòng 1 key)
                string keyFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gemini_key.txt");
                if (System.IO.File.Exists(keyFile))
                {
                    var lines = System.IO.File.ReadAllLines(keyFile);
                    foreach (var line in lines)
                    {
                        string tk = line.Trim();
                        // Bỏ qua dòng trống, dòng chú thích bắt đầu bằng # hoặc //
                        if (string.IsNullOrEmpty(tk) || tk.StartsWith("#") || tk.StartsWith("//"))
                            continue;
                        apiKeys.Add(tk);
                    }
                }

                // 3. Fallback mặc định
                if (apiKeys.Count == 0)
                {
                    apiKeys.Add("AIzaSyBSaGzGNuzE9xja0OXzDGNz1GGj1ecAsWg");
                    apiKeys.Add("AIzaSyD-MveNSsnSEtEg8OvqOCR1EwDg3KubIvw");
                }

                string[] modelsToTry = new string[] { "gemini-2.5-flash", "gemini-2.0-flash", "gemini-2.5-pro", "gemini-flash-latest", "gemini-pro-latest", "gemini-2.0-flash-lite" };

                string resBody = "";
                bool success = false;
                string usedModel = "";
                string lastError = "";

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(3);
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    
                    foreach (var apiKey in apiKeys)
                    {
                        if (client.DefaultRequestHeaders.Contains("x-goog-api-key"))
                        {
                            client.DefaultRequestHeaders.Remove("x-goog-api-key");
                        }
                        client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
                        
                        bool keyExhausted = false;

                        foreach (var model in modelsToTry)
                        {
                            try
                            {
                                usedModel = model;
                                var geminiPayload = new Newtonsoft.Json.Linq.JObject();
                                
                                var genConfig = new Newtonsoft.Json.Linq.JObject();
                                genConfig["temperature"] = 0.2;
                                genConfig["responseMimeType"] = "application/json";
                                
                                if (model.Contains("2.0") || model.Contains("2.5"))
                                {
                                    var thinkingConfig = new Newtonsoft.Json.Linq.JObject();
                                    thinkingConfig["thinkingBudget"] = 0;
                                    genConfig["thinkingConfig"] = thinkingConfig;
                                }
                                
                                geminiPayload["generationConfig"] = genConfig;

                                var partsArray = new Newtonsoft.Json.Linq.JArray();
                                var textPart = new Newtonsoft.Json.Linq.JObject();
                                textPart["text"] = prompt;
                                partsArray.Add(textPart);

                                var contentObj = new Newtonsoft.Json.Linq.JObject();
                                contentObj["role"] = "user";
                                contentObj["parts"] = partsArray;

                                var contentsArray = new Newtonsoft.Json.Linq.JArray();
                                contentsArray.Add(contentObj);

                                geminiPayload["contents"] = contentsArray;

                                string geminiJson = geminiPayload.ToString();
                                var content = new System.Net.Http.StringContent(geminiJson, Encoding.UTF8, "application/json");
                                string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
                                
                                var response = await client.PostAsync(url, content);
                                resBody = await response.Content.ReadAsStringAsync();

                                if (response.IsSuccessStatusCode)
                                {
                                    success = true;
                                    break;
                                }
                                else
                                {
                                    int statusCode = (int)response.StatusCode;
                                    lastError = $"Key {apiKey.Substring(0, Math.Min(apiKey.Length, 8))}... - Model {model} - HTTP {statusCode}: {resBody}";
                                    System.Diagnostics.Debug.WriteLine(lastError);
                                    
                                    // Nếu bị hạn ngạch (429) hoặc sai key (401/403), chuyển sang Key tiếp theo ngay lập tức
                                    if (statusCode == 429 || statusCode == 401 || statusCode == 403)
                                    {
                                        keyExhausted = true;
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                lastError = $"Key {apiKey.Substring(0, Math.Min(apiKey.Length, 8))}... - Model {model} - Exception: {ex.Message}";
                                System.Diagnostics.Debug.WriteLine(lastError);
                            }
                        }
                        
                        if (success)
                        {
                            break;
                        }
                    }

                    if (!success)
                    {
                        throw new Exception($"Gemini API Error: Toàn bộ model hoặc danh sách API Key thử nghiệm đều thất bại/hết hạn ngạch. Chi tiết lỗi cuối cùng: {lastError}");
                    }
                }
                    
                    try 
                    {
                        var parsedJson = Newtonsoft.Json.Linq.JObject.Parse(resBody);
                        var textVal = parsedJson["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                        if (string.IsNullOrEmpty(textVal))
                        {
                            throw new Exception("Gemini không trả về nội dung hợp lệ.");
                        }

                        string cleanJson = textVal.Trim();
                        if (cleanJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase)) {
                            cleanJson = cleanJson.Substring(7);
                        } else if (cleanJson.StartsWith("```")) {
                            cleanJson = cleanJson.Substring(3);
                        }
                        if (cleanJson.EndsWith("```")) {
                            cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                        }
                        cleanJson = cleanJson.Trim();

                        var dataObj = Newtonsoft.Json.Linq.JObject.Parse(cleanJson);

                        StringBuilder html = new StringBuilder();
                        html.Append("<html><head><meta http-equiv='X-UA-Compatible' content='IE=edge' /><meta charset='utf-8'>");
                        html.Append("<style>");
                        html.Append("@import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');");
                        html.Append("body { font-family: 'Inter', 'Segoe UI', Roboto, sans-serif; background-color: #f1f5f9; color: #1e293b; margin: 0; padding: 24px; line-height: 1.5; } ");
                        html.Append(".container { max-width: 960px; margin: auto; } ");
                        
                        // Header
                        html.Append(".header { background: linear-gradient(135deg, #1e3a8a 0%, #3b82f6 100%); color: #ffffff; padding: 32px; border-radius: 16px; margin-bottom: 24px; box-shadow: 0 4px 20px -2px rgba(59, 130, 246, 0.15); } ");
                        html.Append(".header h1 { margin: 0; font-size: 26px; font-weight: 700; letter-spacing: -0.5px; } ");
                        html.Append(".header p { margin: 8px 0 0 0; opacity: 0.9; font-size: 14px; } ");
                        
                        // Stats Grid
                        html.Append(".stats-grid { display: flex; gap: 16px; margin-bottom: 24px; } ");
                        html.Append(".stat-card { flex: 1; background: #ffffff; padding: 20px; border-radius: 12px; border: 1px solid #e2e8f0; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); } ");
                        html.Append(".stat-label { font-size: 12px; font-weight: 600; text-transform: uppercase; color: #64748b; letter-spacing: 0.5px; } ");
                        html.Append(".stat-value { font-size: 20px; font-weight: 700; color: #0f172a; margin-top: 4px; } ");
                        html.Append(".stat-desc { font-size: 12px; color: #64748b; margin-top: 4px; } ");
                        
                        // Cards & Sections
                        html.Append(".card { background: #ffffff; padding: 24px; border-radius: 16px; border: 1px solid #e2e8f0; margin-bottom: 24px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); } ");
                        html.Append("h2 { font-size: 18px; font-weight: 700; color: #0f172a; margin-top: 0; margin-bottom: 16px; padding-bottom: 8px; border-bottom: 2px solid #f1f5f9; } ");
                        
                        // Feasibility alerts
                        html.Append(".alert { padding: 16px; border-radius: 12px; display: flex; align-items: center; gap: 12px; margin-bottom: 20px; } ");
                        html.Append(".alert-success { background-color: #f0fdf4; border: 1px solid #bbf7d0; color: #166534; } ");
                        html.Append(".alert-danger { background-color: #fef2f2; border: 1px solid #fecaca; color: #991b1b; } ");
                        html.Append(".alert-title { font-weight: 700; font-size: 15px; } ");
                        
                        // Tables
                        html.Append("table { width: 100%; border-collapse: collapse; margin-top: 8px; font-size: 14px; } ");
                        html.Append("th, td { padding: 12px 16px; text-align: left; border-bottom: 1px solid #e2e8f0; } ");
                        html.Append("th { background-color: #f8fafc; color: #475569; font-weight: 600; } ");
                        html.Append("tr:hover { background-color: #f8fafc; } ");
                        
                        // Tab system
                        html.Append(".tabs-header { display: flex; gap: 8px; border-bottom: 2px solid #e2e8f0; margin-bottom: 20px; padding-bottom: 8px; } ");
                        html.Append(".tab-btn { display: inline-block; padding: 10px 20px; border-radius: 8px; border: none; font-family: inherit; font-size: 14px; font-weight: 600; cursor: pointer; background: #e2e8f0; color: #475569; transition: all 0.2s; text-align: center; user-select: none; } ");
                        html.Append(".tab-btn:hover { background: #cbd5e1; color: #1e293b; } ");
                        html.Append(".tab-btn.active { background: #3b82f6; color: #ffffff; box-shadow: 0 4px 12px rgba(59, 130, 246, 0.25); } ");
                        html.Append(".tab-content { display: none; } ");
                        
                        // Pre tags for drawings
                        html.Append("pre { background: #0f172a; color: #e2e8f0; padding: 20px; border-radius: 12px; overflow-x: auto; font-family: 'Consolas', 'Courier New', monospace; font-size: 13px; line-height: 1.4; border: 1px solid #1e293b; box-shadow: inset 0 2px 4px rgba(0,0,0,0.3); } ");
                        html.Append(".badge { display: inline-block; padding: 4px 8px; font-size: 11px; font-weight: 700; border-radius: 6px; } ");
                        html.Append(".badge-primary { background-color: #dbeafe; color: #1e40af; } ");
                        html.Append(".badge-secondary { background-color: #f1f5f9; color: #475569; } ");
                        html.Append(".badge-success { background-color: #dcfce7; color: #15803d; } ");
                        
                        // Notes list
                        html.Append(".notes-list { list-style-type: none; padding-left: 0; margin: 0; } ");
                        html.Append(".notes-list li { background: #f8fafc; margin-bottom: 8px; padding: 12px 16px; border-left: 4px solid #3b82f6; border-radius: 0 8px 8px 0; font-size: 14px; color: #334155; } ");
                        html.Append(".option-specs { display: flex; gap: 16px; margin-bottom: 16px; flex-wrap: wrap; } ");
                        html.Append(".option-spec-card { flex: 1; min-width: 220px; background: #f8fafc; border: 1px solid #e2e8f0; padding: 12px 16px; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.05); } ");
                        html.Append(".option-spec-label { font-size: 11px; text-transform: uppercase; color: #64748b; font-weight: 600; margin-bottom: 4px; letter-spacing: 0.5px; } ");
                        html.Append(".option-spec-value { font-size: 15px; font-weight: 700; color: #0f172a; } ");
                        html.Append("</style>");
                        
                        // Lấy danh sách sketchOptions sớm hơn để phục vụ Stats Bar và Javascript
                        sketchOptions = dataObj["sketch_options"] as Newtonsoft.Json.Linq.JArray;
                        if (sketchOptions == null && dataObj["sketch"] != null)
                        {
                            sketchOptions = new Newtonsoft.Json.Linq.JArray();
                            var fallbackOpt = new Newtonsoft.Json.Linq.JObject();
                            fallbackOpt["option_name"] = "Thiết kế tiêu chuẩn";
                            fallbackOpt["panel"] = dataObj["sketch"]?["panel"];
                            fallbackOpt["cabinet"] = dataObj["cabinet"];
                            fallbackOpt["busbar"] = dataObj["busbar"];
                            sketchOptions.Add(fallbackOpt);
                        }

                        var cabinet = dataObj["cabinet"] as Newtonsoft.Json.Linq.JObject;
                        var validation = dataObj["validation"] as Newtonsoft.Json.Linq.JObject;
                        var globalBusbar = dataObj["busbar"] as Newtonsoft.Json.Linq.JObject;

                        // 1. Khởi tạo giá trị Stats ban đầu từ Option 1 nếu có
                        string cabW = "---";
                        string cabH = "---";
                        string cabD = "---";
                        string cabSrc = "Cung cấp bởi User";
                        string loadA = "---";
                        string mainBbSize = "---";
                        string totalLen = "---";

                        if (sketchOptions != null && sketchOptions.Count > 0)
                        {
                            var optCabinet = sketchOptions[0]["cabinet"] as Newtonsoft.Json.Linq.JObject;
                            var optBusbar = sketchOptions[0]["busbar"] as Newtonsoft.Json.Linq.JObject;

                            cabW = optCabinet?["width_mm"]?.ToString() ?? "---";
                            cabH = optCabinet?["height_mm"]?.ToString() ?? "---";
                            cabD = optCabinet?["depth_mm"]?.ToString() ?? "---";
                            cabSrc = optCabinet?["source"]?.ToString() == "recommended" ? "Đề xuất bởi AI" : "Cung cấp bởi User";

                            loadA = optBusbar?["total_current_a"]?.ToString() ?? "---";
                            mainBbSize = optBusbar?["selected_size"]?.ToString() ?? "---";
                            totalLen = optBusbar?["total_length_mm"]?.ToString() ?? "---";
                        }
                        else
                        {
                            cabW = cabinet?["width_mm"]?.ToString() ?? "---";
                            cabH = cabinet?["height_mm"]?.ToString() ?? "---";
                            cabD = cabinet?["depth_mm"]?.ToString() ?? "---";
                            cabSrc = cabinet?["source"]?.ToString() == "recommended" ? "Đề xuất bởi AI" : "Cung cấp bởi User";

                            loadA = globalBusbar?["total_current_a"]?.ToString() ?? "---";
                            mainBbSize = globalBusbar?["selected_size"]?.ToString() ?? "---";
                            totalLen = globalBusbar?["total_length_mm"]?.ToString() ?? "---";
                        }

                        // JS Tab Switcher kết hợp cập nhật thông số Stats Bar động
                        var jsData = new Newtonsoft.Json.Linq.JArray();
                        if (sketchOptions != null)
                        {
                            for (int i = 0; i < sketchOptions.Count; i++)
                            {
                                var optCabinet = sketchOptions[i]["cabinet"] as Newtonsoft.Json.Linq.JObject;
                                var optBusbar = sketchOptions[i]["busbar"] as Newtonsoft.Json.Linq.JObject;

                                string optCabW = optCabinet?["width_mm"]?.ToString() ?? "---";
                                string optCabH = optCabinet?["height_mm"]?.ToString() ?? "---";
                                string optCabD = optCabinet?["depth_mm"]?.ToString() ?? "---";
                                string optCabSrc = optCabinet?["source"]?.ToString() == "recommended" ? "Đề xuất bởi AI" : "Cung cấp bởi User";

                                string optLoadA = optBusbar?["total_current_a"]?.ToString() ?? loadA;
                                string optBbSize = optBusbar?["selected_size"]?.ToString() ?? "---";
                                string optBbLen = optBusbar?["total_length_mm"]?.ToString() ?? "---";

                                var item = new Newtonsoft.Json.Linq.JObject();
                                item["cabDims"] = $"{optCabH}x{optCabW}x{optCabD}";
                                item["cabDesc"] = optCabSrc;
                                item["incomerLoad"] = optLoadA;
                                item["mainBbSize"] = optBbSize;
                                item["totalLen"] = optBbLen;
                                jsData.Add(item);
                            }
                        }
                        string optionsDataJson = jsData.ToString(Newtonsoft.Json.Formatting.None);

                        // Script sẽ được chèn cuối <body> để DOM render xong trước
                        
                        html.Append("</head><body><div class='container'>");

                        // Header Banner
                        html.Append("<div class='header'>");
                        html.Append("<h1>BÁO CÁO PHÂN TÍCH TỦ ĐIỆN VÀ THIẾT KẾ LAYOUT AI</h1>");
                        html.Append("<p>Hệ thống tự động tính toán kích thước tủ, đồng thanh cái và bố trí thiết bị dựa trên AI</p>");
                        html.Append("</div>");

                        // 1. Stats Bar (2 Cards: Cabinet & Incomer/Busbar)
                        html.Append("<div class='stats-grid'>");
                        
                        // Cabinet Card
                        html.Append("<div class='stat-card'>");
                        html.Append("<div class='stat-label'>Kích Thước Vỏ Tủ</div>");
                        html.Append($"<div class='stat-value' id='stat-cabinet-dims'>{cabH}x{cabW}x{cabD} <span style='font-size:12px; font-weight:normal; color:#64748b;'>mm</span></div>");
                        html.Append($"<div class='stat-desc' id='stat-cabinet-desc'>{cabSrc}</div>");
                        html.Append("</div>");

                        // Incomer & Main Busbar Card
                        html.Append("<div class='stat-card'>");
                        html.Append("<div class='stat-label'>Thông Số Incomer & Thanh Cái</div>");
                        html.Append($"<div class='stat-value' id='stat-incomer-load'>{loadA} A</div>");
                        html.Append($"<div class='stat-desc' id='stat-incomer-desc'>Thanh cái chính: {mainBbSize} (L={totalLen} mm)</div>");
                        html.Append("</div>");
                        
                        html.Append("</div>");

                        // Hướng dẫn lựa chọn dọc vs ngang
                        html.Append("<div class='card' style='background: #fdfbf7; border: 1px solid #f59e0b; margin-bottom: 24px;'>");
                        html.Append("  <h3 style='margin-top:0; color:#d97706; font-size:15px;'>💡 PHƯƠNG PHÁP LỰA CHỌN PHƯƠNG ÁN (DỌC VS NGANG)</h3>");
                        html.Append("  <p style='font-size:13px; margin:0 0 12px 0; color:#451a03;'>Tùy chọn bố trí thiết bị cần tối ưu dựa trên dòng điện định mức và không gian lắp đặt:</p>");
                        html.Append("  <table style='font-size:12px; margin-top:5px; background:#ffffff; border-radius:8px; width:100%; border-collapse:collapse;'>");
                        html.Append("    <thead>");
                        html.Append("      <tr style='background:#fef3c7; color:#451a03;'>");
                        html.Append("        <th style='padding:8px 12px; font-weight:700;'>Tiêu chí</th>");
                        html.Append("        <th style='padding:8px 12px; font-weight:700;'>Phương án Dọc (Dọc giữa)</th>");
                        html.Append("        <th style='padding:8px 12px; font-weight:700;'>Phương án Ngang (Xếp ngang)</th>");
                        html.Append("      </tr>");
                        html.Append("    </thead>");
                        html.Append("    <tbody>");
                        html.Append("      <tr>");
                        html.Append("        <td style='padding:8px 12px;'><b>Dòng định mức</b></td>");
                        html.Append("        <td style='padding:8px 12px;'>Phù hợp dòng lớn (≥ 250A), tản nhiệt tốt, chia pha đều.</td>");
                        html.Append("        <td style='padding:8px 12px;'>Thích hợp dòng nhỏ (&lt; 250A) để lắp gọn.</td>");
                        html.Append("      </tr>");
                        html.Append("      <tr>");
                        html.Append("        <td style='padding:8px 12px;'><b>Đồng thanh cái</b></td>");
                        html.Append("        <td style='padding:8px 12px;'>Busbar chính dọc giữa, uốn đồng nhánh ngắn nhất sang hai bên.</td>");
                        html.Append("        <td style='padding:8px 12px;'>Busbar chính ngang (trên/dưới), đồng nhánh dài hơn.</td>");
                        html.Append("      </tr>");
                        html.Append("      <tr>");
                        html.Append("        <td style='padding:8px 12px;'><b>Mạch nhánh</b></td>");
                        html.Append("        <td style='padding:8px 12px;'>Nhiều át nhánh (&gt; 6 cái) xếp đối xứng hai bên.</td>");
                        html.Append("        <td style='padding:8px 12px;'>Ít át nhánh, xếp 1 hàng ngang duy nhất.</td>");
                        html.Append("      </tr>");
                        html.Append("      <tr>");
                        html.Append("        <td style='padding:8px 12px;'><b>Ưu điểm kích thước</b></td>");
                        html.Append("        <td style='padding:8px 12px;'>Tối ưu chiều rộng (tủ thon cao, tiết kiệm bề ngang).</td>");
                        html.Append("        <td style='padding:8px 12px;'>Tối ưu chiều cao (tủ lùn rộng, phù hợp trần thấp).</td>");
                        html.Append("      </tr>");
                        html.Append("    </tbody>");
                        html.Append("  </table>");
                        html.Append("</div>");

                        // 2. Feasibility & Validation
                        if (validation != null)
                        {
                            bool isFeasible = validation["is_feasible"]?.ToObject<bool>() ?? true;
                            if (isFeasible)
                            {
                                html.Append("<div class='alert alert-success'>");
                                html.Append("<div><div class='alert-title'>✓ LAYOUT HỢP LỆ & KHẢ THI</div>");
                                html.Append("<div style='font-size:13px; margin-top:2px;'>Bố trí thiết bị đảm bảo khoảng hở hình học và khả năng kết nối thanh cái.</div></div>");
                                html.Append("</div>");
                            }
                            else
                            {
                                html.Append("<div class='alert alert-danger'>");
                                html.Append("<div><div class='alert-title'>⚠ CẢNH BÁO: BỐ TRÍ KHÔNG KHẢ THI</div>");
                                
                                var issues = validation["issues"] as Newtonsoft.Json.Linq.JArray;
                                if (issues != null && issues.Count > 0)
                                {
                                    html.Append("<ul style='margin: 8px 0 0 0; padding-left: 20px; font-size:13px;'>");
                                    foreach (var issue in issues)
                                    {
                                        html.Append($"<li>{System.Net.WebUtility.HtmlEncode(issue.ToString())}</li>");
                                    }
                                    html.Append("</ul>");
                                }
                                html.Append("</div></div>");
                            }
                        }





                        if (sketchOptions != null && sketchOptions.Count > 0)
                        {
                            html.Append("<h2 style='font-size:18px; font-weight:700; color:#0f172a; margin:24px 0 16px 0; padding-bottom:8px; border-bottom:2px solid #e2e8f0;'>Bản Vẽ Phác Thảo Trực Quan (Layout Options)</h2>");

                            // Hiển thị tất cả option liền nhau, mỗi cái trong 1 card riêng
                            for (int i = 0; i < sketchOptions.Count; i++)
                            {
                                string optName = sketchOptions[i]["option_name"]?.ToString() ?? $"Phương án {i + 1}";
                                string panelSketch = sketchOptions[i]["panel"]?.ToString() ?? "Không có bản vẽ panel.";

                                // --- POST-PROCESS: Tự động căn chỉnh ASCII art thẳng hàng ---
                                panelSketch = AlignPanelDrawing(panelSketch, optName);

                                var optCabinet = sketchOptions[i]["cabinet"] as Newtonsoft.Json.Linq.JObject;
                                var optBusbar  = sketchOptions[i]["busbar"]  as Newtonsoft.Json.Linq.JObject;

                                string optCabW   = optCabinet?["width_mm"]?.ToString()  ?? "---";
                                string optCabH   = optCabinet?["height_mm"]?.ToString() ?? "---";
                                string optCabD   = optCabinet?["depth_mm"]?.ToString()  ?? "---";
                                string optBbSize = optBusbar?["selected_size"]?.ToString()    ?? "---";
                                string optBbLen  = optBusbar?["total_length_mm"]?.ToString()  ?? "---";
                                string optLoadA  = optBusbar?["total_current_a"]?.ToString()  ?? loadA;

                                // Badge màu xen kế
                                string[] badgeColors = new[] { "#3b82f6", "#10b981", "#f59e0b", "#8b5cf6" };
                                string badgeColor = badgeColors[i % badgeColors.Length];

                                html.Append("<div class='card' style='margin-bottom:24px;'>");

                                // Tiêu đề Option
                                html.Append($"<div style='display:flex; align-items:center; gap:12px; margin-bottom:16px; padding-bottom:12px; border-bottom:2px solid #f1f5f9;'>");
                                html.Append($"<span style='background:{badgeColor}; color:#fff; font-size:13px; font-weight:700; padding:4px 14px; border-radius:20px; white-space:nowrap;'>Phương án {i + 1}</span>");
                                html.Append($"<span style='font-size:16px; font-weight:700; color:#0f172a;'>{System.Net.WebUtility.HtmlEncode(optName)}</span>");
                                html.Append("</div>");

                                // 4 Spec Cards đều nhau
                                string optTotalWeight = optBusbar?["total_weight_kg"]?.ToString() ?? "---";
                                var optBranches0 = optBusbar?["branches"] as Newtonsoft.Json.Linq.JArray;
                                string branchCount = optBranches0 != null ? optBranches0.Count.ToString() : "---";

                                html.Append("<div class='option-specs'>");
                                html.Append("  <div class='option-spec-card'>");
                                html.Append("    <div class='option-spec-label'>&#127968; K&#237;ch Th&#432;&#7899;c V&#7887; T&#7911;</div>");
                                html.Append($"    <div class='option-spec-value'>{System.Net.WebUtility.HtmlEncode(optCabH)}&times;{System.Net.WebUtility.HtmlEncode(optCabW)}&times;{System.Net.WebUtility.HtmlEncode(optCabD)}</div>");
                                html.Append("    <div style='font-size:11px;color:#94a3b8;margin-top:2px;'>H &times; W &times; D (mm)</div>");
                                html.Append("  </div>");
                                html.Append("  <div class='option-spec-card'>");
                                html.Append("    <div class='option-spec-label'>&#9889; Thanh C&#225;i Ch&#237;nh</div>");
                                html.Append($"    <div class='option-spec-value'>{System.Net.WebUtility.HtmlEncode(optBbSize)}</div>");
                                html.Append($"    <div style='font-size:11px;color:#94a3b8;margin-top:2px;'>D&#224;i {System.Net.WebUtility.HtmlEncode(optBbLen)} mm &nbsp;|&nbsp; {System.Net.WebUtility.HtmlEncode(optTotalWeight)} kg</div>");
                                html.Append("  </div>");
                                html.Append("  <div class='option-spec-card'>");
                                html.Append("    <div class='option-spec-label'>&#128200; T&#7893;ng D&#242;ng T&#7843;i</div>");
                                html.Append($"    <div class='option-spec-value'>{System.Net.WebUtility.HtmlEncode(optLoadA)} A</div>");
                                html.Append("    <div style='font-size:11px;color:#94a3b8;margin-top:2px;'>D&#242;ng &#273;&#7883;nh m&#7913;c t&#7893;ng h&#7907;p</div>");
                                html.Append("  </div>");
                                html.Append("  <div class='option-spec-card'>");
                                html.Append("    <div class='option-spec-label'>&#128268; S&#7889; M&#7841;ch Nh&#225;nh</div>");
                                html.Append($"    <div class='option-spec-value'>{branchCount}</div>");
                                html.Append("    <div style='font-size:11px;color:#94a3b8;margin-top:2px;'>&#193;t nh&#225;nh c&#243; thanh &#273;&#7891;ng ri&#234;ng</div>");
                                html.Append("  </div>");
                                html.Append("</div>");

                                // Bản vẽ ASCII
                                html.Append($"<pre>{System.Net.WebUtility.HtmlEncode(panelSketch)}</pre>");                                     // Nút áp dụng phương án
                                 string optBbMat = optBusbar?["material"]?.ToString() ?? "Cu";
                                 html.Append($"<div style='margin-top:20px; text-align:right;'>");
                                 html.Append($"  <a href='app://apply-option?opt_idx={i}&w={optCabW}&h={optCabH}&d={optCabD}&bb={optBbSize}&bb_len={optBbLen}&mat={System.Net.WebUtility.UrlEncode(optBbMat)}' style='display:inline-block; background-color:#10b981; color:#ffffff; text-decoration:none; padding:10px 24px; border-radius:8px; font-size:14px; font-weight:600; box-shadow:0 4px 6px -1px rgba(16,185,129,0.3); transition:all 0.2s;'>✓ Áp dụng phương án {i + 1}</a>");
                                 html.Append($"</div>");

                                // Bảng thanh cái 7 cột chi tiết
                                if (optBusbar != null)
                                {
                                    html.Append("<div style='margin-top:20px; border-top:1px solid #e2e8f0; padding-top:16px;'>");
                                    html.Append("<h3 style='font-size:14px; font-weight:700; color:#0f172a; margin-bottom:4px;'>T&#237;nh To&#225;n Quy C&#225;ch &#272;&#7891;ng Thanh C&#225;i Chi Ti&#7871;t</h3>");
                                    html.Append("<p style='font-size:12px; color:#64748b; margin:0 0 10px 0;'>Ti&#7871;t di&#7879;n = I&#8260;4 (mm&#178;) l&#224;m tr&#242;n l&#234;n, kh&#7889;i l&#432;&#7907;ng = L(m) &times; ti&#7871;t di&#7879;n(mm&#178;) &times; 8.9(g/cm&#179;)</p>");
                                    html.Append("<table><thead><tr>");
                                    html.Append("<th style='min-width:160px'>Thi&#7871;t b&#7883; / &#193;t nh&#225;nh</th>");
                                    html.Append("<th style='width:120px;text-align:center'>K&#237;ch th&#432;&#7899;c &#193;t (HxWxD)</th>");
                                    html.Append("<th style='width:80px;text-align:center'>D&#242;ng (A)</th>");
                                    html.Append("<th style='width:100px;text-align:center'>Ti&#7871;t di&#7879;n (mm&#178;)</th>");
                                    html.Append("<th style='width:110px;text-align:center'>Quy c&#225;ch</th>");
                                    html.Append("<th style='width:70px;text-align:center'>V&#7853;t li&#7879;u</th>");
                                    html.Append("<th style='width:90px;text-align:center'>D&#224;i (mm)</th>");
                                    html.Append("<th style='width:80px;text-align:center'>KL (kg)</th>");
                                    html.Append("<th>Ghi ch&#250; &#273;&#432;&#7901;ng &#273;i</th>");
                                    html.Append("</tr></thead><tbody>");

                                    // Hàng thanh cái chính
                                    string mainCross = optBusbar["cross_section_mm2"]?.ToString();
                                     if (string.IsNullOrEmpty(mainCross) || mainCross == "---" || mainCross == "0")
                                     {
                                         mainCross = CalculateCrossSection(optBbSize);
                                     }
                                    string mainMat   = optBusbar["material"]?.ToString()          ?? "Cu";
                                    html.Append("<tr style='background:#eff6ff; font-weight:700;'>");
                                    html.Append("<td>&#9889; Thanh c&#225;i ch&#237;nh (Incomer)</td>");
                                    html.Append("<td style='text-align:center;color:#94a3b8;'>-</td>");
                                    html.Append($"<td style='text-align:center'>{optLoadA}</td>");
                                    html.Append($"<td style='text-align:center'>{mainCross}</td>");
                                    html.Append($"<td style='text-align:center'><span class='badge badge-primary'>{optBbSize}</span></td>");
                                    html.Append($"<td style='text-align:center'>{mainMat}</td>");
                                    html.Append($"<td style='text-align:center'>{optBbLen}</td>");
                                    html.Append($"<td style='text-align:center'>{optTotalWeight}</td>");
                                    html.Append("<td style='font-size:12px;color:#475569;'>Ch&#7841;y to&#224;n b&#7897; chi&#7873;u cao t&#7911; (Incomer busbar)</td>");
                                    html.Append("</tr>");

                                    // Hàng các nhánh (Gộp cùng sản phẩm và cộng lại mm)
                                    var branches = optBusbar["branches"] as Newtonsoft.Json.Linq.JArray;
                                    if (branches != null && branches.Count > 0)
                                    {
                                        var bList = new System.Collections.Generic.List<GpbBranch>();
                                        foreach (var branch in branches)
                                        {
                                            string rawName = branch["breaker_name"]?.ToString() ?? "Át nhánh";
                                            // Loại bỏ các phần chú thích phụ như (nhánh 1), (nhánh 2) ở cuối để gộp nhóm
                                            string normName = System.Text.RegularExpressions.Regex.Replace(rawName, @"\s*\(nhánh\s*\d+\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                            normName = System.Text.RegularExpressions.Regex.Replace(normName, @"\s*\(\d+\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                                    
                                            double curVal = 0;
                                            double.TryParse(branch["current_a"]?.ToString(), out curVal);
                                            string bSize = branch["busbar_size"]?.ToString() ?? "---";
                                            string bMat = branch["material"]?.ToString() ?? "Cu";
                                            double lenVal = 0;
                                            double.TryParse(branch["length_mm"]?.ToString(), out lenVal);
                                            double wVal = 0;
                                            double.TryParse(branch["weight_kg"]?.ToString(), out wVal);
                                            string bRoute = branch["route_note"]?.ToString() ?? "";
                                    
                                            string bBreakerSize = branch["breaker_size"]?.ToString() ?? "---";
                                            bList.Add(new GpbBranch
                                            {
                                                Name = normName,
                                                Current = curVal,
                                                Size = bSize,
                                                Material = bMat,
                                                Length = lenVal,
                                                Weight = wVal,
                                                Route = bRoute,
                                                BreakerSize = bBreakerSize
                                            });
                                        }
                                    
                                        // Nhóm các át nhánh cùng tên, cùng quy cách đồng và cùng vật liệu
                                        var grouped = bList
                                            .GroupBy(x => new { x.Name, x.Size, x.Material })
                                            .Select(g => new GpbBranch
                                            {
                                                Name = g.Key.Name + (g.Count() > 1 ? $" (x{g.Count()} thiết bị)" : ""),
                                                Current = g.Max(x => x.Current),
                                                Size = g.Key.Size,
                                                Material = g.Key.Material,
                                                Length = g.Sum(x => x.Length),
                                                Weight = g.Sum(x => x.Weight),
                                                Route = string.Join("; ", g.Select(x => x.Route).Distinct().Where(r => !string.IsNullOrEmpty(r))),
                                                BreakerSize = g.FirstOrDefault()?.BreakerSize ?? "---"
                                            })
                                            .ToList();
                                    
                                        foreach (var gItem in grouped)
                                        {
                                            string bCross = CalculateCrossSection(gItem.Size);
                                            html.Append("<tr>");
                                            html.Append($"<td>{System.Net.WebUtility.HtmlEncode(gItem.Name)}</td>");
                                            html.Append($"<td style='text-align:center;color:#475569;'>{System.Net.WebUtility.HtmlEncode(gItem.BreakerSize)}</td>");
                                            html.Append($"<td style='text-align:center;font-weight:600'>{gItem.Current}</td>");
                                            html.Append($"<td style='text-align:center'>{bCross}</td>");
                                            html.Append($"<td style='text-align:center'><span class='badge badge-secondary'>{gItem.Size}</span></td>");
                                            html.Append($"<td style='text-align:center;color:#0369a1;font-weight:600'>{gItem.Material}</td>");
                                            html.Append($"<td style='text-align:center'>{gItem.Length}</td>");
                                            html.Append($"<td style='text-align:center'>{gItem.Weight}</td>");
                                            html.Append($"<td style='font-size:12px;color:#475569;'>{System.Net.WebUtility.HtmlEncode(gItem.Route)}</td>");
                                            html.Append("</tr>");
                                        }
                                    }
                                    html.Append("</tbody></table>");

                                    if (optBusbar["calculation_note"] != null && !string.IsNullOrEmpty(optBusbar["calculation_note"].ToString()))
                                        html.Append($"<p style='font-size:12px;color:#64748b;margin-top:10px;font-style:italic;'>*Ghi ch&#250;: {System.Net.WebUtility.HtmlEncode(optBusbar["calculation_note"].ToString())}</p>");

                                    html.Append("</div>");
                                }

                                // --- B\u1ea3ng Kho\u1ea3ng H\u1edf Thi\u1ebft B\u1ecb \u0110\u1ebfn V\u00e1ch T\u1ee7 ---
                                var clearances = sketchOptions[i]["clearances"] as Newtonsoft.Json.Linq.JArray;
                                if (clearances != null && clearances.Count > 0)
                                {
                                    html.Append("<div style='margin-top:20px; border-top:1px solid #e2e8f0; padding-top:16px;'>");
                                    html.Append("<h3 style='font-size:14px; font-weight:700; color:#0f172a; margin-bottom:4px;'>Ki\u1ec3m Tra Kho\u1ea3ng H\u1edf Thi\u1ebft B\u1ecb \u0110\u1ebfn V\u00e1ch T\u1ee7</h3>");
                                    html.Append("<p style='font-size:12px; color:#64748b; margin:0 0 10px 0;'>\u2705 OK \u2265 50mm &nbsp; \u26a0\ufe0f H\u01a1i s\u00e1t 25\u201349mm &nbsp; \ud83d\udd34 Nguy hi\u1ec3m &lt; 25mm</p>");
                                    html.Append("<table><thead><tr>");
                                    html.Append("<th>Thi\u1ebft b\u1ecb</th>");
                                    html.Append("<th>K\u00edch th\u01b0\u1edbc (W\u00d7H\u00d7D mm)</th>");
                                    html.Append("<th>Tr\u00e1i \u2194 V\u00e1ch</th>");
                                    html.Append("<th>Ph\u1ea3i \u2194 V\u00e1ch</th>");
                                    html.Append("<th>Tr\u00ean \u2194 Tr\u1ea7n</th>");
                                    html.Append("<th>D\u01b0\u1edbi \u2194 S\u00e0n</th>");
                                    html.Append("<th>S\u00e2u (gap)</th>");
                                    html.Append("<th>Tr\u1ea1ng th\u00e1i</th>");
                                    html.Append("</tr></thead><tbody>");

                                    foreach (var clr in clearances)
                                    {
                                        string devName  = clr["device_name"]?.ToString() ?? "---";
                                        string devW     = clr["device_width_mm"]?.ToString()  ?? "?";
                                        string devH     = clr["device_height_mm"]?.ToString() ?? "?";
                                        string devD     = clr["device_depth_mm"]?.ToString()  ?? "?";
                                        string gLeft    = clr["gap_to_left_wall_mm"]?.ToString()  ?? "?";
                                        string gRight   = clr["gap_to_right_wall_mm"]?.ToString() ?? "?";
                                        string gTop     = clr["gap_to_top_mm"]?.ToString()        ?? "?";
                                        string gBottom  = clr["gap_to_bottom_mm"]?.ToString()     ?? "?";
                                        string gDepth   = clr["depth_gap_mm"]?.ToString()         ?? "?";
                                        string status   = clr["status"]?.ToString()?.ToLower()    ?? "ok";
                                        string warnNote = clr["warning_note"]?.ToString()         ?? "";

                                        // M\u00e0u n\u1ec1n h\u00e0ng theo status t\u1ed5ng
                                        string rowBg = status == "danger" ? "#fef2f2"
                                                     : status == "tight"  ? "#fffbeb"
                                                     : "transparent";

                                        html.Append($"<tr style='background:{rowBg};'>");
                                        html.Append($"<td style='font-weight:600;'>{System.Net.WebUtility.HtmlEncode(devName)}</td>");
                                        html.Append($"<td style='color:#475569; font-size:12px;'>{devW}\u00d7{devH}\u00d7{devD}</td>");
                                        html.Append(RenderClearanceCell(gLeft));
                                        html.Append(RenderClearanceCell(gRight));
                                        html.Append(RenderClearanceCell(gTop));
                                        html.Append(RenderClearanceCell(gBottom));
                                        html.Append(RenderClearanceCell(gDepth));

                                        // Status badge + warning
                                        string badgeTxt   = status == "danger" ? "\ud83d\udd34 Nguy hi\u1ec3m"
                                                          : status == "tight"  ? "\u26a0\ufe0f H\u01a1i s\u00e1t"
                                                          : "\u2705 OK";
                                        string clrBadgeColor = status == "danger" ? "#dc2626"
                                                          : status == "tight"  ? "#d97706"
                                                          : "#16a34a";
                                        html.Append($"<td><span style='color:{clrBadgeColor}; font-weight:700; font-size:12px;'>{badgeTxt}</span>");
                                        if (!string.IsNullOrEmpty(warnNote))
                                            html.Append($"<br><span style='font-size:11px; color:#64748b; font-style:italic;'>{System.Net.WebUtility.HtmlEncode(warnNote)}</span>");
                                        html.Append("</td>");
                                        html.Append("</tr>");
                                    }

                                    html.Append("</tbody></table></div>");
                                }

                                html.Append("</div>"); // end card
                            }
                        }                            // Hiển thị mảng Đề xuất tối ưu hóa ở ngoài cùng
                        var notesArr = dataObj["notes"] as Newtonsoft.Json.Linq.JArray;
                        if (notesArr != null && notesArr.Count > 0)
                        {
                            html.Append("<div class='card' style='background: #fff; border-radius: 12px; padding: 24px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); margin-bottom: 24px;'>");
                            html.Append("<h2 style='font-size: 18px; font-weight: 700; color: #1e293b; margin-top: 0; margin-bottom: 16px; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px; display: flex; align-items: center; gap: 8px;'>💡 Đề xuất tối ưu hóa vỏ tủ & đồng thanh cái</h2>");
                            html.Append("<ul class='notes-list' style='list-style-type: none; padding-left: 0; margin: 0;'>");
                            foreach (var note in notesArr)
                            {
                                html.Append($"<li style='background: #f8fafc; margin-bottom: 8px; padding: 12px 16px; border-left: 4px solid #10b981; border-radius: 0 8px 8px 0; font-size: 14px; color: #334155;'>{System.Net.WebUtility.HtmlEncode(note.ToString())}</li>");
                            }
                            html.Append("</ul>");
                            html.Append("</div>");
                        }

                        // Raw JSON Debug (collapsed by default using details tag)
                        html.Append("<details style='margin-top: 30px; border: 1px solid #e2e8f0; border-radius: 8px; padding: 12px; background:#f8fafc;'>");
                        html.Append("<summary style='font-size: 13px; font-weight:600; color:#64748b; cursor:pointer;'>Xem Dữ Liệu Gốc Phản Hồi Từ AI (Raw JSON Debug)</summary>");
                        html.Append($"<pre style='background:#f1f5f9; color:#334155; border:1px solid #cbd5e1; margin-top:10px; max-height: 400px; overflow-y:auto;'>{System.Net.WebUtility.HtmlEncode(parsedJson.ToString(Newtonsoft.Json.Formatting.Indented))}</pre>");
                        html.Append("</details>");

                        // Không dùng JavaScript - tab switching xử lý 100% bởi C# Navigating event
                        html.Append("</div></body></html>");

                        // Ghi ra file và dùng Navigate() để IE cho phép JS chạy (Local Machine zone)
                        string htmlFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_cabinet_layout.html");
                        try
                        {
                            string wsPath = @"e:\VNECCO\ElectricalCacbinetQuotationSoftware\debug_cabinet_layout.html";
                            System.IO.File.WriteAllText(wsPath, html.ToString(), Encoding.UTF8);
                            System.IO.File.WriteAllText(htmlFilePath, html.ToString(), Encoding.UTF8);
                        }
                        catch { }
                        // Navigate file:// để IE engine load JS đúng (thay vì dùng DocumentText bị chặn JS)
                        if (webResult != null && !webResult.IsDisposed && !webResult.Disposing)
                        {
                            webResult.Navigate(new Uri(htmlFilePath).AbsoluteUri);
                        }
                    }
                    catch 
                    {
                        if (webResult != null && !webResult.IsDisposed && !webResult.Disposing)
                        {
                            webResult.DocumentText = $"<html><body style='font-family:sans-serif; padding:20px;'><h3 style='color:red;'>Raw JSON Result:</h3><pre>{System.Net.WebUtility.HtmlEncode(resBody)}</pre></body></html>";
                        }
                    }
            }
            catch (Exception ex)
            {
                if (webResult != null && !webResult.IsDisposed && !webResult.Disposing)
                {
                    webResult.DocumentText = $"<html><body style='font-family:sans-serif; padding:20px;'><h3 style='color:red;'>LỖI KẾT NỐI:</h3><p>{System.Net.WebUtility.HtmlEncode(ex.Message)}</p></body></html>";
                }
            }
        }
        private string AlignPanelDrawing(string panelSketch, string optionName)
        {
            if (string.IsNullOrEmpty(panelSketch)) return panelSketch;

            bool isVertical = true;
            string optLower = optionName.ToLower();
            if (optLower.Contains("ngang") || optLower.Contains("option 2") || optLower.Contains("phương án 2") || optLower.Contains("horizontal"))
            {
                isVertical = false;
            }
            else if (optLower.Contains("dọc") || optLower.Contains("option 1") || optLower.Contains("phương án 1") || optLower.Contains("vertical"))
            {
                isVertical = true;
            }
            else
            {
                isVertical = !panelSketch.Contains("  +----------------+  +----------------+");
            }

            var lines = panelSketch.Split(new[] { '\n' }, StringSplitOptions.None);
            for (int li = 0; li < lines.Length; li++)
                lines[li] = lines[li].TrimEnd('\r');

            int maxLeftBoxWidth = 0;
            int maxRightBoxWidth = 0;
            int maxMiddleWidth = 0;
            int maxBoxWidth = 0;
            int totalCabinetWidth = 0;

            var borderBoxRegex = new Regex(@"([+])([-]{2,})([+])");
            var contentBoxRegex = new Regex(@"([|])([^|]{2,})([|])");
            var generalBoxRegex = new Regex(@"(\|[^|]{2,}\||\+[-]{2,}\+)");

            // Split cabinet part and comment part for all lines first using smart splitter
            var cabinetParts = new string[lines.Length];
            var commentParts = new string[lines.Length];
            for (int li = 0; li < lines.Length; li++)
            {
                SplitLine(lines[li], out cabinetParts[li], out commentParts[li]);
            }

            if (isVertical)
            {
                var layoutLineRegex = new Regex(@"^(?<left_border>\|\s*\[M\d+\]\s*<-\d+->\s*\|)(?<content>.*)(?<right_border>\|\s*<-\d+->\s*\|?\[M\d+\]\s*\|)$", RegexOptions.IgnoreCase);
                for (int li = 0; li < lines.Length; li++)
                {
                    var match = layoutLineRegex.Match(cabinetParts[li]);
                    if (match.Success)
                    {
                        string content = match.Groups["content"].Value;
                        string leftBox = "";
                        string rightBox = "";
                        string middle = "";

                        int firstPipe = content.IndexOfAny(new[] { '|', '+' });
                        int lastPipe = content.LastIndexOfAny(new[] { '|', '+' });

                        if (firstPipe >= 0 && lastPipe >= 0 && firstPipe < lastPipe)
                        {
                            int leftBoxEndIdx = -1;
                            char leftChar = content[firstPipe];
                            for (int i = firstPipe + 2; i <= lastPipe; i++)
                            {
                                if (content[i] == leftChar)
                                {
                                    leftBoxEndIdx = i;
                                    break;
                                }
                            }

                            int rightBoxStartIdx = -1;
                            char rightChar = content[lastPipe];
                            for (int i = lastPipe - 2; i >= firstPipe; i--)
                            {
                                if (content[i] == rightChar)
                                {
                                    rightBoxStartIdx = i;
                                    break;
                                }
                            }

                            if (leftBoxEndIdx >= 0)
                            {
                                leftBox = content.Substring(firstPipe, leftBoxEndIdx - firstPipe + 1).Trim();
                            }

                            if (rightBoxStartIdx >= 0 && rightBoxStartIdx >= leftBoxEndIdx)
                            {
                                if (rightBoxStartIdx == firstPipe)
                                {
                                    rightBox = "";
                                    middle = content.Substring(leftBoxEndIdx + 1).Trim();
                                }
                                else
                                {
                                    string rawRight = content.Substring(rightBoxStartIdx, lastPipe - rightBoxStartIdx + 1).Trim();
                                    if (rawRight.StartsWith("|") || rawRight.StartsWith("+"))
                                    {
                                        if (rawRight.Contains("=") || rawRight.Contains("-") && !rawRight.StartsWith("+"))
                                        {
                                            int lastConnIdx = -1;
                                            for (int i = rightBoxStartIdx; i < lastPipe; i++)
                                            {
                                                if (content[i] == '=' || content[i] == '-' || content[i] == '|')
                                                    lastConnIdx = i;
                                                else
                                                    break;
                                            }
                                            if (lastConnIdx >= 0 && lastConnIdx < lastPipe)
                                            {
                                                rightBox = content.Substring(lastConnIdx + 1, lastPipe - lastConnIdx).Trim();
                                                if (!rightBox.StartsWith("|") && !rightBox.StartsWith("+"))
                                                    rightBox = "|" + rightBox;
                                            }
                                        }
                                        else
                                        {
                                            rightBox = rawRight;
                                        }
                                    }
                                    else
                                    {
                                        rightBox = rawRight;
                                    }

                                    int middleStart = leftBoxEndIdx + 1;
                                    int middleEnd = rightBoxStartIdx;
                                    if (!string.IsNullOrEmpty(rightBox))
                                    {
                                        string cleanRight = rightBox.Replace("|", "").Replace("+", "").Trim();
                                        if (!string.IsNullOrEmpty(cleanRight))
                                        {
                                            int idx = content.IndexOf(cleanRight, middleStart);
                                            if (idx >= 0) middleEnd = idx;
                                        }
                                    }
                                    middle = content.Substring(middleStart, middleEnd - middleStart).Trim();
                                }
                            }
                            else
                            {
                                rightBox = "";
                                middle = content.Substring(leftBoxEndIdx + 1).Trim();
                            }
                        }

                        if (leftBox.Length > maxLeftBoxWidth) maxLeftBoxWidth = leftBox.Length;
                        if (rightBox.Length > maxRightBoxWidth) maxRightBoxWidth = rightBox.Length;
                        if (middle.Length > maxMiddleWidth) maxMiddleWidth = middle.Length;
                    }
                }

                if (maxLeftBoxWidth < 10) maxLeftBoxWidth = 22;
                if (maxRightBoxWidth < 10) maxRightBoxWidth = 22;
                if (maxMiddleWidth < 6) maxMiddleWidth = 16;

                if (maxLeftBoxWidth % 2 != 0) maxLeftBoxWidth++;
                if (maxRightBoxWidth % 2 != 0) maxRightBoxWidth++;
                if (maxMiddleWidth % 2 != 0) maxMiddleWidth++;

                int totalContentWidth = maxLeftBoxWidth + maxMiddleWidth + maxRightBoxWidth;
                totalCabinetWidth = 14 + 1 + totalContentWidth + 15; // 15 instead of 16
            }
            else
            {
                var layoutLineRegex = new Regex(@"^(?<left_border>\|\s*\[M\d+\]\s*<-\d+->\s*\|?)(?<content>.*)(?<right_border>\|\s*<-\d+->\s*\|?\[M\d+\]\s*\|)$", RegexOptions.IgnoreCase);
                for (int li = 0; li < lines.Length; li++)
                {
                    var match = layoutLineRegex.Match(cabinetParts[li]);
                    if (match.Success)
                    {
                        string content = match.Groups["content"].Value;
                        bool isBorderLine = content.Contains("+---") || content.Contains("+--") || content.Trim().StartsWith("+");
                        var activeRegex = isBorderLine ? borderBoxRegex : contentBoxRegex;

                        var boxMatches = activeRegex.Matches(content);
                        foreach (Match bm in boxMatches)
                        {
                            string boxVal = bm.Value.Trim();
                            if (boxVal.Length > maxBoxWidth) maxBoxWidth = boxVal.Length;
                        }
                    }
                }

                if (maxBoxWidth < 10) maxBoxWidth = 18;
                if (maxBoxWidth % 2 != 0) maxBoxWidth++;

                int targetContentWidth = 4 * maxBoxWidth + 3 * 3; // 4 boxes, spacing of 3
                totalCabinetWidth = 14 + 1 + targetContentWidth + 15; // 15 instead of 16
            }

            var newLines = new string[lines.Length];

            if (isVertical)
            {
                var layoutLineRegex = new Regex(@"^(?<left_border>\|\s*\[M\d+\]\s*<-\d+->\s*\|)(?<content>.*)(?<right_border>\|\s*<-\d+->\s*\|?\[M\d+\]\s*\|)$", RegexOptions.IgnoreCase);
                for (int li = 0; li < lines.Length; li++)
                {
                    string cabinetPart = cabinetParts[li];
                    string commentPart = commentParts[li];

                    if (string.IsNullOrWhiteSpace(cabinetPart))
                    {
                        newLines[li] = lines[li];
                        continue;
                    }

                    var match = layoutLineRegex.Match(cabinetPart);
                    if (match.Success)
                    {
                        string leftBorder = match.Groups["left_border"].Value;
                        string content = match.Groups["content"].Value;
                        string rightBorder = match.Groups["right_border"].Value;

                        string leftBox = "";
                        string rightBox = "";
                        string middle = "";

                        int firstPipe = content.IndexOfAny(new[] { '|', '+' });
                        int lastPipe = content.LastIndexOfAny(new[] { '|', '+' });

                        if (firstPipe >= 0 && lastPipe >= 0 && firstPipe < lastPipe)
                        {
                            int leftBoxEndIdx = -1;
                            char leftChar = content[firstPipe];
                            for (int i = firstPipe + 2; i <= lastPipe; i++)
                            {
                                if (content[i] == leftChar)
                                {
                                    leftBoxEndIdx = i;
                                    break;
                                }
                            }

                            int rightBoxStartIdx = -1;
                            char rightChar = content[lastPipe];
                            for (int i = lastPipe - 2; i >= firstPipe; i--)
                            {
                                if (content[i] == rightChar)
                                {
                                    rightBoxStartIdx = i;
                                    break;
                                }
                            }

                            if (leftBoxEndIdx >= 0)
                            {
                                leftBox = content.Substring(firstPipe, leftBoxEndIdx - firstPipe + 1).Trim();
                            }

                            if (rightBoxStartIdx >= 0 && rightBoxStartIdx >= leftBoxEndIdx)
                            {
                                if (rightBoxStartIdx == firstPipe)
                                {
                                    rightBox = "";
                                    middle = content.Substring(leftBoxEndIdx + 1).Trim();
                                }
                                else
                                {
                                    string rawRight = content.Substring(rightBoxStartIdx, lastPipe - rightBoxStartIdx + 1).Trim();
                                    if (rawRight.StartsWith("|") || rawRight.StartsWith("+"))
                                    {
                                        if (rawRight.Contains("=") || rawRight.Contains("-") && !rawRight.StartsWith("+"))
                                        {
                                            int lastConnIdx = -1;
                                            for (int i = rightBoxStartIdx; i < lastPipe; i++)
                                            {
                                                if (content[i] == '=' || content[i] == '-' || content[i] == '|')
                                                    lastConnIdx = i;
                                                else
                                                    break;
                                            }
                                            if (lastConnIdx >= 0 && lastConnIdx < lastPipe)
                                            {
                                                rightBox = content.Substring(lastConnIdx + 1, lastPipe - lastConnIdx).Trim();
                                                if (!rightBox.StartsWith("|") && !rightBox.StartsWith("+"))
                                                    rightBox = "|" + rightBox;
                                            }
                                        }
                                        else
                                        {
                                            rightBox = rawRight;
                                        }
                                    }
                                    else
                                    {
                                        rightBox = rawRight;
                                    }

                                    int middleStart = leftBoxEndIdx + 1;
                                    int middleEnd = rightBoxStartIdx;
                                    if (!string.IsNullOrEmpty(rightBox))
                                    {
                                        string cleanRight = rightBox.Replace("|", "").Replace("+", "").Trim();
                                        if (!string.IsNullOrEmpty(cleanRight))
                                        {
                                            int idx = content.IndexOf(cleanRight, middleStart);
                                            if (idx >= 0) middleEnd = idx;
                                        }
                                    }
                                    middle = content.Substring(middleStart, middleEnd - middleStart).Trim();
                                }
                            }
                            else
                            {
                                rightBox = "";
                                middle = content.Substring(leftBoxEndIdx + 1).Trim();
                            }
                        }

                        // Format components
                        string formattedLeftBox = "";
                        if (!string.IsNullOrEmpty(leftBox))
                        {
                            char borderCharStart = leftBox[0];
                            string innerText = leftBox.Substring(1, leftBox.Length - 2);
                            if (borderCharStart == '+' || borderCharStart == '-')
                            {
                                formattedLeftBox = "+" + new string('-', maxLeftBoxWidth - 2) + "+";
                            }
                            else
                            {
                                string cleanText = innerText.Trim();
                                int padTotal = maxLeftBoxWidth - 2 - cleanText.Length;
                                if (padTotal < 0) padTotal = 0;
                                int padLeft = padTotal / 2;
                                int padRight = padTotal - padLeft;
                                formattedLeftBox = "|" + new string(' ', padLeft) + cleanText + new string(' ', padRight) + "|";
                            }
                        }
                        else
                        {
                            formattedLeftBox = new string(' ', maxLeftBoxWidth);
                        }

                        string formattedRightBox = "";
                        if (!string.IsNullOrEmpty(rightBox))
                        {
                            char borderCharStart = rightBox[0];
                            string innerText = rightBox.Substring(1, rightBox.Length - 2);
                            if (borderCharStart == '+' || borderCharStart == '-')
                            {
                                formattedRightBox = "+" + new string('-', maxRightBoxWidth - 2) + "+";
                            }
                            else
                            {
                                string cleanText = innerText.Trim();
                                int padTotal = maxRightBoxWidth - 2 - cleanText.Length;
                                if (padTotal < 0) padTotal = 0;
                                int padLeft = padTotal / 2;
                                int padRight = padTotal - padLeft;
                                formattedRightBox = "|" + new string(' ', padLeft) + cleanText + new string(' ', padRight) + "|";
                            }
                        }
                        else
                        {
                            formattedRightBox = new string(' ', maxRightBoxWidth);
                        }

                        string formattedMiddle = "";
                        if (middle.Contains("=") || middle.Contains("-"))
                        {
                            char connChar = middle.Contains("=") ? '=' : '-';
                            formattedMiddle = new string(connChar, maxMiddleWidth);
                        }
                        else if (middle.Contains("||"))
                        {
                            int padTotal = maxMiddleWidth - middle.Length;
                            if (padTotal < 0) padTotal = 0;
                            int padLeft = padTotal / 2;
                            int padRight = padTotal - padLeft;
                            formattedMiddle = new string(' ', padLeft) + middle + new string(' ', padRight);
                        }
                        else
                        {
                            formattedMiddle = new string(' ', maxMiddleWidth);
                        }

                        string standardLeftBorder = leftBorder.Trim();
                        if (standardLeftBorder.EndsWith("|")) standardLeftBorder = standardLeftBorder.Substring(0, standardLeftBorder.Length - 1).Trim();
                        if (standardLeftBorder.Length < 13) standardLeftBorder = standardLeftBorder.PadRight(13);
                        standardLeftBorder = standardLeftBorder.Substring(0, 13) + "|";

                        string standardRightBorder = rightBorder.Trim();
                        if (standardRightBorder.StartsWith("|")) standardRightBorder = standardRightBorder.Substring(1).Trim();
                        standardRightBorder = "| " + standardRightBorder;
                        standardRightBorder = standardRightBorder.TrimEnd(); // TrimEnd to get exactly 15 chars

                        string alignedCabinetPart = (standardLeftBorder + " " + formattedLeftBox + formattedMiddle + formattedRightBox + standardRightBorder).PadRight(totalCabinetWidth);
                        
                        string alignedCommentPart = "";
                        string cleanComment = commentPart.TrimStart();
                        if (!string.IsNullOrEmpty(cleanComment))
                        {
                            if (cleanComment.StartsWith("|"))
                            {
                                alignedCommentPart = "  |  " + cleanComment.Substring(1).TrimStart();
                            }
                            else if (cleanComment.StartsWith("---"))
                            {
                                alignedCommentPart = "  ---";
                            }
                            else
                            {
                                alignedCommentPart = "  " + cleanComment;
                            }
                        }

                        newLines[li] = alignedCabinetPart + alignedCommentPart;
                    }
                    else
                    {
                        string alignedCabinetPart = AlignNonLayoutLine(cabinetPart, totalCabinetWidth, generalBoxRegex);
                        string alignedCommentPart = "";
                        string cleanComment = commentPart.TrimStart();
                        if (!string.IsNullOrEmpty(cleanComment))
                        {
                            if (cleanComment.StartsWith("|"))
                            {
                                alignedCommentPart = "  |  " + cleanComment.Substring(1).TrimStart();
                            }
                            else if (cleanComment.StartsWith("---"))
                            {
                                alignedCommentPart = "  ---";
                            }
                            else
                            {
                                alignedCommentPart = "  " + cleanComment;
                            }
                        }

                        newLines[li] = alignedCabinetPart + alignedCommentPart;
                    }
                }
            }
            else
            {
                var layoutLineRegex = new Regex(@"^(?<left_border>\|\s*\[M\d+\]\s*<-\d+->\s*\|?)(?<content>.*)(?<right_border>\|\s*<-\d+->\s*\|?\[M\d+\]\s*\|)$", RegexOptions.IgnoreCase);
                for (int li = 0; li < lines.Length; li++)
                {
                    string cabinetPart = cabinetParts[li];
                    string commentPart = commentParts[li];

                    if (string.IsNullOrWhiteSpace(cabinetPart))
                    {
                        newLines[li] = lines[li];
                        continue;
                    }

                    var match = layoutLineRegex.Match(cabinetPart);
                    if (match.Success)
                    {
                        string leftBorder = match.Groups["left_border"].Value;
                        string content = match.Groups["content"].Value;
                        string rightBorder = match.Groups["right_border"].Value;

                        bool isBorderLine = content.Contains("+---") || content.Contains("+--") || content.Trim().StartsWith("+");
                        var activeRegex = isBorderLine ? borderBoxRegex : contentBoxRegex;

                        var boxMatches = activeRegex.Matches(content);
                        var formattedBoxes = new List<string>();

                        foreach (Match bm in boxMatches)
                        {
                            string box = bm.Value.Trim();
                            char borderCharStart = box[0];
                            string innerText = box.Substring(1, box.Length - 2);

                            if (borderCharStart == '+' || borderCharStart == '-')
                            {
                                formattedBoxes.Add("+" + new string('-', maxBoxWidth - 2) + "+");
                            }
                            else
                            {
                                string cleanText = innerText.Trim();
                                int padTotal = maxBoxWidth - 2 - cleanText.Length;
                                if (padTotal < 0) padTotal = 0;
                                int padLeft = padTotal / 2;
                                int padRight = padTotal - padLeft;
                                formattedBoxes.Add("|" + new string(' ', padLeft) + cleanText + new string(' ', padRight) + "|");
                            }
                        }

                        string contentStr = string.Join("   ", formattedBoxes);
                        int targetContentWidth = 4 * maxBoxWidth + 9;
                        if (contentStr.Length < targetContentWidth)
                        {
                            contentStr = contentStr.PadRight(targetContentWidth);
                        }

                        string standardLeftBorder = leftBorder.Trim();
                        if (standardLeftBorder.EndsWith("|")) standardLeftBorder = standardLeftBorder.Substring(0, standardLeftBorder.Length - 1).Trim();
                        if (standardLeftBorder.Length < 13) standardLeftBorder = standardLeftBorder.PadRight(13);
                        standardLeftBorder = standardLeftBorder.Substring(0, 13) + "|";

                        string standardRightBorder = rightBorder.Trim();
                        if (standardRightBorder.StartsWith("|")) standardRightBorder = standardRightBorder.Substring(1).Trim();
                        standardRightBorder = "| " + standardRightBorder;
                        standardRightBorder = standardRightBorder.TrimEnd(); // TrimEnd to get exactly 15 chars

                        string alignedCabinetPart = (standardLeftBorder + " " + contentStr + standardRightBorder).PadRight(totalCabinetWidth);
                        
                        string alignedCommentPart = "";
                        string cleanComment = commentPart.TrimStart();
                        if (!string.IsNullOrEmpty(cleanComment))
                        {
                            if (cleanComment.StartsWith("|"))
                            {
                                alignedCommentPart = "  |  " + cleanComment.Substring(1).TrimStart();
                            }
                            else if (cleanComment.StartsWith("---"))
                            {
                                alignedCommentPart = "  ---";
                            }
                            else
                            {
                                alignedCommentPart = "  " + cleanComment;
                            }
                        }

                        newLines[li] = alignedCabinetPart + alignedCommentPart;
                    }
                    else
                    {
                        string alignedCabinetPart = AlignNonLayoutLine(cabinetPart, totalCabinetWidth, generalBoxRegex);
                        string alignedCommentPart = "";
                        string cleanComment = commentPart.TrimStart();
                        if (!string.IsNullOrEmpty(cleanComment))
                        {
                            if (cleanComment.StartsWith("|"))
                            {
                                alignedCommentPart = "  |  " + cleanComment.Substring(1).TrimStart();
                            }
                            else if (cleanComment.StartsWith("---"))
                            {
                                alignedCommentPart = "  ---";
                            }
                            else
                            {
                                alignedCommentPart = "  " + cleanComment;
                            }
                        }

                        newLines[li] = alignedCabinetPart + alignedCommentPart;
                    }
                }
            }

            return string.Join("\n", newLines);
        }

        private static void SplitLine(string line, out string cabinetPart, out string commentPart)
        {
            cabinetPart = line;
            commentPart = "";

            if (string.IsNullOrEmpty(line)) return;

            // 1. Check if line ends with a comment keyword
            var keywordRegex = new Regex(@"\s+(\|[\s|]*(Khoang|CT|Do Luong|\d+mm|Doc|Ngang|Cap ra|He Busbar|Phan Phoi|xep DOC|2 COT|1 HANG|xep NGANG|2 HANG).*)$", RegexOptions.IgnoreCase);
            var match = keywordRegex.Match(line);
            if (match.Success)
            {
                cabinetPart = line.Substring(0, match.Index);
                commentPart = match.Value;
                return;
            }

            // 2. Check if line ends with ---
            var dashRegex = new Regex(@"\s+(--+\s*)$");
            var dashMatch = dashRegex.Match(line);
            if (dashMatch.Success)
            {
                cabinetPart = line.Substring(0, dashMatch.Index);
                commentPart = dashMatch.Value;
                return;
            }

            // 3. Scan from right to find empty separator '|'
            int lastPipe = line.LastIndexOf('|');
            if (lastPipe >= 0)
            {
                // Find previous '|' or '+'
                int prevPipe = line.LastIndexOfAny(new[] { '|', '+' }, lastPipe - 1);
                if (prevPipe >= 0 && (lastPipe - prevPipe) <= 6)
                {
                    // Check if there are only spaces between them
                    string between = line.Substring(prevPipe + 1, lastPipe - prevPipe - 1);
                    if (string.IsNullOrWhiteSpace(between))
                    {
                        cabinetPart = line.Substring(0, prevPipe + 1);
                        commentPart = line.Substring(prevPipe + 1);
                        return;
                    }
                }
            }
        }

        private string AlignNonLayoutLine(string line, int totalCabinetWidth, Regex boxRegex)
        {
            if (string.IsNullOrWhiteSpace(line)) return line;

            if (line.Trim().StartsWith("+") && line.Trim().EndsWith("+") && line.Trim().Contains("-") && !line.Contains("Khoang"))
            {
                return "+" + new string('-', totalCabinetWidth - 2) + "+";
            }

            string cabinetPart = line;

            if (cabinetPart.StartsWith("|") && cabinetPart.Length >= 5)
            {
                string inside = cabinetPart.Substring(1, cabinetPart.Length - 2);
                
                var boxMatch = boxRegex.Match(inside);
                if (boxMatch.Success)
                {
                    string box = boxMatch.Value;
                    int bodyContentWidth = totalCabinetWidth - 2;
                    int padTotal = bodyContentWidth - box.Length;
                    if (padTotal < 0) padTotal = 0;
                    int padLeft = padTotal / 2;
                    int padRight = padTotal - padLeft;
                    
                    return "|" + new string(' ', padLeft) + box + new string(' ', padRight) + "|";
                }
                else
                {
                    string trimmedInside = inside.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedInside))
                    {
                        return "|" + new string(' ', totalCabinetWidth - 2) + "|";
                    }

                    if (trimmedInside.StartsWith("<") && trimmedInside.EndsWith(">"))
                    {
                        string text = trimmedInside.Replace("<", "").Replace(">", "").Replace("-", "").Trim();
                        int arrowWidth = totalCabinetWidth - 2;
                        int textLen = text.Length + 2;
                        int dashHalf = (arrowWidth - 2 - textLen) / 2;
                        if (dashHalf < 0) dashHalf = 0;
                        string leftArrow = "<" + new string('-', dashHalf);
                        string rightArrow = new string('-', arrowWidth - 2 - dashHalf - textLen) + ">";
                        return "|" + leftArrow + " " + text + " " + rightArrow + "|";
                    }

                    int bodyContentWidth = totalCabinetWidth - 2;
                    int padTotal = bodyContentWidth - trimmedInside.Length;
                    if (padTotal < 0) padTotal = 0;
                    int padLeft = padTotal / 2;
                    int padRight = padTotal - padLeft;
                    
                    return "|" + new string(' ', padLeft) + trimmedInside + new string(' ', padRight) + "|";
                }
            }

            return line;
        }        private class GpbBranch
        {
            public string Name { get; set; }
            public double Current { get; set; }
            public string Size { get; set; }
            public string Material { get; set; }
            public double Length { get; set; }
            public double Weight { get; set; }
            public string Route { get; set; }
            public string BreakerSize { get; set; }
        }
    }
}