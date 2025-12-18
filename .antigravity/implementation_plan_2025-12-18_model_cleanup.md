# Models 深度清理計畫

除了移除完全無用的檔案外，本計畫也針對現有 Model 內部「部分失效」的程式碼進行精確清理。

## 擬議變更

### 1. [Component: Models]
移除過時且無引用的模型與屬性定義。

#### [DELETE] [BpmRequisitionModels.cs](file:///c:/Lotus/FourPLWebAPI/src/Models/BpmRequisitionModels.cs)
*   該檔案所有類別目前皆無引用。

#### [MODIFY] [SODTOs.cs](file:///c:/Lotus/FourPLWebAPI/src/Models/SODTOs.cs)
*   移除 `SOQueryRequest` 類別（確認 Controller 無引用）。

#### [MODIFY] [BatchQueryModels.cs](file:///c:/Lotus/FourPLWebAPI/src/Models/BatchQueryModels.cs)
*   從 `OrderBatchItem` 移除 `PARENT` 屬性。
*   從 `OrderFreeGoodsBatchItem` 移除 `CHILD` 屬性。

### 2. [Component: Services]
同步更新 SQL 查詢以減少資料傳輸開銷。

#### [MODIFY] [DataTransformService.cs](file:///c:/Lotus/FourPLWebAPI/src/Services/Implementations/DataTransformService.cs)
*   更新 `FetchOrderDetailsFromQueueAsync` 方法中的 SQL，移除 `D.PARENT` 與 `D2.CHILD` 欄位。

## 驗證計畫
- 執行 `dotnet build` 確保編譯成功且無任何警告。
