using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECQ_Soft.Model
{
    public class Record
    {
        public int? Stt {  get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public uint? Quantity { get; set; }
        public float? WeightperUnit { get; set; }      
        public uint? MarketUnitPrice { get; set; }
        public ulong MarketTotalPrice { get; set; }
        public string Note { get; set; }
        public uint? HMEUnitPrice { get; set; }
        public ulong HMETotalPrice { get; set; }       
        public uint? VPAUnitPrice { get; set; }
        public ulong VPATotalPrice { get; set; }

        public float? Weight { get; set; }

    }
}
