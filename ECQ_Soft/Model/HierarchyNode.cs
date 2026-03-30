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
