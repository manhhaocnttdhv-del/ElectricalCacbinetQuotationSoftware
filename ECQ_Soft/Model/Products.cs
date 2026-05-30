using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECQ_Soft.Model
{
    public class Products
    {
        public int SheetRowIndex { get; set; } // Lưu số thứ tự dòng trên Google Sheets (bắt đầu từ 1)
        public int Id { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public string SKU { get; set; }
        public string Price { get; set; }
        public string PriceCost { get; set; }
        public string Weight { get; set; }
        public string Length { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string Category { get; set; }
        public string Type { get; set; }
        public string HÃNG { get; set; }
        public string TrangThai { get; set; }
        public string Pole { get; set; }
        public string Ir { get; set; }
        public string Icu { get; set; }
        public string PriceList { get; set; }
        public bool IsSelected { get; set; }
        [Browsable(false)]
        public bool IsHeader { get; set; }

        /// <summary>True nếu đây là dòng tiêu đề cấu hình (group). Chỉ dùng cho UI tree trong tab Xây dựng cấu hình.</summary>
        [Browsable(false)]
        public bool IsConfigHeader { get; set; }

        /// <summary>Key gom nhóm các sản phẩm con vào cùng một header (ví dụ: "Donggoi_1|tủ điện").</summary>
        [Browsable(false)]
        public string ConfigGroupKey { get; set; }

        /// <summary>True nếu group đang được expand (hiển thị sản phẩm con).</summary>
        [Browsable(false)]
        public bool IsConfigExpanded { get; set; }

        /// <summary>Số lượng sản phẩm con trong group (chỉ set cho dòng header).</summary>
        [Browsable(false)]
        public int ConfigChildCount { get; set; }
        /// <summary>Số lượng từ gói đóng gói khi tải về dataGridView1.</summary>
        public int SoLuong { get; set; } = 1;
        // public string TienDo { get; set; }

        /// <summary>
        /// Các thuộc tính mở rộng ngoài cột chuẩn (icu, ir, pole, color...).
        /// Key = tên cột (lowercase), Value = giá trị chuỗi.
        /// </summary>
        [Browsable(false)]
        public Dictionary<string, string> ExtraAttributes { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private List<KeyValuePair<string, string>> _normalizedExtraAttributes;
        private int _normalizedExtraAttributesCount = -1;

        private static string NormalizeExtraKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "";
            return key.Replace("\n", " ").Replace("\r", " ").Trim().ToLower();
        }

        private void EnsureNormalizedExtraAttributes()
        {
            if (ExtraAttributes == null)
            {
                _normalizedExtraAttributes = null;
                _normalizedExtraAttributesCount = -1;
                return;
            }

            if (_normalizedExtraAttributes != null && _normalizedExtraAttributesCount == ExtraAttributes.Count) return;

            _normalizedExtraAttributes = new List<KeyValuePair<string, string>>(ExtraAttributes.Count);
            foreach (var kvp in ExtraAttributes)
            {
                _normalizedExtraAttributes.Add(new KeyValuePair<string, string>(NormalizeExtraKey(kvp.Key), kvp.Value));
            }
            _normalizedExtraAttributesCount = ExtraAttributes.Count;
        }

        private string GetExtraAttributeWithFallback(string k)
        {
            if (ExtraAttributes.TryGetValue(k, out string v))
                return v ?? "";

            EnsureNormalizedExtraAttributes();
            string normalizedK = NormalizeExtraKey(k);

            if (_normalizedExtraAttributes == null) return "";
            for (int i = 0; i < _normalizedExtraAttributes.Count; i++)
            {
                var kvp = _normalizedExtraAttributes[i];
                string normKey = kvp.Key ?? "";
                if (normKey.StartsWith(normalizedK + " ", StringComparison.OrdinalIgnoreCase) ||
                    normKey.StartsWith(normalizedK + "(", StringComparison.OrdinalIgnoreCase) ||
                    normKey.Equals(normalizedK, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value ?? "";
                }
            }
            return "";
        }

        public string GetAttribute(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";

            string k = key.Replace("\n", " ").Replace("\r", " ").Trim();
            if (k.Length == 0) return "";
            k = k.ToLower();
            
            // 1. Kiểm tra trực tiếp các cụm từ/chứa từ khóa quan trọng để ánh xạ về thuộc tính chuẩn
            if (k.Contains("height") || k.Contains("cao") || k == "h")
                return !string.IsNullOrEmpty(Height) ? Height : GetExtraAttributeWithFallback("height");
                
            if (k.Contains("width") || k.Contains("rộng") || k.Contains("rong") || k == "w")
                return !string.IsNullOrEmpty(Width) ? Width : GetExtraAttributeWithFallback("width");
                
            if (k.Contains("length") || k.Contains("dài") || k.Contains("dai") || k.Contains("sâu") || k.Contains("sau") || k == "l" || k == "d")
                return !string.IsNullOrEmpty(Length) ? Length : GetExtraAttributeWithFallback("length");
                
            if (k.Contains("weight") || k.Contains("khối lượng") || k.Contains("khoi luong") || k.Contains("nặng") || k.Contains("nang") || k == "kl" || k == "kg")
                return !string.IsNullOrEmpty(Weight) ? Weight : GetExtraAttributeWithFallback("weight");
                
            if (k == "price" || k == "p" || k == "gv" || k.Contains("giá bán") || k.Contains("gia ban"))
                return !string.IsNullOrEmpty(Price) ? Price : GetExtraAttributeWithFallback("price");
                
            if (k == "pricecost" || k == "cost" || k == "goc" || k.Contains("giá vốn") || k.Contains("gia von"))
                return !string.IsNullOrEmpty(PriceCost) ? PriceCost : GetExtraAttributeWithFallback("pricecost");
                
            if (k == "pole" || k.Contains("số cực") || k.Contains("so cuc"))
                return !string.IsNullOrEmpty(Pole) ? Pole : GetExtraAttributeWithFallback("pole");
                
            if (k == "ir" || k.Contains("i rate"))
                return !string.IsNullOrEmpty(Ir) ? Ir : GetExtraAttributeWithFallback("ir");
                
            if (k == "icu")
                return !string.IsNullOrEmpty(Icu) ? Icu : GetExtraAttributeWithFallback("icu");

            // 2. Tra cứu switch case cho các trường còn lại
            switch (k)
            {
                case "name": return Name ?? "";
                case "model": return Model ?? "";
                case "sku": return SKU ?? "";
                case "category": return Category ?? "";
                case "type": return Type ?? "";
                case "hãng": case "hang": case "brand": return !string.IsNullOrEmpty(HÃNG) ? HÃNG : GetExtraAttributeWithFallback("hãng");
                case "trangthai": return !string.IsNullOrEmpty(TrangThai) ? TrangThai : GetExtraAttributeWithFallback("trangthai");
                case "pricelist": return !string.IsNullOrEmpty(PriceList) ? PriceList : GetExtraAttributeWithFallback("pricelist");
                default:
                    return GetExtraAttributeWithFallback(k);
            }
        }
    }
}
