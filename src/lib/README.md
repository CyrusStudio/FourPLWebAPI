# SAP NCo 3.1 DLL 放置說明

## 必要檔案

請將以下 **x64** 版本的 DLL 檔案放入此資料夾：

1. `sapnco.dll` - SAP .NET Connector 核心元件
2. `sapnco_utils.dll` - SAP NCo 工具類別

## 取得方式

1. 登入 [SAP Software Download Center](https://support.sap.com/swdc)
2. 搜尋 "SAP .NET Connector 3.1"
3. 下載 **NCo 3.1 Patch Level** 最新版本 (x64)
4. 解壓縮後將上述 DLL 複製到此資料夾

## 注意事項

- **禁止**使用未經官方驗證的第三方 NuGet 套件
- 請確保下載的是 **x64** 版本，非 x86 版本
- 正式部署環境需安裝 **Microsoft Visual C++ Redistributable (x64)**

## 驗證

放入 DLL 後，執行以下命令確認專案可正常編譯：

```bash
dotnet build
```

若出現 `DllNotFoundException`，請確認：
1. DLL 檔案是 x64 版本
2. 伺服器已安裝對應版本的 VC++ Runtime
