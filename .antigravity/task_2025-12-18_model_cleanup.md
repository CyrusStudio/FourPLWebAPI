# 重構 Jobs 目錄結構

將 Job 執行引擎與具體任務定義分離，提升目錄組織性。

## 待辦事項
- [ ] 撰寫實作計畫
- [x] 建立子目錄結構
    - [x] `src/Jobs/Infrastructure/` (放置 JobExecutor)
    - [x] `src/Jobs/Handlers/` (放置具體 Job)
- [x] 移動檔案並更新 Namespace
    - [x] `JobExecutor.cs` -> `FourPLWebAPI.Jobs.Infrastructure`
    - [x] `SOSyncJob.cs` -> `FourPLWebAPI.Jobs.Handlers`
    - [x] `SapFileProcessJob.cs` -> `FourPLWebAPI.Jobs.Handlers`
- [x] 更新 `Program.cs` 的 DI 註冊
- [x] 清理 Models 資料夾
    - [x] 移除 `BpmRequisitionModels.cs` (已確認無引用)
    - [x] 檢查其他個別類別引用
- [x] 驗證編譯與測試
- [x] 儲存開發紀錄
