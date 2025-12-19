# PriceMaster Logic Refinement Walkthrough

進一步優化了 `SapMasterDataService.cs` 中的處理邏輯，改用傳入的 `typeName` 參數來進行業務邏輯區分。

## 變更內容
1.  **SapMasterDataService.cs**:
    -   將 `if (dataList is List<Models.PriceMaster> priceList)` 改為更直觀的 `if (typeName == "Price")`。
    -   使用 `System.Linq` 的 `Cast<T>` 方法來存取及處理價格資料。
    -   程式碼邏輯現在與方法參數 (`typeName`) 緊密結合，語意更清晰。

## 執行步驟
1.  修改 `SapMasterDataService.cs` 引入 `System.Linq`。
2.  更新 `ProcessFileAsync<T>` 中的判斷條件。
3.  執行 `dotnet build` 驗證成功。

## 驗證結果
- [x] 編譯成功 (Exit code: 0)
- [x] 邏輯更直觀：直接使用方法參數 `typeName` 進行分支判斷。
