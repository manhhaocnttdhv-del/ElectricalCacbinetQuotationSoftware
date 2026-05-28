using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using ECQ_Soft.Helper;
using ECQ_Soft.Model;
using Newtonsoft.Json;

namespace ECQ_Soft.Services
{
    public static class DatabaseService
    {
        private static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["SqlServerVnecco"]?.ConnectionString
                ?? "Server=103.121.91.94,1433;Database=vnecco_erp;User ID=erp_vnecco;Password=@vnecco123;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=15;";
        }

        public static SqlConnection GetConnection()
        {
            var conn = new SqlConnection(GetConnectionString());
            conn.Open();
            return conn;
        }

        public static DataTable ExecuteQuery(string sql, SqlParameter[] parameters = null)
        {
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public static int ExecuteNonQuery(string sql, SqlParameter[] parameters = null)
        {
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        public static void EnsureEcqBuildConfigTables()
        {
            const string sql = @"
IF OBJECT_ID(N'[dbo].[ECQ_BuildConfigItem]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ECQ_BuildConfigItem] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ECQ_BuildConfigItem] PRIMARY KEY,
        [BuildConfigId] INT NOT NULL,
        [SortOrder] INT NOT NULL,
        [ProductId] INT NULL,
        [ProductSheetRowIndex] INT NULL,
        [STT] NVARCHAR(50) NULL,
        [ProductName] NVARCHAR(500) NULL,
        [Model] NVARCHAR(255) NULL,
        [SKU] NVARCHAR(255) NULL,
        [Price] NVARCHAR(100) NULL,
        [PriceCost] NVARCHAR(100) NULL,
        [Weight] NVARCHAR(100) NULL,
        [Length] NVARCHAR(100) NULL,
        [Width] NVARCHAR(100) NULL,
        [Height] NVARCHAR(100) NULL,
        [Category] NVARCHAR(255) NULL,
        [Type] NVARCHAR(255) NULL,
        [Brand] NVARCHAR(255) NULL,
        [Status] NVARCHAR(255) NULL,
        [Pole] NVARCHAR(100) NULL,
        [Ir] NVARCHAR(100) NULL,
        [Icu] NVARCHAR(100) NULL,
        [PriceList] NVARCHAR(255) NULL,
        [Quantity] INT NOT NULL CONSTRAINT [DF_ECQ_BuildConfigItem_Quantity] DEFAULT(1),
        [Unit] NVARCHAR(100) NULL,
        [Origin] NVARCHAR(255) NULL,
        [SellPrice] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_ECQ_BuildConfigItem_SellPrice] DEFAULT(0),
        [SellAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_ECQ_BuildConfigItem_SellAmount] DEFAULT(0),
        [BuyPrice] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_ECQ_BuildConfigItem_BuyPrice] DEFAULT(0),
        [BuyAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_ECQ_BuildConfigItem_BuyAmount] DEFAULT(0),
        [Profit] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_ECQ_BuildConfigItem_Profit] DEFAULT(0),
        [QuotationPrice] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_ECQ_BuildConfigItem_QuotationPrice] DEFAULT(0),
        [Note] NVARCHAR(MAX) NULL,
        [IsHeader] BIT NOT NULL CONSTRAINT [DF_ECQ_BuildConfigItem_IsHeader] DEFAULT(0),
        [IsSummary] BIT NOT NULL CONSTRAINT [DF_ECQ_BuildConfigItem_IsSummary] DEFAULT(0),
        [ExtraAttributesJson] NVARCHAR(MAX) NULL,
        [CreatedOnUtc] DATETIME2(0) NOT NULL CONSTRAINT [DF_ECQ_BuildConfigItem_CreatedOnUtc] DEFAULT(SYSUTCDATETIME())
    );
END;

