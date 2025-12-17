-- =============================================
-- FourPL_DataTrans_Export_Verify 驗證表
-- 基於原 FourPL_DataTrans_Export 結構，增加狀態欄位
-- ItemPurpose 已改為 NVARCHAR(MAX) 避免截斷
-- =============================================

USE [SAPDS_QAS]
GO

-- 刪除舊表（如存在）
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'FourPL_DataTrans_Export_Verify')
BEGIN
    DROP TABLE [dbo].[FourPL_DataTrans_Export_Verify]
    PRINT '已刪除舊的 FourPL_DataTrans_Export_Verify 表'
END
GO

-- 建立新表
CREATE TABLE [dbo].[FourPL_DataTrans_Export_Verify](
    -- 主鍵改用 INT IDENTITY
    [ExportID] [int] IDENTITY(1,1) NOT NULL,
    
    -- 原有欄位
    [RequisitionID] [varchar](64) NOT NULL,
    [FormNo] [varchar](50) NOT NULL,
    [FormItem] [varchar](6) NOT NULL,
    [FormRefItem] [varchar](6) NOT NULL,
    [ApplicantID] [varchar](50) NOT NULL,
    [SalesOrg] [varchar](4) NOT NULL,
    [DistributionChannel] [varchar](4) NOT NULL,
    [Division] [varchar](2) NOT NULL,
    [ReceivingParty] [char](1) NOT NULL,
    [CustomerNumber] [varchar](10) NOT NULL,
    [CustomerName] [nvarchar](35) NOT NULL,
    [SPNumber] [varchar](10) NOT NULL,
    [ApprovalDate] [varchar](8) NULL,
    [Remark] [nvarchar](max) NULL,
    [ItemCategory] [varchar](4) NULL,
    [PricingType] [char](1) NULL,
    [PricingGroup] [varchar](10) NULL,
    [MaterialCode] [nvarchar](40) NULL,
    [Batch] [varchar](10) NULL,
    [SalesChannel] [varchar](10) NULL,
    [Qty] [numeric](14, 3) NULL,
    [SalesUnit] [varchar](3) NULL,
    [DebitCreditType] [char](1) NULL,
    [Currency] [varchar](5) NULL,
    [InvoicePriceWithTax] [numeric](11, 2) NULL,
    [InvoicePrice] [numeric](11, 2) NULL,
    [TotalInvoicePriceWithTax] [numeric](11, 2) NULL,
    [TotalInvoicePrice] [numeric](11, 2) NULL,
    [FixedPriceWithTax] [numeric](11, 2) NULL,
    [PricingUnit] [nchar](10) NULL,
    [ItemPurpose] [nvarchar](max) NULL,  -- 改為 MAX 避免截斷
    [ReturnCode] [varchar](3) NULL,
    [SalesDate] [varchar](8) NULL,
    [OriginSONumber] [varchar](20) NULL,
    [OriginSOItem] [varchar](6) NULL,
    [NewSONumber] [varchar](20) NULL,
    [NewSOItem] [varchar](6) NULL,
    [InvoiceNumber] [varchar](16) NULL,
    [InvoiceDate] [varchar](8) NULL,
    [CreditNote] [varchar](30) NULL,
    [ValidityPeriod] [varchar](8) NULL,
    [Sloc] [varchar](8) NULL,
    [CostCenter] [varchar](50) NULL,
    
    -- 新增狀態欄位
    [ExportStatus] [int] NOT NULL DEFAULT 0,  -- 0=待匯出, 1=已匯出, 2=錯誤
    [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
    [ExportedAt] [datetime] NULL,
    [ErrorMessage] [nvarchar](500) NULL,
    
    CONSTRAINT [PK_FourPL_DataTrans_Export_Verify] PRIMARY KEY CLUSTERED ([ExportID] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

-- 建立索引
CREATE INDEX [IX_Export_Verify_RequisitionID] ON [dbo].[FourPL_DataTrans_Export_Verify]([RequisitionID])
CREATE INDEX [IX_Export_Verify_ExportStatus] ON [dbo].[FourPL_DataTrans_Export_Verify]([ExportStatus])
CREATE INDEX [IX_Export_Verify_CreatedAt] ON [dbo].[FourPL_DataTrans_Export_Verify]([CreatedAt])
GO

PRINT '已建立 FourPL_DataTrans_Export_Verify 表'
GO

-- =============================================
-- FourPL_DataTrans_Queue_Verify 佇列表
-- 記錄已轉換的 RequisitionID，避免重複處理
-- =============================================

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'FourPL_DataTrans_Queue_Verify')
BEGIN
    DROP TABLE [dbo].[FourPL_DataTrans_Queue_Verify]
    PRINT '已刪除舊的 FourPL_DataTrans_Queue_Verify 表'
END
GO

CREATE TABLE [dbo].[FourPL_DataTrans_Queue_Verify] (
    [QueueID] [int] IDENTITY(1,1) NOT NULL,
    [RequisitionID] [nvarchar](64) NOT NULL,
    [DiagramID] [nvarchar](32) NOT NULL,
    [SerialID] [nvarchar](64) NULL,
    [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),  -- INSERT 時間
    [ProcessedAt] [datetime] NULL,  -- NULL=待處理, 有值=已處理
    [ExportCount] [int] NOT NULL DEFAULT 0,
    
    CONSTRAINT [PK_Queue_Verify] PRIMARY KEY CLUSTERED ([QueueID] ASC),
    CONSTRAINT [UQ_Queue_Verify_RequisitionID] UNIQUE ([RequisitionID])
) ON [PRIMARY]
GO

-- 建立索引
CREATE INDEX [IX_Queue_Verify_DiagramID] ON [dbo].[FourPL_DataTrans_Queue_Verify]([DiagramID])
CREATE INDEX [IX_Queue_Verify_ProcessedAt] ON [dbo].[FourPL_DataTrans_Queue_Verify]([ProcessedAt])
CREATE INDEX [IX_Queue_Verify_CreatedAt] ON [dbo].[FourPL_DataTrans_Queue_Verify]([CreatedAt])
GO

PRINT '已建立 FourPL_DataTrans_Queue_Verify 表'
GO
