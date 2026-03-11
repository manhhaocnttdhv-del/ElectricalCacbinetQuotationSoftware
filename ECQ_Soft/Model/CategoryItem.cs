using System;

namespace ECQ_Soft.Model
{
    public class CategoryItem
    {
        // Chuỗi dùng để hiển thị lên ComboBox có chứa ">> " thụt lề
        public string DisplayText { get; set; }

        // Chuỗi gốc đầy đủ chứa toàn bộ đường dẫn cha (Dùng để Query Google Sheet)
        public string FullPath { get; set; }
    }
}
