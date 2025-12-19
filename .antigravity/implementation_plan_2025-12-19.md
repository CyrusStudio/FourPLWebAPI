# Release Build Implementation Plan

執行項目的 Release 建置並發布至指定目錄。

## Proposed Changes

### Build & Publish
使用 .NET CLI 進行發布。

- 執行 `dotnet publish src/FourPLWebAPI.csproj -c Release -o src/publish`

## Verification Plan

### Manual Verification
- 檢查 `src/publish` 目錄是否包含可執行的 DLL 及相關設定檔。
