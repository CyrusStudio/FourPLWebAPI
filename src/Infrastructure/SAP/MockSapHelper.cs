using FourPLWebAPI.Infrastructure.Abstractions;

namespace FourPLWebAPI.Infrastructure.SAP;

/// <summary>
/// SAP Helper 的 Mock 實作
/// 用於本機開發環境，無需實際連線 SAP 即可測試 API 流程
/// 透過 appsettings.json 的 "UseMockSap": true 啟用
/// </summary>
public class MockSapHelper(ILogger<MockSapHelper> logger) : ISapHelper
{
    private readonly ILogger<MockSapHelper> _logger = logger;

    /// <inheritdoc />
    public Task<SapRfcResult> ExecuteRfcAsync(string rfcName, Action<SapRfcInputBuilder> inputBuilder)
    {
        _logger.LogInformation("[MOCK] 模擬執行 SAP RFC: {RfcName}", rfcName);

        // 建構輸入參數 (記錄用)
        var builder = new SapRfcInputBuilder();
        inputBuilder(builder);

        _logger.LogDebug("[MOCK] Import 參數: {Params}",
            string.Join(", ", builder.ImportParameters.Select(p => $"{p.Key}={p.Value}")));

        // 回傳模擬資料
        var result = GenerateMockResult(rfcName);

        return Task.FromResult(result);
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
            return [];
        }

        if (!result.Tables.TryGetValue(tableName, out var tableData))
        {
            _logger.LogWarning("[MOCK] 模擬結果中找不到 Table: {TableName}", tableName);
            return [];
        }

        return ParseTableToTypedList<T>(tableData);
    }

    /// <inheritdoc />
    public Task<SapRfcResult> ExecuteBapiAsync(
        string rfcName,
        Action<SapRfcInputBuilder> inputBuilder,
        bool autoCommit = true)
    {
        _logger.LogInformation("[MOCK] 模擬執行 BAPI: {RfcName} (AutoCommit: {AutoCommit})", rfcName, autoCommit);

        return ExecuteRfcAsync(rfcName, inputBuilder);
    }

    /// <summary>
    /// 根據 RFC 名稱生成模擬資料
    /// </summary>
    private SapRfcResult GenerateMockResult(string rfcName)
    {
        var result = new SapRfcResult { Success = true };

        // 根據不同的 RFC 回傳不同的模擬資料
        switch (rfcName.ToUpperInvariant())
        {
            case "BAPI_MATERIAL_GETLIST":
                result.Tables["MATNRLIST"] = GenerateMockMaterialList();
                break;

            case "RFC_READ_TABLE":
                result.Tables["DATA"] = GenerateMockTableData();
                break;

            default:
                // 預設回傳通用模擬資料
                result.Tables["ET_DATA"] = GenerateMockCabinetData();
                result.ExportParameters["EV_MESSAGE"] = "模擬執行成功";
                result.ExportParameters["EV_RETURN_CODE"] = "0";
                break;
        }

        return result;
    }

    /// <summary>
    /// 生成模擬的機櫃資料
    /// </summary>
    private static List<Dictionary<string, object>> GenerateMockCabinetData()
    {
        return
        [
            new()
            {
                { "CABINET_ID", "CAB001" },
                { "CABINET_NAME", "測試機櫃 A" },
                { "LOCATION", "台北資料中心" },
                { "CAPACITY", 42 },
                { "USED_SLOTS", 28 },
                { "STATUS", "ACTIVE" },
                { "CREATED_DATE", DateTime.Now.AddMonths(-6).ToString("yyyyMMdd") }
            },
            new()
            {
                { "CABINET_ID", "CAB002" },
                { "CABINET_NAME", "測試機櫃 B" },
                { "LOCATION", "高雄資料中心" },
                { "CAPACITY", 42 },
                { "USED_SLOTS", 35 },
                { "STATUS", "ACTIVE" },
                { "CREATED_DATE", DateTime.Now.AddMonths(-3).ToString("yyyyMMdd") }
            },
            new()
            {
                { "CABINET_ID", "CAB003" },
                { "CABINET_NAME", "測試機櫃 C" },
                { "LOCATION", "台中資料中心" },
                { "CAPACITY", 48 },
                { "USED_SLOTS", 12 },
                { "STATUS", "MAINTENANCE" },
                { "CREATED_DATE", DateTime.Now.AddMonths(-1).ToString("yyyyMMdd") }
            }
        ];
    }

    /// <summary>
    /// 生成模擬的物料清單
    /// </summary>
    private static List<Dictionary<string, object>> GenerateMockMaterialList()
    {
        return
        [
            new() { { "MATNR", "MAT001" }, { "MAKTX", "測試物料 1" } },
            new() { { "MATNR", "MAT002" }, { "MAKTX", "測試物料 2" } }
        ];
    }

    /// <summary>
    /// 生成模擬的表格資料
    /// </summary>
    /// <summary>
    /// 生成模擬的表格資料 (對應 ZT4PL_BILLING)
    /// </summary>
    private static List<Dictionary<string, object>> GenerateMockTableData()
    {
        return
        [
            // 回傳 15 個欄位，以分號分隔
            // "ARSHPNO;ARSSHPIM;MATNR;VRKME;FKDAT;INVONO;INVODATE;KWMENG;CHARG;KBETR_ZTW2;ARBLPNO;KUNNR;VFDAT;FORMNO;KUNNR_SH"
            new() { { "WA", "SHP001;ITEM01;MAT-A;PC;20240101;INV-001;20240105;10;BATCH01;1000;BP001;CUST01;20240201;FORM01;SH_CUST01" } },
            new() { { "WA", "SHP002;ITEM02;MAT-B;KG;20240102;INV-002;20240106;20;BATCH02;2000;BP002;CUST02;20240202;FORM02;SH_CUST02" } }
        ];
    }

    /// <summary>
    /// 將 Table 資料解析為強型別列表
    /// </summary>
    private static List<T> ParseTableToTypedList<T>(List<Dictionary<string, object>> tableData) where T : new()
    {
        var result = new List<T>();
        var properties = typeof(T).GetProperties();

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
                    catch
                    {
                        // 忽略轉換錯誤
                    }
                }
            }
            result.Add(item);
        }

        return result;
    }
}
