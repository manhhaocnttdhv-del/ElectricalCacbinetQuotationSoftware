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

        /// <summary>
        /// Xây cây <see cref="CategoryTreeNode"/> từ danh sách chuỗi raw phân cách ">>".
        /// Dùng đệ quy để Insert đúng vị trí cha → con ở mọi độ sâu.
        /// </summary>
        public static List<CategoryTreeNode> ParseToTreeNodes(IEnumerable<string> rawCategories)
        {
            var roots = new List<CategoryTreeNode>();

            var uniquePaths = rawCategories
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.TrimEnd(';').Trim())
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            foreach (var path in uniquePaths)
            {
                var parts = path
                    .Split(new[] { ">>" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToArray();

                if (parts.Length == 0) continue;

                // Đệ quy insert vào cây
                InsertPath(roots, parts, 0, "");
            }

            return roots;
        }

        /// <summary>
        /// Đệ quy: tìm hoặc tạo node con tại <paramref name="depth"/> trong <paramref name="siblings"/>,
        /// rồi tiếp tục đi sâu hơn với phần còn lại của mảng parts.
        /// </summary>
        private static void InsertPath(List<CategoryTreeNode> siblings, string[] parts, int depth, string parentPath)
        {
            if (depth >= parts.Length) return;

            string label = parts[depth];
            string fullPath = string.IsNullOrEmpty(parentPath) ? label : $"{parentPath} >> {label}";

            // Tìm node đã tồn tại ở cùng cấp
            var existing = siblings.FirstOrDefault(n =>
                string.Equals(n.Label, label, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                existing = new CategoryTreeNode
                {
                    Label    = label,
                    FullPath = fullPath,
                    Level    = depth,
                    IsExpanded = false   // mặc định đóng, user bấm mới mở
                };
                siblings.Add(existing);
            }

            // Đệ quy vào level tiếp theo
            InsertPath(existing.Children, parts, depth + 1, fullPath);
        }
    }
}
