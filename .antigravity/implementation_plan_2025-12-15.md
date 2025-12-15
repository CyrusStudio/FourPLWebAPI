# 檔案交換功能整合至 FourPLWebAPI

將現有 `sFTPDataExchange` Console 應用程式重構並整合至 FourPLWebAPI 專案，遵循 DI 模式、使用 Serilog 記錄、支援 Hangfire 排程執行。

## 功能摘要

原程式執行以下四種檔案交換場景：

1. **SAP → BPM**：從 SAP AP 資料夾複製 XML 檔案至 BPM，依檔名前綴分類存放
2. **BPM → SAP**：從 BPM 上傳 XML 檔案至 SAP AP 資料夾，並備份
3. **BPM → ZL**：透過 sFTP 上傳檔案至 ZL 伺服器
4. **BPM → ARICH**：透過 sFTP 上傳檔案至 ARICH 伺服器

---

## Proposed Changes

### Infrastructure 基礎設施層

#### [NEW] [INetworkDiskHelper.cs](file:///c:/Lotus/FourPLWebAPI/src/Infrastructure/INetworkDiskHelper.cs)
- 定義網路磁碟連線介面
- 方法：`ConnectAsync()`、`DisconnectAsync()`

#### [NEW] [NetworkDiskHelper.cs](file:///c:/Lotus/FourPLWebAPI/src/Infrastructure/NetworkDiskHelper.cs)
- 實作 P/Invoke 呼叫 `mpr.dll` 的 `WNetAddConnection2` / `WNetCancelConnection2`
- 從設定檔讀取網路磁碟路徑與認證資訊
- 支援多組網路磁碟對應

#### [NEW] [IEmailHelper.cs](file:///c:/Lotus/FourPLWebAPI/src/Infrastructure/IEmailHelper.cs)
- 定義郵件通知介面
- 方法：`SendNotificationAsync(subject, body)`

#### [NEW] [EmailHelper.cs](file:///c:/Lotus/FourPLWebAPI/src/Infrastructure/EmailHelper.cs)
- 使用 `SmtpClient` 發送郵件
- 從設定檔讀取 SMTP 伺服器設定

---

### Services 服務層

#### [NEW] [IDataExchangeService.cs](file:///c:/Lotus/FourPLWebAPI/src/Services/IDataExchangeService.cs)
- 定義檔案交換服務介面
- 方法：
  - `ExecuteAllAsync()` - 執行所有交換流程
  - `GetFilesFromSapAsync()` - 場景 1
  - `UploadFilesToSapAsync()` - 場景 2
  - `UploadFilesToZLAsync()` - 場景 3
  - `UploadFilesToARICHAsync()` - 場景 4

#### [NEW] [DataExchangeService.cs](file:///c:/Lotus/FourPLWebAPI/src/Services/DataExchangeService.cs)
- 實作 `IDataExchangeService`
- 注入 `ISftpHelper`、`INetworkDiskHelper`、`IEmailHelper`
- 使用 async/await 模式
- 使用 Serilog 記錄操作日誌
- 統一錯誤處理與通知

---

### Configuration 設定

#### [MODIFY] [appsettings.Development.json](file:///c:/Lotus/FourPLWebAPI/src/appsettings.Development.json)
新增以下設定區段：

```json
{
  "DataExchange": {
    "NetworkDisks": [
      {
        "RemotePath": "\\\\server\\share",
        "LocalDrive": "X:",
        "Username": "domain\\user",
        "Password": "****"
      }
    ],
    "SapToLocal": {
      "SourcePath": "X:\\SAP\\Outbound",
      "TargetBasePath": "D:\\BPM\\Import\\",
      "FileMapping": {
        "CSTM": "Customer",
        "MARA": "Material",
        "SALES": "Sales",
        "PRICE": "Price"
      }
    },
    "LocalToSap": {
      "SourcePath": "D:\\BPM\\Export\\SAP",
      "TargetPath": "X:\\SAP\\Inbound",
      "BackupPath": "D:\\BPM\\Backup\\SAP"
    },
    "SftpTargets": {
      "ZL": {
        "Host": "sftp.zl.example.com",
        "Port": 22,
        "Username": "zl_user",
        "Password": "****",
        "SourcePath": "D:\\BPM\\Export\\ZL",
        "TargetPath": "/upload",
        "BackupPath": "D:\\BPM\\Backup\\ZL"
      },
      "ARICH": {
        "Host": "sftp.arich.example.com",
        "Port": 22,
        "Username": "arich_user",
        "Password": "****",
        "SourcePath": "D:\\BPM\\Export\\ARICH",
        "TargetPath": "/upload",
        "BackupPath": "D:\\BPM\\Backup\\ARICH"
      }
    }
  },
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "sender@example.com",
    "Password": "****",
    "FromAddress": "sender@example.com",
    "NotifyRecipients": ["admin@example.com"]
  }
}
```

---

### Jobs 排程

#### [NEW] [DataExchangeJob.cs](file:///c:/Lotus/FourPLWebAPI/src/jobs/DataExchangeJob.cs)
- Hangfire 排程任務
- 注入 `IDataExchangeService`
- 執行 `ExecuteAllAsync()`

#### [MODIFY] [Program.cs](file:///c:/Lotus/FourPLWebAPI/src/Program.cs)
- 註冊新服務：`INetworkDiskHelper`、`IEmailHelper`、`IDataExchangeService`
- 註冊 `DataExchangeJob`

---

## 優化重點

| 原程式問題 | 優化方案 |
|------------|----------|
| 靜態方法，無法測試 | 使用介面與 DI |
| 同步 I/O 阻塞 | 使用 async/await |
| `Console.WriteLine` 輸出 | 使用 Serilog 結構化日誌 |
| 硬編碼設定在 `App.config` | 使用 `appsettings.json` |
| 例外處理不一致 | 統一錯誤處理與通知 |
| 重複程式碼 | 抽取共用方法 |

---

## Verification Plan

### 自動化驗證

1. **建置專案**
   ```powershell
   cd c:\Lotus\FourPLWebAPI\src
   dotnet build
   ```
   期望結果：Build succeeded，無錯誤

### 手動驗證

1. **確認服務註冊**
   - 執行 `dotnet run`，檢查無 DI 解析錯誤

2. **測試 API 端點**（若新增 Controller）
   - 透過 Swagger 呼叫測試端點

3. **Hangfire Dashboard**
   - 確認 DataExchangeJob 出現在排程清單中

> [!NOTE]
> 由於原程式依賴實際網路磁碟、SFTP 伺服器、SMTP 伺服器，完整的端到端測試需要使用者提供實際環境設定或 Mock 伺服器。
