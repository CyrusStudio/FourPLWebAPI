namespace FourPLWebAPI.Models;

/// <summary>
/// SQL 參數模型
/// 從資料庫查詢的 SAP 呼叫參數
/// </summary>
public class SapParameterData
{
    /// <summary>
    /// 參數 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// RFC 函式名稱
    /// </summary>
    public string RfcName { get; set; } = string.Empty;

    /// <summary>
    /// SAP 系統代碼
    /// </summary>
    public string SapSystemCode { get; set; } = string.Empty;

    /// <summary>
    /// 公司代碼
    /// </summary>
    public string CompanyCode { get; set; } = string.Empty;

    /// <summary>
    /// 工廠代碼
    /// </summary>
    public string? PlantCode { get; set; }

    /// <summary>
    /// 額外參數 (JSON 格式)
    /// </summary>
    public string? AdditionalParams { get; set; }

    /// <summary>
    /// 輸出 Table 名稱
    /// </summary>
    public string OutputTableName { get; set; } = "ET_DATA";

    /// <summary>
    /// sFTP 上傳目錄
    /// </summary>
    public string SftpDirectory { get; set; } = "/export";

    /// <summary>
    /// XML 範本代碼
    /// </summary>
    public string? XmlTemplateCode { get; set; }

    /// <summary>
    /// 是否啟用
    /// </summary>
    public bool IsActive { get; set; } = true;
}
