# 專案架構重構報告

本次重構將專案調整為更符合 C# 最佳實踐的結構，提升了程式碼的可讀性與維護性。

## 修改內容

### 1. Controller 按權限分層
將 API 依照用途搬移至子目錄，保持邏輯清晰：
- **`Controllers/External`**：存放對外整合呼叫的 API。
  - `DataExchangeController.cs`
  - `IntegrationController.cs`
- **`Controllers/Internal`**：存放系統內部管理與排程用的 API。
  - `SapFileController.cs`
  - `DataTransformController.cs`

### 2. Service 介面與實作分離
為了解決介面與實作混雜的問題，建立了專用目錄：
- **`Services/Abstractions`**：存放所有服務介面 (`I...Service`)。
  - 拆分出 `ISOService.cs`。
- **`Services/Implementations`**：存放具體的服務實作類別。

### 3. Namespace 與引用更新
- 更新了所有受影響檔案的 `namespace`。
- 修正了 `Program.cs` 中的依賴注入註冊。
- 更新了背景任務 (`Jobs`) 的 `using` 指令。

### 4.- **Swagger 二分化**：透過 `DocInclusionPredicate` 實作自動分類視圖。
- **C# 12 特性應用**：全面採用主要建構函式 (Primary Constructors) 與集合運算式 (`[]`)。
- **效能與邏輯修復**：優化數值解析與集合檢查邏輯，移除所有 IDE 警告。
- **Jobs 目錄架構優化**：核心執行引擎與任務處理器分離。
- **Model 資料夾深度清理**：
    - 移除冗餘檔案：`BpmRequisitionModels.cs`。
    - 清理失效類別與屬性：移除 `SOQueryRequest`、`OrderBatchItem.PARENT`、`OrderFreeGoodsBatchItem.CHILD`。
    - SQL 查詢優化：同步更新 `DataTransformService` 的查詢語法，移除無用欄位讀取。
- **外部整合 (External)**：僅顯示 `Controllers.External` 命名空間下的 API。
- **內部管理 (Internal)**：僅顯示 `Controllers.Internal` 命名空間下的 API。
- **切換功能**：在 Swagger UI 右上角提供下拉選單進行視圖切換。

## 驗證結果

### 編譯測試
- 執行 `dotnet build`：**編譯成功 (0 錯誤, 0 警告)**。

### 目錄結構預覽
```text
src/
├── Controllers/
│   ├── External/ (對外整合)
│   └── Internal/ (對內管理/排程)
├── Services/
│   ├── Abstractions/ (介面定義)
│   └── Implementations/ (具體實作)
└── ...
```
