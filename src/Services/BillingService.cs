using FourPLWebAPI.Infrastructure;
using FourPLWebAPI.Models;

namespace FourPLWebAPI.Services;

public interface IBillingService
{
    Task<List<BillingData>> GetBillingDataAsync(BillingQueryRequest request);
}

public class BillingService : IBillingService
{
    private readonly ISapHelper _sapHelper;
    private readonly ILogger<BillingService> _logger;

    public BillingService(ISapHelper sapHelper, ILogger<BillingService> logger)
    {
        _sapHelper = sapHelper;
        _logger = logger;
    }

    public async Task<List<BillingData>> GetBillingDataAsync(BillingQueryRequest request)
    {
        // 1. 決定查詢條件
        string query;
        if (request.Vm == "AR")
        {
            query = "ORDLA IN ( 'A' , 'L' )";
        }
        else
        {
            query = "ORDLA IN ( 'Z' , 'B' )";
        }

        // 2. 日期範圍 (若未提供則預設昨天)
        var bDate = string.IsNullOrWhiteSpace(request.StartDate) 
            ? DateTime.Now.AddDays(-1).ToString("yyyyMMdd")
            : request.StartDate;

        // 確保日期格式正確 (只取數字)
        bDate = new string(bDate.Where(char.IsDigit).ToArray());
        if (bDate.Length != 8)
        {
            bDate = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
        }

        // 3. 定義需要的欄位
        var fields = new[]
        {
            "ARSHPNO", "ARSSHPIM", "MATNR", "VRKME", "FKDAT",
            "INVONO", "INVODATE", "KWMENG", "CHARG", "KBETR_ZTW2",
            "ARBLPNO", "KUNNR", "VFDAT", "FORMNO", "KUNNR_SH"
        };

        // 4. 呼叫 RFC_READ_TABLE
        _logger.LogInformation("查詢 ZT4PL_BILLING, 條件: UPDSTS='S' AND {Query} AND INVODATE>='{Date}'", query, bDate);

        var result = await _sapHelper.ExecuteRfcAsync("RFC_READ_TABLE", builder =>
        {
            builder.SetImport("QUERY_TABLE", "ZT4PL_BILLING");
            builder.SetImport("DELIMITER", ";");

            // OPTIONS Table - 每行 TEXT 欄位最多 72 字元
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

        if (!result.Success)
        {
            _logger.LogError("SAP 查詢失敗: {Error}", result.ErrorMessage);
            throw new Exception($"SAP 查詢失敗: {result.ErrorMessage}");
        }

        // 5. 解析 DATA Table
        // RFC_READ_TABLE 回傳的資料在 DATA 表中，通常只有一個欄位 WA (Work Area)
        if (!result.Tables.ContainsKey("DATA"))
        {
            return new List<BillingData>();
        }

        var dataTable = result.Tables["DATA"];
        var billingList = new List<BillingData>();

        foreach (var row in dataTable)
        {
            // 取得 WA 欄位內容 (即整行字串)
            if (row.TryGetValue("WA", out var waObj) && waObj is string waStr)
            {
                var values = waStr.Split(';');
                if (values.Length >= fields.Length)
                {
                    try 
                    {
                        var data = new BillingData
                        {
                            Arshpno = values[0].Trim(),
                            Arsshpim = values[1].Trim(),
                            Matnr = values[2].Trim(),
                            Vrkme = values[3].Trim(),
                            Fkdat = values[4].Trim(),
                            Invono = values[5].Trim(),
                            Invodate = values[6].Trim(),
                            Kwmeng = decimal.Parse(values[7].Trim()),
                            Charg = values[8].Trim(),
                            KbetrZtw2 = decimal.Parse(values[9].Trim()),
                            Arblpno = values[10].Trim(),
                            Kunnr = values[11].Trim(),
                            Vfdat = values[12].Trim(),
                            Formno = values[13].Trim(),
                            KunnrSh = values[14].Trim()
                        };
                        billingList.Add(data);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "解析資料列失敗: {Row}", waStr);
                    }
                }
            }
        }

        return billingList;
    }
}
