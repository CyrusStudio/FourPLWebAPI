# 三大核心功能重構與配置優化完工報告 (Walkthrough)

我已成功將 API 與設定檔結構依照業務語義進行徹底拆分與優化。現在系統架構與配置邏輯已完美達成三權分立，職責層級分明。

## 1. 設定檔功能模組化 (appsettings.json)

不再使用單一龐大的 `DataExchange` 區段，而是依業務功能拆分為：

- **全域配置優先**：將 `NetworkDisk` 提升至頂層，便於全域管理。
- **功能模組化**：依業務功能拆分為 `SapSoSync`, `SapMasterData`, `BpmDataUpload` 等區段。
- **冗餘清理**：徹底移除已確認無用的 `Sftp` 與 `Smtp` 區段及其關聯實作為 `ISftpHelper` 與 `IEmailHelper`。
- **安全性獨立**：獨立的安全開關區塊 (`IsProdMode`, `SafeOutputPath`)。

## 2. 代碼與配置精確對齊

已同步更新以下核心組件，確保讀取路徑準確：

- **[NetworkDiskHelper.cs]**: 配合 `NetworkDisk` 配置路徑遷移。
- **[SftpConnectionFactory.cs]**: 精確對接 `BpmDataUpload` 下的專屬 SFTP 設定。
- **[BpmDataUploadService.cs]**: 精確對接 `BpmDataUpload` 配置。

## 5. Fail-Safe 安全配置隔離

為了徹底排除「誤連正式資料庫/SAP」的風險，實施了以下安全重構：

- **生產環境隔離**：建立了 `appsettings.Production.json`，將所有真實的生產 IP、正式資料庫連線字串、SFTP 帳密等物理隔離在此文件。
- **基礎配置去敏感化**：將 `appsettings.json` 中的正式資訊全部替換為 `localhost` 或安全的佔位符。
- **安全失敗設計 (Fail-Safe)**：若系統在啟動時未收到環境變數（導致預設為生產模式但沒讀到 `Production.json`），會因連向 `localhost` 而安全地報錯停止，確保絕對不會影響到正式資料。

## 6. 配置修復與 SapRFC 更名

依照需求進行了最終的結構調整：

- **SapRFC 更名**：將設定檔中的 `Sap` 區段統一更名為 `SapRFC`，並同步更新了 `SapDestinationConfig.cs` 與 `SapHelper.cs` 中的讀取路徑。
- **遺失屬性恢復**：修復了手動編輯時遺失的關鍵設定：
    - `NetworkDisk`: 恢復 `LocalDrives` 為 `Z:;Y:`。
    - `BpmDataUpload`: 恢復了遺失的 SAP 上傳路徑 (`LocalExportPath`, `RemoteTargetPath` 等)。
- **安全性完善**：確保正式環境敏感資訊 (Host, User, Pass) 僅存在於 `appsettings.Production.json` 中，實現結構化的安全管理。

## 7. 對稱性路徑命名重構 (Symmetrical Naming)

為了解決「RemoteTargetPath」等名稱不夠直觀的問題，我們實施了對稱性命名系統：

### SapMasterData (下載流程: SAP → 本地)
- **SapSourcePath** (原 RemoteSourcePath): 指向 SAP 的來源路徑 (Z 槽)。
- **LocalTargetPath**: 指向本地接收資料夾。

### BpmDataUpload (上傳流程: 本地 → SAP)
- **LocalSourcePath** (原 LocalExportPath): 指向本地產生的 XML 來源。
- **SapTargetPath** (原 RemoteTargetPath): 指向 SAP 的目標路徑 (Y 槽)。

這使得「Sap」開頭的 Key 永遠代表 SAP 端的網路磁碟，「Local」開頭的 Key 代表本地磁碟，且「Source/Target」精確描述了檔案的流動方向。

## 8. 極致簡化：單一維護點重構 (Single Source of Truth)

為了解決多個環境設定檔重複維護的問題，我們實施了最終優化：

- **appsettings.json (唯一主檔)**：現在包含所有真實的路徑、SAP IP、SFTP 帳密。
- **資料庫隔離**：只有 `ConnectionStrings` 保留在各個環境檔中（基礎檔設為 `localhost` 作為安全防線）。
- **維護簡化**：未來修改路徑或 SAP 憑證時，僅需修改一份 `appsettings.json`，極大化減少了維護成本。

## 9. SAP 下載安全保護 (SAFETY KEEP)

為了解決與舊系統並行驗證時的檔案衝突問題，我們優化了下載邏輯：

- **條件式刪除**：在 `SapMasterDataService` 下載檔案時，系統會先讀取 `SafetySystem:IsProdMode`。
- **非生產模式保護**：若 `IsProdMode` 為 `false`，系統會顯示 `【SAFETY KEEP】` 並保留原始檔案（僅執行複製），確保新系統的驗證過程不會「搶走」舊系統需要處理的檔案。
- **生產模式正常化**：僅在 `IsProdMode` 改為 `true` 時，系統才會像往常一樣在下載後刪除來源。

## 10. Hangfire Job 類名相容性補丁

為解決类名重構後掛載在資料庫中的舊 Job 無法執行的問題：

- **別名映射 (Alias Mapping)**：在 `JobExecutor` 中實施了靜態映射，能自動將舊的 `SOSyncJob` 導向新的 `SapSoSyncJob`。
- **彈性方法調用**：同步支援 `Execute` 與 `ExecuteAsync` 方法名稱，確保不同時期的 Job 都能被正確觸發。

## 3. 拼字一致性驗證

- 已清查全案文字，確認資料表與變數命名皆統一為 `Verify`（修正了潛在的 `Vertify` 拼字錯誤）。
- 所有 SQL 查詢語句皆已核對，確保與資料庫實體表對應。

## 4. 最終驗證結果

- **建置狀態**：通過 (`dotnet build`)
- **錯誤**：0
- **警告**：0
- **環境一致性**：`appsettings.Development.json` 已同步套用相同結構。

---

> [!NOTE]
> 現在系統配置更具擴展性。若未來需新增 SFTP 目標或調整產出邏輯，僅需在對應功能區塊下調整即可，不會影響其他模組。
