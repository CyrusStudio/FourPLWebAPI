# Infrastructure 目錄重構計畫 (含 Abstractions 分離)

本計畫旨在透過「介面分離」與「功能分組」來優化 `Infrastructure` 目錄。

## 擬議變更

### 1. 目錄結構與檔案分配

| 目錄位置 | 包含檔案 | 建議 Namespace |
| :--- | :--- | :--- |
| **Abstractions** | 所有 `I...Helper.cs`, `ISftpConnectionFactory.cs` | `FourPLWebAPI.Infrastructure.Abstractions` |
| **SAP** | `SapHelper.cs`, `MockSapHelper.cs`, `SapDestinationConfig.cs`, `SapMasterDataAttributes.cs`, `SapMasterDataRepository.cs` | `FourPLWebAPI.Infrastructure.SAP` |
| **Persistence** | `SqlHelper.cs` | `FourPLWebAPI.Infrastructure.Persistence` |
| **Messaging** | `EmailHelper.cs` | `FourPLWebAPI.Infrastructure.Messaging` |
| **Files** | `SftpHelper.cs`, `SftpConnectionFactory.cs`, `NetworkDiskHelper.cs` | `FourPLWebAPI.Infrastructure.Files` |
| **Scheduling** | (現有檔案) | `FourPLWebAPI.Infrastructure.Scheduling` |

### 2. 重構步驟
1. **建立目錄架構**：
   - `Infrastructure/Abstractions`
   - `Infrastructure/SAP`
   - `Infrastructure/Persistence`
   - `Infrastructure/Messaging`
   - `Infrastructure/Files`
2. **移動檔案**：將介面全數移入 `Abstractions`，實作檔案按分類移入對應資料夾。
3. **更新 Namespace**：大量調整檔案內部的 `namespace` 宣告。
4. **全域引用修正**：修正 `Services`、`Jobs`、`Controllers` 中的引用路徑。
5. **DI 註冊更新**：同步修改 `Program.cs` 中的服務註冊。

## 驗證計畫
- 透過 `dotnet build` 驗證編譯。
- 確保所有背景 Job 仍能正確透過 DI 取得所需服務。
