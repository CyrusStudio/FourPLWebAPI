using System.Text.Json.Serialization;

namespace FourPLWebAPI.Models;

/// <summary>
/// 帳單查詢請求
/// </summary>
public class BillingQueryRequest
{
    /// <summary>
    /// 業務模組 (例如 "AR")
    /// </summary>
    public string Vm { get; set; } = string.Empty;

    /// <summary>
    /// 查詢起始日 (YYYYMMDD) - 預設為昨天
    /// </summary>
    public string? StartDate { get; set; }

}

/// <summary>
/// 帳單資料 DTO
/// 對應 SAP Table ZT4PL_BILLING 欄位
/// </summary>
public class BillingData
{
    [JsonPropertyName("ARSHPNO")]
    public string Arshpno { get; set; } = string.Empty;

    [JsonPropertyName("ARSSHPIM")]
    public string Arsshpim { get; set; } = string.Empty;

    [JsonPropertyName("MATNR")]
    public string Matnr { get; set; } = string.Empty;

    [JsonPropertyName("VRKME")]
    public string Vrkme { get; set; } = string.Empty;

    [JsonPropertyName("FKDAT")]
    public string Fkdat { get; set; } = string.Empty;

    [JsonPropertyName("INVONO")]
    public string Invono { get; set; } = string.Empty;

    [JsonPropertyName("INVODATE")]
    public string Invodate { get; set; } = string.Empty;

    [JsonPropertyName("KWMENG")]
    public decimal Kwmeng { get; set; }

    [JsonPropertyName("CHARG")]
    public string Charg { get; set; } = string.Empty;

    [JsonPropertyName("KBETR_ZTW2")]
    public decimal KbetrZtw2 { get; set; }

    [JsonPropertyName("ARBLPNO")]
    public string Arblpno { get; set; } = string.Empty;

    [JsonPropertyName("KUNNR")]
    public string Kunnr { get; set; } = string.Empty;

    [JsonPropertyName("VFDAT")]
    public string Vfdat { get; set; } = string.Empty;

    [JsonPropertyName("FORMNO")]
    public string Formno { get; set; } = string.Empty;

    [JsonPropertyName("KUNNR_SH")]
    public string KunnrSh { get; set; } = string.Empty;
}
