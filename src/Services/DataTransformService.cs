using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using FourPLWebAPI.Infrastructure;
using FourPLWebAPI.Models;
using FourPLWebAPI.Extensions;

namespace FourPLWebAPI.Services;

/// <summary>
/// 資料轉換服務實作
/// 將 BPM 表單資料轉換為 SAP 匯出格式
/// </summary>
public class DataTransformService : IDataTransformService
{
    private readonly ISqlHelper _sqlHelper;
    private readonly ILogger<DataTransformService> _logger;
    private readonly string _sapdsDatabase;

    // 連線字串名稱常數
    private const string BpmProConnection = "BPMProConnection";
    private const string SapdsConnection = "SAPDSConnection";

    // 目標資料表名稱（不含資料庫前綴）
    private const string ExportTableName = "[dbo].[FourPL_DataTrans_Export_Verify]";
    private const string QueueTableName = "[dbo].[FourPL_DataTrans_Queue_Verify]";

    // 完整表名（含資料庫前綴），供 BulkInsert 使用
    private string ExportVerifyTable => ExportTableName;
    private string QueueVerifyTable => QueueTableName;

    // 完整表名（含資料庫前綴），供跨資料庫 SQL 使用
    private string FullExportTable => $"[{_sapdsDatabase}].{ExportTableName}";
    private string FullQueueTable => $"[{_sapdsDatabase}].{QueueTableName}";

    // Export DTO 共用常數（減少重複程式碼）
    private static readonly ExportPriceInfo ZeroPrice = new(PricingUnit: "1000");
    private static readonly ExportReturnInfo EmptyReturn = new();
    private static readonly ExportReturnInfo SampleCostCenter = new(CostCenter: "TW02_72100");

    public DataTransformService(ISqlHelper sqlHelper, ILogger<DataTransformService> logger, IConfiguration configuration)
    {
        _sqlHelper = sqlHelper;
        _logger = logger;

        // 從連線字串解析資料庫名稱
        var connStr = configuration.GetConnectionString(SapdsConnection)
            ?? throw new InvalidOperationException($"未設定 {SapdsConnection} 連線字串");
        _sapdsDatabase = ParseDatabaseName(connStr);
        _logger.LogDebug("SAPDS 資料庫名稱: {Database}", _sapdsDatabase);
    }

    /// <summary>
    /// 從連線字串解析 Initial Catalog（資料庫名稱）
    /// </summary>
    private static string ParseDatabaseName(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        return builder.InitialCatalog;
    }

