# SAP 整合中間件 API - 完成紀錄 (v1.2)

## 專案概述

成功建立供 Power Automate 呼叫的整合 API，串接 SQL Server, SAP RFC, XML 轉換與 sFTP 傳輸，**新增 Hangfire 資料庫驅動排程功能**。

---

## 專案結構

```
FourPLWebAPI/
├── .vscode/                    # VS Code 設定
├── lib/                        # SAP NCo DLL
├── scripts/                    # 資料庫腳本
│   └── App_ScheduleConfig.sql  # 排程設定表
├── src/
│   ├── Controllers/
│   ├── Infrastructure/
│   │   └── Scheduling/         # Hangfire 排程模組
│   ├── Jobs/                   # 背景任務
│   ├── Models/
│   └── Services/
├── docs/
└── postman/
```

---

## v1.2 新增功能：Hangfire 動態排程

### 核心元件

| 元件 | 檔案 | 說明 |
|------|------|------|
| 排程設定 DTO | [ScheduleConfig.cs](file:///c:/Lotus/FourPLWebAPI/src/Infrastructure/Scheduling/ScheduleConfig.cs) | 對應 App_ScheduleConfig 表 |
| Cron 驗證器 | [CronValidator.cs](file:///c:/Lotus/FourPLWebAPI/src/Infrastructure/Scheduling/CronValidator.cs) | 使用 Cronos 驗證格式 |
| 排程同步服務 | [ScheduleSyncService.cs](file:///c:/Lotus/FourPLWebAPI/src/Infrastructure/Scheduling/ScheduleSyncService.cs) | 定期同步 DB 到 Hangfire |
| 範例 Job | [CabinetExportJob.cs](file:///c:/Lotus/FourPLWebAPI/src/Jobs/CabinetExportJob.cs) | 背景匯出任務 |

### 資料表 Schema

```sql
CREATE TABLE App_ScheduleConfig (
    JobId NVARCHAR(100) PRIMARY KEY,
    CronExpression NVARCHAR(50) NOT NULL,
    JobType NVARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    Description NVARCHAR(500)
);
```

---

## 驗證結果

### 編譯成功
```
建置成功。
    0 個警告
    0 個錯誤
```

---

## 交付項目

- [x] 完整原始碼 (.NET 8 Solution)
- [x] [swagger.json](file:///c:/Lotus/FourPLWebAPI/swagger.json)
- [x] [App_ScheduleConfig.sql](file:///c:/Lotus/FourPLWebAPI/scripts/App_ScheduleConfig.sql) 建表腳本
- [x] [開發者上手指南](file:///c:/Lotus/FourPLWebAPI/docs/developer-guide.md)
- [x] [IIS 部署手冊](file:///c:/Lotus/FourPLWebAPI/docs/deployment-guide.md) (含 Hangfire 設定)
- [x] [Postman Collection](file:///c:/Lotus/FourPLWebAPI/postman/FourPLWebAPI.postman_collection.json)

---

## 後續步驟

1. **放入 SAP NCo DLL** - 將 `sapnco.dll` 與 `sapnco_utils.dll` 放入 `lib` 資料夾
2. **執行資料庫腳本** - 執行 `scripts/App_ScheduleConfig.sql` 建立排程設定表
3. **設定連線參數** - 更新 `appsettings.json` 的 SQL/SAP/sFTP 連線資訊
4. **IIS 部署** - 依照部署手冊設定 (注意 AlwaysRunning 設定)
5. **設定排程** - 在 `App_ScheduleConfig` 表中新增排程設定
