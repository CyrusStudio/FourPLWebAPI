-- ============================================
-- 建立 FourPL_DataTrans_Export_Verify 驗證表
-- 用於驗證 C# API 轉換結果與原 SQL 觸發程序結果一致
-- ============================================

USE [SAPDS_QAS]
GO

-- 如果表已存在則先刪除
IF OBJECT_ID('[dbo].[FourPL_DataTrans_Export_Verify]', 'U') IS NOT NULL
    DROP TABLE [dbo].[FourPL_DataTrans_Export_Verify]
GO

-- 從原表複製結構並新增狀態欄位
SELECT TOP 0
    CAST(0 AS INT) AS ExportID,  -- 這會成為 IDENTITY 欄位需另外處理
    RequisitionID, FormNo, FormItem, FormRefItem, ApplicantID,
    SalesOrg, DistributionChannel, Division, ReceivingParty, CustomerNumber,
    CustomerName, SPNumber, ApprovalDate, Remark, ItemCategory,
    PricingType, PricingGroup, MaterialCode, Batch, SalesChannel,
    Qty, SalesUnit, DebitCreditType, Currency, InvoicePriceWithTax, InvoicePrice,
    TotalInvoicePriceWithTax, TotalInvoicePrice, FixedPriceWithTax, PricingUnit, ItemPurpose,
    ReturnCode, SalesDate, OriginSONumber, OriginSOItem, NewSONumber,
    NewSOItem, InvoiceNumber, InvoiceDate, CreditNote, ValidityPeriod, Sloc, CostCenter,
    CAST(0 AS INT) AS ExportStatus,
    GETDATE() AS CreatedAt,
    CAST(NULL AS DATETIME) AS ProcessedAt
INTO [dbo].[FourPL_DataTrans_Export_Verify]
FROM [dbo].[FourPL_DataTrans_Export]
GO

-- 刪除並重新建立以加入 IDENTITY
DROP TABLE [dbo].[FourPL_DataTrans_Export_Verify]
GO

-- 使用正確的欄位型態建立表
CREATE TABLE [dbo].[FourPL_DataTrans_Export_Verify]
(
    [ExportID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [RequisitionID] NVARCHAR(64) NULL,
    [FormNo] NVARCHAR(50) NULL,
    [FormItem] NVARCHAR(20) NULL,
    [FormRefItem] NVARCHAR(20) NULL,
    [ApplicantID] NVARCHAR(50) NULL,
    [SalesOrg] NVARCHAR(10) NULL,
    [DistributionChannel] NVARCHAR(10) NULL,
    [Division] NVARCHAR(10) NULL,
    [ReceivingParty] NVARCHAR(10) NULL,
    [CustomerNumber] NVARCHAR(20) NULL,
    [CustomerName] NVARCHAR(100) NULL,
    [SPNumber] NVARCHAR(20) NULL,
    [ApprovalDate] NVARCHAR(20) NULL,
    [Remark] NVARCHAR(500) NULL,
    [ItemCategory] NVARCHAR(10) NULL,
    [PricingType] NVARCHAR(10) NULL,
    [PricingGroup] NVARCHAR(20) NULL,
    [MaterialCode] NVARCHAR(50) NULL,
    [Batch] NVARCHAR(20) NULL,
    [SalesChannel] NVARCHAR(20) NULL,
    [Qty] DECIMAL(18,4) NULL,
    [SalesUnit] NVARCHAR(10) NULL,
    [DebitCreditType] NVARCHAR(5) NULL,
    [Currency] NVARCHAR(10) NULL,
    [InvoicePriceWithTax] DECIMAL(18,4) NULL,
    [InvoicePrice] DECIMAL(18,4) NULL,
    [TotalInvoicePriceWithTax] DECIMAL(18,4) NULL,
    [TotalInvoicePrice] DECIMAL(18,4) NULL,
    [FixedPriceWithTax] DECIMAL(18,4) NULL,
    [PricingUnit] NVARCHAR(10) NULL,
    [ItemPurpose] NVARCHAR(100) NULL,
    [ReturnCode] NVARCHAR(20) NULL,
    [SalesDate] NVARCHAR(20) NULL,
    [OriginSONumber] NVARCHAR(50) NULL,
    [OriginSOItem] NVARCHAR(20) NULL,
    [NewSONumber] NVARCHAR(50) NULL,
    [NewSOItem] NVARCHAR(20) NULL,
    [InvoiceNumber] NVARCHAR(50) NULL,
    [InvoiceDate] NVARCHAR(20) NULL,
    [CreditNote] NVARCHAR(50) NULL,
    [ValidityPeriod] NVARCHAR(20) NULL,
    [Sloc] NVARCHAR(20) NULL,
    [CostCenter] NVARCHAR(20) NULL,
    -- 額外狀態欄位
    [ExportStatus] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [ProcessedAt] DATETIME NULL
)
GO

-- 建立索引加速查詢
CREATE INDEX IX_Export_Verify_RequisitionID ON [dbo].[FourPL_DataTrans_Export_Verify]([RequisitionID])
CREATE INDEX IX_Export_Verify_ExportStatus ON [dbo].[FourPL_DataTrans_Export_Verify]([ExportStatus])
GO

PRINT '驗證表 FourPL_DataTrans_Export_Verify 建立完成'
GO