    /// <inheritdoc />
    public async Task<DataTransformResult> ProcessPendingAsync(DateTime? startDate = null)
    {
        var effectiveStartDate = startDate ?? new DateTime(2025, 11, 1);
        _logger.LogInformation("批次處理待處理資料，起始日期: {StartDate}", effectiveStartDate);

        var result = new DataTransformResult { Success = true };
        var allExportItems = new List<DataTransExport>();
        var errors = new List<string>();

        // ===== Step 1: 將新資料加入 Queue =====
        try
        {
            var newMasters = await FetchAllMastersAsync();
            if (newMasters.Count > 0)
            {
                _logger.LogInformation("發現 {Count} 筆新資料，加入 Queue", newMasters.Count);
                await InsertAllToQueueAsync(newMasters);
            }
            else
            {
                _logger.LogInformation("沒有新資料需要加入 Queue");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Step 1 失敗：新增資料到 Queue 時發生錯誤");
            errors.Add($"[Queue] 新增資料失敗: {ex.Message}");
        }

        // ===== Step 2: 處理 TWC1D002 訂單（獨立處理）=====
        try
        {
            var (orderItems, freeGoods, addOns) = await FetchOrderDetailsFromQueueAsync();
            if (orderItems.Count > 0 || addOns.Count > 0)
            {
                _logger.LogInformation("[TWC1D002] 訂單明細：{Count} 筆, 贈品：{FreeCount} 筆, 加購：{AddOnCount} 筆",
                    orderItems.Count, freeGoods.Count, addOns.Count);
                var (exports, orderErrors) = TransformOrderBatchItems(orderItems, freeGoods, addOns);
                allExportItems.AddRange(exports);
                foreach (var err in orderErrors)
                {
                    _logger.LogWarning("[TWC1D002] 轉換錯誤: {Error}", err);
                    errors.Add($"[TWC1D002] {err}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TWC1D002] 訂單處理失敗");
            errors.Add($"[TWC1D002] 訂單處理失敗: {ex.Message}");
        }

        // ===== Step 3: 處理 TWC0D003 樣品（獨立處理）=====
        try
        {
            var sampleItems = await FetchSampleDetailsFromQueueAsync();
            if (sampleItems.Count > 0)
            {
                _logger.LogInformation("[TWC0D003] 樣品明細：{Count} 筆", sampleItems.Count);
                var exports = TransformSampleBatchItems(sampleItems);
                allExportItems.AddRange(exports);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TWC0D003] 樣品處理失敗");
            errors.Add($"[TWC0D003] 樣品處理失敗: {ex.Message}");
        }

        // ===== Step 4: 處理 TWC0D004 退貨（獨立處理）=====
        try
        {
            var returnItems = await FetchReturnDetailsFromQueueAsync();
            if (returnItems.Count > 0)
            {
                _logger.LogInformation("[TWC0D004] 退貨明細：{Count} 筆", returnItems.Count);
                var exports = TransformReturnBatchItems(returnItems);
                allExportItems.AddRange(exports);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TWC0D004] 退貨處理失敗");
            errors.Add($"[TWC0D004] 退貨處理失敗: {ex.Message}");
        }

        // ===== Step 5: 批次寫入 Export =====
        if (allExportItems.Count > 0)
        {
            _logger.LogInformation("開始批次寫入，共 {Count} 筆 Export 項目", allExportItems.Count);

            try
            {
                // 嘗試 BulkInsert
                var insertCount = await _sqlHelper.BulkInsertAsync(ExportVerifyTable, allExportItems);
                result.ProcessedCount = insertCount;
                _logger.LogInformation("批次寫入成功，共 {Count} 筆", insertCount);
            }
            catch (Exception bulkEx)
            {
                // BulkInsert 失敗，降級為逐筆寫入
                _logger.LogWarning(bulkEx, "BulkInsert 失敗，降級為逐筆寫入以追蹤錯誤");
                var (successCount, insertErrors) = await InsertExportsRowByRowAsync(allExportItems);
                result.ProcessedCount = successCount;
                errors.AddRange(insertErrors);
            }

            // Step 6: 更新 Queue ProcessedAt（只有成功的會被標記）
            if (result.ProcessedCount > 0)
            {
                await MarkQueueAsProcessedAsync();
            }
        }
        else
        {
            _logger.LogInformation("沒有待處理的 Export 資料");
        }

        result.Errors = errors;
        result.Success = errors.Count == 0;
        result.Message = errors.Count == 0
            ? $"處理完成，共 {result.ProcessedCount} 筆成功"
            : $"處理完成，共 {result.ProcessedCount} 筆成功，{errors.Count} 個錯誤";

        if (errors.Count > 0)
        {
            _logger.LogWarning("處理完成但有錯誤，共 {ErrorCount} 個錯誤", errors.Count);
        }

        return result;
    }


    #region Queue-First 流程方法

    /// <summary>
    /// 一次查詢所有主表（不 JOIN M，只查 S 和 Queue）
    /// </summary>
    private async Task<List<(string RequisitionID, string SerialID, string DiagramID)>> FetchAllMastersAsync()
    {
        var sql = $@"
            SELECT S.RequisitionID, S.SerialID, S.DiagramID
            FROM [dbo].[FSe7en_Sys_Requisition] S
            LEFT JOIN {FullQueueTable} Q ON S.RequisitionID = Q.RequisitionID
            WHERE S.Status = 1
              AND S.DiagramID IN ('TWC1D002', 'TWC0D003', 'TWC0D004')
              AND (S.TimeStart >= CONVERT(DATETIME, '2025-12-01 00:00:00', 102))
              AND (S.TimeLastAction < CONVERT(DATETIME, '2025-12-17 00:00:00', 102))
              AND Q.RequisitionID IS NULL";

        return (await _sqlHelper.QueryWithConnectionAsync<(string RequisitionID, string SerialID, string DiagramID)>(
            sql, null, BpmProConnection)).ToList();
    }

    /// <summary>
    /// 將所有主表 INSERT 到 Queue（ProcessedAt = NULL）
    /// </summary>
    private async Task InsertAllToQueueAsync(List<(string RequisitionID, string SerialID, string DiagramID)> masters)
    {
        if (masters.Count == 0) return;

        _logger.LogInformation("寫入 {Count} 筆到 Queue", masters.Count);

        var now = DateTime.Now;
        var queueEntries = masters.Select(m => new QueueEntry
        {
            RequisitionID = m.RequisitionID,
            DiagramID = m.DiagramID,
            SerialID = m.SerialID,
            CreatedAt = now,
            ProcessedAt = null,  // NULL = 待處理
            ExportCount = 0
        }).ToList();

        await _sqlHelper.BulkInsertAsync(QueueVerifyTable, queueEntries);
    }


    /// <summary>
    /// 查詢訂單明細（JOIN Queue WHERE ProcessedAt IS NULL）
    /// </summary>
    private async Task<(List<OrderBatchItem> Items, List<OrderFreeGoodsBatchItem> FreeGoods, List<OrderAddOnBatchItem> AddOns)>
        FetchOrderDetailsFromQueueAsync()
    {
        // D 明細（只處理 Invoice=2,3 且 RequestType=1,3,4）
        var mainSql = $@"
            SELECT 
                S.RequisitionID, S.SerialID, S.TimeLastAction,
                M.ApplicantID, M.Invoice, M.RequestType, M.CustomerCode, M.CustomerName, 
                M.CustomerSPCode, M.Remark, M.QuotationType, M.PriceGroup,
                D.ItemNo, D.MaterialCode, D.Qty, D.UOM, D.Purpose, D.PriceType, D.PARENT,
                D.OldInvoicePriceWithTax, D.OldSalePriceWithTax, D.NewInvoicePriceWithTax, D.NewSalePriceWithTax
            FROM [dbo].[FSe7en_Sys_Requisition] S
            INNER JOIN [dbo].[FM7T_TWF1D002_M] M ON S.RequisitionID = M.RequisitionID
            INNER JOIN [dbo].[FM7T_TWF1D002_D] D ON M.RequisitionID = D.RequisitionID
            INNER JOIN {FullQueueTable} Q ON S.RequisitionID = Q.RequisitionID
            WHERE Q.DiagramID = 'TWC1D002' AND Q.ProcessedAt IS NULL
              AND M.Invoice IN (2,3) AND M.RequestType IN (1,3,4)
            ORDER BY S.RequisitionID, D.ItemNo";

        var items = (await _sqlHelper.QueryWithConnectionAsync<OrderBatchItem>(
            mainSql, null, BpmProConnection)).ToList();

        // D2 贈品（需要 JOIN M 表確保條件一致）
        var freeGoodsSql = $@"
            SELECT D2.RequisitionID, D2.MaterialCode, D2.FreeMaterialCode, D2.FreeQty, D2.UOM, D2.Purpose, D2.CHILD
            FROM [dbo].[FM7T_TWF1D002_D2] D2
            INNER JOIN [dbo].[FM7T_TWF1D002_M] M ON D2.RequisitionID = M.RequisitionID
            INNER JOIN {FullQueueTable} Q ON D2.RequisitionID = Q.RequisitionID
            WHERE Q.DiagramID = 'TWC1D002' AND Q.ProcessedAt IS NULL 
              AND M.Invoice IN (2,3) AND M.RequestType IN (1,3,4)
              AND D2.FreeQty > 0";

        var freeGoods = (await _sqlHelper.QueryWithConnectionAsync<OrderFreeGoodsBatchItem>(
            freeGoodsSql, null, BpmProConnection)).ToList();

        // D3 加購品（包含 M 表資訊，可獨立處理）
        var addOnsSql = $@"
            SELECT 
                S.RequisitionID, S.SerialID, S.TimeLastAction,
                M.ApplicantID, M.Invoice, M.CustomerCode, M.CustomerName, M.CustomerSPCode, M.Remark,
                D3.ItemNo, D3.MaterialCode, D3.AddQty, D3.UOM, D3.Purpose
            FROM [dbo].[FSe7en_Sys_Requisition] S
            INNER JOIN [dbo].[FM7T_TWF1D002_M] M ON S.RequisitionID = M.RequisitionID
            INNER JOIN [dbo].[FM7T_TWF1D002_D3] D3 ON M.RequisitionID = D3.RequisitionID
            INNER JOIN {FullQueueTable} Q ON S.RequisitionID = Q.RequisitionID
            WHERE Q.DiagramID = 'TWC1D002' AND Q.ProcessedAt IS NULL 
              AND M.Invoice IN (2,3) AND M.RequestType IN (1,3,4)
              AND D3.AddQty > 0
            ORDER BY S.RequisitionID, D3.ItemNo";

        var addOns = (await _sqlHelper.QueryWithConnectionAsync<OrderAddOnBatchItem>(
            addOnsSql, null, BpmProConnection)).ToList();

        return (items, freeGoods, addOns);
    }

    /// <summary>
    /// 查詢樣品明細（JOIN Queue WHERE ProcessedAt IS NULL）
    /// </summary>
    private async Task<List<SampleBatchItem>> FetchSampleDetailsFromQueueAsync()
    {
        var sql = $@"
            SELECT 
                S.RequisitionID, S.SerialID, S.TimeLastAction,
                M.ApplicantID, M.Invoice, M.CustomerCode, M.CustomerName, 
                ISNULL(M.CustomerSPCode, M.CustomerCode) AS CustomerSPCode, 
                M.Remark, M.FundingSource,
                D.DNo, D.MaterialCode, D.Qty, D.UOM, 
                ISNULL(D.Purpose, '') AS Purpose
            FROM [dbo].[FSe7en_Sys_Requisition] S
            INNER JOIN [dbo].[FM7T_TWF0D003_M] M ON S.RequisitionID = M.RequisitionID
            INNER JOIN [dbo].[FM7T_TWF0D003_D] D ON M.RequisitionID = D.RequisitionID
            INNER JOIN {FullQueueTable} Q ON S.RequisitionID = Q.RequisitionID
            WHERE Q.DiagramID = 'TWC0D003' AND Q.ProcessedAt IS NULL AND M.Invoice IN (2,3)
            ORDER BY S.RequisitionID, D.DNo";

        return (await _sqlHelper.QueryWithConnectionAsync<SampleBatchItem>(
            sql, null, BpmProConnection)).ToList();
    }

    /// <summary>
    /// 查詢退貨明細（JOIN Queue WHERE ProcessedAt IS NULL）
    /// </summary>
    private async Task<List<ReturnBatchItem>> FetchReturnDetailsFromQueueAsync()
    {
        var sql = $@"
            SELECT 
                S.RequisitionID, S.SerialID, S.TimeLastAction,
                M.ApplicantID, M.Invoice, M.CustomerCode, M.CustomerName,
                (SELECT TOP 1 SPNumber 
                 FROM (SELECT SPNumber FROM [{_sapdsDatabase}].[dbo].[Sales_ArichSOMaster] WITH (NOLOCK) 
                       WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem
                       UNION ALL
                       SELECT SPNumber FROM [{_sapdsDatabase}].[dbo].[Sales_ZLSOMaster] WITH (NOLOCK) 
                       WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem) AS SP
                ) AS SPNumber,
                M.Remark, M.RequestType,
                D.ItemNo, D.MaterialCode, D.Qty, D.UOM, ISNULL(D.Notes, '') AS Notes, D.Batch,
                D.SalesOrderNumber, D.SOItem, D.InvoiceNumber, D.InvoiceDate,
                D.OldUnitPriceInTax, D.TotalUnitPriceInTax, D.NewUnitPriceInTax, D.NewTotalUnitPriceInTax,
                D.ExchangeOut, D.ReturnCode, D.SalesDate, D.DiscountOrderNumber
            FROM [dbo].[FSe7en_Sys_Requisition] S
            INNER JOIN [dbo].[FM7T_TWF0D004_M] M ON S.RequisitionID = M.RequisitionID
            INNER JOIN [dbo].[FM7T_TWF0D004_D] D ON M.RequisitionID = D.RequisitionID
            INNER JOIN {FullQueueTable} Q ON S.RequisitionID = Q.RequisitionID
            WHERE Q.DiagramID = 'TWC0D004' AND Q.ProcessedAt IS NULL AND M.Invoice IN (2,3)
            ORDER BY S.RequisitionID, D.ItemNo";

        return (await _sqlHelper.QueryWithConnectionAsync<ReturnBatchItem>(
            sql, null, BpmProConnection)).ToList();
    }

    /// <summary>
    /// 更新 Queue ProcessedAt（標記為已處理）
    /// </summary>
    private async Task MarkQueueAsProcessedAsync()
    {
        var sql = $@"
            UPDATE {FullQueueTable}
            SET ProcessedAt = GETDATE()
            WHERE ProcessedAt IS NULL";

        var affected = await _sqlHelper.ExecuteAsync(sql, null);
        _logger.LogInformation("已標記 {Count} 筆 Queue 為已處理", affected);
    }

    /// <summary>
    /// 逐筆寫入 Export（用於 BulkInsert 失敗時的降級處理）
    /// </summary>
    private async Task<(int SuccessCount, List<string> Errors)> InsertExportsRowByRowAsync(List<DataTransExport> items)
    {
        var errors = new List<string>();
        var successCount = 0;

        _logger.LogInformation("開始逐筆寫入，共 {Count} 筆", items.Count);

        var insertSql = $@"
            INSERT INTO {FullExportTable}
            (RequisitionID, FormNo, FormItem, FormRefItem, ApplicantID, SalesOrg, DistributionChannel, Division,
             ReceivingParty, CustomerNumber, CustomerName, SPNumber, ApprovalDate, Remark, ItemCategory,
             PricingType, PricingGroup, MaterialCode, Batch, SalesChannel, Qty, SalesUnit, DebitCreditType,
             Currency, InvoicePriceWithTax, InvoicePrice, TotalInvoicePriceWithTax, TotalInvoicePrice,
             FixedPriceWithTax, PricingUnit, ItemPurpose, ReturnCode, SalesDate, OriginSONumber, OriginSOItem,
             NewSONumber, NewSOItem, InvoiceNumber, InvoiceDate, CreditNote, ValidityPeriod, Sloc, CostCenter,
             ExportStatus, CreatedAt)
            VALUES
            (@RequisitionID, @FormNo, @FormItem, @FormRefItem, @ApplicantID, @SalesOrg, @DistributionChannel, @Division,
             @ReceivingParty, @CustomerNumber, @CustomerName, @SPNumber, @ApprovalDate, @Remark, @ItemCategory,
             @PricingType, @PricingGroup, @MaterialCode, @Batch, @SalesChannel, @Qty, @SalesUnit, @DebitCreditType,
             @Currency, @InvoicePriceWithTax, @InvoicePrice, @TotalInvoicePriceWithTax, @TotalInvoicePrice,
             @FixedPriceWithTax, @PricingUnit, @ItemPurpose, @ReturnCode, @SalesDate, @OriginSONumber, @OriginSOItem,
             @NewSONumber, @NewSOItem, @InvoiceNumber, @InvoiceDate, @CreditNote, @ValidityPeriod, @Sloc, @CostCenter,
             @ExportStatus, @CreatedAt)";

        foreach (var item in items)
        {
            try
            {
                await _sqlHelper.ExecuteAsync(insertSql, item);
                successCount++;
            }
            catch (Exception ex)
            {
                var errorMsg = $"RequisitionID={item.RequisitionID}, FormNo={item.FormNo}, FormItem={item.FormItem}, Category={item.ItemCategory}: {ex.Message}";
                _logger.LogError(ex, "[Export 寫入失敗] {Error}", errorMsg);
                errors.Add($"[Export] {errorMsg}");
            }
        }

        _logger.LogInformation("逐筆寫入完成，成功 {Success} 筆，失敗 {Failed} 筆", successCount, errors.Count);
        return (successCount, errors);
    }

    #endregion



    #region 批次轉換方法

    /// <summary>
    /// 轉換訂單批次資料為 Export 格式
    /// D + D2 綁一起（D2 依賴 D 的 MaterialCode），D3 獨立
    /// </summary>
    private (List<DataTransExport> Exports, List<string> Errors) TransformOrderBatchItems(
        List<OrderBatchItem> items,
        List<OrderFreeGoodsBatchItem> freeGoods,
        List<OrderAddOnBatchItem> addOns)
    {
        var exports = new List<DataTransExport>();
        var errors = new List<string>();

        var freeGoodsLookup = freeGoods.ToLookup(x => x.RequisitionID);

        // 用於追蹤每個 RequisitionID 的 itemSerialNo（供 D3 使用）
        var itemSerialNoTracker = new Dictionary<string, int>();

        // ===== 處理 D + D2（綁一起）=====
        var grouped = items.GroupBy(x => x.RequisitionID);
        foreach (var group in grouped)
        {
            try
            {
                var requisitionId = group.Key;
                var first = group.First();
                var formNo = GetFormNo(first.SerialID);
                var approvalDate = first.TimeLastAction.ToString("yyyyMMdd");
                var itemSerialNo = 1;

                foreach (var detail in group.OrderBy(x => x.ItemNo))
                {
                    var refItem = detail.ItemNo;  // 使用原始項次（如 10、20、30）

                    // ZTW1
                    exports.Add(CreateBatchOrderExport(detail, formNo, itemSerialNo++, refItem, approvalDate, "ZTW1", "D"));

                    // ZTW2 條件檢查
                    if (ShouldCreateBatchZTW2(detail))
                    {
                        var ztw2 = CreateBatchOrderZTW2(detail, formNo, itemSerialNo++, refItem, approvalDate);
                        if (ztw2 != null) exports.Add(ztw2);
                    }

                    // ZTW5 贈品 - 只用 MaterialCode 匹配（D2 依賴 D）
                    var matchingFreeGoods = freeGoodsLookup[requisitionId]
                        .Where(f => f.MaterialCode == detail.MaterialCode)
                        .ToList();
                    foreach (var fg in matchingFreeGoods)
                    {
                        exports.Add(CreateBatchFreeGoodsExport(detail, fg, formNo, itemSerialNo++, refItem, approvalDate));
                    }
                }

                // 記錄此 RequisitionID 的 itemSerialNo（供 D3 使用）
                itemSerialNoTracker[requisitionId] = itemSerialNo;
            }
            catch (Exception ex)
            {
                errors.Add($"{group.Key}: {ex.Message}");
            }
        }

        // ===== 處理 D3 加購品（獨立，使用自身的 M 表欄位）=====
        var addOnsGrouped = addOns.GroupBy(x => x.RequisitionID);
        foreach (var group in addOnsGrouped)
        {
            try
            {
                var requisitionId = group.Key;
                var first = group.First();
                var formNo = GetFormNo(first.SerialID);
                var approvalDate = first.TimeLastAction.ToString("yyyyMMdd");

                // 如果 D 表有此 RequisitionID，繼續使用那邊的 itemSerialNo
                // 如果沒有，從 1 開始（獨立處理）
                var itemSerialNo = itemSerialNoTracker.TryGetValue(requisitionId, out var trackedNo) ? trackedNo : 1;

                foreach (var addOn in group.OrderBy(x => x.ItemNo))
                {
                    // 使用獨立方法，直接使用 addOn 自身的 M 表欄位
                    exports.Add(CreateAddOnExportIndependent(addOn, formNo, itemSerialNo++, approvalDate));
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{group.Key}: {ex.Message}");
            }
        }

        return (exports, errors);
    }



    /// <summary>
    /// 轉換退貨批次資料為 Export 格式
    /// </summary>
    private List<DataTransExport> TransformReturnBatchItems(List<ReturnBatchItem> items)
    {
        var exports = new List<DataTransExport>();

        // 按 RequisitionID 分組處理
        var grouped = items.GroupBy(x => x.RequisitionID);
        foreach (var group in grouped)
        {
            var first = group.First();
            var formNo = GetFormNo(first.SerialID);
            var approvalDate = first.TimeLastAction.ToString("yyyyMMdd");
            var itemSerialNo = 1;

            foreach (var detail in group.OrderBy(x => x.ItemNo))
            {
                switch (first.RequestType)
                {
                    case 1: // ZTW4
                        exports.Add(CreateReturnType1Export(detail, formNo, itemSerialNo++, approvalDate));
                        break;
                    case 2: // ZTW7 + ZTW8
                        var refItem2 = itemSerialNo;
                        exports.Add(CreateReturnType2ExportZTW7(detail, formNo, itemSerialNo++, refItem2, approvalDate));
                        exports.Add(CreateReturnType2ExportZTW8(detail, formNo, itemSerialNo++, refItem2, approvalDate));
                        break;
                    case 3: // ZTW3 + ZTWB
                        var refItem3 = itemSerialNo;
                        exports.Add(CreateReturnType3ExportZTW3(detail, formNo, itemSerialNo++, refItem3, approvalDate));
                        exports.Add(CreateReturnType3ExportZTWB(detail, formNo, itemSerialNo++, refItem3, approvalDate));
                        break;
                    case 4: // ZTW3
                        exports.Add(CreateReturnType4Export(detail, formNo, itemSerialNo++, approvalDate));
                        break;
                }
            }
        }

        return exports;
    }

    #endregion

    #region 批次 Export 建立方法


    /// <summary>
    /// 統一的 Export 建立方法 - 使用 Record DTOs 封裝參數
    /// </summary>
    private DataTransExport CreateExportBase(
        ExportFormInfo form,
        ExportCustomerInfo customer,
        ExportItemInfo item,
        ExportPriceInfo price,
        ExportReturnInfo returnInfo)
    {
        return new DataTransExport
        {
            RequisitionID = form.RequisitionID,
            FormNo = form.FormNo,
            FormItem = FormatFormItem(form.ItemNo),
            FormRefItem = form.FormRefItem,
            ApplicantID = customer.ApplicantID,
            SalesOrg = "TW02",
            DistributionChannel = "10",
            Division = "10",
            ReceivingParty = customer.Invoice == 2 ? "L" : "B",
            CustomerNumber = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            SPNumber = customer.SPNumber,
            ApprovalDate = form.ApprovalDate,
            Remark = string.IsNullOrEmpty(customer.Remark) ? null : customer.Remark,
            ItemCategory = item.ItemCategory,
            PricingType = item.PricingType,
            PricingGroup = item.PricingGroup,
            MaterialCode = item.MaterialCode,
            Batch = item.Batch,
            SalesChannel = "",
            Qty = item.Qty,
            SalesUnit = item.SalesUnit,
            DebitCreditType = item.DebitCreditType,
            Currency = "TWD",
            InvoicePriceWithTax = price.InvoicePriceWithTax,
            InvoicePrice = price.InvoicePrice,
            TotalInvoicePriceWithTax = price.TotalInvoicePriceWithTax,
            TotalInvoicePrice = price.TotalInvoicePrice,
            FixedPriceWithTax = price.FixedPriceWithTax,
            PricingUnit = price.PricingUnit,
            ItemPurpose = returnInfo.ItemPurpose,
            ReturnCode = returnInfo.ReturnCode,
            SalesDate = returnInfo.SalesDate,
            OriginSONumber = returnInfo.OriginSONumber,
            OriginSOItem = returnInfo.OriginSOItem,
            NewSONumber = "",
            NewSOItem = "",
            InvoiceNumber = returnInfo.InvoiceNumber,
            InvoiceDate = returnInfo.InvoiceDate,
            CreditNote = returnInfo.CreditNote,
            ValidityPeriod = "",
            Sloc = "",
            CostCenter = string.IsNullOrEmpty(returnInfo.CostCenter) ? null : returnInfo.CostCenter,
            ExportStatus = 0,
            CreatedAt = DateTime.Now
        };
    }


    // ZTW1: 訂單銷貨
    private DataTransExport CreateBatchOrderExport(OrderBatchItem i, string formNo, int itemNo, int refItem, string approvalDate, string category, string debitCredit)
        => CreateExportBase(
            i.ToFormInfo(formNo, itemNo, refItem, approvalDate),
            i.ToCustomerInfo(),
            i.ToItemInfo(category, debitCredit),
            i.ToPriceInfo(),
            EmptyReturn with { ItemPurpose = i.Purpose.OrEmpty() });

    private bool ShouldCreateBatchZTW2(OrderBatchItem item)
        => item.ToDiscountPriceInfo().Price != null;

    // ZTW2: 銷貨折讓
    private DataTransExport? CreateBatchOrderZTW2(OrderBatchItem i, string formNo, int itemNo, int refItem, string approvalDate)
    {
        var (discountPrice, _) = i.ToDiscountPriceInfo();
        if (discountPrice == null) return null;

        return CreateExportBase(
            i.ToFormInfo(formNo, itemNo, refItem, approvalDate),
            i.ToCustomerInfo(),
            i.ToItemInfo("ZTW2", "C"),
            discountPrice,
            EmptyReturn with { ItemPurpose = i.Purpose.OrEmpty(), ReturnCode = "201" });
    }

    // ZTW5: 贈品
    private DataTransExport CreateBatchFreeGoodsExport(OrderBatchItem m, OrderFreeGoodsBatchItem fg, string formNo, int itemNo, int refItem, string approvalDate)
        => CreateExportBase(
            m.ToFormInfo(formNo, itemNo, refItem, approvalDate),
            m.ToCustomerInfo(),
            fg.ToItemInfo(m),
            ZeroPrice,
            EmptyReturn with { ItemPurpose = fg.Purpose.OrEmpty() });


    // ZTW5: 加購品（獨立處理，使用自身的 M 表欄位）
    private DataTransExport CreateAddOnExportIndependent(OrderAddOnBatchItem addOn, string formNo, int itemNo, string approvalDate)
        => CreateExportBase(
            addOn.ToFormInfo(formNo, itemNo, approvalDate),
            addOn.ToCustomerInfo(),
            addOn.ToItemInfo(),
            ZeroPrice,
            EmptyReturn with { ItemPurpose = addOn.Purpose.OrEmpty() });

    /// <summary>
    /// 轉換樣品批次資料為 Export 格式
    /// </summary>
    // ZTW6: 樣品
    private List<DataTransExport> TransformSampleBatchItems(List<SampleBatchItem> items)
    {
        // 按 RequisitionID 分組，用第一筆的 TimeLastAction 作為 approvalDate
        var grouped = items.GroupBy(x => x.RequisitionID);
        var exports = new List<DataTransExport>();

        foreach (var group in grouped)
        {
            var first = group.First();
            var formNo = GetFormNo(first.SerialID);
            var approvalDate = first.TimeLastAction.ToString("yyyyMMdd");

            foreach (var i in group)
            {
                var remark = !string.IsNullOrEmpty(i.FundingSource)
                    ? $"{i.Remark ?? ""}(掛帳{i.FundingSource})"
                    : i.Remark ?? "";

                exports.Add(CreateExportBase(
                    i.ToFormInfo(formNo, approvalDate),
                    i.ToCustomerInfo(remark),
                    i.ToItemInfo(),
                    ZeroPrice,
                    SampleCostCenter with { ItemPurpose = i.Purpose ?? "" }));
            }
        }
        return exports;
    }

    // ZTW4: 銷貨退回
    private DataTransExport CreateReturnType1Export(ReturnBatchItem i, string formNo, int itemNo, string approvalDate)
        => CreateExportBase(
            i.ToFormInfo(formNo, itemNo, itemNo, approvalDate),
            i.ToCustomerInfo(),
            i.ToItemInfo("ZTW4", "C", i.Qty, i.Batch.OrEmpty()),
            i.ToPriceInfo(i.OldUnitPriceInTax, i.TotalUnitPriceInTax),
            i.ToReturnInfo(i.ReturnCode.OrEmpty()));

    // ZTW7: 退貨入庫
    private DataTransExport CreateReturnType2ExportZTW7(ReturnBatchItem i, string formNo, int itemNo, int refItem, string approvalDate)
        => CreateExportBase(
            i.ToFormInfo(formNo, itemNo, refItem, approvalDate),
            i.ToCustomerInfo(),
            i.ToItemInfo("ZTW7", "C", i.Qty, i.Batch.OrEmpty()),
            ZeroPrice,
            i.ToReturnInfo(i.ReturnCode.OrEmpty()));

    // ZTW8: 換貨出庫
    private DataTransExport CreateReturnType2ExportZTW8(ReturnBatchItem i, string formNo, int itemNo, int refItem, string approvalDate)
        => CreateExportBase(
            i.ToFormInfo(formNo, itemNo, refItem, approvalDate),
            i.ToCustomerInfo(),
            i.ToItemInfo("ZTW8", "D", i.ExchangeOut),
            ZeroPrice,
            i.ToReturnInfo(i.ReturnCode.OrEmpty()));

    // ZTW3: 退貨折讓（使用舊價格）
    private DataTransExport CreateReturnType3ExportZTW3(ReturnBatchItem i, string formNo, int itemNo, int refItem, string approvalDate)
    {
        var unitPrice = i.Qty > 0 ? i.TotalUnitPriceInTax / i.Qty * i.ExchangeOut : 0;
        return CreateExportBase(
            i.ToFormInfo(formNo, itemNo, refItem, approvalDate),
            i.ToCustomerInfo(),
            i.ToItemInfo("ZTW3", "C", i.ExchangeOut),
            i.ToPriceInfo(i.OldUnitPriceInTax, unitPrice),
            i.ToReturnInfo(i.ReturnCode.OrEmpty(), i.DiscountOrderNumber.OrEmpty()));
    }

    // ZTWB: 換貨重開（使用新價格）
    private DataTransExport CreateReturnType3ExportZTWB(ReturnBatchItem i, string formNo, int itemNo, int refItem, string approvalDate)
        => CreateExportBase(
            i.ToFormInfo(formNo, itemNo, refItem, approvalDate),
            i.ToCustomerInfo(),
            i.ToItemInfo("ZTWB", "D", i.ExchangeOut),
            i.ToPriceInfo(i.NewUnitPriceInTax, i.NewTotalUnitPriceInTax),
            i.ToReturnInfo(i.ReturnCode.OrEmpty(), includeInvoice: false));

    // Type4: 銷貨折讓
    private DataTransExport CreateReturnType4Export(ReturnBatchItem i, string formNo, int itemNo, string approvalDate)
        => CreateExportBase(
            i.ToFormInfo(formNo, itemNo, itemNo, approvalDate),
            i.ToCustomerInfo(),
            i.ToItemInfo("ZTW3", "C", i.Qty),
            i.ToPriceInfo(0, i.NewTotalUnitPriceInTax),
            i.ToReturnInfo("200"));

    #endregion


    #region 私有輔助方法 - 共用

    /// <summary>
    /// 從 SerialID 取得 FormNo（取第5-14字元）
    /// </summary>
    private static string GetFormNo(string serialId)
    {
        if (string.IsNullOrEmpty(serialId) || serialId.Length < 14)
            return serialId;
        return serialId.Substring(4, 10);
    }

    /// <summary>
    /// 格式化 FormItem（右邊補零至 6 位，使用特殊格式）
    /// </summary>
    private static string FormatFormItem(int itemNo)
    {
        // 原始 SQL: RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', len(@ItemSerialNo) + 1), len(@ItemSerialNo) + 1) as NVARCHAR), 6)
        var itemStr = itemNo.ToString();
        var padLength = itemStr.Length + 1;
        var paddedItem = itemStr.PadRight(padLength, '0');
        return paddedItem.PadLeft(6, '0');
    }



    #endregion
}
