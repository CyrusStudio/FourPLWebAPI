using FourPLWebAPI.Infrastructure;
using FourPLWebAPI.Models;

namespace FourPLWebAPI.Services;

/// <summary>
/// SO 主檔同步服務介面
/// </summary>
public interface ISOService
{
    /// <summary>
    /// 同步所有 SO 主檔資料 (同時處理 ZL 和 Arich)
    /// </summary>
    /// <param name="startDate">查詢起始日 (YYYYMMDD)，預設為昨天</param>
    /// <returns>同步結果 (包含兩個表的結果)</returns>
    Task<SOSyncAllResult> SyncAllSOMasterAsync(string? startDate = null);
}

/// <summary>
/// SO 主檔同步服務實作
/// 從 SAP ZT4PL_BILLING 表查詢資料並同步至 SQL Server
/// </summary>
public class SOService : ISOService
{
    private readonly ISapHelper _sapHelper;
    private readonly ISqlHelper _sqlHelper;
    private readonly ILogger<SOService> _logger;

    // SAPDS 資料庫連線字串名稱
    private const string SAPDSConnectionName = "SAPDSConnection";

    // 定義同步配置
    private static readonly (string Query, string TargetTable, string Name)[] SyncConfigs =
    {
        ("ORDLA IN ( 'A' , 'L' )", "Sales_ArichSOMaster", "Arich"),
        ("ORDLA IN ( 'Z' , 'B' )", "Sales_ZLSOMaster", "ZL")
    };

