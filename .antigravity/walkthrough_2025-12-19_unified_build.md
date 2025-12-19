# Unified Release Build Walkthrough

已成功將發布路徑統一至根目錄的 `publish/`，並清理了多餘的 `src/publish/` 目錄。

## 執行步驟
1. 刪除冗餘的 `src/publish/` 資料夾。
2. 執行 `dotnet publish src/FourPLWebAPI.csproj -c Release -o publish` 將產出物統一放在根目錄。
3. 驗證 `git status`，確認 `publish/` 已被 `.gitignore` 正確忽略。

## 驗證結果
- [x] 多餘目錄已清理
- [x] 產出物已正確發布至 `c:\Lotus\FourPLWebAPI\publish\`
- [x] Git 狀態保持乾淨，無預期外的 Untracked files。
