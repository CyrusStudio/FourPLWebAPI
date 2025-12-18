# 重構 Infrastructure 目錄結構

將底層 Helper 與 Repository 按照功能分類，建立清晰的子目錄結構與 Namespace。

## 待辦事項
- [x] 撰寫實作計畫與目錄對照表
- [x] 建立子目錄結構
    - [x] `Infrastructure/Abstractions/` (所有傳入介面)
    - [x] `Infrastructure/SAP/` (SAP 實作與內部類別)
    - [x] `Infrastructure/Persistence/` (資料庫實作)
    - [x] `Infrastructure/Messaging/` (郵件通知實作)
    - [x] `Infrastructure/Files/` (SFTP/網路磁碟實作)
    - [x] `Infrastructure/Scheduling/` (現有)
- [x] 移動檔案並更新 Namespace
- [x] 更新 Service 與 Controller 的引用
- [x] 更新 `Program.cs` 的 DI 註冊
- [x] 驗證編譯與測試
- [x] 儲存開發紀錄
