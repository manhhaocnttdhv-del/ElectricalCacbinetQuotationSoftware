using ECQ_Soft.Model;
using Newtonsoft.Json;

namespace ECQ_Soft.Services.Database
{
    /// <summary>
    /// DTO ánh xạ 1 dòng từ <see cref="Products"/> hoặc <see cref="ConfigProductItem"/>
    /// sang record để insert vào bảng ECQ_BuildConfigItem.
    /// </summary>
    internal class BuildConfigItemSnapshot
    {
        public int SortOrder { get; set; }
        public int? ProductId { get; set; }
        public int? ProductSheetRowIndex { get; set; }
        public string STT { get; set; }
        public string ProductName { get; set; }
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
        public string Brand { get; set; }
        public string Status { get; set; }
        public string Pole { get; set; }
        public string Ir { get; set; }
        public string Icu { get; set; }
        public string PriceList { get; set; }
        public int Quantity { get; set; }
        public string Unit { get; set; }
        public string Origin { get; set; }
        public decimal SellPrice { get; set; }
        public decimal SellAmount { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal BuyAmount { get; set; }
        public decimal Profit { get; set; }
        public decimal QuotationPrice { get; set; }
        public string Note { get; set; }
        public bool IsHeader { get; set; }
        public bool IsSummary { get; set; }
        public string ExtraAttributesJson { get; set; }

        public static BuildConfigItemSnapshot FromProduct(Products product, int sortOrder)
        {
            int quantity = product?.SoLuong > 0 ? product.SoLuong : 1;
            decimal sellPrice = ParseDecimal(product?.Price);
            decimal buyPrice = ParseDecimal(product?.PriceCost);

            return new BuildConfigItemSnapshot
            {
                SortOrder = sortOrder,
                ProductId = product?.Id,
                ProductSheetRowIndex = product?.SheetRowIndex,
                STT = sortOrder.ToString(),
                ProductName = product?.Name,
                Model = product?.Model,
                SKU = product?.SKU,
                Price = product?.Price,
                PriceCost = product?.PriceCost,
                Weight = product?.Weight,
                Length = product?.Length,
                Width = product?.Width,
                Height = product?.Height,
                Category = product?.Category,
                Type = product?.Type,
                Brand = product?.HÃNG,
                Status = product?.TrangThai,
                Pole = product?.Pole,
                Ir = product?.Ir,
                Icu = product?.Icu,
                PriceList = product?.PriceList,
                Quantity = quantity,
                Unit = ConfigProductItem.IsPinned(product?.Name) ? "TỦ" : "Cái",
                Origin = product?.HÃNG,
                SellPrice = sellPrice,
                SellAmount = sellPrice * quantity,
                BuyPrice = buyPrice,
                BuyAmount = buyPrice * quantity,
                Profit = (sellPrice - buyPrice) * quantity,
                ExtraAttributesJson = product?.ExtraAttributes == null || product.ExtraAttributes.Count == 0
                    ? null
                    : JsonConvert.SerializeObject(product.ExtraAttributes)
            };
        }

        public static BuildConfigItemSnapshot FromConfigItem(ConfigProductItem item, Products sourceProduct, int sortOrder)
        {
            var snapshot = sourceProduct == null
                ? new BuildConfigItemSnapshot()
                : FromProduct(sourceProduct, sortOrder);

            snapshot.SortOrder = sortOrder;
            snapshot.STT = item?.STT ?? sortOrder.ToString();
            snapshot.ProductName = item?.TenHang ?? snapshot.ProductName;
            snapshot.SKU = item?.MaHang ?? snapshot.SKU;
            snapshot.Quantity = item?.SoLuong > 0 ? item.SoLuong : snapshot.Quantity;
            snapshot.Unit = item?.DonVi ?? snapshot.Unit;
            snapshot.Origin = item?.XuatXu ?? snapshot.Origin;
            snapshot.SellPrice = item?.DonGiaVND ?? snapshot.SellPrice;
            snapshot.SellAmount = item?.ThanhTienVND ?? snapshot.SellAmount;
            snapshot.BuyPrice = item?.GiaNhap ?? snapshot.BuyPrice;
            snapshot.BuyAmount = item?.ThanhTien ?? snapshot.BuyAmount;
            snapshot.Profit = item?.LoiNhuan ?? snapshot.Profit;
            snapshot.QuotationPrice = item?.BangGia ?? snapshot.QuotationPrice;
            snapshot.Note = item?.GhiChu;
            snapshot.IsHeader = item?.IsHeader ?? false;
            snapshot.IsSummary = item?.IsSummary ?? false;
            return snapshot;
        }

        private static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0m;
            string clean = value.Replace(".", "").Replace(",", "").Replace("₫", "").Trim();
            decimal.TryParse(clean, out decimal result);
            return result;
        }
    }
}