    public SOService(ISapHelper sapHelper, ISqlHelper sqlHelper, ILogger<SOService> logger)
    {
        _sapHelper = sapHelper;
        _sqlHelper = sqlHelper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SOSyncAllResult> SyncAllSOMasterAsync(string? startDate = null)
    {
        var allResult = new SOSyncAllResult();

        // 日期範圍 (若未提供則預設昨天)
        var bDate = string.IsNullOrWhiteSpace(startDate)
            ? DateTime.Now.AddDays(-1).ToString("yyyyMMdd")
            : startDate;

        // 確保日期格式正確 (只取數字)
        bDate = new string(bDate.Where(char.IsDigit).ToArray());
        if (bDate.Length != 8)
        {
            bDate = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
        }

        _logger.LogInformation("開始同步所有 SO 資料 - StartDate: {StartDate}", bDate);

        // 依序處理每個配置
        foreach (var (query, targetTable, name) in SyncConfigs)
        {
            var result = await SyncSingleTableAsync(query, targetTable, name, bDate);
            allResult.Results.Add(result);
        }

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
    /// 同步單一資料表
    /// </summary>
    private async Task<SOSyncResult> SyncSingleTableAsync(string query, string targetTable, string name, string bDate)
    {
        var result = new SOSyncResult { TargetTable = targetTable };

        try
        {
            _logger.LogInformation("開始同步 {Name} 資料 - TargetTable: {Table}", name, targetTable);

            // 定義需要的欄位
            var fields = new[]
            {
                "ARSHPNO", "ARSSHPIM", "MATNR", "VRKME", "FKDAT",
                "INVONO", "INVODATE", "KWMENG", "CHARG", "KBETR_ZTW2",
                "ARBLPNO", "KUNNR", "VFDAT", "FORMNO", "KUNNR_SH"
            };

            // 呼叫 RFC_READ_TABLE
            _logger.LogInformation("查詢 ZT4PL_BILLING, 條件: UPDSTS='S' AND {Query} AND INVODATE>='{Date}'", query, bDate);

            var sapResult = await _sapHelper.ExecuteRfcAsync("RFC_READ_TABLE", builder =>
            {
                builder.SetImport("QUERY_TABLE", "ZT4PL_BILLING");
                builder.SetImport("DELIMITER", ";");

                // OPTIONS Table
                var optionsList = new List<Dictionary<string, object>>
                {
                    new() { { "TEXT", "UPDSTS EQ 'S'" } },
                    new() { { "TEXT", " AND " + query } },
                    new() { { "TEXT", $" AND INVODATE GE '{bDate}'" } }
                };
                builder.AddTable("OPTIONS", optionsList);

                // FIELDS Table
                var fieldList = new List<Dictionary<string, object>>();
                foreach (var f in fields)
                {
                    fieldList.Add(new Dictionary<string, object> { { "FIELDNAME", f } });
                }
                builder.AddTable("FIELDS", fieldList);
            });

            if (!sapResult.Success)
            {
                _logger.LogError("SAP 查詢失敗 ({Name}): {Error}", name, sapResult.ErrorMessage);
                result.Success = false;
                result.Message = $"SAP 查詢失敗: {sapResult.ErrorMessage}";
                return result;
            }

            // 解析 DATA Table
            var soDataList = new List<SOMasterData>();

            if (sapResult.Tables.TryGetValue("DATA", out var dataTable))
            {
                foreach (var row in dataTable)
                {
                    if (row.TryGetValue("WA", out var waObj) && waObj is string waStr)
                    {
                        var values = waStr.Split(';');
                        if (values.Length >= 15)
                        {
                            try
                            {
                                var data = ParseSOMasterData(values);
                                soDataList.Add(data);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "解析資料列失敗: {Row}", waStr);
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("從 SAP 取得 {Name} {Count} 筆資料", name, soDataList.Count);
            result.Data = soDataList;

            // 執行資料同步 (刪除舊資料 + Bulk Insert)
            if (soDataList.Count > 0)
            {
                // 刪除舊資料
                var deleteSql = $"DELETE FROM {targetTable} WHERE InvoiceDate >= @InvoiceDate";
                var deleteCount = await _sqlHelper.ExecuteWithConnectionAsync(
                    deleteSql,
                    new { InvoiceDate = bDate },
                    SAPDSConnectionName);

                result.DeletedCount = deleteCount;
                _logger.LogInformation("刪除 {Name} 舊資料: {Count} 筆", name, deleteCount);

                // Bulk Insert 新資料
                var insertCount = await _sqlHelper.BulkInsertAsync(targetTable, soDataList, SAPDSConnectionName);
                result.InsertedCount = insertCount;
                _logger.LogInformation("新增 {Name} 資料: {Count} 筆", name, insertCount);
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
    /// 解析 SAP 回傳的資料列
    /// </summary>
    private static SOMasterData ParseSOMasterData(string[] values)
    {
        // 處理 FixedPrice 計算 (原值 * 100 / 1000 = * 0.1)
        decimal fixedPrice = 0;
        if (decimal.TryParse(values[9].Trim(), out var tempPrice))
        {
            fixedPrice = (tempPrice * 100) / 1000;
        }

        // 處理 Qty
        decimal qty = 0;
        decimal.TryParse(values[7].Trim(), out qty);

        return new SOMasterData
        {
            SONumber = values[0].Trim(),
            SOItem = values[1].Trim(),
            MaterialCode = values[2].Trim(),
            MaterialDesc = string.Empty,  // 透過後續 UPDATE 填入
            SalesUnit = values[3].Trim() == "BOT" ? "BT" : values[3].Trim(),
            SalesDate = values[4].Trim(),
            Invoice = values[5].Trim(),
            InvoiceDate = values[6].Trim() == "0" ? string.Empty : values[6].Trim(),
            Qty = qty,
            Batch = values[8].Trim(),
            ValidityPeriod = values[12].Trim(),
            FixedPrice = fixedPrice,
            CreditMemo = values[10].Trim(),
            CustomerCode = values[11].Trim(),
            BPMOriginNumber = values[13].Trim(),
            SPNumber = values[14].Trim()
        };
    }

    /// <summary>
    /// 更新 MaterialDesc 欄位
    /// </summary>
    private async Task UpdateMaterialDescAsync()
    {
        var updateSql = @"
            UPDATE S SET S.MaterialDesc = M.MaterialDesc 
            FROM Sales_ZLSOMaster AS S 
            INNER JOIN Sales_MaterialMaster AS M ON S.MaterialCode = M.MaterialCode;
            
            UPDATE S SET S.MaterialDesc = M.MaterialDesc 
            FROM Sales_ArichSOMaster AS S 
            INNER JOIN Sales_MaterialMaster AS M ON S.MaterialCode = M.MaterialCode;";

        try
        {
            var rowsAffected = await _sqlHelper.ExecuteWithConnectionAsync(updateSql, null, SAPDSConnectionName);
            _logger.LogInformation("更新 MaterialDesc 完成，共 {Count} 筆資料受影響", rowsAffected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 MaterialDesc 失敗");
            throw;
        }
    }
}
