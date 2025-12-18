# 修正 IDE 程式碼分析問題

根據 IDE 回報的建議，對全案進行語法現代化與效能優化。

## 待辦事項
- [x] 撰寫實作計畫
- [x] 修復語法與效能問題
    - [x] `BatchItemExtensions.cs`：移除未使用參數
    - [x] `ISapHelper.cs`：簡化集合初始化
    - [x] `NetworkDiskHelper.cs`：評估 P/Invoke (保留 DllImport 以維持穩定)
    - [x] `SftpConnectionFactory.cs`：採用主要建構函式與簡化集合
    - [x] `EmailHelper.cs`：簡化條件運算式與優化 `Any()` -> `Count`
    - [x] `SqlHelper.cs`：採用主要建構函式
    - [x] `MockSapHelper.cs`：採用主要建構函式、標記靜態成員、簡化集合
    - [x] `SapMasterDataAttributes.cs`：採用主要建構函式與簡化集合
    - [x] `SapMasterDataRepository.cs`：採用主要建構函式、優化 `Any()`、簡化 Null 檢查
- [x] 驗證編譯與測試 (0 警告, 0 錯誤)
- [x] 儲存開發紀錄
