# SO 資料同步服務重構 - 完成報告

## 變更摘要

成功將舊版 `GetSO` 程式碼重構至新架構，並依使用者需求改為**自動雙表處理模式**，一次呼叫即可同步兩個資料表。

## 變更檔案

| 檔案 | 變更類型 | 說明 |
|------|----------|------|
| [appsettings.json](file:///c:/Lotus/FourPLWebAPI/src/appsettings.json) | 修改 | 新增 `SAPDSConnection` 連線字串 |
| [SODTOs.cs](file:///c:/Lotus/FourPLWebAPI/src/Models/SODTOs.cs) | 新增 | SO 相關 DTO 模型 (`SOSyncRequest`, `SOMasterData`, `SOSyncResult`, `SOSyncAllResult`) |
| [ISqlHelper.cs](file:///c:/Lotus/FourPLWebAPI/src/Infrastructure/ISqlHelper.cs) | 修改 | 新增 `BulkInsertAsync` 和 `ExecuteWithConnectionAsync` |
| [SqlHelper.cs](file:///c:/Lotus/FourPLWebAPI/src/Infrastructure/SqlHelper.cs) | 修改 | 實作 Bulk Insert 功能 |
| [SOService.cs](file:///c:/Lotus/FourPLWebAPI/src/Services/SOService.cs) | 新增 | SO 主檔同步服務 (**自動處理雙表**) |
| [IntegrationController.cs](file:///c:/Lotus/FourPLWebAPI/src/Controllers/IntegrationController.cs) | 修改 | 新增 `so-sync` 端點 |
| [Program.cs](file:///c:/Lotus/FourPLWebAPI/src/Program.cs) | 修改 | 註冊 `ISOService` |

## API 使用方式

```http
POST /api/integration/so-sync
Content-Type: application/json

{
    "startDate": "20251201"  // 可選，預設為昨天
}
```

**一次呼叫自動同步兩個表**：
- `Sales_ArichSOMaster` (ORDLA IN ('A', 'L'))
- `Sales_ZLSOMaster` (ORDLA IN ('Z', 'B'))

**回傳範例**：
```json
{
    "success": true,
    "message": "同步完成，共刪除 15 筆，新增 50 筆",
    "totalDeletedCount": 15,
    "totalInsertedCount": 50,
    "results": [
        {
            "success": true,
            "targetTable": "Sales_ArichSOMaster",
            "deletedCount": 10,
            "insertedCount": 25
        },
        {
            "success": true,
            "targetTable": "Sales_ZLSOMaster",
            "deletedCount": 5,
            "insertedCount": 25
        }
    ]
}
```

## 驗證結果

- ✅ 編譯成功 (0 錯誤, 1 警告)
- ⏳ API 測試待使用者驗證
