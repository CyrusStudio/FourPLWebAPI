-- =====================================================
-- App_ScheduleConfig 資料表
-- 用於 Hangfire 動態排程管理
-- =====================================================

-- 建立排程設定表
CREATE TABLE App_ScheduleConfig (
    -- 排程 ID (唯一識別碼)
    JobId NVARCHAR(100) NOT NULL PRIMARY KEY,
    
    -- Cron 表達式 (標準 5 欄位格式)
    -- 格式: 分 時 日 月 週
    -- 範例: "0 8 * * *" = 每天早上 8:00
    CronExpression NVARCHAR(50) NOT NULL,
    
    -- Job 類型 (完整類別名稱)
    -- 範例: "FourPLWebAPI.Jobs.CabinetExportJob"
    JobType NVARCHAR(200) NOT NULL,
    
    -- 是否啟用
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- 描述說明
    Description NVARCHAR(500) NULL,
    
    -- 最後修改時間
    LastModified DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- 建立時間
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- 建立索引
CREATE INDEX IX_App_ScheduleConfig_IsActive 
ON App_ScheduleConfig (IsActive);
GO

-- 插入範例資料
INSERT INTO App_ScheduleConfig (JobId, CronExpression, JobType, IsActive, Description)
VALUES 
(
    'cabinet-export-daily',
    '0 8 * * *',
    'FourPLWebAPI.Jobs.CabinetExportJob',
    1,
    '每日早上 8:00 執行機櫃資料匯出'
),
(
    'cabinet-export-hourly',
    '0 * * * *',
    'FourPLWebAPI.Jobs.CabinetExportJob',
    0,
    '每小時執行機櫃資料匯出 (預設停用)'
);
GO

-- =====================================================
-- 使用說明
-- =====================================================
-- 1. 新增排程: INSERT INTO App_ScheduleConfig (...)
-- 2. 停用排程: UPDATE App_ScheduleConfig SET IsActive = 0 WHERE JobId = 'xxx'
-- 3. 修改時間: UPDATE App_ScheduleConfig SET CronExpression = '新表達式' WHERE JobId = 'xxx'
-- 4. 系統會每 5 分鐘自動同步排程設定 (可在 appsettings.json 調整)
--
-- Cron 表達式範例:
-- "0 8 * * *"      = 每天 08:00
-- "0 */2 * * *"    = 每 2 小時
-- "30 9 * * 1-5"   = 週一至週五 09:30
-- "0 0 1 * *"      = 每月 1 號 00:00
-- =====================================================
