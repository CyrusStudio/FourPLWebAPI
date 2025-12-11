# FourPL Web API - 開發者上手指南

## 目錄

1. [環境需求](#環境需求)
2. [專案結構](#專案結構)
3. [VS Code 開發](#vs-code-開發)
4. [Mock 模式](#mock-模式)
5. [API 測試](#api-測試)
6. [常見問題](#常見問題)

---

## 環境需求

### 必要軟體

| 軟體 | 版本 | 說明 |
|------|------|------|
| .NET SDK | 8.0+ | [下載連結](https://dotnet.microsoft.com/download/dotnet/8.0) |
| VS Code | Latest | [下載連結](https://code.visualstudio.com/) |
| C# Dev Kit | Latest | VS Code 擴充功能 |

### 選用軟體

- **Postman**: API 測試工具
- **Azure Data Studio**: SQL Server 管理工具

---

## 專案結構

```
FourPLWebAPI/
├── .vscode/                    # VS Code 設定
│   ├── launch.json            # 偵錯設定
│   └── tasks.json             # 建置任務
├── lib/                        # SAP NCo DLL
│   ├── README.md              # DLL 放置說明
│   ├── sapnco.dll             # (需手動放入)
│   └── sapnco_utils.dll       # (需手動放入)
├── src/                        # 原始碼
│   ├── Controllers/           # API Controller
│   ├── Infrastructure/        # 通用底層模組
│   ├── Models/                # DTO 模型
│   ├── Services/              # 業務邏輯層
│   ├── Program.cs             # 應用程式進入點
│   └── appsettings.json       # 設定檔
├── docs/                       # 文件
├── postman/                    # Postman 測試集合
└── FourPLWebAPI.sln           # Solution 檔案
```

---

## VS Code 開發

### 開啟專案

```powershell
cd c:\Lotus\FourPLWebAPI
code .
```

### 啟動偵錯

1. 按 `F5` 啟動偵錯
2. 程式會自動開啟瀏覽器至 Swagger UI
3. 預設網址: `https://localhost:5001/swagger`

### 建置專案

```powershell
# 在專案根目錄執行
dotnet build
```

### 執行專案 (不偵錯)

```powershell
cd src
dotnet run
```

### 發布專案

```powershell
dotnet publish src/FourPLWebAPI.csproj -c Release -o ./publish
```

---

## Mock 模式

### 啟用 Mock SAP

開發環境預設啟用 Mock 模式，無需連線 SAP 即可測試 API。

設定檔: `src/appsettings.Development.json`

```json
{
  "UseMockSap": true
}
```

### Mock 模式行為

當 `UseMockSap` 為 `true` 時：
- `ISapHelper` 會使用 `MockSapHelper` 實作
- SAP RFC 呼叫會回傳模擬資料
- 日誌會標註 `[MOCK]` 前綴

### 停用 Mock

```json
{
  "UseMockSap": false
}
```

> **⚠️ 注意**: 停用 Mock 後，需確保：
> 1. SAP NCo DLL 已放入 `lib` 資料夾
> 2. `appsettings.json` 的 SAP 連線參數正確

---

## API 測試

### 使用 Swagger UI

1. 啟動專案
2. 開啟 `https://localhost:5001/swagger`
3. 展開 `/api/integration/cabinet-export` 端點
4. 點擊 **Try it out**
5. 填入測試資料並執行

### 健康檢查

```bash
curl https://localhost:5001/api/integration/health
```

預期回應：
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-01T00:00:00Z",
  "version": "1.0.0"
}
```

### 機櫃匯出 API

**Endpoint**: `POST /api/integration/cabinet-export`

**請求範例**:
```json
{
  "requestId": "REQ-2024-001",
  "organizationCode": "ORG001",
  "cabinetType": "RACK",
  "dataCenterCode": "DC-TPE",
  "startDate": "20240101",
  "endDate": "20241231",
  "includeInactive": false
}
```

**回應範例**:
```json
{
  "success": true,
  "requestId": "REQ-2024-001",
  "message": "機櫃資料匯出成功",
  "processedCount": 3,
  "xmlFileName": "CabinetExport_ORG001_20240101120000.xml",
  "uploadedPath": "/export/CabinetExport_ORG001_20240101120000.xml",
  "startTime": "2024-01-01T12:00:00Z",
  "endTime": "2024-01-01T12:00:05Z",
  "elapsedMilliseconds": 5000
}
```

---

## 常見問題

### Q: 編譯時出現 SAP NCo 相關錯誤？

**A**: 請確認：
1. `lib` 資料夾中已放入 `sapnco.dll` 與 `sapnco_utils.dll`
2. DLL 為 **x64** 版本
3. 若只是開發測試，可啟用 Mock 模式略過實際 SAP 連線

### Q: F5 偵錯無法啟動？

**A**: 請確認：
1. 已安裝 VS Code C# Dev Kit 擴充功能
2. 已安裝 .NET 8 SDK
3. 嘗試重新載入 VS Code 視窗

### Q: 如何查看日誌？

**A**: 日誌儲存於 `src/logs/` 目錄，每日一個檔案：
```
logs/
├── fourplwebapi-20240101.log
├── fourplwebapi-20240102.log
└── ...
```

### Q: 如何設定 SQL Server 連線？

**A**: 修改 `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your_server;Database=FourPL;User Id=user;Password=pass;"
  }
}
```

---

## 聯絡資訊

如有問題，請聯繫 FourPL 開發團隊。
