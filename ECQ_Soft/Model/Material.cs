using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECQ_Soft.Model
{
    public class Material
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public float Q {  get; set; }
        public float Thick { get; set; }
        public int Price { get; set; }
        public int PCFee { get; set; }   // Powder Coating - Sơn tĩnh điện
        public int HDGFee { get; set; } // Hot-Dip Galvanizing - Mạ kẽm nhúng nóng
        public int OrtherFee { get; set; } // Chi phí khác
    }
}
