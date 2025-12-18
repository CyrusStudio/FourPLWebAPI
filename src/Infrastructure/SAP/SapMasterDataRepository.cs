using System.Collections.Concurrent;
using System.Reflection;
using System.Xml.Linq;
using FourPLWebAPI.Models;
using FourPLWebAPI.Infrastructure.Abstractions;

namespace FourPLWebAPI.Infrastructure.SAP;

/// <summary>
/// 通用 SAP 主資料 Repository 實作
/// </summary>
public class SapMasterDataRepository(
    ISqlHelper sqlHelper,
    ILogger<SapMasterDataRepository> logger) : ISapMasterDataRepository
{
    private readonly ISqlHelper _sqlHelper = sqlHelper;
    private readonly ILogger<SapMasterDataRepository> _logger = logger;
    private const string ConnectionName = "SAPDSConnection";

    // 快取：屬性對應 (Key: Type FullName)
    private static readonly ConcurrentDictionary<string, List<PropertyMapping>> PropertyMappingsCache = new();

    // 快取：SapMasterDataAttribute (Key: Type FullName)
    private static readonly ConcurrentDictionary<string, SapMasterDataAttribute> MasterAttributeCache = new();

    /// <inheritdoc />
    public Task<IEnumerable<T>> ReadFromXmlAsync<T>(string filePath) where T : class, new()
    {
        return Task.Run(() =>
        {
            var results = new List<T>();
            var type = typeof(T);
            var typeName = type.FullName ?? type.Name;

            // 取得類別標註 (使用快取)
            var masterAttr = GetCachedMasterAttribute(type, typeName);

            // 取得屬性對應 (使用快取)
            var propertyMappings = GetCachedPropertyMappings(type, typeName);

            try
            {
                _logger.LogDebug("讀取 XML 檔案: {FilePath}, 類型: {TypeName}", filePath, type.Name);
                var doc = XDocument.Load(filePath);

                // 取得所有資料列
                var rows = doc.Descendants(masterAttr.XmlRootElement);
                if (rows.Count() == 0)
                {
                    _logger.LogWarning("XML 中找不到 {Element} 元素", masterAttr.XmlRootElement);
                    return (IEnumerable<T>)results;
                }

                foreach (var row in rows)
                {
                    var item = new T();

                    foreach (var mapping in propertyMappings)
                    {
                        // 跳過不從 XML 讀取的欄位 (保留預設值)
                        if (mapping.SkipXmlRead)
                        {
                            continue;
                        }

                        var value = row.Element(mapping.XmlField)?.Value?.Trim() ?? "";

                        // 處理布林旗標轉換：X → 1，其他 → 0
                        if (mapping.IsBooleanFlag)
                        {
                            value = string.Equals(value, "X", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
                        }

                        // 處理數值欄位：空字串設為 null (讓資料庫接收 DBNull)
                        if (mapping.IsNumeric && string.IsNullOrWhiteSpace(value))
                        {
                            mapping.Property.SetValue(item, null);
                            continue;
                        }

                        mapping.Property.SetValue(item, value);
                    }

                    results.Add(item);
                }

                _logger.LogInformation("讀取 XML 完成: {FilePath}, 共 {Count} 筆", filePath, results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "讀取 XML 檔案失敗: {FilePath}", filePath);
                throw;
            }

            return (IEnumerable<T>)results;
        });
    }

    /// <inheritdoc />
    public async Task<UpsertBatchResult> TruncateAndBulkInsertAsync<T>(IEnumerable<T> data) where T : class, new()
    {
        var dataList = data.ToList();
        var result = new UpsertBatchResult { TotalCount = dataList.Count };

        if (dataList.Count == 0)
        {
            return result;
        }

        var type = typeof(T);
        var typeName = type.FullName ?? type.Name;

        // 取得類別標註和屬性對應 (使用快取)
        var masterAttr = GetCachedMasterAttribute(type, typeName);
        var propertyMappings = GetCachedPropertyMappings(type, typeName);

        // 使用正式 Staging 表 (不是暫存表) 以便跨連線存取
        var stagingTableName = $"{masterAttr.TableName}_Staging_{DateTime.Now:yyyyMMddHHmmss}";

        _logger.LogInformation("開始處理 {TypeName}，共 {Count} 筆", type.Name, dataList.Count);

        try
        {
            // 步驟 1：建立 Staging 表 (複製正式表結構)
            var createStagingSql = $"SELECT TOP 0 * INTO {stagingTableName} FROM {masterAttr.TableName}";
            await _sqlHelper.ExecuteWithConnectionAsync(createStagingSql, null, ConnectionName);
            _logger.LogDebug("已建立 Staging 表: {StagingTable}", stagingTableName);

            // 步驟 2：嘗試 Bulk Insert 到 Staging 表
            bool bulkInsertSuccess = false;
            try
            {
                var insertedCount = await _sqlHelper.BulkInsertAsync(stagingTableName, dataList, ConnectionName);
                bulkInsertSuccess = true;
                _logger.LogDebug("Bulk Insert 到 Staging 表成功，共 {Count} 筆", insertedCount);
            }
            catch (Exception bulkEx)
            {
                _logger.LogWarning(bulkEx, "Bulk Insert 失敗，降級為逐筆處理來找出問題筆");

                // 清空 Staging 表準備逐筆處理
                await _sqlHelper.ExecuteWithConnectionAsync($"TRUNCATE TABLE {stagingTableName}", null, ConnectionName);

                // 逐筆處理找出問題筆
                await ProcessRowByRowAsync(stagingTableName, dataList, masterAttr, result);
            }

            // 步驟 3：如果 Bulk Insert 成功，使用 MERGE 進行全欄位比對
            if (bulkInsertSuccess)
            {
                // 建立 MERGE SQL (主鍵比對 + 欄位差異才 UPDATE)
                var mergeSql = BuildFullColumnMergeSql(masterAttr.TableName, stagingTableName, propertyMappings, masterAttr.PrimaryKeyProperties);
                _logger.LogDebug("執行 MERGE SQL: {Sql}", mergeSql);

                await _sqlHelper.ExecuteWithConnectionAsync(mergeSql, null, ConnectionName);

                result.SuccessCount = dataList.Count;
                _logger.LogInformation("{TypeName} MERGE 處理完成，共 {Count} 筆", type.Name, result.SuccessCount);
            }

            // 步驟 4：清理 Staging 表
            await _sqlHelper.ExecuteWithConnectionAsync($"DROP TABLE {stagingTableName}", null, ConnectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{TypeName} 處理失敗", type.Name);
            result.ErrorSummary = ex.Message;

            if (result.FailedItems.Count == 0)
            {
                result.FailedItems.Add(new FailedItem
                {
                    PrimaryKey = "UNKNOWN",
                    ErrorMessage = ex.Message,
                    RawData = $"共 {dataList.Count} 筆資料"
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 建立 MERGE SQL
    /// ON 條件使用主鍵 (支援複合主鍵)，MATCHED 時比對所有欄位判斷是否需要 UPDATE
    /// </summary>
    private static string BuildFullColumnMergeSql(
        string targetTable,
        string sourceTable,
        List<PropertyMapping> mappings,
        string[] primaryKeyColumns)
    {
        // 所有欄位
        var allColumns = mappings.Select(m => m.DbColumn).ToList();

        // 非 SkipXmlRead 且非主鍵的欄位 (用於比對是否有變更)
        var compareColumns = mappings
            .Where(m => !m.SkipXmlRead && !primaryKeyColumns.Contains(m.DbColumn))
            .Select(m => m.DbColumn)
            .ToList();

        // 非主鍵的欄位 (用於 UPDATE)
        var nonPkColumns = allColumns.Where(c => !primaryKeyColumns.Contains(c)).ToList();

        // 建立 ON 條件 (複合主鍵的所有欄位都要匹配)
        var onConditions = primaryKeyColumns.Select(pk => $"target.{pk} = source.{pk}");
        var onClause = string.Join(" AND ", onConditions);

        // 建立欄位差異比對條件 (有任何一個欄位不同就要 UPDATE)
        // 使用 NULL-safe 比較：NOT (col = col OR (col IS NULL AND col IS NULL))
        var diffConditions = compareColumns.Select(col =>
            $"NOT (target.{col} = source.{col} OR (target.{col} IS NULL AND source.{col} IS NULL))");
        var diffClause = string.Join(" OR ", diffConditions);

        // 建立 UPDATE SET (更新所有非主鍵欄位)
        var updateSets = nonPkColumns.Select(col => $"target.{col} = source.{col}");

        // 建立 INSERT 欄位
        var insertColumns = string.Join(", ", allColumns);
        var insertValues = string.Join(", ", allColumns.Select(col => $"source.{col}"));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"MERGE INTO {targetTable} AS target");
        sb.AppendLine($"USING {sourceTable} AS source");
        sb.AppendLine($"ON {onClause}");

        // WHEN MATCHED AND 有欄位不同: UPDATE 所有非主鍵欄位
        if (compareColumns.Count > 0)
        {
            sb.AppendLine($"WHEN MATCHED AND ({diffClause}) THEN UPDATE SET {string.Join(", ", updateSets)}");
        }

        // WHEN NOT MATCHED: INSERT 新資料
        sb.AppendLine($"WHEN NOT MATCHED BY TARGET THEN INSERT ({insertColumns}) VALUES ({insertValues});");

        return sb.ToString();
    }

    /// <summary>
    /// 逐筆處理資料 (用於 Bulk Insert 失敗時的降級處理)
    /// </summary>
    private async Task ProcessRowByRowAsync<T>(
        string tempTableName,
        List<T> dataList,
        SapMasterDataAttribute masterAttr,
        UpsertBatchResult result) where T : class, new()
    {
        var type = typeof(T);
        var propertyMappings = GetCachedPropertyMappings(type, type.FullName ?? type.Name);

        // 建立 INSERT SQL
        var columns = propertyMappings.Select(m => m.DbColumn).ToList();
        var parameters = propertyMappings.Select(m => $"@{m.DbColumn}").ToList();
        var insertSql = $"INSERT INTO {tempTableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)})";

        foreach (var item in dataList)
        {
            try
            {
                var paramObj = BuildParameters(item, propertyMappings);
                await _sqlHelper.ExecuteWithConnectionAsync(insertSql, paramObj, ConnectionName);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                var pkValue = GetPrimaryKeyValue(item, masterAttr.PrimaryKeyProperty)?.ToString() ?? "Unknown";

                _logger.LogWarning(ex, "{TypeName} {PK} 寫入失敗", type.Name, pkValue);

                result.FailedItems.Add(new FailedItem
                {
                    PrimaryKey = pkValue,
                    ErrorMessage = ex.Message,
                    RawData = System.Text.Json.JsonSerializer.Serialize(item)
                });
            }
        }

        // 如果有成功的資料，使用 MERGE 更新正式表
        if (result.SuccessCount > 0)
        {
            var mergeSql = BuildFullColumnMergeSql(masterAttr.TableName, tempTableName, propertyMappings, masterAttr.PrimaryKeyProperties);
            await _sqlHelper.ExecuteWithConnectionAsync(mergeSql, null, ConnectionName);

            _logger.LogInformation("{TypeName} 逐筆處理完成，成功 {Success} 筆，失敗 {Failed} 筆",
                type.Name, result.SuccessCount, result.FailedCount);
        }

        if (result.FailedItems.Count > 0)
        {
            result.ErrorSummary = $"處理完成，成功 {result.SuccessCount} 筆，失敗 {result.FailedCount} 筆";
        }
    }

    /// <summary>
    /// 建立參數物件
    /// </summary>
    private static object BuildParameters<T>(T item, List<PropertyMapping> mappings) where T : class
    {
        var expando = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;

        foreach (var mapping in mappings)
        {
            var value = mapping.Property.GetValue(item);
            expando[mapping.DbColumn] = value;
        }

        return expando;
    }

    /// <summary>
    /// 取得主鍵值
    /// </summary>
    private static object? GetPrimaryKeyValue<T>(T item, string pkPropertyName) where T : class
    {
        var prop = typeof(T).GetProperty(pkPropertyName);
        return prop?.GetValue(item);
    }

    #region 快取取得方法

    /// <summary>
    /// 取得快取的 SapMasterDataAttribute
    /// </summary>
    private static SapMasterDataAttribute GetCachedMasterAttribute(Type type, string typeName)
    {
        return MasterAttributeCache.GetOrAdd(typeName, _ =>
        {
            var attr = type.GetCustomAttribute<SapMasterDataAttribute>();
            if (attr is null)
            {
                throw new InvalidOperationException($"類型 {type.Name} 缺少 SapMasterDataAttribute 標註");
            }
            return attr;
        });
    }

    /// <summary>
    /// 取得快取的屬性對應清單
    /// </summary>
    private static List<PropertyMapping> GetCachedPropertyMappings(Type type, string typeName)
    {
        return PropertyMappingsCache.GetOrAdd(typeName, _ => BuildPropertyMappings(type));
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 建立屬性對應清單
    /// </summary>
    private static List<PropertyMapping> BuildPropertyMappings(Type type)
    {
        var mappings = new List<PropertyMapping>();

        foreach (var prop in type.GetProperties())
        {
            var xmlAttr = prop.GetCustomAttribute<XmlFieldAttribute>();
            if (xmlAttr != null)
            {
                mappings.Add(new PropertyMapping
                {
                    Property = prop,
                    XmlField = xmlAttr.XmlElementName,
                    DbColumn = prop.Name,
                    IsBooleanFlag = xmlAttr.IsBooleanFlag,
                    SkipXmlRead = xmlAttr.SkipXmlRead,
                    IsNumeric = xmlAttr.IsNumeric,
                    IsDateTime = xmlAttr.IsDateTime
                });
            }
        }

        return mappings;
    }

    #endregion

    /// <summary>
    /// 屬性對應資訊
    /// </summary>
    private class PropertyMapping
    {
        public PropertyInfo Property { get; set; } = null!;
        public string XmlField { get; set; } = "";
        public string DbColumn { get; set; } = "";
        public bool IsBooleanFlag { get; set; }
        public bool SkipXmlRead { get; set; }
        public bool IsNumeric { get; set; }
        public bool IsDateTime { get; set; }
    }
}
