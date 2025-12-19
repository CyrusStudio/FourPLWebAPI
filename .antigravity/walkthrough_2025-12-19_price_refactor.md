# PriceMaster Calculation Refactoring Walkthrough

已完成 `PriceMaster` 的價格計算邏輯調整，並成功執行發布建置。

## 變更內容
1.  **SapMasterDataService.cs**:
    -   新增 `ProcessPriceFileAsync` 方法專門處理價格主資料。
    -   實作換算邏輯：`InvoicePrice = InvoicePrice / ConditionPriceUnit` 且 `FixedPrice = FixedPrice / ConditionPriceUnit`。
    -   處理過程中會自動將結果格式化為 4 位小數 (`F4`)。

## 執行步驟
1.  修改 `SapMasterDataService.cs` 程式碼。
2.  執行 `dotnet publish` 確保編譯成功並更新發布檔。
3.  驗證發布目錄 `publish/` 是否更新。

## 驗證結果
- [x] 編譯成功 (Exit code: 0)
- [x] 邏輯實作符合需求：`InvoicePrice` 與 `FixedPrice` 會根據 `ConditionPriceUnit` 進行除法運算。
- [x] 成功將發布檔輸出至根目錄 `publish/`。
