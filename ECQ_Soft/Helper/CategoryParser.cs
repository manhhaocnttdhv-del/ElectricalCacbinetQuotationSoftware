using ECQ_Soft.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ECQ_Soft.Helper
{
    public class CategoryParser
    {
        public static List<CategoryItem> ParseToTree(IEnumerable<string> rawCategories)
        {
            // Loại bỏ trùng lặp và râu ria (khoảng trắng, dấu chấm phẩy)
            var uniqueRaw = rawCategories
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.TrimEnd(';').Trim())
                .Distinct()
                .ToList();

            // Tách chuỗi thành mảng các cấp độ
            var parsedPaths = uniqueRaw
                .Select(c => new
                {
                    OriginalPath = c,
                    Nodes = c.Split(new[] { ">>" }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(p => p.Trim())
                             .ToArray()
                })
                .ToList();

            // SẮP XẾP: Quan trọng nhất để các node con nằm ngay dưới node cha theo thứ tự Alphabet
            parsedPaths.Sort((a, b) =>
            {
                int minLength = Math.Min(a.Nodes.Length, b.Nodes.Length);
                for (int i = 0; i < minLength; i++)
                {
                    int cmp = string.Compare(a.Nodes[i], b.Nodes[i], StringComparison.OrdinalIgnoreCase);
                    if (cmp != 0) return cmp;
                }
                return a.Nodes.Length.CompareTo(b.Nodes.Length);
            });

            var result = new List<CategoryItem>();
            string[] prevNodes = new string[0];

            foreach (var path in parsedPaths)
            {
                for (int depth = 0; depth < path.Nodes.Length; depth++)
                {
                    // So sánh với phần tử trước đó để xem có phải node mới không
                    if (depth >= prevNodes.Length || path.Nodes[depth] != prevNodes[depth])
                    {
                        // Tạo prefix thụt lề ">> " theo độ sâu (depth)
                        string prefix = "";
                        for (int i = 0; i < depth; i++) prefix += ">> ";

                        // Tạo chuỗi đường dẫn đầy đủ đến node này (Ví dụ: "Cha >> Con")
                        string currentFullPath = string.Join(" >> ", path.Nodes.Take(depth + 1));

                        result.Add(new CategoryItem
                        {
                            DisplayText = prefix + path.Nodes[depth],
                            FullPath = currentFullPath
                        });
                    }
                }
                prevNodes = path.Nodes;
            }

            return result;
        }
    }
}
