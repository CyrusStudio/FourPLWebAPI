# SAP Master Data Logic Alignment Plan

Confirm and align the current C# implementation with the old SQL trigger logic to ensure data consistency. We will continue using the `_new` tables for comparison.

## Proposed Changes

### SapMaster Data Service

#### [MODIFY] [SapMasterDataService.cs](file:///c:/Lotus/FourPLWebAPI/src/Services/Implementations/SapMasterDataService.cs)
- 在 `ProcessFileAsync<T>` 中加入對 `CustomerMaster` 的轉換邏輯：將 `CreditLimit` 除以 `10000`。
- 在 `ProcessFileAsync<T>` 中加入對 `SalesMaster` 的轉換邏輯：
    - 將 `District` 設為 `"TW"`。
    - 將 `EmployeeID` 進行子字串處理 `SUBSTRING(4, 5)`。
- 在 `ProcessFileAsync<T>` 中調整 `PriceMaster` 的處理：
    - 確保轉換邏輯正確應用於 `InvoicePrice` 與 `FixedPrice`。
- **[NEW] 修正 `PriceMaster` 欄位對應**：確認 `DocCurrency` (`WAERK`) 與 `Currency` (`KONWA`) 在 XML 中的對應是否正確，目前觀察到 `DocCurrency` 為空可能導致資料讀取異常。
- **[NEW] 移除 `Trim()` 邏輯**：在 `SapMasterDataRepository.cs` 中移除 `Trim()`，以保留 XML 原始資料的所有空白。
- **[NEW] 實作「後蓋前」去重邏輯**：在 `SapMasterDataRepository` 寫入之前，根據主鍵對資料進行去重，僅保留 XML 中最後出現的記錄，以模擬舊系統觸發器的行為。
- **[NEW] 修改 `ProcessFileAsync` (單個檔案處理)**：當 `IsProdMode` 為 `false` 時，不將處理完的檔案移動到 `Success` 或 `Fail` 資料夾，以方便重複測試。

### Sap Models

#### [MODIFY] [PriceMaster.cs](file:///c:/Lotus/FourPLWebAPI/src/Models/PriceMaster.cs)
- 從 `SapMasterData` attribute 的主鍵清單中移除 `ValidOn`，以符合舊 SQL 邏輯（相同組合的新日期應覆蓋舊資料）。

## Verification Plan

### Automated Tests
- 無

### Manual Verification
- 執行 `ProcessAllAsync` 並檢查 `_new` 資料表中的資料是否已正確轉換。
- 檢查 `Sales_CustomerMaster_new.CreditLimit` 是否已除以 10000。
- 檢查 `Sales_SalesMaster_new.District` 是否皆為 "TW"。
- 檢查 `Sales_SalesMaster_new.EmployeeID` 是否已擷取正確位數。
- 檢查 `Sales_PriceMaster_new` 是否正確更新（不重疊日期資料）。
