namespace FourPLWebAPI.Infrastructure;

/// <summary>
/// SAP RFC 呼叫結果
/// </summary>
public class SapRfcResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 錯誤訊息 (若有)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// RFC 回傳的 Table 資料 (以 Dictionary 形式儲存)
    /// Key: Table 名稱, Value: Table 資料列表
    /// </summary>
    public Dictionary<string, List<Dictionary<string, object>>> Tables { get; set; } = new();

    /// <summary>
    /// RFC 回傳的 Export 參數
    /// </summary>
    public Dictionary<string, object> ExportParameters { get; set; } = new();
}

/// <summary>
/// SAP 連線參數建構器
/// </summary>
public class SapRfcInputBuilder
{
    /// <summary>
    /// Import 參數
    /// </summary>
    public Dictionary<string, object> ImportParameters { get; } = new();

    /// <summary>
    /// Table 參數
    /// </summary>
    public Dictionary<string, List<Dictionary<string, object>>> Tables { get; } = new();

    /// <summary>
    /// 設定 Import 參數
    /// </summary>
    /// <param name="name">參數名稱</param>
    /// <param name="value">參數值</param>
    /// <returns>Builder 本身 (支援鏈式呼叫)</returns>
    public SapRfcInputBuilder SetImport(string name, object value)
    {
        ImportParameters[name] = value;
        return this;
    }

    /// <summary>
    /// 新增 Table 資料
    /// </summary>
    /// <param name="tableName">Table 名稱</param>
    /// <param name="rows">資料列</param>
    /// <returns>Builder 本身 (支援鏈式呼叫)</returns>
    public SapRfcInputBuilder AddTable(string tableName, List<Dictionary<string, object>> rows)
    {
        Tables[tableName] = rows;
        return this;
    }
}

/// <summary>
/// SAP RFC 呼叫的通用介面
/// 封裝 SAP NCo 3.1 的複雜度
/// </summary>
public interface ISapHelper
{
    /// <summary>
    /// 執行 SAP RFC 呼叫
    /// </summary>
    /// <param name="rfcName">RFC 函式名稱</param>
    /// <param name="inputBuilder">輸入參數建構 Lambda</param>
    /// <returns>RFC 執行結果</returns>
    Task<SapRfcResult> ExecuteRfcAsync(string rfcName, Action<SapRfcInputBuilder> inputBuilder);

    /// <summary>
    /// 執行 SAP RFC 呼叫並解析 Table 結果為強型別
    /// </summary>
    /// <typeparam name="T">結果類型</typeparam>
    /// <param name="rfcName">RFC 函式名稱</param>
    /// <param name="tableName">要解析的 Table 名稱</param>
    /// <param name="inputBuilder">輸入參數建構 Lambda</param>
    /// <returns>強型別結果列表</returns>
    Task<List<T>> ExecuteRfcAndParseTableAsync<T>(
        string rfcName, 
        string tableName, 
        Action<SapRfcInputBuilder> inputBuilder) where T : new();

    /// <summary>
    /// 呼叫 BAPI 並自動處理 Commit
    /// </summary>
    /// <param name="rfcName">RFC 函式名稱</param>
    /// <param name="inputBuilder">輸入參數建構 Lambda</param>
    /// <param name="autoCommit">是否自動 Commit (預設 true)</param>
    /// <returns>RFC 執行結果</returns>
    Task<SapRfcResult> ExecuteBapiAsync(
        string rfcName, 
        Action<SapRfcInputBuilder> inputBuilder, 
        bool autoCommit = true);
}
