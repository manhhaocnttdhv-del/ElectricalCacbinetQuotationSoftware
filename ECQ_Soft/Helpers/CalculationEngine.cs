using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ECQ_Soft.Helpers
{
    public static class CalculationEngine
    {
        /// <summary>
        /// Tính toán giá trị của biểu thức dựa trên bản đồ biến số.
        /// Hỗ trợ: +, -, *, /, (, ), MAX(a,b,...), MIN(a,b,...)
        /// Tính năng đặc biệt: MAX(w102) tự động mở rộng thành MAX(w102_1, w102_2, ...)
        /// nếu có nhiều instance trong varMap, đảm bảo lấy đúng giá trị lớn nhất từng item.
        /// </summary>
        public static double Evaluate(string expression, Dictionary<string, double> variables)
        {
            if (string.IsNullOrWhiteSpace(expression)) return 0;

            // Loại bỏ dấu '=' ở đầu nếu có
            string processed = expression.Trim();
            if (processed.StartsWith("=")) processed = processed.Substring(1);

            // 0a. Context-aware sl: w102 * sl → w102 * sl102  (PHẢI CHẠY TRƯỚC khi bung MAX)
            //     Khi một biến có dạng {prefix}{id} nhân với 'sl' (không có id suffix),
            //     tự động thay sl → sl{id} để lấy đúng tổng số lượng của loại thiết bị đó.
            //     Chạy trước ExpandWildcardFunctions để MAX(w102) * sl → MAX(w102) * sl102
            //     trước khi w102 bị bung thành chuỗi số cụ thể mà không còn nhận ra ID.
            processed = ExpandContextualQuantity(processed, variables);

            // 0c. Auto-CEIL: sl/2 → CEIL(sl/2) nếu sl là số nguyên lẻ.
            //     Quy tắc: khi chia sl cho 2, nếu dư 1 thì +1 (làm tròn lên).
            //     Áp dụng cho cả sl102/2, sl/2, sl103/2...
            processed = AutoCeilSlDiv2(processed, variables);

            // 0b. Pre-expand: MAX(w102) → MAX(w102_1, w102_2, ...) nếu có instance trong varMap.
            //     Điều này giúp MAX/MIN trong công thức hoạt động chính xác theo từng thiết bị,
            //     thay vì nhận giá trị cộng dồn của w102 (= w102_1 + w102_2 + ...).
            processed = ExpandWildcardFunctions(processed, variables);

            // 1. Thay thế biến số đã biết
            var sortedKeys = variables.Keys.OrderByDescending(k => k.Length).ToList();
            foreach (var key in sortedKeys)
            {
                // Sử dụng Regex để thay thế chính xác tên biến (\b là boundary)
                string pattern = $@"\b{Regex.Escape(key)}\b";
                processed = Regex.Replace(processed, pattern, variables[key].ToString(CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);
            }

            // 2. Thay thế các biến còn sót lại (chưa được định nghĩa) bằng 0 để tránh lỗi Syntax
            // Chỉ thay thế các định danh (identifier) không phải là tên hàm (MAX, MIN, CEIL, FLOOR)
            string idPattern = @"\b(?!(?:MAX|MIN|CEIL|FLOOR)\b)[a-z]+[0-9]*(_[0-9]+)?\b";
            processed = Regex.Replace(processed, idPattern, "0", RegexOptions.IgnoreCase);

            // 2. Xử lý hàm MAX và MIN (thực hiện lặp để hỗ trợ lồng nhau cơ bản)
            bool changed = true;
            while (changed)
            {
                string old = processed;
                processed = ProcessFunctions(processed);
                changed = (old != processed);
            }

            // 3. Tính toán biểu thức toán học cơ bản bằng DataTable.Compute
            try
            {
                DataTable dt = new DataTable();
                var result = dt.Compute(processed, "");
                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                // Nếu lỗi, có thể do còn biến chưa được thay thế hoặc lỗi cú pháp
                throw new Exception($"Lỗi tính toán biểu thức '{expression}' (sau khi xử lý: '{processed}'): {ex.Message}");
            }
        }

        /// <summary>
        /// Mở rộng các hàm MAX/MIN có tham số dạng prefix (không có _suffix) thành danh sách instance đầy đủ.
        /// Ví dụ: MAX(w102) → MAX(500, 800) nếu varMap chứa w102_1=500, w102_2=800.
        /// Nếu không tìm thấy instance nào, giữ nguyên (sẽ dùng giá trị cộng dồn).
        /// </summary>
        private static string ExpandWildcardFunctions(string expression, Dictionary<string, double> variables)
        {
            // Pattern tìm MAX( hoặc MIN( với một tham số đơn (không có dấu phẩy = chỉ 1 biến prefix)
            var pattern = new Regex(@"\b(MAX|MIN)\((\s*[a-z][a-z0-9]*\s*)\)", RegexOptions.IgnoreCase);

            return pattern.Replace(expression, match =>
            {
                string funcName = match.Groups[1].Value.ToUpper();
                string varName = match.Groups[2].Value.Trim().ToLower();

                // Tìm tất cả instance keys: varName_1, varName_2, ...
                var instanceKeys = variables.Keys
                    .Where(k => Regex.IsMatch(k, $@"^{Regex.Escape(varName)}_\d+$", RegexOptions.IgnoreCase))
                    .OrderBy(k => k)
                    .ToList();

                if (instanceKeys.Count == 0)
                {
                    // Không có instance → giữ nguyên, dùng giá trị cộng dồn
                    return match.Value;
                }

                // Bung ra danh sách giá trị instance: MAX(500, 800, ...)
                string expanded = string.Join(", ", instanceKeys.Select(k => variables[k].ToString(CultureInfo.InvariantCulture)));
                return $"{funcName}({expanded})";
            });
        }

        /// <summary>
        /// Tự động chuyển sl/2 → CEIL(sl/2) để đảm bảo làm tròn lên khi sl lẻ.
        /// Ví dụ: sl102/2 với sl102=3 → CEIL(3/2) = 2 (không phải 1).
        /// Chỉ áp dụng khi chia cho 2 (quy tắc chia đôi thanh cái, tủ, etc.)
        /// </summary>
        private static string AutoCeilSlDiv2(string expression, Dictionary<string, double> variables)
        {
            // Pattern: sl hoặc sl{id} theo sau là /2
            var pattern = new Regex(@"\b(sl\d*)\b(\s*/\s*2)\b", RegexOptions.IgnoreCase);
            return pattern.Replace(expression, m =>
            {
                string slVar = m.Groups[1].Value;
                // Nếu sl lẻ → cộng 1 để thành số chẵn trước khi chia
                // sl=29 (lẻ) → (sl+1)/2 → sau khi thay biến: (29+1)/2 = 30/2 = 15
                // sl=4  (chẵn) → giữ nguyên sl/2 = 4/2 = 2
                if (variables.TryGetValue(slVar, out double slVal) && slVal % 2 != 0)
                    return $"({slVar}+1)/2";
                return m.Value;
            });
        }

        /// <summary>
        /// Mở rộng 'sl' không có ID thành 'sl{id}' dựa theo biến đứng cạnh nó trong phép nhân.
        /// Ví dụ: w102 * sl  → w102 * sl102
        ///         sl * h103  → sl103 * h103
        /// Quy tắc: nếu một biến có dạng {chữ}{số} nằm cạnh 'sl' (không có suffix số),
        /// thì extract số ID đó và gắn vào sl thành sl{id}.
        /// Chỉ thay thế khi sl{id} tồn tại trong varMap (để tránh replace sai).
        /// </summary>
        private static string ExpandContextualQuantity(string expression, Dictionary<string, double> variables)
        {
            // Pattern 1: {attr}{id} [)] * sl
            // Dấu ')' optional để khớp cả "w102 * sl" và "MAX(w102) * sl"
            var left = new Regex(@"\b([a-z]+)(\d+)\)?(\s*\*\s*)\bsl\b", RegexOptions.IgnoreCase);
            expression = left.Replace(expression, m =>
            {
                string id = m.Groups[2].Value;
                string slKey = "sl" + id;
                if (variables.ContainsKey(slKey))
                {
                    // Giữ nguyên phần trước dấu *, chỉ thay sl → sl{id}
                    // m.Groups[3] là phần ")*" hoặc "*" (bao gồm dấu ')' nếu có)
                    string prefix = m.Groups[1].Value + id + (m.Value.Contains(")") ? ")" : "");
                    return prefix + m.Groups[3].Value + slKey;
                }
                return m.Value;
            });

            // Pattern 2: sl * {attr}{id}  hoặc  sl * ({attr}{id})
            var right = new Regex(@"\bsl\b(\s*\*\s*)\(?([a-z]+)(\d+)\b", RegexOptions.IgnoreCase);
            expression = right.Replace(expression, m =>
            {
                string id = m.Groups[3].Value;
                string slKey = "sl" + id;
                if (variables.ContainsKey(slKey))
                {
                    string openParen = m.Value.Contains("(") ? "(" : "";
                    return slKey + m.Groups[1].Value + openParen + m.Groups[2].Value + id;
                }
                return m.Value;
            });

            return expression;
        }

        private static string ProcessFunctions(string expression)
        {
            // Xử lý MAX(a, b, c) hoặc MAX(a; b; c)
            expression = ProcessFunction(expression, "MAX", values => values.Max());
            // Xử lý MIN(a, b, c) hoặc MIN(a; b; c)
            expression = ProcessFunction(expression, "MIN", values => values.Min());
            // Xử lý CEIL(expr) - làm tròn lên
            expression = ProcessFunction(expression, "CEIL", values => Math.Ceiling(values.First()));
            // Xử lý FLOOR(expr) - làm tròn xuống
            expression = ProcessFunction(expression, "FLOOR", values => Math.Floor(values.First()));

            return expression;
        }

        private static string ProcessFunction(string expression, string functionName, Func<IEnumerable<double>, double> aggregateFunc)
        {
            // Tìm hàm với ngoặc đơn gần nhất (để xử lý từ trong ra ngoài nếu lồng nhau)
            string pattern = $@"{functionName}\(([^()]+)\)";
            var matches = Regex.Matches(expression, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                string inner = match.Groups[1].Value;
                // Tách các tham số bằng dấu phẩy hoặc dấu chấm phẩy
                string[] parts = inner.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                
                List<double> values = new List<double>();
                foreach (var part in parts)
                {
                    try
                    {
                        // Tính toán từng tham số (vì tham số có thể là biểu thức con)
                        DataTable dt = new DataTable();
                        var val = dt.Compute(part, "");
                        values.Add(Convert.ToDouble(val));
                    }
                    catch
                    {
                        // Nếu không tính được, giữ nguyên hoặc báo lỗi. 
                        // Ở đây ta tạm thời bỏ qua để tránh crash nếu tham số không hợp lệ
                    }
                }

                if (values.Count > 0)
                {
                    double result = aggregateFunc(values);
                    expression = expression.Replace(match.Value, result.ToString(CultureInfo.InvariantCulture));
                }
            }

            return expression;
        }
    }
}
