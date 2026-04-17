using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECQ_Soft.Model
{
    public class ConfigProductItem
    {
        public string STT { get; set; }
        public string TenHang { get; set; }
        public string MaHang { get; set; }
        public string XuatXu { get; set; }
        public string DonVi { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGiaVND { get; set; }
        public decimal ThanhTienVND { get; set; }
        public string GhiChu { get; set; }
        public decimal GiaNhap { get; set; }
        public decimal ThanhTien { get; set; }
        public decimal BangGia { get; set; }
        // public string TienDo { get; set; }
        
        // Cờ để xác định đây là dòng Header chung/tổng ở trên cùng
        public bool IsHeader { get; set; }

        // Cờ để xác định đây là dòng tổng kết (TỔNG CỘNG, THUẾ VAT, THÀNH TIỀN)
        public bool IsSummary { get; set; }

        // Vị trí dòng trên sheet (0-based, row 2+ trên sheet)
        public int SheetRowIndex { get; set; } = -1;

        // Danh sách các tên sản phẩm "cố định" (không cho di chuyển/ẩn nút ▲▼)
        public static readonly string[] PinnedItemNames =
        {
            "Vỏ tủ trong nhà",
            "Hệ thống đồng thanh cái",
            "Phụ kiện, Vật tư phụ (thanh cái, máng cáp, đầu Cos, sứ đỡ, dây cáp, ruột gà, lạt nhựa, bulong ốc vít...)",
            "Nhân công lắp đặt bên trong tủ"
        };

        public static bool IsPinned(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return false;
            // Kiểm tra khớp hoàn toàn hoặc chứa từ khóa đặc thù (để linh hoạt hơn)
            return PinnedItemNames.Any(p => itemName.Equals(p, StringComparison.OrdinalIgnoreCase) 
                                         || (p.Length > 20 && itemName.Contains(p.Substring(0, 20))));
        }

        public ConfigProductItem Clone()
        {
            return (ConfigProductItem)this.MemberwiseClone();
        }
    }
}
