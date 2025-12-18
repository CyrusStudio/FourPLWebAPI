using FourPLWebAPI.Infrastructure.Abstractions;
using FourPLWebAPI.Models;
using FourPLWebAPI.Services.Abstractions;

namespace FourPLWebAPI.Services.Implementations;

/// <summary>
/// SO 主檔同步服務實作
/// 從 SAP ZT4PL_BILLING 表查詢資料並同步至 SQL Server
/// </summary>
public class SOService(
    ISapHelper sapHelper,
    ISqlHelper sqlHelper,
    ILogger<SOService> logger) : ISOService
{
    private readonly ISapHelper _sapHelper = sapHelper;
    private readonly ISqlHelper _sqlHelper = sqlHelper;
    private readonly ILogger<SOService> _logger = logger;

    // SAPDS 資料庫連線字串名稱
    private const string SAPDSConnectionName = "SAPDSConnection";

    // SAP 查詢欄位 (提取為常數，避免重複建立)
    private static readonly string[] SapQueryFields =
    [
        "ARSHPNO", "ARSSHPIM", "MATNR", "VRKME", "FKDAT",
        "INVONO", "INVODATE", "KWMENG", "CHARG", "KBETR_ZTW2",
        "ARBLPNO", "KUNNR", "VFDAT", "FORMNO", "KUNNR_SH"
    ];

    // SAP 欄位索引常數 (增強可讀性)
    private static class FieldIndex
    {
        public const int Arshpno = 0;      // SONumber
        public const int Arsshpim = 1;     // SOItem
        public const int Matnr = 2;        // MaterialCode
        public const int Vrkme = 3;        // SalesUnit
        public const int Fkdat = 4;        // SalesDate
        public const int Invono = 5;       // Invoice
        public const int Invodate = 6;     // InvoiceDate
        public const int Kwmeng = 7;       // Qty
        public const int Charg = 8;        // Batch
        public const int KbetrZtw2 = 9;    // FixedPrice (原始值)
        public const int Arblpno = 10;     // CreditMemo
        public const int Kunnr = 11;       // CustomerCode
        public const int Vfdat = 12;       // ValidityPeriod
        public const int Formno = 13;      // BPMOriginNumber
        public const int KunnrSh = 14;     // SPNumber
    }

    // 定義同步配置
    private static readonly (string Query, string TargetTable, string Name)[] SyncConfigs =
    [
        ("ORDLA IN ( 'A' , 'L' )", "Sales_ArichSOMaster", "Arich"),
        ("ORDLA IN ( 'Z' , 'B' )", "Sales_ZLSOMaster", "ZL")
    ];

    /// <inheritdoc />
    public async Task<SOSyncAllResult> SyncAllSOMasterAsync(string? startDate = null)
    {
        var allResult = new SOSyncAllResult();

        // 格式化日期 (若未提供則預設昨天)
        var bDate = FormatDate(startDate);

        _logger.LogInformation("開始同步所有 SO 資料 - StartDate: {StartDate}", bDate);

        // 並行處理所有同步配置
        var syncTasks = SyncConfigs.Select(config =>
            SyncSingleTableAsync(config.Query, config.TargetTable, config.Name, bDate));

        var results = await Task.WhenAll(syncTasks);
        allResult.Results.AddRange(results);

        // 同步完成後更新 MaterialDesc
        try
        {
            await UpdateMaterialDescAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 MaterialDesc 失敗，但同步已完成");
        }

        // 計算總計
        allResult.TotalDeletedCount = allResult.Results.Sum(r => r.DeletedCount);
        allResult.TotalInsertedCount = allResult.Results.Sum(r => r.InsertedCount);
        allResult.Success = allResult.Results.All(r => r.Success);
        allResult.Message = allResult.Success
            ? $"同步完成，共刪除 {allResult.TotalDeletedCount} 筆，新增 {allResult.TotalInsertedCount} 筆"
            : $"部分同步失敗: {string.Join("; ", allResult.Results.Where(r => !r.Success).Select(r => r.Message))}";

        _logger.LogInformation("所有 SO 資料同步完成 - {Message}", allResult.Message);

        return allResult;
    }

    /// <summary>
    /// 格式化日期字串
    /// </summary>
    private static string FormatDate(string? startDate)
    {
        if (string.IsNullOrWhiteSpace(startDate))
        {
            return DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
        }

        var digits = new string([.. startDate.Where(char.IsDigit)]);
        return digits.Length == 8 ? digits : DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
    }

    /// <summary>
    /// 同步單一資料表
    /// </summary>
    private async Task<SOSyncResult> SyncSingleTableAsync(string query, string targetTable, string name, string bDate)
    {
        var result = new SOSyncResult { TargetTable = targetTable };

        try
        {
            _logger.LogInformation("開始同步 {Name} 資料 - TargetTable: {Table}", name, targetTable);

            // 呼叫 RFC_READ_TABLE
            _logger.LogInformation("查詢 ZT4PL_BILLING, 條件: UPDSTS='S' AND {Query} AND INVODATE>='{Date}'", query, bDate);

            var sapResult = await _sapHelper.ExecuteRfcAsync("RFC_READ_TABLE", builder =>
            {
                builder.SetImport("QUERY_TABLE", "ZT4PL_BILLING");
                builder.SetImport("DELIMITER", ";");

                // OPTIONS Table
                builder.AddTable("OPTIONS",
                [
                    new() { { "TEXT", "UPDSTS EQ 'S'" } },
                    new() { { "TEXT", " AND " + query } },
                    new() { { "TEXT", $" AND INVODATE GE '{bDate}'" } }
                ]);

                // FIELDS Table
                builder.AddTable("FIELDS",
                    [.. SapQueryFields.Select(f => new Dictionary<string, object> { { "FIELDNAME", f } })]);
            });

            if (!sapResult.Success)
            {
                _logger.LogError("SAP 查詢失敗 ({Name}): {Error}", name, sapResult.ErrorMessage);
                result.Success = false;
                result.Message = $"SAP 查詢失敗: {sapResult.ErrorMessage}";
                return result;
            }

            // 解析 DATA Table
            var soDataList = ParseDataTable(sapResult, name);
            _logger.LogInformation("從 SAP 取得 {Name} {Count} 筆資料", name, soDataList.Count);
            result.Data = soDataList;

            // 執行資料同步 (刪除舊資料 + Bulk Insert)
            if (soDataList.Count > 0)
            {
                // 刪除舊資料
                var deleteSql = $"DELETE FROM {targetTable} WHERE InvoiceDate >= @InvoiceDate";
                result.DeletedCount = await _sqlHelper.ExecuteWithConnectionAsync(
                    deleteSql,
                    new { InvoiceDate = bDate },
                    SAPDSConnectionName);
                _logger.LogInformation("刪除 {Name} 舊資料: {Count} 筆", name, result.DeletedCount);

                // Bulk Insert 新資料
                result.InsertedCount = await _sqlHelper.BulkInsertAsync(targetTable, soDataList, SAPDSConnectionName);
                _logger.LogInformation("新增 {Name} 資料: {Count} 筆", name, result.InsertedCount);
            }

            result.Success = true;
            result.Message = $"{name} 同步完成，刪除 {result.DeletedCount} 筆，新增 {result.InsertedCount} 筆";
            _logger.LogInformation("{Name} 同步完成 - {Message}", name, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Name} 資料同步發生錯誤", name);
            result.Success = false;
            result.Message = $"{name} 同步失敗: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 解析 SAP 回傳的 DATA Table
    /// </summary>
    private List<SOMasterData> ParseDataTable(SapRfcResult sapResult, string name)
    {
        var soDataList = new List<SOMasterData>();

        if (!sapResult.Tables.TryGetValue("DATA", out var dataTable))
        {
            return soDataList;
        }

        foreach (var row in dataTable)
        {
            if (row.TryGetValue("WA", out var waObj) && waObj is string waStr)
            {
                var values = waStr.Split(';');
                if (values.Length >= SapQueryFields.Length)
                {
                    try
                    {
                        soDataList.Add(ParseSOMasterData(values));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "解析資料列失敗 ({Name}): {Row}", name, waStr);
                    }
                }
            }
        }

        return soDataList;
    }

    /// <summary>
    /// 解析 SAP 回傳的資料列
    /// </summary>
    private static SOMasterData ParseSOMasterData(string[] values)
    {
        // 解析數值欄位
        if (!decimal.TryParse(values[FieldIndex.Kwmeng].Trim(), out var qty)) qty = 0;
        if (!decimal.TryParse(values[FieldIndex.KbetrZtw2].Trim(), out var rawPrice)) rawPrice = 0;

        // 計算 FixedPrice (原值 * 0.1)
        var fixedPrice = rawPrice * 0.1m;

        // 處理銷售單位轉換
        var salesUnit = values[FieldIndex.Vrkme].Trim();
        if (salesUnit == "BOT") salesUnit = "BT";

        // 處理發票日期 (0 轉為空字串)
        var invoiceDate = values[FieldIndex.Invodate].Trim();
        if (invoiceDate == "0") invoiceDate = string.Empty;

        return new SOMasterData
        {
            SONumber = values[FieldIndex.Arshpno].Trim(),
            SOItem = values[FieldIndex.Arsshpim].Trim(),
            MaterialCode = values[FieldIndex.Matnr].Trim(),
            MaterialDesc = string.Empty,  // 透過後續 UPDATE 填入
            SalesUnit = salesUnit,
            SalesDate = values[FieldIndex.Fkdat].Trim(),
            Invoice = values[FieldIndex.Invono].Trim(),
            InvoiceDate = invoiceDate,
            Qty = qty,
            Batch = values[FieldIndex.Charg].Trim(),
            ValidityPeriod = values[FieldIndex.Vfdat].Trim(),
            FixedPrice = fixedPrice,
            CreditMemo = values[FieldIndex.Arblpno].Trim(),
            CustomerCode = values[FieldIndex.Kunnr].Trim(),
            BPMOriginNumber = values[FieldIndex.Formno].Trim(),
            SPNumber = values[FieldIndex.KunnrSh].Trim()
        };
    }

    /// <summary>
    /// 更新 MaterialDesc 欄位
    /// </summary>
    private async Task UpdateMaterialDescAsync()
    {
        const string updateSql = @"
            UPDATE S SET S.MaterialDesc = M.MaterialDesc 
            FROM Sales_ZLSOMaster AS S 
            INNER JOIN Sales_MaterialMaster AS M ON S.MaterialCode = M.MaterialCode;
            
            UPDATE S SET S.MaterialDesc = M.MaterialDesc 
            FROM Sales_ArichSOMaster AS S 
            INNER JOIN Sales_MaterialMaster AS M ON S.MaterialCode = M.MaterialCode;";

        var rowsAffected = await _sqlHelper.ExecuteWithConnectionAsync(updateSql, null, SAPDSConnectionName);
        _logger.LogInformation("更新 MaterialDesc 完成，共 {Count} 筆資料受影響", rowsAffected);
    }
}
