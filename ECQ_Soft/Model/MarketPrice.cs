using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECQ_Soft.Model
{
    public class MarketPrice
    {
        public int Stt { get; set; }
        public int MaterialId {get; set;}
        public int CabinetId {get; set;}
        public int CoatingTypeId {get; set;}
        public int CommonPrice { get; set;}
        public int VPAPrice {  get; set;}

    }
}
