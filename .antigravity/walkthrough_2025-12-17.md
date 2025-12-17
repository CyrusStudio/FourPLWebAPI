# Return Export 方法重構 Walkthrough

## 完成日期
2025-12-17

## 目標
重構 `DataTransformService.cs` 中的 Export 建立方法，減少程式碼重複。

## 完成的工作

### 1. 建立統一的 `CreateExportBase` 方法
建立一個通用的基礎方法，接受所有必要參數來建立 `DataTransExport` 物件：
- 32 個參數涵蓋所有動態欄位
- 固定值（如 `SalesOrg = "TW02"`）直接在方法內設定
- 新增 `pricingUnit` 和 `costCenter` 參數支援 Sample 特殊需求
- `formRefItem` 改為 string 類型支援空字串

### 2. 重構的方法（共 12 個）

| 方法 | ItemCategory | 說明 |
|------|-------------|------|
| `CreateBatchOrderExport` | ZTW1 | 訂單銷貨 |
| `CreateBatchOrderZTW2` | ZTW2 | 銷貨折讓 |
| `CreateBatchFreeGoodsExport` | ZTW5 | 贈品 |
| `CreateBatchAddOnExport` | ZTW5 | 加購品 |
| `TransformSampleBatchItems` | ZTW6 | 樣品 (特殊: costCenter="TW02_72100") |
| `CreateReturnType1Export` | ZTW4 | 銷貨退回 |
| `CreateReturnType2ExportZTW7` | ZTW7 | 退貨入庫 |
| `CreateReturnType2ExportZTW8` | ZTW8 | 換貨出庫 |
| `CreateReturnType3ExportZTW3` | ZTW3 | 退貨折讓 |
| `CreateReturnType3ExportZTWB` | ZTWB | 換貨重開 |
| `CreateReturnType4Export` | ZTW3 | 銷貨折讓 (Type4) |

### 3. 程式碼減少
- **重構前 (Session開始)**: ~1068 行
- **重構後**: 560 行
- **減少**: ~508 行（約 **52%**）

## 驗證結果
```
建置成功。
    0 個警告
    0 個錯誤
```

## 下一步
1. 執行 SQL 腳本更新資料庫
2. 測試 API 功能
3. Commit 變更

