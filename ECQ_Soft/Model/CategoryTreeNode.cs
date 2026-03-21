using System.Collections.Generic;

namespace ECQ_Soft.Model
{
    /// <summary>
    /// Node trong cây danh mục đa cấp. Dùng đệ quy để vẽ CategoryTreeDropdown.
    /// </summary>
    public class CategoryTreeNode
    {
        /// <summary>Tên hiển thị của node (không có tiền tố >>).</summary>
        public string Label { get; set; }

        /// <summary>Đường dẫn đầy đủ từ gốc đến node này, ví dụ "Cha >> Con >> Cháu".</summary>
        public string FullPath { get; set; }

        /// <summary>Độ sâu của node trong cây (root = 0).</summary>
        public int Level { get; set; }

        /// <summary>Trạng thái mở rộng – true = đang hiện các node con.</summary>
        public bool IsExpanded { get; set; } = true;

        /// <summary>Danh sách node con trực tiếp.</summary>
        public List<CategoryTreeNode> Children { get; set; } = new List<CategoryTreeNode>();

        /// <summary>True nếu node này là lá (không có con).</summary>
        public bool IsLeaf => Children == null || Children.Count == 0;
    }
}
