# Release Build Walkthrough (Re-run)

成功重新執行專案的 Release 建置，並確認產出物已被 Git 正確忽略。

## 執行步驟
1. 執行 `dotnet publish src/FourPLWebAPI.csproj -c Release -o src/publish`。
2. 檢查 `git status`，確認 `src/publish/` 目錄不再出現在「未追蹤的檔案」清單中。

## 驗證結果
- [x] 建置成功 (Exit code: 0)
- [x] Git 忽略狀態正確 ( `src/publish/` 已不在 `git status` 中)
- [x] 產出物完整 (包含 78 個檔案與 25 個子目錄)
