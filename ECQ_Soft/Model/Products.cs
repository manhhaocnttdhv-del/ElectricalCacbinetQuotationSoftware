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
        public string PriceList { get; set; }
        public bool IsSelected { get; set; }
        // public string TienDo { get; set; }

        /// <summary>
        /// Các thuộc tính mở rộng ngoài cột chuẩn (icu, ir, pole, color...).
        /// Key = tên cột (lowercase), Value = giá trị chuỗi.
        /// </summary>
        [Browsable(false)]
        public Dictionary<string, string> ExtraAttributes { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Lấy giá trị thuộc tính theo tên biến (hỗ trợ cả cột chuẩn lẫn ExtraAttributes).
        /// </summary>
        public string GetAttribute(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            string k = key.Trim().ToLower();
            switch (k)
            {
                case "height": case "h": return Height ?? "";
                case "width":  case "w": return Width  ?? "";
                case "length": case "l": return Length ?? "";
                case "weight": case "kl": case "kg": return Weight ?? "";
                case "price":  case "p": case "gv": return Price ?? "";
                case "pricecost": case "cost": case "goc": return PriceCost ?? "";
                case "name": return Name ?? "";
                case "model": return Model ?? "";
                case "sku": return SKU ?? "";
                case "category": return Category ?? "";
                case "type": return Type ?? "";
                case "hãng": case "hang": case "brand": return HÃNG ?? "";
                case "pricelist": return PriceList ?? "";
                default:
                    if (ExtraAttributes.TryGetValue(k, out string v))
                        return v ?? "";
                    
                    // Thử tìm khớp tương đối (ví dụ: Config truyền 'ir' hoặc 'pole', nhưng trong Excel là 'Ir (I Rate)' hoặc 'Pole (số Cực)')
                    foreach (var kvp in ExtraAttributes)
                    {
                        if (kvp.Key.StartsWith(k + " ", StringComparison.OrdinalIgnoreCase) || 
                            kvp.Key.StartsWith(k + "(", StringComparison.OrdinalIgnoreCase) ||
                            kvp.Key.Equals(k, StringComparison.OrdinalIgnoreCase))
                        {
                            return kvp.Value ?? "";
                        }
                    }
                    return "";
            }
        }
    }
}
