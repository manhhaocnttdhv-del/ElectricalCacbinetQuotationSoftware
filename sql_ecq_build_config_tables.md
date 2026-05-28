# SQL tạo bảng xây dựng cấu hình

```sql
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
END;
```
