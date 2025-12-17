-- =============================================
-- FourPL_DataTrans_Queue_Verify 佇列表
-- 記錄已轉換的 RequisitionID，避免重複處理
-- =============================================

-- 建立佇列表（如不存在）
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FourPL_DataTrans_Queue_Verify')
BEGIN
    CREATE TABLE [dbo].[FourPL_DataTrans_Queue_Verify] (
        [QueueID] INT IDENTITY(1,1) PRIMARY KEY,
        [RequisitionID] NVARCHAR(64) NOT NULL,
        [DiagramID] NVARCHAR(32) NOT NULL,
        [SerialID] NVARCHAR(64) NULL,
        [ProcessedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [ExportCount] INT NOT NULL DEFAULT 0,
        CONSTRAINT [UQ_Queue_Verify_RequisitionID] UNIQUE ([RequisitionID])
    );
    
    -- 建立索引
    CREATE INDEX [IX_Queue_Verify_DiagramID] ON [dbo].[FourPL_DataTrans_Queue_Verify]([DiagramID]);
    CREATE INDEX [IX_Queue_Verify_ProcessedAt] ON [dbo].[FourPL_DataTrans_Queue_Verify]([ProcessedAt]);
    
    PRINT '已建立 FourPL_DataTrans_Queue_Verify 表';
END
ELSE
BEGIN
    PRINT 'FourPL_DataTrans_Queue_Verify 表已存在';
END
GO
