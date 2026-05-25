using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
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

            // Bung SUM(w102*sl) -> (w102_1*sl102_1 + w102_2*sl102_2) trước khi xử lý các biến khác
            processed = ExpandSumFunctions(processed, variables);

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
            // Chỉ thay thế các định danh (identifier) không phải là tên hàm (MAX, MIN, SUM, CEIL, FLOOR) hoặc toán tử (AND, OR, v.v.)
            string idPattern = @"\b(?!(?:MAX|MIN|SUM|CEIL|FLOOR|AND|OR|NOT|TRUE|FALSE|MOD)\b)[a-z]+[0-9]*(_[0-9]+)?\b";
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
        /// Trả về chuỗi biểu thức sau khi đã thay thế các biến (dùng cho mục đích Debug)
        /// để người dùng thấy rõ các biến như MAX(w102) hay sl đã được bung ra như thế nào.
        /// </summary>
        public static string GetDebugExpression(string expression, Dictionary<string, double> variables)
        {
            if (string.IsNullOrWhiteSpace(expression)) return "";

            string processed = expression.Trim();
            if (processed.StartsWith("=")) processed = processed.Substring(1);

            // Bung SUM(w102*sl) -> (w102_1*sl102_1 + w102_2*sl102_2)
            processed = ExpandSumFunctions(processed, variables);

            processed = ExpandContextualQuantity(processed, variables);
            processed = AutoCeilSlDiv2(processed, variables);
            processed = ExpandWildcardFunctions(processed, variables);

            var sortedKeys = variables.Keys.OrderByDescending(k => k.Length).ToList();
            foreach (var key in sortedKeys)
            {
                string pattern = $@"\b{Regex.Escape(key)}\b";
                processed = Regex.Replace(processed, pattern, variables[key].ToString(CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);
            }

            return processed;
        }

        /// <summary>
        /// Mở rộng các hàm MAX/MIN/SUM có tham số dạng prefix (không có _suffix) thành danh sách instance đầy đủ.
        /// Ví dụ: MAX(w102) → MAX(500, 800) nếu varMap chứa w102_1=500, w102_2=800.
        /// Nếu không tìm thấy instance nào, giữ nguyên (sẽ dùng giá trị cộng dồn).
        /// </summary>
        private static string ExpandWildcardFunctions(string expression, Dictionary<string, double> variables)
        {
            if (string.IsNullOrWhiteSpace(expression)) return expression;

            int index = 0;
            while (true)
            {
                var match = Regex.Match(expression.Substring(index), @"\b(MAX|MIN|SUM)\(", RegexOptions.IgnoreCase);
                if (!match.Success) break;

                int funcStart = index + match.Index;
                string funcName = match.Groups[1].Value.ToUpper();
                int openParenIndex = funcStart + match.Length - 1;

                int closeParenIndex = FindClosingParenthesis(expression, openParenIndex);
                if (closeParenIndex == -1)
                {
                    index = openParenIndex + 1;
                    continue;
                }

                string inner = expression.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);
                inner = ExpandWildcardFunctions(inner, variables);

                var args = SplitArguments(inner);
                var expandedArgs = new List<string>();

                foreach (var arg in args)
                {
                    if (Regex.IsMatch(arg, @"^[a-z][a-z0-9]*$", RegexOptions.IgnoreCase))
                    {
                        string varName = arg.ToLower();
                        var instanceKeys = variables.Keys
                            .Where(k => Regex.IsMatch(k, $@"^{Regex.Escape(varName)}_\d+$", RegexOptions.IgnoreCase))
                            .OrderBy(k => k)
                            .ToList();

                        if (instanceKeys.Count > 0)
                        {
                            expandedArgs.AddRange(instanceKeys.Select(k => variables[k].ToString(CultureInfo.InvariantCulture)));
                            continue;
                        }
                    }
                    expandedArgs.Add(arg);
                }

                string replacement = $"{funcName}({string.Join(", ", expandedArgs)})";
                expression = expression.Substring(0, funcStart) + replacement + expression.Substring(closeParenIndex + 1);
                index = funcStart + replacement.Length;
            }

            return expression;
        }

        private static int FindClosingParenthesis(string str, int openParenIndex)
        {
            int parenCount = 1;
            for (int i = openParenIndex + 1; i < str.Length; i++)
            {
                if (str[i] == '(') parenCount++;
                else if (str[i] == ')') parenCount--;

                if (parenCount == 0) return i;
            }
            return -1;
        }

        private static List<string> SplitArguments(string inner)
        {
            var args = new List<string>();
            int parenLevel = 0;
            int start = 0;
            for (int i = 0; i < inner.Length; i++)
            {
                if (inner[i] == '(') parenLevel++;
                else if (inner[i] == ')') parenLevel--;
                else if (inner[i] == ',' && parenLevel == 0)
                {
                    args.Add(inner.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            args.Add(inner.Substring(start).Trim());
            return args;
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
            // Xử lý SUM(a, b, c) hoặc SUM(a; b; c)
            expression = ProcessFunction(expression, "SUM", values => values.Sum());
            // Xử lý CEIL(expr) - làm tròn lên
            expression = ProcessFunction(expression, "CEIL", values => Math.Ceiling(values.First()));
            // Xử lý FLOOR(expr) - làm tròn xuống
            expression = ProcessFunction(expression, "FLOOR", values => Math.Floor(values.First()));

            return expression;
        }

        private static string ProcessFunction(string expression, string functionName, Func<IEnumerable<double>, double> aggregateFunc)
        {
            int index = 0;
            while (true)
            {
                var match = Regex.Match(expression.Substring(index), $@"\b{functionName}\(", RegexOptions.IgnoreCase);
                if (!match.Success) break;

                int funcStart = index + match.Index;
                int openParenIndex = funcStart + match.Length - 1;

                int closeParenIndex = FindClosingParenthesis(expression, openParenIndex);
                if (closeParenIndex == -1)
                {
                    index = openParenIndex + 1;
                    continue;
                }

                string inner = expression.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);
                
                // Đệ quy xử lý các hàm con bên trong inner trước
                inner = ProcessFunctions(inner);

                // Tách các đối số của hàm
                var args = SplitArguments(inner);
                var values = new List<double>();
                foreach (var arg in args)
                {
                    if (string.IsNullOrWhiteSpace(arg)) continue;
                    try
                    {
                        DataTable dt = new DataTable();
                        var val = dt.Compute(arg, "");
                        values.Add(Convert.ToDouble(val));
                    }
                    catch
                    {
                        // Nếu không tính được trực tiếp (chưa thay hết biến), giữ nguyên
                    }
                }

                if (values.Count > 0)
                {
                    double result = aggregateFunc(values);
                    string replacement = result.ToString(CultureInfo.InvariantCulture);
                    expression = expression.Substring(0, funcStart) + replacement + expression.Substring(closeParenIndex + 1);
                    index = funcStart + replacement.Length;
                }
                else
                {
                    index = closeParenIndex + 1;
                }
            }

            return expression;
        }

        /// <summary>
        /// Mở rộng hàm SUM(w102*sl) hoặc SUM(w102*sl + w103*sl) thành tổng các instance riêng biệt.
        /// Ví dụ: SUM(w102*sl) → (w102_1*sl102_1 + w102_2*sl102_2)
        /// </summary>
        private static string ExpandSumFunctions(string expression, Dictionary<string, double> variables)
        {
            if (string.IsNullOrWhiteSpace(expression)) return expression;

            int index = 0;
            while (true)
            {
                var match = Regex.Match(expression.Substring(index), @"\bSUM\(", RegexOptions.IgnoreCase);
                if (!match.Success) break;

                int funcStart = index + match.Index;
                int openParenIndex = funcStart + match.Length - 1;

                int closeParenIndex = FindClosingParenthesis(expression, openParenIndex);
                if (closeParenIndex == -1)
                {
                    index = openParenIndex + 1;
                    continue;
                }

                string inner = expression.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);
                
                // Phân tích các số hạng cộng trừ trong SUM
                List<char> operators;
                var terms = SplitTerms(inner, out operators);
                var expandedTerms = new List<string>();

                for (int t = 0; t < terms.Count; t++)
                {
                    string term = terms[t];
                    // Tìm ID trong số hạng này (ví dụ w102 => ID là 102)
                    var idMatches = Regex.Matches(term, @"\b([a-zA-Z]+)(\d+)\b");
                    string foundId = null;
                    foreach (Match idM in idMatches)
                    {
                        string id = idM.Groups[2].Value;
                        // Kiểm tra xem ID này có instance trong variables không
                        bool hasInstances = variables.Keys.Any(k => k.Contains(id) && Regex.IsMatch(k, @"_\d+$"));
                        if (hasInstances)
                        {
                            foundId = id;
                            break;
                        }
                    }

                    if (foundId != null)
                    {
                        // Tìm maxIndex của ID này
                        int maxIndex = 0;
                        foreach (var key in variables.Keys)
                        {
                            if (key.Contains(foundId))
                            {
                                var mKey = Regex.Match(key, @"_(\d+)$");
                                if (mKey.Success)
                                {
                                    if (int.TryParse(mKey.Groups[1].Value, out int idx) && idx > maxIndex)
                                    {
                                        maxIndex = idx;
                                    }
                                }
                            }
                        }

                        if (maxIndex > 0)
                        {
                            var instanceTerms = new List<string>();
                            for (int i = 1; i <= maxIndex; i++)
                            {
                                string instanceTerm = term;
                                // Thay thế các biến có chứa ID bằng suffix _i
                                // Ví dụ: w102 => w102_1
                                instanceTerm = Regex.Replace(instanceTerm, $@"\b([a-zA-Z]+)({foundId})\b", $"$1$2_{i}", RegexOptions.IgnoreCase);
                                
                                // Thay thế sl (nếu đứng độc lập) hoặc sl{id} thành sl{id}_{i}
                                instanceTerm = Regex.Replace(instanceTerm, $@"\bsl\b", $"sl{foundId}_{i}", RegexOptions.IgnoreCase);
                                instanceTerm = Regex.Replace(instanceTerm, $@"\bsl{foundId}\b", $"sl{foundId}_{i}", RegexOptions.IgnoreCase);
                                
                                instanceTerms.Add(instanceTerm);
                            }
                            expandedTerms.Add("(" + string.Join(" + ", instanceTerms) + ")");
                        }
                        else
                        {
                            expandedTerms.Add(term);
                        }
                    }
                    else
                    {
                        expandedTerms.Add(term);
                    }
                }

                // Tái cấu trúc lại chuỗi kết quả
                var sb = new StringBuilder();
                for (int t = 0; t < expandedTerms.Count; t++)
                {
                    sb.Append(expandedTerms[t]);
                    if (t < operators.Count)
                    {
                        sb.Append(" " + operators[t] + " ");
                    }
                }

                string replacement = sb.ToString();
                expression = expression.Substring(0, funcStart) + replacement + expression.Substring(closeParenIndex + 1);
                index = funcStart + replacement.Length;
            }

            return expression;
        }

        private static List<string> SplitTerms(string expression, out List<char> operators)
        {
            var terms = new List<string>();
            operators = new List<char>();
            int parenLevel = 0;
            int start = 0;
            for (int i = 0; i < expression.Length; i++)
            {
                if (expression[i] == '(') parenLevel++;
                else if (expression[i] == ')') parenLevel--;
                else if ((expression[i] == '+' || expression[i] == '-') && parenLevel == 0)
                {
                    terms.Add(expression.Substring(start, i - start).Trim());
                    operators.Add(expression[i]);
                    start = i + 1;
                }
            }
            terms.Add(expression.Substring(start).Trim());
            return terms;
        }
    }
}
