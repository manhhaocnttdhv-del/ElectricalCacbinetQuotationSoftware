using System;

namespace ECQ_Soft.Model
{
    public class ExportInfo
    {
        public string KinhGui { get; set; }
        public string DiaChi { get; set; }
        public string NguoiNhan { get; set; }
        public string MaSoThue { get; set; }
        public string NoiDung { get; set; }
        public string Format { get; set; } // "Excel" or "PDF"
    }
}
