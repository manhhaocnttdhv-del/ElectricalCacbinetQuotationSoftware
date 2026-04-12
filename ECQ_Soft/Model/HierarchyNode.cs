using System;
using System.Collections.Generic;

namespace ECQ_Soft.Model
{
    /// <summary>
    /// Đại diện cho một nút trong cây phân cấp cấu hình.
    /// Có thể chứa các nút con (Children) hoặc danh sách linh kiện cần thêm (Components).
    /// </summary>
    public class HierarchyNode
    {
        public string Name { get; set; }
        public List<HierarchyNode> Children { get; set; } = new List<HierarchyNode>();
        public List<string> Components { get; set; } = new List<string>();
        
        /// <summary>
        /// Giá trị cột "Config" từ Google Sheet Workflow (ví dụ: "search_sản phẩm", "Id_List"...).
        /// Nếu có giá trị → hiển thị expand panel tương ứng bên dưới node này.
        /// </summary>
        public string Config { get; set; } = "";

        /// <summary>
        /// Công thức tính toán từ cột "Công thức" (ví dụ: =a*b*c, =L*W*H).
        /// Được evaluate sau khi chọn sản phẩm. Biến: a/L=Dài, b/W=Rộng, c/H=Cao, p=Giá.
        /// </summary>
        public string Formula { get; set; } = "";

        public string Type { get; set; } = "";
        public string Category { get; set; } = "";
        public string OnlyOne { get; set; } = "";
        public string Nghia { get; set; } = "";
        public string Bien { get; set; } = "";


        public HierarchyNode(string name)
        {
            Name = name;
        }

        public void AddChild(HierarchyNode child)
        {
            Children.Add(child);
        }

        public void AddComponent(string component)
        {
            Components.Add(component);
        }
    }
}
