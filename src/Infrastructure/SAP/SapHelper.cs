using System.Reflection;
using SAP.Middleware.Connector;

using FourPLWebAPI.Models;
using FourPLWebAPI.Infrastructure.Abstractions;

namespace FourPLWebAPI.Infrastructure.SAP;

/// <summary>
/// SAP Helper 實作
/// 封裝 SAP NCo 3.1，管理 Destination Configuration 與 Connection Pooling
/// </summary>
public class SapHelper : ISapHelper
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SapHelper> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    public SapHelper(IConfiguration configuration, ILogger<SapHelper> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // 取得目標環境 (DEV/QAS/PRD)
        var sapEnv = configuration.GetValue<string>("SapEnvironment") ?? "DEV";
        var sapSection = configuration.GetSection($"Sap:{sapEnv}");

        if (!sapSection.Exists())
        {
            sapSection = configuration.GetSection("Sap");
        }

        _logger.LogInformation("SAP Helper 初始化 - 環境: {Env}, Host: {Host}, SID: {Sid}",
            sapEnv,
            sapSection["AppServerHost"],
            sapSection["SystemId"]);
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
            return [];
        }

        if (!result.Tables.TryGetValue(tableName, out var tableData))
        {
            _logger.LogWarning("SAP RFC 結果中找不到 Table: {TableName}", tableName);
            return [];
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

    private static bool _isDestinationRegistered = false;
    private static readonly object _registrationLock = new();

    /// <summary>
    /// 註冊 Destination Configuration
    /// </summary>
    private void RegisterDestination()
    {
        if (_isDestinationRegistered) return;

        lock (_registrationLock)
        {
            if (_isDestinationRegistered) return;

            var config = new SapDestinationConfig(_configuration);
            RfcDestinationManager.RegisterDestinationConfiguration(config);
            _isDestinationRegistered = true;

            _logger.LogInformation("SAP Destination Configuration 已註冊");
        }
    }

    /// <summary>
    /// 內部執行 RFC 的實作
    /// </summary>
    private SapRfcResult ExecuteRfcInternal(
        string rfcName,
        Action<SapRfcInputBuilder> inputBuilder,
        bool commitOnSuccess)
    {
        _logger.LogInformation("開始執行 SAP RFC: {RfcName}", rfcName);

        try
        {
            // 確保 Destination 已註冊
            RegisterDestination();

            // 取得 Destination
            var destination = RfcDestinationManager.GetDestination("FourPL_SAP");

            // 建立 Function 實例
            IRfcFunction function = destination.Repository.CreateFunction(rfcName);

            // 建構輸入參數
            var builder = new SapRfcInputBuilder();
            inputBuilder(builder);

            // 1. 設定 Import 參數
            foreach (var param in builder.ImportParameters)
            {
                function.SetValue(param.Key, param.Value);
            }

            // 2. 設定 Table 參數
            foreach (var tableEntry in builder.Tables)
            {
                var sapTable = function.GetTable(tableEntry.Key);
                foreach (var rowData in tableEntry.Value)
                {
                    sapTable.Append();
                    foreach (var field in rowData)
                    {
                        sapTable.SetValue(field.Key, field.Value);
                    }
                }
            }

            // 3. 執行 RFC
            function.Invoke(destination);

            // 4. 解析回傳結果
            var result = new SapRfcResult { Success = true };

            // 4.1 讀取 Export 參數
            for (int i = 0; i < function.Metadata.ParameterCount; i++)
            {
                var metadata = function.Metadata[i];
                if (metadata.Direction == RfcDirection.EXPORT || metadata.Direction == RfcDirection.CHANGING)
                {
                    if (metadata.DataType != RfcDataType.TABLE)
                    {
                        result.ExportParameters[metadata.Name] = function.GetValue(metadata.Name);
                    }
                }
            }

            // 4.2 讀取 Table 結果
            for (int i = 0; i < function.Metadata.ParameterCount; i++)
            {
                var metadata = function.Metadata[i];
                if (metadata.DataType == RfcDataType.TABLE)
                {
                    var sapTable = function.GetTable(metadata.Name);
                    var tableList = new List<Dictionary<string, object>>();

                    for (int rowIdx = 0; rowIdx < sapTable.RowCount; rowIdx++)
                    {
                        sapTable.CurrentIndex = rowIdx;
                        var rowDict = new Dictionary<string, object>();

                        for (int colIdx = 0; colIdx < sapTable.ElementCount; colIdx++)
                        {
                            var element = sapTable.GetElementMetadata(colIdx);
                            rowDict[element.Name] = sapTable.GetValue(element.Name);
                        }
                        tableList.Add(rowDict);
                    }
                    result.Tables[metadata.Name] = tableList;
                }
            }

            // 5. 自動 Commit (如果需要)
            if (commitOnSuccess)
            {
                var commitFunc = destination.Repository.CreateFunction("BAPI_TRANSACTION_COMMIT");
                commitFunc.SetValue("WAIT", "X");
                commitFunc.Invoke(destination);
            }

            _logger.LogInformation("SAP RFC 執行成功: {RfcName}", rfcName);
            return result;
        }
        catch (RfcAbapException abapEx)
        {
            _logger.LogError(abapEx, "SAP ABAP 錯誤: {RfcName} - {Key}: {Message}",
                rfcName, abapEx.Key, abapEx.Message);
            return new SapRfcResult
            {
                Success = false,
                ErrorMessage = $"ABAP Error [{abapEx.Key}]: {abapEx.Message}"
            };
        }
        catch (RfcCommunicationException commEx)
        {
            _logger.LogError(commEx, "SAP 通訊錯誤: {RfcName}", rfcName);
            return new SapRfcResult
            {
                Success = false,
                ErrorMessage = $"通訊錯誤: {commEx.Message}"
            };
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
