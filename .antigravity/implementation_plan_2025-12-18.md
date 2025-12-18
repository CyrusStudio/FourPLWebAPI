# 修復 Hangfire Job 執行錯誤計畫

## 目標
修復因類名重構導致的 `System.InvalidOperationException: 找不到 Job 類型` 錯誤，確保 Hangfire 能正確執行重構後的 Job。

## 現狀分析
- **錯誤訊息**: `找不到 Job 類型: FourPLWebAPI.Jobs.SOSyncJob`。
- **原因**: `SOSyncJob` 已更名為 `SapSoSyncJob`，但 Hangfire 調度器或資料庫中仍引用舊名稱。
- **位置**: `JobExecutor.ExecuteAsync` 接收到的類型名稱與現有定義不符。

## 重構 XML 生成邏輯 (多單合併)
- **目標**: 將同一場景 (ToSAP, ToARICH, ToZL) 的所有待處理單據合併為單一 XML 檔案，而非一單一檔。
- **改動點**: 修改 `BpmDataUploadService.GenerateXmlForScenarioAsync`，移除 `GroupBy(x => x.FormNo)`。
- **檔名命名規則**: `BPM_{yyyyMMddHHmmss}_{yyyyMMddHHmmss}.xml` (例如：`BPM_20251218110005_20251218110010`)。
- **結構**: 根節點 `<Records>` 維持不變，內部包含多個 `<Record>`。

## 擬定改進
1. **強化 JobExecutor 容錯性**: 
   - 在 `JobExecutor` 中加入舊名稱對應到新名稱的映射（Mapping），確保過渡期不會報錯。
2. **更新 Hangfire 註冊路徑**:
   - 檢查 `Program.cs` 中 `RecurringJob.AddOrUpdate` 的呼叫，確保使用新的類名。
3. **清理舊 Job 紀錄**:
   - (視情況) 提供 SQL 語句或透過程式碼清除資料庫中殘留的舊 Job 紀錄。

## 驗證計畫
- 執行 `dotnet build`。
- 觀察 Log 確保 `SapSoSync` 執行成功。
