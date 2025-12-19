# PriceMaster Calculation Integration Walkthrough

依照建議，已將 `PriceMaster` 的特殊價格換算邏輯整合進通用的泛型處理方法中，大幅簡化了程式碼結構。

## 變更內容
1.  **SapMasterDataService.cs**:
    -   移除冗餘的 `ProcessPriceFileAsync` 方法。
    -   在通用的 `ProcessFileAsync<T>` 方法中加入類型判斷 (`if (dataList is List<Models.PriceMaster> priceList)`)。
    -   將價格除以單位的邏輯整合進去，確保代碼不重複且易於維護。
    -   恢復 `ExecuteFileProcessingAsync` 為純粹的泛型呼叫。

## 執行步驟
1.  重構 `SapMasterDataService.cs` 程式碼。
2.  執行 `dotnet build` 驗證語法與正確性。
3.  確認 Release 建置成功。

## 驗證結果
- [x] 編譯成功 (Exit code: 0)
- [x] 代碼結構優化：消除約 60 行重複程式碼。
- [x] 邏輯正確：`PriceMaster` 的價格換算已包含在通用的匯入流程中。
