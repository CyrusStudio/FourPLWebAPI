# Git Cleanup Implementation Plan

解決 `src/publish/` 檔案被 Git 追蹤的問題。

## Proposed Changes

### Configuration
#### [MODIFY] [.gitignore](file:///c:/Lotus/FourPLWebAPI/.gitignore)
- 加入 `[Pp]ublish/` 以排除發布目錄。

### Git Operations
- 已確認 `src/publish/` 僅為 untracked，更新 `.gitignore` 即可排除。

## Verification Plan
- 執行 `git status` 確認 `src/publish/` 不再出現在檔案清單中。
