# FourPL Web API - IIS 部署手冊

## 目錄

1. [部署前準備](#部署前準備)
2. [系統需求](#系統需求)
3. [發布應用程式](#發布應用程式)
4. [IIS 設定](#iis-設定)
5. [權限設定](#權限設定)
6. [驗證部署](#驗證部署)
7. [故障排除](#故障排除)
8. [部署檢查清單](#部署檢查清單)

---

## 部署前準備

### 必要檔案

在部署前，請確認以下檔案已準備完成：

- [ ] 已發布的應用程式 (publish 資料夾)
- [ ] SAP NCo 3.1 DLL (x64 版本)
  - `sapnco.dll`
  - `sapnco_utils.dll`
- [ ] 正式環境設定檔 (`appsettings.json`)

---

## 系統需求

### 作業系統

- Windows Server 2016 / 2019 / 2022

### 必要元件

| 元件 | 版本 | 檢查方式 |
|------|------|----------|
| IIS | 10.0+ | 伺服器管理員 → 角色與功能 |
| ASP.NET Core Hosting Bundle | 8.0 | [下載連結](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Visual C++ Redistributable (x64) | 2015-2022 | 控制台 → 程式和功能 |

### ⚠️ Visual C++ Redistributable 重要提醒

> **SAP NCo 3.1 依賴 Visual C++ Runtime**
> 
> 若未安裝，會發生 `DllNotFoundException` 錯誤。
> 
> 下載連結: [VC++ Redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe)

---

## 發布應用程式

### 使用 CLI 發布

```powershell
# 在專案根目錄執行
dotnet publish src/FourPLWebAPI.csproj -c Release -o ./publish
```

### 發布輸出結構

```
publish/
├── FourPLWebAPI.dll
├── FourPLWebAPI.exe
├── appsettings.json
├── web.config
├── sapnco.dll           # 需手動確認存在
├── sapnco_utils.dll     # 需手動確認存在
└── ...
```

### 確認 SAP DLL

發布後，請確認 `sapnco.dll` 與 `sapnco_utils.dll` 存在於 publish 資料夾中。

若不存在，請手動從 `lib` 資料夾複製：

```powershell
Copy-Item .\lib\sapnco.dll .\publish\
Copy-Item .\lib\sapnco_utils.dll .\publish\
```

---

## IIS 設定

### 1. 建立應用程式集區

1. 開啟 **IIS 管理員**
2. 右鍵 **應用程式集區** → **新增應用程式集區**
3. 設定如下：

| 設定項 | 值 |
|--------|-----|
| 名稱 | FourPLWebAPI |
| .NET CLR 版本 | 沒有受控碼 |
| Managed Pipeline Mode | Integrated |

### 2. ⚠️ 關鍵設定：停用 32 位元應用程式

> **這是最重要的設定，錯誤會導致 SAP NCo 載入失敗！**

1. 選取 **FourPLWebAPI** 應用程式集區
2. 點擊右側 **進階設定**
3. 找到 **啟用 32 位元應用程式**
4. 設定為 **`False`**

### 3. ⚠️ Hangfire 排程存活設定 (關鍵)

> **若未設定，IIS 閒置休眠會導致背景排程失效！**

**應用程式集區進階設定：**

| 設定項 | 值 | 說明 |
|--------|-----|------|
| Start Mode | **AlwaysRunning** | 應用程式集區隨 IIS 啟動 |
| Idle Time-out (minutes) | **0** | 停用閒置超時 |
| Regular Time Interval (minutes) | **0** | 停用定期回收 |

**網站進階設定：**

| 設定項 | 值 | 說明 |
|--------|-----|------|
| Preload Enabled | **True** | 預先載入應用程式 |

設定步驟：
1. 選取應用程式集區 → **進階設定**
2. **Process Model** 區段：`Idle Time-out` = `0`
3. **General** 區段：`Start Mode` = `AlwaysRunning`
4. 選取網站 → **進階設定** → `Preload Enabled` = `True`

### 3. 建立網站

1. 右鍵 **站台** → **新增網站**
2. 設定如下：

| 設定項 | 值 |
|--------|-----|
| 站台名稱 | FourPLWebAPI |
| 應用程式集區 | FourPLWebAPI |
| 實體路徑 | C:\inetpub\wwwroot\FourPLWebAPI |
| 繫結類型 | https |
| 連接埠 | 443 (或自訂) |
| SSL 憑證 | (選擇或建立) |

### 4. 複製檔案

```powershell
# 複製發布檔案到 IIS 站台目錄
Copy-Item .\publish\* C:\inetpub\wwwroot\FourPLWebAPI\ -Recurse -Force
```

---

## 權限設定

### 應用程式集區身分識別

建議使用**網域帳號**作為應用程式集區身分識別：

1. 選取應用程式集區 → 進階設定
2. 找到 **識別** → 點擊 `...`
3. 選擇 **自訂帳戶** → 設定網域帳號

### 必要權限

| 資料夾 | 權限 | 說明 |
|--------|------|------|
| 網站根目錄 | 讀取與執行 | 應用程式執行 |
| logs 資料夾 | 修改 | Serilog 寫入日誌 |

### 設定權限

```powershell
# 假設應用程式集區使用 IIS AppPool\FourPLWebAPI 身分
$acl = Get-Acl "C:\inetpub\wwwroot\FourPLWebAPI"
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "IIS AppPool\FourPLWebAPI",
    "ReadAndExecute",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl "C:\inetpub\wwwroot\FourPLWebAPI" $acl

# logs 資料夾需要寫入權限
$logsPath = "C:\inetpub\wwwroot\FourPLWebAPI\logs"
if (!(Test-Path $logsPath)) { New-Item -ItemType Directory -Path $logsPath }
$aclLogs = Get-Acl $logsPath
$ruleLogs = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "IIS AppPool\FourPLWebAPI",
    "Modify",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow"
)
$aclLogs.SetAccessRule($ruleLogs)
Set-Acl $logsPath $aclLogs
```

---

## 驗證部署

### 健康檢查

```bash
curl https://your-server/api/integration/health
```

預期回應：
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-01T00:00:00Z",
  "version": "1.0.0"
}
```

### Swagger UI (選用)

若已啟用 Swagger，可訪問：
```
https://your-server/swagger
```

---

## 故障排除

### 錯誤：DllNotFoundException (sapnco.dll)

**原因**：
1. SAP NCo DLL 不存在
2. VC++ Runtime 未安裝
3. 使用了 32 位元 DLL

**解決方案**：
1. 確認 `sapnco.dll` 存在於網站根目錄
2. 安裝 Visual C++ Redistributable x64
3. 確認 DLL 為 x64 版本

### 錯誤：502.5 - Process Failure

**原因**：應用程式啟動失敗

**解決方案**：
1. 檢查 `appsettings.json` 設定是否正確
2. 查看 Windows 事件檢視器 → 應用程式日誌
3. 啟用 stdout 日誌：

修改 `web.config`:
```xml
<aspNetCore processPath="dotnet" 
            arguments=".\FourPLWebAPI.dll" 
            stdoutLogEnabled="true" 
            stdoutLogFile=".\logs\stdout" />
```

### 錯誤：無法連線 SAP

**原因**：網路或防火牆問題

**解決方案**：
1. 確認伺服器可存取 SAP 主機
2. 檢查防火牆規則 (SAP 預設 Port: 3300)
3. 確認 SAP 連線參數正確

---

## 部署檢查清單

### 部署前

- [ ] 安裝 ASP.NET Core Hosting Bundle 8.0
- [ ] 安裝 Visual C++ Redistributable x64
- [ ] 確認 SAP NCo DLL (x64) 已準備
- [ ] 確認正式環境設定檔已更新
- [ ] 執行 `scripts/App_ScheduleConfig.sql` 建立排程設定表

### IIS 設定

- [ ] 建立應用程式集區 (.NET CLR = 沒有受控碼)
- [ ] **啟用 32 位元應用程式 = False**
- [ ] **Start Mode = AlwaysRunning** (Hangfire 必要)
- [ ] **Idle Time-out = 0** (Hangfire 必要)
- [ ] 設定應用程式集區身分識別
- [ ] 建立網站並繫結 HTTPS
- [ ] **Preload Enabled = True** (Hangfire 必要)

### 檔案與權限

- [ ] 複製發布檔案至網站目錄
- [ ] 確認 sapnco.dll 存在
- [ ] 確認 sapnco_utils.dll 存在
- [ ] 設定網站目錄讀取權限
- [ ] 設定 logs 目錄寫入權限

### 驗證

- [ ] 健康檢查 API 回應正常
- [ ] 日誌檔案正常產生
- [ ] 測試 cabinet-export API
- [ ] Hangfire Dashboard 可存取 (`/hangfire`)
- [ ] 排程設定已正確同步

---

## 附錄：appsettings.json 範例

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Hangfire": "Information"
    }
  },
  "AllowedHosts": "*",
  "UseMockSap": false,
  "ConnectionStrings": {
    "DefaultConnection": "Server=SQL_SERVER;Database=FourPL;User Id=USER;Password=PASS;",
    "HangfireConnection": "Server=SQL_SERVER;Database=FourPL;User Id=USER;Password=PASS;"
  },
  "Hangfire": {
    "DashboardPath": "/hangfire",
    "ScheduleSyncIntervalMinutes": 5
  },
  "Sap": {
    "AppServerHost": "SAP_SERVER",
    "SystemNumber": "00",
    "SystemId": "PRD",
    "User": "SAP_USER",
    "Password": "SAP_PASSWORD",
    "Client": "100",
    "Language": "ZH"
  },
  "Sftp": {
    "Host": "SFTP_SERVER",
    "Port": 22,
    "Username": "SFTP_USER",
    "Password": "SFTP_PASSWORD"
  }
}
```

