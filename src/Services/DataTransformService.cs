using Dapper;
using Microsoft.Data.SqlClient;
using FourPLWebAPI.Infrastructure;
using FourPLWebAPI.Models;

namespace FourPLWebAPI.Services;

/// <summary>
/// 資料轉換服務實作
/// 將 BPM 表單資料轉換為 SAP 匯出格式
/// </summary>
public class DataTransformService : IDataTransformService
{
    private readonly ISqlHelper _sqlHelper;
    private readonly ILogger<DataTransformService> _logger;

    // 連線字串名稱常數
    private const string BpmProConnection = "BPMProConnection";
    private const string SapdsConnection = "SAPDSConnection";

    // 目標資料表名稱
    private const string ExportVerifyTable = "[dbo].[FourPL_DataTrans_Export_Verify]";
    private const string QueueVerifyTable = "[dbo].[FourPL_DataTrans_Queue_Verify]";

    public DataTransformService(ISqlHelper sqlHelper, ILogger<DataTransformService> logger)
    {
        _sqlHelper = sqlHelper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DataTransformResult> ProcessPendingAsync(DateTime? startDate = null)
    {
        var effectiveStartDate = startDate ?? new DateTime(2025, 11, 1);
        _logger.LogInformation("批次處理待處理資料，起始日期: {StartDate}", effectiveStartDate);

        try
        {
            var result = new DataTransformResult { Success = true };
            var allExportItems = new List<DataTransExport>();
            var errors = new List<string>();

            // ===== Step 1: 將新資料加入 Queue =====
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

            // ===== Step 2: 處理 Queue 中待處理的記錄（ProcessedAt IS NULL）=====
            // 處理訂單 (TWC1D002)
            var (orderItems, freeGoods, addOns) = await FetchOrderDetailsFromQueueAsync();
            if (orderItems.Count > 0 || addOns.Count > 0)
            {
                _logger.LogInformation("訂單明細：{Count} 筆, 贈品：{FreeCount} 筆, 加購：{AddOnCount} 筆",
                    orderItems.Count, freeGoods.Count, addOns.Count);
                var (exports, orderErrors) = TransformOrderBatchItems(orderItems, freeGoods, addOns);
                allExportItems.AddRange(exports);
                errors.AddRange(orderErrors);
            }

            // 處理樣品 (TWC0D003)
            var sampleItems = await FetchSampleDetailsFromQueueAsync();
            if (sampleItems.Count > 0)
            {
                _logger.LogInformation("樣品明細：{Count} 筆", sampleItems.Count);
                var exports = TransformSampleBatchItems(sampleItems);
                allExportItems.AddRange(exports);
            }

            // 處理退貨 (TWC0D004)
            var returnItems = await FetchReturnDetailsFromQueueAsync();
            if (returnItems.Count > 0)
            {
                _logger.LogInformation("退貨明細：{Count} 筆", returnItems.Count);
                var exports = TransformReturnBatchItems(returnItems);
                allExportItems.AddRange(exports);
            }

            // ===== Step 3: 批次寫入 Export =====
            if (allExportItems.Count > 0)
            {
                _logger.LogInformation("開始批次寫入，共 {Count} 筆 Export 項目", allExportItems.Count);
                var insertCount = await _sqlHelper.BulkInsertAsync(ExportVerifyTable, allExportItems);
                result.ProcessedCount = insertCount;
                _logger.LogInformation("批次寫入完成，共 {Count} 筆", insertCount);

                // Step 4: 更新 Queue ProcessedAt
                await MarkQueueAsProcessedAsync();
            }
            else
            {
                _logger.LogInformation("沒有待處理的 Export 資料");
            }


            result.Errors = errors;
            result.Message = $"處理完成，共 {result.ProcessedCount} 筆成功，{errors.Count} 筆錯誤";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批次處理失敗");
            return new DataTransformResult
            {
                Success = false,
                Message = $"批次處理失敗: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }


    #region Queue-First 流程方法

    /// <summary>
    /// 一次查詢所有主表（不 JOIN M，只查 S 和 Queue）
    /// </summary>
    private async Task<List<(string RequisitionID, string SerialID, string DiagramID)>> FetchAllMastersAsync()
    {
        const string sql = @"
            SELECT S.RequisitionID, S.SerialID, S.DiagramID
            FROM [dbo].[FSe7en_Sys_Requisition] S
            LEFT JOIN [SAPDS_QAS].[dbo].[FourPL_DataTrans_Queue_Verify] Q ON S.RequisitionID = Q.RequisitionID
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
        const string mainSql = @"
            SELECT 
                S.RequisitionID, S.SerialID, S.TimeLastAction,
                M.ApplicantID, M.Invoice, M.RequestType, M.CustomerCode, M.CustomerName, 
                M.CustomerSPCode, M.Remark, M.QuotationType, M.PriceGroup,
                D.ItemNo, D.MaterialCode, D.Qty, D.UOM, D.Purpose, D.PriceType, D.PARENT,
                D.OldInvoicePriceWithTax, D.OldSalePriceWithTax, D.NewInvoicePriceWithTax, D.NewSalePriceWithTax
            FROM [dbo].[FSe7en_Sys_Requisition] S
            INNER JOIN [dbo].[FM7T_TWF1D002_M] M ON S.RequisitionID = M.RequisitionID
            INNER JOIN [dbo].[FM7T_TWF1D002_D] D ON M.RequisitionID = D.RequisitionID
            INNER JOIN [SAPDS_QAS].[dbo].[FourPL_DataTrans_Queue_Verify] Q ON S.RequisitionID = Q.RequisitionID
            WHERE Q.DiagramID = 'TWC1D002' AND Q.ProcessedAt IS NULL
              AND M.Invoice IN (2,3) AND M.RequestType IN (1,3,4)
            ORDER BY S.RequisitionID, D.ItemNo";

        var items = (await _sqlHelper.QueryWithConnectionAsync<OrderBatchItem>(
            mainSql, null, BpmProConnection)).ToList();

        // D2 贈品（需要 JOIN M 表確保條件一致）
        const string freeGoodsSql = @"
            SELECT D2.RequisitionID, D2.MaterialCode, D2.FreeMaterialCode, D2.FreeQty, D2.UOM, D2.Purpose, D2.CHILD
            FROM [dbo].[FM7T_TWF1D002_D2] D2
            INNER JOIN [dbo].[FM7T_TWF1D002_M] M ON D2.RequisitionID = M.RequisitionID
            INNER JOIN [SAPDS_QAS].[dbo].[FourPL_DataTrans_Queue_Verify] Q ON D2.RequisitionID = Q.RequisitionID
            WHERE Q.DiagramID = 'TWC1D002' AND Q.ProcessedAt IS NULL 
              AND M.Invoice IN (2,3) AND M.RequestType IN (1,3,4)
              AND D2.FreeQty > 0";

        var freeGoods = (await _sqlHelper.QueryWithConnectionAsync<OrderFreeGoodsBatchItem>(
            freeGoodsSql, null, BpmProConnection)).ToList();

        // D3 加購品（包含 M 表資訊，可獨立處理）
        const string addOnsSql = @"
            SELECT 
                S.RequisitionID, S.SerialID, S.TimeLastAction,
                M.ApplicantID, M.Invoice, M.CustomerCode, M.CustomerName, M.CustomerSPCode, M.Remark,
                D3.ItemNo, D3.MaterialCode, D3.AddQty, D3.UOM, D3.Purpose
            FROM [dbo].[FSe7en_Sys_Requisition] S
            INNER JOIN [dbo].[FM7T_TWF1D002_M] M ON S.RequisitionID = M.RequisitionID
            INNER JOIN [dbo].[FM7T_TWF1D002_D3] D3 ON M.RequisitionID = D3.RequisitionID
            INNER JOIN [SAPDS_QAS].[dbo].[FourPL_DataTrans_Queue_Verify] Q ON S.RequisitionID = Q.RequisitionID
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
        const string sql = @"
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
            INNER JOIN [SAPDS_QAS].[dbo].[FourPL_DataTrans_Queue_Verify] Q ON S.RequisitionID = Q.RequisitionID
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
        const string sql = @"
            SELECT 
                S.RequisitionID, S.SerialID, S.TimeLastAction,
                M.ApplicantID, M.Invoice, M.CustomerCode, M.CustomerName,
                (SELECT TOP 1 SPNumber 
                 FROM (SELECT SPNumber FROM [SAPDS_QAS].[dbo].[Sales_ArichSOMaster] WITH (NOLOCK) 
                       WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem
                       UNION ALL
                       SELECT SPNumber FROM [SAPDS_QAS].[dbo].[Sales_ZLSOMaster] WITH (NOLOCK) 
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
            INNER JOIN [SAPDS_QAS].[dbo].[FourPL_DataTrans_Queue_Verify] Q ON S.RequisitionID = Q.RequisitionID
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
        const string sql = @"
            UPDATE [SAPDS_QAS].[dbo].[FourPL_DataTrans_Queue_Verify]
            SET ProcessedAt = GETDATE()
            WHERE ProcessedAt IS NULL";

        var affected = await _sqlHelper.ExecuteAsync(sql, null);
        _logger.LogInformation("已標記 {Count} 筆 Queue 為已處理", affected);
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
                    var refItem = itemSerialNo;

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

                // 如果 D 表有此 RequisitionID，繼續使用那邊的 itemSerialNo
                // 如果沒有，從 1 開始（獨立處理）
                var itemSerialNo = itemSerialNoTracker.TryGetValue(requisitionId, out var trackedNo) ? trackedNo : 1;

                foreach (var addOn in group.OrderBy(x => x.ItemNo))
                {
                    // 使用獨立方法，直接使用 addOn 自身的 M 表欄位
                    exports.Add(CreateAddOnExportIndependent(addOn, formNo, itemSerialNo++));
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
    /// 轉換樣品批次資料為 Export 格式
    /// </summary>
    // ZTW6: 樣品
    private List<DataTransExport> TransformSampleBatchItems(List<SampleBatchItem> items)
    {
        return items.Select(i =>
        {
            var remark = !string.IsNullOrEmpty(i.FundingSource)
                ? $"{i.Remark ?? ""}(掛帳{i.FundingSource})"
                : i.Remark ?? "";

            return CreateExportBase(i.RequisitionID, GetFormNo(i.SerialID), i.DNo, "", i.TimeLastAction.ToString("yyyyMMdd"),
                i.ApplicantID, i.Invoice, i.CustomerCode, i.CustomerName, i.CustomerSPCode ?? "", remark,
                "ZTW6", "", "", i.MaterialCode, "", i.Qty, i.UOM, "D",
                0, 0, 0, 0, 0,
                "", i.Purpose ?? "", "",
                "", "", "", "", "", "", "TW02_72100");
        }).ToList();
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
    /// 統一的 Export 建立方法 - 所有欄位作為參數傳入
    /// </summary>
    private DataTransExport CreateExportBase(
        string requisitionId, string formNo, int itemNo, string formRefItem, string approvalDate,
        string applicantId, int invoice, string customerCode, string customerName, string spNumber, string remark,
        string itemCategory, string pricingType, string pricingGroup, string materialCode,
        string batch, decimal qty, string salesUnit, string debitCreditType,
        decimal invoicePriceWithTax, decimal invoicePrice,
        decimal totalInvoicePriceWithTax, decimal totalInvoicePrice, decimal fixedPriceWithTax,
        string pricingUnit, string itemPurpose, string returnCode,
        string salesDate, string originSONumber, string originSOItem,
        string invoiceNumber, string invoiceDate, string creditNote, string costCenter)
    {
        return new DataTransExport
        {
            RequisitionID = requisitionId,
            FormNo = formNo,
            FormItem = FormatFormItem(itemNo),
            FormRefItem = formRefItem,
            ApplicantID = applicantId,
            SalesOrg = "TW02",
            DistributionChannel = "10",
            Division = "10",
            ReceivingParty = invoice == 2 ? "L" : "B",
            CustomerNumber = customerCode,
            CustomerName = customerName,
            SPNumber = spNumber,
            ApprovalDate = approvalDate,
            Remark = string.IsNullOrEmpty(remark) ? null : remark,
            ItemCategory = itemCategory,
            PricingType = pricingType,
            PricingGroup = pricingGroup,
            MaterialCode = materialCode,
            Batch = batch,
            SalesChannel = "",
            Qty = qty,
            SalesUnit = salesUnit,
            DebitCreditType = debitCreditType,
            Currency = "TWD",
            InvoicePriceWithTax = invoicePriceWithTax,
            InvoicePrice = invoicePrice,
            TotalInvoicePriceWithTax = totalInvoicePriceWithTax,
            TotalInvoicePrice = totalInvoicePrice,
            FixedPriceWithTax = fixedPriceWithTax,
            PricingUnit = pricingUnit,
            ItemPurpose = itemPurpose,
            ReturnCode = returnCode,
            SalesDate = salesDate,
            OriginSONumber = originSONumber,
            OriginSOItem = originSOItem,
            NewSONumber = "",
            NewSOItem = "",
            InvoiceNumber = invoiceNumber,
            InvoiceDate = invoiceDate,
            CreditNote = creditNote,
            ValidityPeriod = "",
            Sloc = "",
            CostCenter = string.IsNullOrEmpty(costCenter) ? null : costCenter,
            ExportStatus = 0,
            CreatedAt = DateTime.Now
        };
    }

    // ZTW1: 訂單銷貨
    private DataTransExport CreateBatchOrderExport(OrderBatchItem i, string formNo, int itemNo, int refItem, string approvalDate, string category, string debitCredit)
    {
        var isOldPrice = i.RequestType == 1 || i.RequestType == 4;
        var invoicePrice = isOldPrice ? i.OldInvoicePriceWithTax : i.NewInvoicePriceWithTax;
        var fixedPrice = isOldPrice ? i.OldSalePriceWithTax : i.NewSalePriceWithTax;

        return CreateExportBase(i.RequisitionID, formNo, itemNo, FormatFormItem(refItem), approvalDate,
            i.ApplicantID, i.Invoice, i.CustomerCode, i.CustomerName, i.CustomerSPCode ?? "", i.Remark ?? "",
            category, i.QuotationType ?? "", i.QuotationType == "1" ? i.CustomerCode : i.PriceGroup ?? "",
            i.MaterialCode, "", i.Qty, i.UOM, debitCredit,
            invoicePrice * 1000, invoicePrice / 1.05m * 1000,
            invoicePrice * i.Qty, invoicePrice / 1.05m * i.Qty, fixedPrice * 1000,
            "1000", i.Purpose ?? "", "",
            "", "", "", "", "", "", "");
    }

    private bool ShouldCreateBatchZTW2(OrderBatchItem item)
    {
        // 根據 RequestType 決定用舊價或新價
        // RequestType = 1 或 4 用舊價，否則用新價
        decimal invoicePrice, fixedPrice;
        if (item.RequestType == 1 || item.RequestType == 4)
        {
            invoicePrice = item.OldInvoicePriceWithTax;
            fixedPrice = item.OldSalePriceWithTax;
        }
        else
        {
            invoicePrice = item.NewInvoicePriceWithTax;
            fixedPrice = item.NewSalePriceWithTax;
        }
        // 當發票價 != 固定價時產生 ZTW2
        return invoicePrice != fixedPrice;
    }

    // ZTW2: 銷貨折讓
    private DataTransExport? CreateBatchOrderZTW2(OrderBatchItem i, string formNo, int itemNo, int refItem, string approvalDate)
    {
        var isOldPrice = i.RequestType == 1 || i.RequestType == 4;
        var invoicePrice = isOldPrice ? i.OldInvoicePriceWithTax : i.NewInvoicePriceWithTax;
        var salePrice = isOldPrice ? i.OldSalePriceWithTax : i.NewSalePriceWithTax;
        var priceDiff = (invoicePrice - salePrice) * i.Qty;
        if (priceDiff == 0) return null;

        return CreateExportBase(i.RequisitionID, formNo, itemNo, FormatFormItem(refItem), approvalDate,
            i.ApplicantID, i.Invoice, i.CustomerCode, i.CustomerName, i.CustomerSPCode ?? "", i.Remark ?? "",
            "ZTW2", i.QuotationType ?? "", i.QuotationType == "1" ? i.CustomerCode : i.PriceGroup ?? "",
            i.MaterialCode, "", i.Qty, i.UOM, "C",
            0, 0, priceDiff, priceDiff / 1.05m, 0,
            "1000", i.Purpose ?? "", "201",
            "", "", "", "", "", "", "");
    }

    // ZTW5: 贈品
    private DataTransExport CreateBatchFreeGoodsExport(OrderBatchItem m, OrderFreeGoodsBatchItem fg, string formNo, int itemNo, int refItem, string approvalDate)
        => CreateExportBase(m.RequisitionID, formNo, itemNo, FormatFormItem(refItem), approvalDate,
            m.ApplicantID, m.Invoice, m.CustomerCode, m.CustomerName, m.CustomerSPCode ?? "", m.Remark ?? "",
            "ZTW5", m.QuotationType ?? "", m.QuotationType == "1" ? m.CustomerCode : m.PriceGroup ?? "",
            fg.FreeMaterialCode, "", fg.FreeQty, fg.UOM, "D",
            0, 0, 0, 0, 0,
            "1000", fg.Purpose ?? "", "",
            "", "", "", "", "", "", "");

    // ZTW5: 加購品（依賴 D 表時使用）
    private DataTransExport CreateBatchAddOnExport(OrderBatchItem m, OrderAddOnBatchItem addOn, string formNo, int itemNo, string approvalDate)
        => CreateExportBase(m.RequisitionID, formNo, itemNo, FormatFormItem(itemNo), approvalDate,
            m.ApplicantID, m.Invoice, m.CustomerCode, m.CustomerName, m.CustomerSPCode ?? "", m.Remark ?? "",
            "ZTW5", "", "", addOn.MaterialCode, "", addOn.AddQty, addOn.UOM, "D",
            0, 0, 0, 0, 0,
            "1000", addOn.Purpose ?? "", "",
            "", "", "", "", "", "", "");

    // ZTW5: 加購品（獨立處理，使用自身的 M 表欄位）
    private DataTransExport CreateAddOnExportIndependent(OrderAddOnBatchItem addOn, string formNo, int itemNo)
        => CreateExportBase(addOn.RequisitionID, formNo, itemNo, FormatFormItem(itemNo), addOn.TimeLastAction.ToString("yyyyMMdd"),
            addOn.ApplicantID, addOn.Invoice, addOn.CustomerCode, addOn.CustomerName, addOn.CustomerSPCode ?? "", addOn.Remark ?? "",
            "ZTW5", "", "", addOn.MaterialCode, "", addOn.AddQty, addOn.UOM, "D",
            0, 0, 0, 0, 0,
            "1000", addOn.Purpose ?? "", "",
            "", "", "", "", "", "", "");

    // ZTW4: 銷貨退回
    private DataTransExport CreateReturnType1Export(ReturnBatchItem i, string formNo, int itemNo, string approvalDate)
        => CreateExportBase(i.RequisitionID, formNo, itemNo, FormatFormItem(itemNo), approvalDate,
            i.ApplicantID, i.Invoice, i.CustomerCode, i.CustomerName, i.SPNumber ?? "", i.Remark ?? "",
            "ZTW4", "", "", i.MaterialCode, i.Batch ?? "", i.Qty, i.UOM, "C",
            i.OldUnitPriceInTax * 1000, i.OldUnitPriceInTax / 1.05m * 1000,
            i.TotalUnitPriceInTax, i.TotalUnitPriceInTax / 1.05m, 0,
            "1000", i.Notes ?? "", i.ReturnCode ?? "",
            i.SalesDate ?? "", i.SalesOrderNumber ?? "", i.SOItem ?? "",
            i.InvoiceNumber ?? "", i.InvoiceDate ?? "", "", "");

    // ZTW7: 退貨入庫
    private DataTransExport CreateReturnType2ExportZTW7(ReturnBatchItem i, string formNo, int itemNo, int refItem, string approvalDate)
        => CreateExportBase(i.RequisitionID, formNo, itemNo, FormatFormItem(refItem), approvalDate,
            i.ApplicantID, i.Invoice, i.CustomerCode, i.CustomerName, i.SPNumber ?? "", i.Remark ?? "",
            "ZTW7", "", "", i.MaterialCode, i.Batch ?? "", i.Qty, i.UOM, "C",
            0, 0, 0, 0, 0,
            "1000", i.Notes ?? "", i.ReturnCode ?? "",
            i.SalesDate ?? "", i.SalesOrderNumber ?? "", i.SOItem ?? "",
            i.InvoiceNumber ?? "", i.InvoiceDate ?? "", "", "");

    // ZTW8: 換貨出庫
    private DataTransExport CreateReturnType2ExportZTW8(ReturnBatchItem i, string formNo, int itemNo, int refItem, string approvalDate)
        => CreateExportBase(i.RequisitionID, formNo, itemNo, FormatFormItem(refItem), approvalDate,
            i.ApplicantID, i.Invoice, i.CustomerCode, i.CustomerName, i.SPNumber ?? "", i.Remark ?? "",
            "ZTW8", "", "", i.MaterialCode, "", i.ExchangeOut, i.UOM, "D",
            0, 0, 0, 0, 0,
            "1000", i.Notes ?? "", i.ReturnCode ?? "",
            i.SalesDate ?? "", i.SalesOrderNumber ?? "", i.SOItem ?? "",
            i.InvoiceNumber ?? "", i.InvoiceDate ?? "", "", "");

    // ZTW3: 退貨折讓（使用舊價格）
    private DataTransExport CreateReturnType3ExportZTW3(ReturnBatchItem i, string formNo, int itemNo, int refItem, string approvalDate)
        => CreateExportBase(i.RequisitionID, formNo, itemNo, FormatFormItem(refItem), approvalDate,
            i.ApplicantID, i.Invoice, i.CustomerCode, i.CustomerName, i.SPNumber ?? "", i.Remark ?? "",
            "ZTW3", "", "", i.MaterialCode, "", i.ExchangeOut, i.UOM, "C",
            i.OldUnitPriceInTax * 1000, i.OldUnitPriceInTax / 1.05m * 1000,
            i.Qty > 0 ? i.TotalUnitPriceInTax / i.Qty * i.ExchangeOut : 0,
            i.Qty > 0 ? i.TotalUnitPriceInTax / i.Qty * i.ExchangeOut / 1.05m : 0, 0,
            "1000", i.Notes ?? "", i.ReturnCode ?? "",
            i.SalesDate ?? "", i.SalesOrderNumber ?? "", i.SOItem ?? "",
            i.InvoiceNumber ?? "", i.InvoiceDate ?? "", i.DiscountOrderNumber ?? "", "");

    // ZTWB: 換貨重開（使用新價格）
    private DataTransExport CreateReturnType3ExportZTWB(ReturnBatchItem i, string formNo, int itemNo, int refItem, string approvalDate)
        => CreateExportBase(i.RequisitionID, formNo, itemNo, FormatFormItem(refItem), approvalDate,
            i.ApplicantID, i.Invoice, i.CustomerCode, i.CustomerName, i.SPNumber ?? "", i.Remark ?? "",
            "ZTWB", "", "", i.MaterialCode, "", i.ExchangeOut, i.UOM, "D",
            i.NewUnitPriceInTax * 1000, i.NewUnitPriceInTax / 1.05m * 1000,
            i.NewTotalUnitPriceInTax, i.NewTotalUnitPriceInTax / 1.05m, 0,
            "1000", i.Notes ?? "", i.ReturnCode ?? "",
            i.SalesDate ?? "", i.SalesOrderNumber ?? "", i.SOItem ?? "",
            "", "", "", "");

    // Type4: 銷貨折讓
    private DataTransExport CreateReturnType4Export(ReturnBatchItem i, string formNo, int itemNo, string approvalDate)
        => CreateExportBase(i.RequisitionID, formNo, itemNo, FormatFormItem(itemNo), approvalDate,
            i.ApplicantID, i.Invoice, i.CustomerCode, i.CustomerName, i.SPNumber ?? "", i.Remark ?? "",
            "ZTW3", "", "", i.MaterialCode, "", i.Qty, i.UOM, "C",
            0, 0, i.NewTotalUnitPriceInTax, i.NewTotalUnitPriceInTax / 1.05m, 0,
            "1000", i.Notes ?? "", "200",
            i.SalesDate ?? "", i.SalesOrderNumber ?? "", i.SOItem ?? "",
            i.InvoiceNumber ?? "", i.InvoiceDate ?? "", "", "");

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
