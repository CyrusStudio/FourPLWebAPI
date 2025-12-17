# 資料轉換服務優化 - 實作計畫

## 目標
將 BPM 表單資料（訂單/樣品/退貨）批次轉換為 SAP 匯出格式，並使用佇列表追蹤已處理項目，避免重複處理。

## 架構概覽

```
ProcessPendingAsync()
    │
    ├─ FetchAllOrderDataAsync()     → 查詢訂單 (排除已處理)
    ├─ FetchSampleBatchDataAsync()  → 查詢樣品 (排除已處理)
    ├─ FetchReturnBatchDataAsync()  → 查詢退貨 (排除已處理)
    │
    ├─ TransformOrderBatchItems()   → 轉換訂單資料
    ├─ TransformSampleBatchItems()  → 轉換樣品資料
    ├─ TransformReturnBatchItems()  → 轉換退貨資料
    │
    ├─ BulkInsertAsync()            → 批次寫入 Export_Verify
    └─ MarkRequisitionsAsProcessedAsync() → 寫入 Queue_Verify
```

---

## 資料表設計

### FourPL_DataTrans_Queue_Verify（佇列表）
| 欄位 | 類型 | 說明 |
|------|------|------|
| QueueID | INT IDENTITY | 主鍵 |
| RequisitionID | NVARCHAR(64) | 簽核單號 (唯一) |
| DiagramID | NVARCHAR(32) | 表單類型 |
| SerialID | NVARCHAR(64) | 流水號 |
| ProcessedAt | DATETIME | 處理時間 |
| ExportCount | INT | 匯出筆數 |

---

## 修改檔案

### [Controllers/DataTransformController.cs](file:///c:/Lotus/FourPLWebAPI/src/Controllers/DataTransformController.cs)
- 精簡為只有 `/api/DataTransform/pending` 端點

### [Services/IDataTransformService.cs](file:///c:/Lotus/FourPLWebAPI/src/Services/IDataTransformService.cs)
- 只保留 `ProcessPendingAsync` 方法

### [Services/DataTransformService.cs](file:///c:/Lotus/FourPLWebAPI/src/Services/DataTransformService.cs)
- `FetchAllOrderDataAsync` - 訂單查詢 + 佇列排除
- `FetchSampleBatchDataAsync` - 樣品查詢 + 佇列排除
- `FetchReturnBatchDataAsync` - 退貨查詢 + 佇列排除
- `MarkRequisitionsAsProcessedAsync` - 寫入佇列記錄

### [NEW] [Scripts/CreateQueueVerifyTable.sql](file:///c:/Lotus/FourPLWebAPI/src/Scripts/CreateQueueVerifyTable.sql)
- 建立佇列表的 SQL 腳本

---

## 驗證計畫

### 自動化驗證
```bash
# 建置
dotnet build

# 執行 API
POST /api/DataTransform/pending?startDate=2025-11-01
```

### 手動驗證
1. 執行 `CreateQueueVerifyTable.sql` 建立佇列表
2. 呼叫 API 處理資料
3. 確認 `FourPL_DataTrans_Export_Verify` 有資料
4. 確認 `FourPL_DataTrans_Queue_Verify` 有記錄
5. 再次呼叫 API，確認不會重複處理
