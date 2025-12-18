# IDE 程式碼分析修正計畫

本計畫旨在回應 IDE 提出的 20+ 項代碼品質建議，將代碼現代化並提升效能。

## 擬議變更

### 1. 語法現代化 (C# 12+)
- **主要建構函式 (Primary Constructors)**：套用於 `SqlHelper`, `MockSapHelper`, `SftpConnectionFactory`, `SapMasterDataRepository` 及相關 Attribute。
- **集合運算式 (`[]`)**：簡化 `ISapHelper`, `MockSapHelper`, `SapMasterDataAttributes` 中的初始化。

### 2. 效能與最佳實踐
- **`LibraryImport` 轉換**：針對 `NetworkDiskHelper` 中的 Win32 API 呼叫，從 `DllImport` 改為源碼產生器支援的 `LibraryImport`（需將類別改為 `partial`）。
- **集合效能**：在 `EmailHelper` 與 `SapMasterDataRepository` 中將 `Any()` 替換為 `Count > 0` 比較。
- **靜態成員**：將 `MockSapHelper` 中不存取執行個體資料的開發方法標記為 `static`。

### 3. 代碼清理
- 移除 `BatchItemExtensions.cs` 中未使用的參數。
- 簡化 `EmailHelper.cs` 的條件判斷與 `SapMasterDataRepository.cs` 的 Null 檢查。

## 驗證計畫
- 執行 `dotnet build` 確保 0 錯誤 0 警告。
