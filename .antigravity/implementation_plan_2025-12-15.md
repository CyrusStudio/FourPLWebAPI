# SO 資料同步服務重構

將舊版 `GetSO` 程式碼重構至新架構，使用專案既有的 `ISapHelper` 和 `ISqlHelper` 基礎設施服務，取代直接的 SAP RFC 呼叫和 ADO.NET 操作。

## User Review Required

> [!IMPORTANT]
> **資料庫連線字串**：原程式碼使用硬編碼的連線字串 (`172.29.40.2` 的 SAPDS 資料庫)。請確認此連線是否應：
> 1. 使用現有的 `DefaultConnection` 設定
> 2. 新增專用的 `SAPDSConnection` 設定

> [!WARNING]
> **破壞性變更**：此重構會移除舊的 `ApiResultDTO<IEnumerable<RefSelectionListMaster>>` 回傳格式，改用新的 `SOMasterData` DTO。如有外部系統依賴舊格式，請告知。

---

## Proposed Changes

### Models 層

#### [NEW] [SODTOs.cs](file:///c:/Lotus/FourPLWebAPI/src/Models/SODTOs.cs)
建立 SO 相關資料模型：
- `SOQueryRequest`：查詢請求參數 (Vm, StartDate)
- `SOMasterData`：SO 主檔資料，對應資料庫欄位
- `SOSyncResult`：同步結果摘要

```csharp
public class SOQueryRequest
{
    public string Vm { get; set; } = string.Empty;  // "AR" 或其他
    public string? StartDate { get; set; }           // YYYYMMDD 格式
}

public class SOMasterData
{
    public string SONumber { get; set; }
    public string SOItem { get; set; }
    public string MaterialCode { get; set; }
    public string MaterialDesc { get; set; }
    public string SalesUnit { get; set; }
    public string SalesDate { get; set; }
    public string Invoice { get; set; }
    public string InvoiceDate { get; set; }
    public decimal Qty { get; set; }
    public string Batch { get; set; }
    public string ValidityPeriod { get; set; }
    public decimal FixedPrice { get; set; }
    public string CreditMemo { get; set; }
    public string CustomerCode { get; set; }
    public string BPMOriginNumber { get; set; }
    public string SPNumber { get; set; }
}

public class SOSyncResult
{
    public int InsertedCount { get; set; }
    public int DeletedCount { get; set; }
    public string TargetTable { get; set; }
    public List<SOMasterData> Data { get; set; }
}
```

---

### Infrastructure 層

#### [MODIFY] [ISqlHelper.cs](file:///c:/Lotus/FourPLWebAPI/src/Infrastructure/ISqlHelper.cs)
新增 Bulk Insert 介面方法：
```csharp
/// <summary>
/// 執行 Bulk Insert 操作
/// </summary>
Task<int> BulkInsertAsync<T>(string tableName, IEnumerable<T> data);
```

#### [MODIFY] [SqlHelper.cs](file:///c:/Lotus/FourPLWebAPI/src/Infrastructure/SqlHelper.cs)
實作 Bulk Insert（使用 Dapper.Contrib 或手動 DataTable）：
```csharp
public async Task<int> BulkInsertAsync<T>(string tableName, IEnumerable<T> data)
{
    // 使用 SqlBulkCopy 實作
}
```

---

### Services 層

#### [NEW] [SOService.cs](file:///c:/Lotus/FourPLWebAPI/src/Services/SOService.cs)
建立 SO 同步服務：

```csharp
public interface ISOService
{
    /// <summary>
    /// 從 SAP 查詢 SO 資料並同步至 SQL Server
    /// </summary>
    Task<SOSyncResult> SyncSOMasterAsync(SOQueryRequest request);
}

public class SOService : ISOService
{
    // 1. 呼叫 ISapHelper.ExecuteRfcAsync 查詢 ZT4PL_BILLING
    // 2. 轉換資料格式 (處理 BOT->BT, FixedPrice 計算等)
    // 3. 呼叫 ISqlHelper.ExecuteAsync 刪除舊資料
    // 4. 呼叫 ISqlHelper.BulkInsertAsync 寫入新資料
    // 5. 呼叫 ISqlHelper.ExecuteAsync 更新 MaterialDesc
}
```

**主要邏輯對應**：
| 原程式碼 | 新架構 |
|---------|-------|
| `_sapRfcService.Execute<SAPInput2, SAPOutput2>` | `_sapHelper.ExecuteRfcAsync("RFC_READ_TABLE", ...)` |
| `new SqlConnection(...)` + `SqlBulkCopy` | `_sqlHelper.BulkInsertAsync(...)` |
| 硬編碼 `ORDLA IN (...)` 條件 | 依 `Vm` 參數動態決定 |
| `DataTable` 轉換 | 直接映射至 `SOMasterData` |

---

### Controllers 層

#### [MODIFY] [IntegrationController.cs](file:///c:/Lotus/FourPLWebAPI/src/Controllers/IntegrationController.cs)
新增 SO 同步端點：

```csharp
[HttpPost("so-sync")]
public async Task<ActionResult<SOSyncResult>> SyncSOMaster([FromBody] SOQueryRequest request)
{
    var result = await _soService.SyncSOMasterAsync(request);
    return Ok(result);
}
```

---

### DI 註冊

#### [MODIFY] [Program.cs](file:///c:/Lotus/FourPLWebAPI/src/Program.cs)
註冊新服務：
```csharp
builder.Services.AddScoped<ISOService, SOService>();
```

---

## Verification Plan

### Automated Tests
目前專案無單元測試框架，跳過自動化測試。

### Manual Verification

1. **編譯驗證**
   ```bash
   cd c:\Lotus\FourPLWebAPI\src
   dotnet build
   ```

2. **API 測試** (使用 Swagger 或 Postman)
   - 啟動應用程式：`dotnet run`
   - 開啟 Swagger：`https://localhost:5001/swagger`
   - 呼叫 `POST /api/integration/so-sync`：
     ```json
     {
       "vm": "AR",
       "startDate": "20251201"
     }
     ```
   - 確認回傳包含 `insertedCount`, `deletedCount`, `data` 等欄位

3. **資料庫驗證** (需使用者確認)
   - 檢查 `Sales_ZLSOMaster` / `Sales_ArichSOMaster` 資料表是否正確更新
   - 確認 `MaterialDesc` 欄位有透過 JOIN 更新
