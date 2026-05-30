using ECQ_Soft.Helper;
using ECQ_Soft.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace ECQ_Soft.Services.Database
{
    /// <summary>
    /// Container kết quả 1 cấu hình + danh sách sản phẩm con đã đọc từ DB.
    /// </summary>
    public class BuildConfigPackage
    {
        public int Id { get; set; }
        public string ConfigName { get; set; }
        public string GoogleSheetName { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
        public List<Products> Items { get; set; } = new List<Products>();
    }

    /// <summary>
    /// Repository CRUD cho 2 bảng ECQ_BuildConfig + ECQ_BuildConfigItem.
    /// Mọi truy cập đều đảm bảo schema đã được tạo (idempotent).
    /// </summary>
    internal static class BuildConfigRepository
    {
        // ── DDL (chạy lần đầu) ─────────────────────────────────────────────

        public static void EnsureTables()
        {
            DbHelpers.ExecuteNonQuery(EcqBuildConfigSchema.CreateScript);
        }

        // ── SAVE (Insert/Update với transaction) ───────────────────────────

        public static int SaveBuildConfigFromProducts(
            string configName,
            string googleSheetName,
            string googleSpreadsheetId,
            IEnumerable<Products> products,
            bool overwriteMode)
        {
            var items = (products ?? Enumerable.Empty<Products>())
                .Select((product, index) => BuildConfigItemSnapshot.FromProduct(product, index + 1))
                .ToList();

            return SaveSnapshot("BUILD_PACKAGE", configName, googleSheetName, googleSpreadsheetId, items, overwriteMode);
        }

        public static int SaveBuildConfigFromConfigItems(
            string configName,
            string googleSheetName,
            string googleSpreadsheetId,
            IEnumerable<ConfigProductItem> configItems,
            IEnumerable<Products> sourceProducts = null)
        {
            var productLookup = (sourceProducts ?? Enumerable.Empty<Products>())
                .Where(p => !string.IsNullOrWhiteSpace(p.SKU))
                .GroupBy(p => p.SKU.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var items = (configItems ?? Enumerable.Empty<ConfigProductItem>())
                .Where(item => !item.IsSummary)
                .Select((item, index) =>
                {
                    Products product = null;
                    if (!string.IsNullOrWhiteSpace(item.MaHang))
                        productLookup.TryGetValue(item.MaHang.Trim(), out product);
                    return BuildConfigItemSnapshot.FromConfigItem(item, product, index + 1);
                })
                .ToList();

            return SaveSnapshot("QUOTATION", configName, googleSheetName, googleSpreadsheetId, items, true);
        }

        private static int SaveSnapshot(
            string configType,
            string configName,
            string googleSheetName,
            string googleSpreadsheetId,
            List<BuildConfigItemSnapshot> items,
            bool overwriteMode)
        {
            EnsureTables();

            using (var conn = DbHelpers.OpenConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    int buildConfigId = FindExistingId(conn, tran, configType, configName, googleSheetName);

                    if (buildConfigId > 0)
                    {
                        DbHelpers.ExecuteNonQuery(conn, tran,
                            @"UPDATE [dbo].[ECQ_BuildConfig]
                              SET [GoogleSpreadsheetId] = @spreadsheetId,
                                  [ItemCount] = @itemCount,
                                  [OverwriteMode] = @overwriteMode,
                                  [CreatedByUserId] = @userId,
                                  [CreatedByUsername] = @username,
                                  [UpdatedOnUtc] = SYSUTCDATETIME(),
                                  [IsDeleted] = 0
                              WHERE [Id] = @id",
                            new[]
                            {
                                DbHelpers.Param("@spreadsheetId", googleSpreadsheetId),
                                DbHelpers.Param("@itemCount", items.Count),
                                DbHelpers.Param("@overwriteMode", overwriteMode),
                                DbHelpers.Param("@userId", UserSession.UserId > 0 ? (object)UserSession.UserId : DBNull.Value),
                                DbHelpers.Param("@username", UserSession.Username),
                                DbHelpers.Param("@id", buildConfigId)
                            });

                        DbHelpers.ExecuteNonQuery(conn, tran,
                            "DELETE FROM [dbo].[ECQ_BuildConfigItem] WHERE [BuildConfigId] = @id",
                            new[] { DbHelpers.Param("@id", buildConfigId) });
                    }
                    else
                    {
                        buildConfigId = Convert.ToInt32(DbHelpers.ExecuteScalar(conn, tran,
                            @"INSERT INTO [dbo].[ECQ_BuildConfig]
                                ([ConfigType], [ConfigName], [GoogleSheetName], [GoogleSpreadsheetId], [ItemCount], [OverwriteMode],
                                 [CreatedByUserId], [CreatedByUsername], [CreatedOnUtc], [UpdatedOnUtc], [IsDeleted])
                              OUTPUT INSERTED.Id
                              VALUES
                                (@type, @name, @sheetName, @spreadsheetId, @itemCount, @overwriteMode,
                                 @userId, @username, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)",
                            new[]
                            {
                                DbHelpers.Param("@type", configType),
                                DbHelpers.Param("@name", configName),
                                DbHelpers.Param("@sheetName", googleSheetName),
                                DbHelpers.Param("@spreadsheetId", googleSpreadsheetId),
                                DbHelpers.Param("@itemCount", items.Count),
                                DbHelpers.Param("@overwriteMode", overwriteMode),
                                DbHelpers.Param("@userId", UserSession.UserId > 0 ? (object)UserSession.UserId : DBNull.Value),
                                DbHelpers.Param("@username", UserSession.Username)
                            }));
                    }

                    foreach (var item in items)
                        InsertItem(conn, tran, buildConfigId, item);

                    tran.Commit();
                    return buildConfigId;
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }

        private static int FindExistingId(SqlConnection conn, SqlTransaction tran,
            string configType, string configName, string googleSheetName)
        {
            object value = DbHelpers.ExecuteScalar(conn, tran,
                @"SELECT TOP 1 [Id]
                  FROM [dbo].[ECQ_BuildConfig]
                  WHERE [ConfigType] = @type
                    AND [ConfigName] = @name
                    AND ISNULL([GoogleSheetName], '') = ISNULL(@sheetName, '')
                    AND [IsDeleted] = 0
                  ORDER BY [Id] DESC",
                new[]
                {
                    DbHelpers.Param("@type", configType),
                    DbHelpers.Param("@name", configName),
                    DbHelpers.Param("@sheetName", googleSheetName)
                });

            return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private static void InsertItem(SqlConnection conn, SqlTransaction tran,
            int buildConfigId, BuildConfigItemSnapshot item)
        {
            DbHelpers.ExecuteNonQuery(conn, tran,
                @"INSERT INTO [dbo].[ECQ_BuildConfigItem]
                    ([BuildConfigId], [SortOrder], [ProductId], [ProductSheetRowIndex], [STT], [ProductName], [Model], [SKU],
                     [Price], [PriceCost], [Weight], [Length], [Width], [Height], [Category], [Type], [Brand], [Status],
                     [Pole], [Ir], [Icu], [PriceList], [Quantity], [Unit], [Origin], [SellPrice], [SellAmount],
                     [BuyPrice], [BuyAmount], [Profit], [QuotationPrice], [Note], [IsHeader], [IsSummary], [ExtraAttributesJson])
                  VALUES
                    (@buildConfigId, @sortOrder, @productId, @productSheetRowIndex, @stt, @productName, @model, @sku,
                     @price, @priceCost, @weight, @length, @width, @height, @category, @type, @brand, @status,
                     @pole, @ir, @icu, @priceList, @quantity, @unit, @origin, @sellPrice, @sellAmount,
                     @buyPrice, @buyAmount, @profit, @quotationPrice, @note, @isHeader, @isSummary, @extraAttributesJson)",
                new[]
                {
                    DbHelpers.Param("@buildConfigId", buildConfigId),
                    DbHelpers.Param("@sortOrder", item.SortOrder),
                    DbHelpers.Param("@productId", item.ProductId),
                    DbHelpers.Param("@productSheetRowIndex", item.ProductSheetRowIndex),
                    DbHelpers.Param("@stt", item.STT),
                    DbHelpers.Param("@productName", item.ProductName),
                    DbHelpers.Param("@model", item.Model),
                    DbHelpers.Param("@sku", item.SKU),
                    DbHelpers.Param("@price", item.Price),
                    DbHelpers.Param("@priceCost", item.PriceCost),
                    DbHelpers.Param("@weight", item.Weight),
                    DbHelpers.Param("@length", item.Length),
                    DbHelpers.Param("@width", item.Width),
                    DbHelpers.Param("@height", item.Height),
                    DbHelpers.Param("@category", item.Category),
                    DbHelpers.Param("@type", item.Type),
                    DbHelpers.Param("@brand", item.Brand),
                    DbHelpers.Param("@status", item.Status),
                    DbHelpers.Param("@pole", item.Pole),
                    DbHelpers.Param("@ir", item.Ir),
                    DbHelpers.Param("@icu", item.Icu),
                    DbHelpers.Param("@priceList", item.PriceList),
                    DbHelpers.Param("@quantity", item.Quantity),
                    DbHelpers.Param("@unit", item.Unit),
                    DbHelpers.Param("@origin", item.Origin),
                    DbHelpers.Param("@sellPrice", item.SellPrice),
                    DbHelpers.Param("@sellAmount", item.SellAmount),
                    DbHelpers.Param("@buyPrice", item.BuyPrice),
                    DbHelpers.Param("@buyAmount", item.BuyAmount),
                    DbHelpers.Param("@profit", item.Profit),
                    DbHelpers.Param("@quotationPrice", item.QuotationPrice),
                    DbHelpers.Param("@note", item.Note),
                    DbHelpers.Param("@isHeader", item.IsHeader),
                    DbHelpers.Param("@isSummary", item.IsSummary),
                    DbHelpers.Param("@extraAttributesJson", item.ExtraAttributesJson)
                });
        }

        // ── DELETE (soft) ──────────────────────────────────────────────────

        public static int Delete(int configId)
        {
            if (configId <= 0) return 0;
            EnsureTables();
            return DbHelpers.ExecuteNonQuery(
                @"UPDATE [dbo].[ECQ_BuildConfig]
                  SET [IsDeleted] = 1, [UpdatedOnUtc] = SYSUTCDATETIME()
                  WHERE [Id] = @id",
                new[] { DbHelpers.Param("@id", configId) });
        }

        // ── READ ───────────────────────────────────────────────────────────

        public static List<BuildConfigPackage> GetAll(string configType = "BUILD_PACKAGE")
        {
            EnsureTables();

            var packages = new List<BuildConfigPackage>();

            const string headerSql = @"
                SELECT [Id], [ConfigName], [GoogleSheetName], [UpdatedOnUtc]
                FROM [dbo].[ECQ_BuildConfig]
                WHERE [ConfigType] = @type AND [IsDeleted] = 0
                ORDER BY [ConfigName] ASC, [Id] ASC";

            const string itemSql = @"
                SELECT [Id], [BuildConfigId], [SortOrder], [ProductId], [ProductSheetRowIndex], [STT],
                       [ProductName], [Model], [SKU], [Price], [PriceCost], [Weight], [Length], [Width], [Height],
                       [Category], [Type], [Brand], [Status], [Pole], [Ir], [Icu], [PriceList],
                       [Quantity], [Unit], [Origin], [SellPrice], [SellAmount], [BuyPrice], [BuyAmount],
                       [Profit], [QuotationPrice], [Note], [IsHeader], [IsSummary], [ExtraAttributesJson]
                FROM [dbo].[ECQ_BuildConfigItem]
                WHERE [BuildConfigId] IN (SELECT [Id] FROM [dbo].[ECQ_BuildConfig]
                                          WHERE [ConfigType] = @type AND [IsDeleted] = 0)
                ORDER BY [BuildConfigId] ASC, [SortOrder] ASC, [Id] ASC";

            DataTable headers = DbHelpers.ExecuteQuery(headerSql, new[] { DbHelpers.Param("@type", configType) });
            DataTable items = DbHelpers.ExecuteQuery(itemSql, new[] { DbHelpers.Param("@type", configType) });

            // Group items by BuildConfigId
            var itemsByConfig = new Dictionary<int, List<Products>>();
            foreach (DataRow row in items.Rows)
            {
                int configId = DbHelpers.ToInt(row["BuildConfigId"]);
                if (!itemsByConfig.TryGetValue(configId, out var list))
                {
                    list = new List<Products>();
                    itemsByConfig[configId] = list;
                }
                list.Add(MapItemRowToProduct(row));
            }

            foreach (DataRow row in headers.Rows)
            {
                int id = DbHelpers.ToInt(row["Id"]);
                var pkg = new BuildConfigPackage
                {
                    Id = id,
                    ConfigName = row["ConfigName"]?.ToString(),
                    GoogleSheetName = row["GoogleSheetName"]?.ToString(),
                    UpdatedOnUtc = row["UpdatedOnUtc"] is DateTime dt ? dt : DateTime.MinValue
                };
                if (itemsByConfig.TryGetValue(id, out var list))
                    pkg.Items.AddRange(list.Where(p => !p.IsHeader));
                packages.Add(pkg);
            }

            return packages;
        }

        private static Products MapItemRowToProduct(DataRow row)
        {
            var p = new Products
            {
                Id = DbHelpers.ToNullableInt(row["ProductId"]) ?? 0,
                SheetRowIndex = DbHelpers.ToNullableInt(row["ProductSheetRowIndex"]) ?? 0,
                Name = row["ProductName"]?.ToString(),
                Model = row["Model"]?.ToString(),
                SKU = row["SKU"]?.ToString(),
                Price = row["Price"]?.ToString(),
                PriceCost = row["PriceCost"]?.ToString(),
                Weight = row["Weight"]?.ToString(),
                Length = row["Length"]?.ToString(),
                Width = row["Width"]?.ToString(),
                Height = row["Height"]?.ToString(),
                Category = row["Category"]?.ToString(),
                Type = row["Type"]?.ToString(),
                HÃNG = row["Brand"]?.ToString(),
                TrangThai = row["Status"]?.ToString(),
                Pole = row["Pole"]?.ToString(),
                Ir = row["Ir"]?.ToString(),
                Icu = row["Icu"]?.ToString(),
                PriceList = row["PriceList"]?.ToString(),
                SoLuong = DbHelpers.ToInt(row["Quantity"]) > 0 ? DbHelpers.ToInt(row["Quantity"]) : 1,
                IsHeader = row["IsHeader"] != DBNull.Value && Convert.ToBoolean(row["IsHeader"])
            };

            string extraJson = row["ExtraAttributesJson"]?.ToString();
            if (!string.IsNullOrWhiteSpace(extraJson))
            {
                try
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(extraJson);
                    if (dict != null)
                    {
                        foreach (var kvp in dict)
                            p.ExtraAttributes[kvp.Key] = kvp.Value;
                    }
                }
                catch { /* JSON xấu thì bỏ qua */ }
            }

            string unit = row["Unit"]?.ToString();
            if (!string.IsNullOrWhiteSpace(unit)) p.ExtraAttributes["DonVi"] = unit;
            string origin = row["Origin"]?.ToString();
            if (!string.IsNullOrWhiteSpace(origin) && string.IsNullOrWhiteSpace(p.HÃNG)) p.HÃNG = origin;
            string note = row["Note"]?.ToString();
            if (!string.IsNullOrWhiteSpace(note)) p.ExtraAttributes["GhiChu"] = note;

            return p;
        }
    }
}
