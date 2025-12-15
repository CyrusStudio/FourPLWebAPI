using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace FourPLWebAPI.Infrastructure;

/// <summary>
/// 通用 SAP 主資料 Repository 介面
/// </summary>
public interface ISapMasterDataRepository
{
    /// <summary>
    /// 從 XML 檔案讀取資料
    /// </summary>
    /// <typeparam name="T">Model 類型 (需標註 SapMasterDataAttribute)</typeparam>
    /// <param name="filePath">XML 檔案路徑</param>
    /// <returns>資料清單</returns>
    Task<IEnumerable<T>> ReadFromXmlAsync<T>(string filePath) where T : class, new();

    /// <summary>
    /// 批次 UPSERT 資料至資料庫
    /// </summary>
    /// <typeparam name="T">Model 類型 (需標註 SapMasterDataAttribute)</typeparam>
    /// <param name="data">資料清單</param>
    /// <returns>處理筆數</returns>
    Task<int> UpsertBatchAsync<T>(IEnumerable<T> data) where T : class, new();
}

/// <summary>
/// 通用 SAP 主資料 Repository 實作
/// 使用快取優化反射效能
/// </summary>
public class SapMasterDataRepository : ISapMasterDataRepository
{
    private readonly ISqlHelper _sqlHelper;
    private readonly ILogger<SapMasterDataRepository> _logger;
    private const string ConnectionName = "SAPDSConnection";

    // 快取：屬性對應 (Key: Type FullName)
    private static readonly ConcurrentDictionary<string, List<PropertyMapping>> PropertyMappingsCache = new();

    // 快取：MERGE SQL (Key: Type FullName)
    private static readonly ConcurrentDictionary<string, string> MergeSqlCache = new();

    // 快取：SapMasterDataAttribute (Key: Type FullName)
    private static readonly ConcurrentDictionary<string, SapMasterDataAttribute> MasterAttributeCache = new();

    /// <summary>
    /// 建構函式
    /// </summary>
    public SapMasterDataRepository(
        ISqlHelper sqlHelper,
        ILogger<SapMasterDataRepository> logger)
    {
        _sqlHelper = sqlHelper;
        _logger = logger;
    }

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
                if (!rows.Any())
                {
                    _logger.LogWarning("XML 中找不到 {Element} 元素", masterAttr.XmlRootElement);
                    return (IEnumerable<T>)results;
                }

                foreach (var row in rows)
                {
                    var item = new T();

                    foreach (var mapping in propertyMappings)
                    {
                        var value = row.Element(mapping.XmlField)?.Value?.Trim() ?? "";
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
    public async Task<int> UpsertBatchAsync<T>(IEnumerable<T> data) where T : class, new()
    {
        var count = 0;
        var type = typeof(T);
        var typeName = type.FullName ?? type.Name;

        // 取得類別標註 (使用快取)
        var masterAttr = GetCachedMasterAttribute(type, typeName);

        // 取得屬性對應 (使用快取)
        var propertyMappings = GetCachedPropertyMappings(type, typeName);

        // 取得 MERGE SQL (使用快取)
        var sql = GetCachedMergeSql(typeName, masterAttr, propertyMappings);

        foreach (var item in data)
        {
            try
            {
                // 建立參數物件
                var parameters = BuildParameters(item, propertyMappings);

                await _sqlHelper.ExecuteWithConnectionAsync(sql, parameters, ConnectionName);
                count++;
            }
            catch (Exception ex)
            {
                var pkValue = GetPrimaryKeyValue(item, masterAttr.PrimaryKeyProperty);
                _logger.LogError(ex, "{TypeName} {PK} 寫入失敗", type.Name, pkValue);
                throw;
            }
        }

        _logger.LogInformation("{TypeName} 批次寫入完成，共 {Count} 筆", type.Name, count);
        return count;
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
            if (attr == null)
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

    /// <summary>
    /// 取得快取的 MERGE SQL
    /// </summary>
    private static string GetCachedMergeSql(string typeName, SapMasterDataAttribute attr, List<PropertyMapping> mappings)
    {
        return MergeSqlCache.GetOrAdd(typeName, _ => BuildMergeSql(attr, mappings));
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
                    DbColumn = prop.Name
                });
            }
        }

        return mappings;
    }

    /// <summary>
    /// 建立 MERGE SQL 語句
    /// </summary>
    private static string BuildMergeSql(SapMasterDataAttribute attr, List<PropertyMapping> mappings)
    {
        var columns = mappings.Select(m => m.DbColumn).ToList();
        var parameters = mappings.Select(m => $"@{m.DbColumn}").ToList();
        var updateSets = mappings
            .Where(m => m.DbColumn != attr.PrimaryKeyProperty)
            .Select(m => $"{m.DbColumn} = @{m.DbColumn}");

        var sb = new StringBuilder();
        sb.AppendLine($"MERGE INTO {attr.TableName} AS target");
        sb.AppendLine($"USING (SELECT @{attr.PrimaryKeyProperty} AS {attr.PrimaryKeyProperty}) AS source");
        sb.AppendLine($"ON target.{attr.PrimaryKeyProperty} = source.{attr.PrimaryKeyProperty}");
        sb.AppendLine("WHEN MATCHED THEN");
        sb.AppendLine($"    UPDATE SET {string.Join(", ", updateSets)}");
        sb.AppendLine("WHEN NOT MATCHED THEN");
        sb.AppendLine($"    INSERT ({string.Join(", ", columns)})");
        sb.AppendLine($"    VALUES ({string.Join(", ", parameters)});");

        return sb.ToString();
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
    /// 取得主索引值
    /// </summary>
    private static object? GetPrimaryKeyValue<T>(T item, string pkPropertyName) where T : class
    {
        var prop = typeof(T).GetProperty(pkPropertyName);
        return prop?.GetValue(item);
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
    }
}
