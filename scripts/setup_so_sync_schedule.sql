-- =============================================
-- SO 同步排程設定 SQL
-- 設定每天 8:00 ~ 23:00 每小時執行一次
-- =============================================

-- 1. 建立 App_ScheduleConfig 資料表 (若不存在)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='App_ScheduleConfig' AND xtype='U')
BEGIN
    CREATE TABLE App_ScheduleConfig (
        JobId NVARCHAR(100) PRIMARY KEY,
        CronExpression NVARCHAR(50) NOT NULL,
        JobType NVARCHAR(255) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        Description NVARCHAR(500) NULL,
        LastModified DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 2. 插入 SO 同步排程設定
-- Cron 表達式: "0 8-23 * * *" = 每天 8:00 到 23:00，每小時的第 0 分執行
-- 格式: 分 時 日 月 週

-- 先刪除舊設定 (若存在)
DELETE FROM App_ScheduleConfig WHERE JobId = 'so-sync-hourly';

-- 插入新設定
INSERT INTO App_ScheduleConfig (JobId, CronExpression, JobType, IsActive, Description, LastModified)
VALUES (
    'so-sync-hourly',
    '0 8-23 * * *',
    'FourPLWebAPI.Jobs.SOSyncJob',
    1,
    'SO 主檔同步 - 每天 8:00~23:00 每小時執行',
    GETDATE()
);

-- 驗證設定
SELECT * FROM App_ScheduleConfig WHERE JobId = 'so-sync-hourly';