IF OBJECT_ID(N'[dbo].[ECQ_BuildConfig]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ECQ_BuildConfig] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ECQ_BuildConfig] PRIMARY KEY,
        [ConfigType] NVARCHAR(50) NOT NULL,
        [ConfigName] NVARCHAR(255) NOT NULL,
        [GoogleSheetName] NVARCHAR(255) NULL,
        [GoogleSpreadsheetId] NVARCHAR(255) NULL,
        [ItemCount] INT NOT NULL CONSTRAINT [DF_ECQ_BuildConfig_ItemCount] DEFAULT(0),
        [OverwriteMode] BIT NOT NULL CONSTRAINT [DF_ECQ_BuildConfig_OverwriteMode] DEFAULT(0),
        [CreatedByUserId] INT NULL,
        [CreatedByUsername] NVARCHAR(255) NULL,
        [CreatedOnUtc] DATETIME2(0) NOT NULL CONSTRAINT [DF_ECQ_BuildConfig_CreatedOnUtc] DEFAULT(SYSUTCDATETIME()),
        [UpdatedOnUtc] DATETIME2(0) NOT NULL CONSTRAINT [DF_ECQ_BuildConfig_UpdatedOnUtc] DEFAULT(SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL CONSTRAINT [DF_ECQ_BuildConfig_IsDeleted] DEFAULT(0)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ECQ_BuildConfigItem_ECQ_BuildConfig'
)
BEGIN
    ALTER TABLE [dbo].[ECQ_BuildConfigItem]
    ADD CONSTRAINT [FK_ECQ_BuildConfigItem_ECQ_BuildConfig]
        FOREIGN KEY ([BuildConfigId]) REFERENCES [dbo].[ECQ_BuildConfig]([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'IX_ECQ_BuildConfig_TypeSheetName' AND object_id = OBJECT_ID(N'[dbo].[ECQ_BuildConfig]')
)
BEGIN
    CREATE INDEX [IX_ECQ_BuildConfig_TypeSheetName]
    ON [dbo].[ECQ_BuildConfig] ([ConfigType], [GoogleSheetName], [ConfigName]);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'IX_ECQ_BuildConfigItem_ConfigSku' AND object_id = OBJECT_ID(N'[dbo].[ECQ_BuildConfigItem]')
)
BEGIN
    CREATE INDEX [IX_ECQ_BuildConfigItem_ConfigSku]
    ON [dbo].[ECQ_BuildConfigItem] ([BuildConfigId], [SKU]);
END;";

            ExecuteNonQuery(sql);
        }

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

            return SaveBuildConfigSnapshot("BUILD_PACKAGE", configName, googleSheetName, googleSpreadsheetId, items, overwriteMode);
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
                    {
                        productLookup.TryGetValue(item.MaHang.Trim(), out product);
                    }

                    return BuildConfigItemSnapshot.FromConfigItem(item, product, index + 1);
                })
                .ToList();

            return SaveBuildConfigSnapshot("QUOTATION", configName, googleSheetName, googleSpreadsheetId, items, true);
        }

        private static int SaveBuildConfigSnapshot(
            string configType,
            string configName,
            string googleSheetName,
            string googleSpreadsheetId,
            List<BuildConfigItemSnapshot> items,
            bool overwriteMode)
        {
            EnsureEcqBuildConfigTables();

            using (var conn = GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    int buildConfigId = FindExistingBuildConfigId(conn, tran, configType, configName, googleSheetName);

                    if (buildConfigId > 0)
                    {
                        ExecuteNonQuery(conn, tran,
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
                                Param("@spreadsheetId", googleSpreadsheetId),
                                Param("@itemCount", items.Count),
                                Param("@overwriteMode", overwriteMode),
                                Param("@userId", UserSession.UserId > 0 ? (object)UserSession.UserId : DBNull.Value),
                                Param("@username", UserSession.Username),
                                Param("@id", buildConfigId)
                            });

                        ExecuteNonQuery(conn, tran,
                            "DELETE FROM [dbo].[ECQ_BuildConfigItem] WHERE [BuildConfigId] = @id",
                            new[] { Param("@id", buildConfigId) });
                    }
                    else
                    {
                        buildConfigId = Convert.ToInt32(ExecuteScalar(conn, tran,
                            @"INSERT INTO [dbo].[ECQ_BuildConfig]
                                ([ConfigType], [ConfigName], [GoogleSheetName], [GoogleSpreadsheetId], [ItemCount], [OverwriteMode],
                                 [CreatedByUserId], [CreatedByUsername], [CreatedOnUtc], [UpdatedOnUtc], [IsDeleted])
                              OUTPUT INSERTED.Id
                              VALUES
                                (@type, @name, @sheetName, @spreadsheetId, @itemCount, @overwriteMode,
                                 @userId, @username, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)",
                            new[]
                            {
                                Param("@type", configType),
                                Param("@name", configName),
                                Param("@sheetName", googleSheetName),
                                Param("@spreadsheetId", googleSpreadsheetId),
                                Param("@itemCount", items.Count),
                                Param("@overwriteMode", overwriteMode),
                                Param("@userId", UserSession.UserId > 0 ? (object)UserSession.UserId : DBNull.Value),
                                Param("@username", UserSession.Username)
                            }));
                    }

                    foreach (var item in items)
                    {
                        InsertBuildConfigItem(conn, tran, buildConfigId, item);
                    }

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

        private static int FindExistingBuildConfigId(SqlConnection conn, SqlTransaction tran, string configType, string configName, string googleSheetName)
        {
            object value = ExecuteScalar(conn, tran,
                @"SELECT TOP 1 [Id]
                  FROM [dbo].[ECQ_BuildConfig]
                  WHERE [ConfigType] = @type
                    AND [ConfigName] = @name
                    AND ISNULL([GoogleSheetName], '') = ISNULL(@sheetName, '')
                    AND [IsDeleted] = 0
                  ORDER BY [Id] DESC",
                new[]
                {
                    Param("@type", configType),
                    Param("@name", configName),
                    Param("@sheetName", googleSheetName)
                });

            return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private static void InsertBuildConfigItem(SqlConnection conn, SqlTransaction tran, int buildConfigId, BuildConfigItemSnapshot item)
        {
            ExecuteNonQuery(conn, tran,
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
                    Param("@buildConfigId", buildConfigId),
                    Param("@sortOrder", item.SortOrder),
                    Param("@productId", item.ProductId),
                    Param("@productSheetRowIndex", item.ProductSheetRowIndex),
                    Param("@stt", item.STT),
                    Param("@productName", item.ProductName),
                    Param("@model", item.Model),
                    Param("@sku", item.SKU),
                    Param("@price", item.Price),
                    Param("@priceCost", item.PriceCost),
                    Param("@weight", item.Weight),
                    Param("@length", item.Length),
                    Param("@width", item.Width),
                    Param("@height", item.Height),
                    Param("@category", item.Category),
                    Param("@type", item.Type),
                    Param("@brand", item.Brand),
                    Param("@status", item.Status),
                    Param("@pole", item.Pole),
                    Param("@ir", item.Ir),
                    Param("@icu", item.Icu),
                    Param("@priceList", item.PriceList),
                    Param("@quantity", item.Quantity),
                    Param("@unit", item.Unit),
                    Param("@origin", item.Origin),
                    Param("@sellPrice", item.SellPrice),
                    Param("@sellAmount", item.SellAmount),
                    Param("@buyPrice", item.BuyPrice),
                    Param("@buyAmount", item.BuyAmount),
                    Param("@profit", item.Profit),
                    Param("@quotationPrice", item.QuotationPrice),
                    Param("@note", item.Note),
                    Param("@isHeader", item.IsHeader),
                    Param("@isSummary", item.IsSummary),
                    Param("@extraAttributesJson", item.ExtraAttributesJson)
                });
        }

        private static object ExecuteScalar(SqlConnection conn, SqlTransaction tran, string sql, SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, conn, tran))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteScalar();
            }
        }

        private static int ExecuteNonQuery(SqlConnection conn, SqlTransaction tran, string sql, SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, conn, tran))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        private static SqlParameter Param(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        public static List<string> GetUserPermissions(int roleId)
        {
            var permissions = new List<string>();
            const string sql = @"
                SELECT pr.SystemName
                FROM [dbo].[PermissionRecord_Role_Mapping] prm
                INNER JOIN [dbo].[PermissionRecord] pr ON pr.Id = prm.PermissionRecord_Id
                WHERE prm.CustomerRole_Id = @roleId";

            try
            {
                DataTable dt = ExecuteQuery(sql, new[]
                {
                    new SqlParameter("@roleId", roleId)
                });

                foreach (DataRow row in dt.Rows)
                {
                    permissions.Add(row["SystemName"].ToString());
                }

                if (permissions.Contains("AccessAdminPanel"))
                {
                    AddEcqAdminPermissions(permissions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Loi tai quyen han SQL Server: " + ex.Message);
            }

            return permissions;
        }

        private static void AddEcqAdminPermissions(List<string> permissions)
        {
            string[] ecqPermissions =
            {
                "quotation:view_all",
                "quotation:view_dept",
                "quotation:update_price",
                "quotation:add_record",
                "quotation:delete_record",
                "quotation:export_excel",
                "relation:view",
                "relation:edit",
                "relation:save",
                "quotation:add_product",
                "quotation:advanced_config",
                "quotation:save_to_quote",
                "quotation:clear_all",
                "config:pack_config",
                "config:view_all",
                "config:view_dept",
                "config:save_quote",
                "config:clear_all",
                "config:export_excel",
                "config:change_sheet",
                "config:load_config",
                "config:sync_sheet",
                "config:edit_formula",
                "quotation:advanced_config:update_name",
                "quotation:advanced_config:apply",
                "quotation:advanced_config:reload",
                "quotation:advanced_config:add_item",
                "quotation:advanced_config:delete_item",
                "user:manage",
                "role:manage",
                "system:view_logs"
            };

            foreach (string permission in ecqPermissions)
            {
                if (!permissions.Contains(permission))
                {
                    permissions.Add(permission);
                }
            }
        }

        public static void InitializeDatabase()
        {
            try
            {
                using (GetConnection())
                {
                }

                EnsureEcqBuildConfigTables();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Loi ket noi SQL Server: " + ex.Message);
            }
        }

        private class BuildConfigItemSnapshot
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
                var snapshot = sourceProduct == null ? new BuildConfigItemSnapshot() : FromProduct(sourceProduct, sortOrder);
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
}
