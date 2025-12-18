# 修正 IDE 程式碼分析問題實作計畫

本計畫旨在修復 IDE 發現的程式碼分析建議，以提升程式碼品質並遵循現代 C# (C# 12+) 標準。

## 擬議變更

### API 控制器與服務最佳化

#### [MODIFY] [SapFileController.cs](file:///c:/Lotus/FourPLWebAPI/src/Controllers/SapFileController.cs)
- **簡化物件初始化** (Line 44): 使用 `new()` 簡化 `SapExecuteResult` 的初始化。

#### [MODIFY] [DataTransformService.cs](file:///c:/Lotus/FourPLWebAPI/src/Services/DataTransformService.cs)
- **標記更多方法為靜態**: 將下列不存取執行個體資料的私有方法標記為 `static`：
    - `CreateBatchOrderExport`
    - `CreateBatchOrderZTW2`
    - `CreateBatchFreeGoodsExport`
    - `CreateAddOnExportIndependent`
    - `CreateReturnType1Export`
    - `CreateReturnType2ExportZTW7`
    - `CreateReturnType2ExportZTW8`
    - `CreateReturnType3ExportZTW3`
    - `CreateReturnType3ExportZTWB`
    - `CreateReturnType4Export`


## 驗證計畫

### 自動化測試
- 由於此變更純屬語法層面的重構，不影響邏輯，將執行編譯檢查。
- 執行 `dotnet build` 確保變更後專案仍可正確編譯。

### 手動驗證
- 檢查程式碼風格是否一致，確保主要建構函式的欄位初始化正確。
