using System;
using System.Collections.Generic;
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
        public string HÃNG { get; set; }
        public string PriceList { get; set; }
        public bool IsSelected { get; set; }
    }
}
