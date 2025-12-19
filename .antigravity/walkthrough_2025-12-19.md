# Release Build Walkthrough

成功執行專案的 Release 建置，並將產出物發布至 `src/publish` 目錄。

## 執行步驟
1. 執行 `dotnet publish src/FourPLWebAPI.csproj -c Release -o src/publish`。
2. 驗證 `src/publish` 目錄內容，確認包含 `FourPLWebAPI.dll`, `FourPLWebAPI.exe` 以及相關依賴項（如 SAP NCo 函式庫）。

## 驗證結果
- [x] 建置成功 (Exit code: 0)
- [x] 產出物完整 (包含 78 個檔案與 25 個子目錄)
