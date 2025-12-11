using System.Reflection;

namespace FourPLWebAPI.Infrastructure;

/// <summary>
/// SAP Helper 實作
/// 封裝 SAP NCo 3.1，管理 Destination Configuration 與 Connection Pooling
/// 
/// 注意：此類別依賴 SAP NCo 3.1 DLL (sapnco.dll, sapnco_utils.dll)
/// 需將 DLL 放入 lib 資料夾後才能正常編譯與執行
/// </summary>
public class SapHelper : ISapHelper
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SapHelper> _logger;
    
    // SAP 連線參數
    private readonly string _appServerHost;
    private readonly string _systemNumber;
    private readonly string _systemId;
    private readonly string _user;
    private readonly string _password;
    private readonly string _client;
    private readonly string _language;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="configuration">設定檔</param>
    /// <param name="logger">日誌記錄器</param>
    public SapHelper(IConfiguration configuration, ILogger<SapHelper> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // 從設定檔讀取 SAP 連線參數
        var sapSection = configuration.GetSection("Sap");
        _appServerHost = sapSection["AppServerHost"] ?? throw new InvalidOperationException("未設定 SAP AppServerHost");
        _systemNumber = sapSection["SystemNumber"] ?? "00";
        _systemId = sapSection["SystemId"] ?? throw new InvalidOperationException("未設定 SAP SystemId");
        _user = sapSection["User"] ?? throw new InvalidOperationException("未設定 SAP User");
        _password = sapSection["Password"] ?? throw new InvalidOperationException("未設定 SAP Password");
        _client = sapSection["Client"] ?? "100";
        _language = sapSection["Language"] ?? "ZH";
    }

    /// <inheritdoc />
    public async Task<SapRfcResult> ExecuteRfcAsync(string rfcName, Action<SapRfcInputBuilder> inputBuilder)
    {
        return await Task.Run(() => ExecuteRfcInternal(rfcName, inputBuilder, false));
    }

    /// <inheritdoc />
    public async Task<List<T>> ExecuteRfcAndParseTableAsync<T>(
        string rfcName, 
        string tableName, 
        Action<SapRfcInputBuilder> inputBuilder) where T : new()
    {
        var result = await ExecuteRfcAsync(rfcName, inputBuilder);
        
        if (!result.Success)
        {
            _logger.LogError("SAP RFC 執行失敗: {Error}", result.ErrorMessage);
            return new List<T>();
        }

        if (!result.Tables.TryGetValue(tableName, out var tableData))
        {
            _logger.LogWarning("SAP RFC 結果中找不到 Table: {TableName}", tableName);
            return new List<T>();
        }

        return ParseTableToTypedList<T>(tableData);
    }

    /// <inheritdoc />
    public async Task<SapRfcResult> ExecuteBapiAsync(
        string rfcName, 
        Action<SapRfcInputBuilder> inputBuilder, 
        bool autoCommit = true)
    {
        return await Task.Run(() => ExecuteRfcInternal(rfcName, inputBuilder, autoCommit));
    }

    /// <summary>
    /// 內部執行 RFC 的實作
    /// 
    /// 注意：此方法使用 SAP NCo 3.1 API
    /// 需要 sapnco.dll 與 sapnco_utils.dll
    /// </summary>
    private SapRfcResult ExecuteRfcInternal(
        string rfcName, 
        Action<SapRfcInputBuilder> inputBuilder,
        bool commitOnSuccess)
    {
        _logger.LogInformation("開始執行 SAP RFC: {RfcName}", rfcName);

        try
        {
            // 建構輸入參數
            var builder = new SapRfcInputBuilder();
            inputBuilder(builder);

            // TODO: 實際的 SAP NCo 呼叫邏輯
            // 以下為虛擬碼，需在實際有 SAP NCo DLL 時實作
            /*
            // 1. 取得 Destination
            var destination = RfcDestinationManager.GetDestination("SAP_DESTINATION");
            
            // 2. 取得 RFC Repository
            var repo = destination.Repository;
            
            // 3. 取得 RFC Function
            var function = repo.CreateFunction(rfcName);
            
            // 4. 設定 Import 參數
            foreach (var param in builder.ImportParameters)
            {
                function.SetValue(param.Key, param.Value);
            }
            
            // 5. 設定 Table 參數
            foreach (var table in builder.Tables)
            {
                var rfcTable = function.GetTable(table.Key);
                foreach (var row in table.Value)
                {
                    rfcTable.Append();
                    foreach (var field in row)
                    {
                        rfcTable.SetValue(field.Key, field.Value);
                    }
                }
            }
            
            // 6. 執行 RFC
            function.Invoke(destination);
            
            // 7. 讀取結果
            var result = new SapRfcResult { Success = true };
            
            // 讀取 Export 參數
            // ...
            
            // 讀取 Table 結果
            // ...
            
            // 8. 若需要 Commit
            if (commitOnSuccess)
            {
                var commitFunction = repo.CreateFunction("BAPI_TRANSACTION_COMMIT");
                commitFunction.SetValue("WAIT", "X");
                commitFunction.Invoke(destination);
            }
            
            return result;
            */

            // 暫時回傳錯誤，提示需要 SAP NCo DLL
            throw new InvalidOperationException(
                "SAP NCo 3.1 DLL 尚未安裝。請將 sapnco.dll 與 sapnco_utils.dll 放入 lib 資料夾。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAP RFC 執行失敗: {RfcName}", rfcName);
            return new SapRfcResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 將 Table 資料解析為強型別列表
    /// </summary>
    private List<T> ParseTableToTypedList<T>(List<Dictionary<string, object>> tableData) where T : new()
    {
        var result = new List<T>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var row in tableData)
        {
            var item = new T();
            foreach (var prop in properties)
            {
                // 支援使用 ColumnAttribute 指定對應欄位名稱
                var columnName = prop.Name.ToUpperInvariant();
                
                if (row.TryGetValue(columnName, out var value) && value != null)
                {
                    try
                    {
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        var convertedValue = Convert.ChangeType(value, targetType);
                        prop.SetValue(item, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "無法轉換欄位 {Column} 的值", columnName);
                    }
                }
            }
            result.Add(item);
        }

        return result;
    }
}
