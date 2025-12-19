# PriceMaster Calculation Adjustment Implementation Plan

調整 `PriceMaster` 的價格匯入邏輯，確保 `InvoicePrice` 與 `FixedPrice` 會除以 `ConditionPriceUnit`。

## Proposed Changes

### [Services]
#### [MODIFY] [SapMasterDataService.cs](file:///c:/Lotus/FourPLWebAPI/src/Services/Implementations/SapMasterDataService.cs)
- 在 `SapMasterDataService` 中特別處理 `PriceMaster` 類型。
- 修改 `ProcessFileAsync<T>` 或在 `ExecuteFileProcessingAsync` 中攔截 `Price` 類型並手動調用計算邏輯。

**實作細節：**
1. 讀取 `PriceMaster` 資料列表。
2. 對於每一筆資料：
   - 取得 `ConditionPriceUnit` (KPEIN)，若為空或 0 則視為 1。
   - 將 `InvoicePrice` (KBETR_PR00) 除以 `ConditionPriceUnit`。
   - 將 `FixedPrice` (KBETR_ZTW2) 除以 `ConditionPriceUnit`。
3. 將處理後的資料列表寫入資料庫。

## Verification Plan

### Manual Verification
- 使用包含 `ConditionPriceUnit` 不為 1 的 XML 檔案進行測試。
- 驗證資料庫中的 `InvoicePrice` 與 `FixedPrice` 是否已正確換算。
- 檢查 `dotnet publish` 是否依然能正確建置。
