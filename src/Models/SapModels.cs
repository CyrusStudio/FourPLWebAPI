using System.Text.Json.Serialization;

namespace FourPLWebAPI.Models;

#region SAP 檔案處理模型 (Master Data)

/// <summary>
/// SAP 檔案下載結果
/// </summary>
public class SapDownloadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ProcessedCount { get; set; }
    public List<SapDownloadFileInfo> ProcessedFiles { get; set; } = [];
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

/// <summary>
/// SAP 下載檔案資訊
/// </summary>
public class SapDownloadFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
}

/// <summary>
/// 各種類型檔案處理結果
/// </summary>
public class FileProcessingResult
{
    public string FileType { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<SingleFileResult> FileResults { get; set; } = [];
}

/// <summary>
/// 單一檔案處理結果
/// </summary>
public class SingleFileResult
{
    public string FileName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DestinationPath { get; set; }
}

/// <summary>
/// SAP 執行總體結果 (下載+處理)
/// </summary>
public class SapExecuteResult
{
    public SapDownloadResult? DownloadResult { get; set; }
    public IEnumerable<FileProcessingResult> ProcessResults { get; set; } = [];
    public int TotalFilesProcessed { get; set; }
    public int TotalSuccess { get; set; }
    public int TotalFailed { get; set; }
}

#endregion

#region SO 同步模型 (SapSoSync)

/// <summary>
/// SO 同步請求
/// </summary>
public class SOSyncRequest
{
    public string? StartDate { get; set; }
}

/// <summary>
/// SO 主檔資料 DTO
/// </summary>
public class SOMasterData
{
    [JsonPropertyName("SONumber")]
    public string SONumber { get; set; } = string.Empty;

    [JsonPropertyName("SOItem")]
    public string SOItem { get; set; } = string.Empty;

    [JsonPropertyName("MaterialCode")]
    public string MaterialCode { get; set; } = string.Empty;

    [JsonPropertyName("MaterialDesc")]
    public string MaterialDesc { get; set; } = string.Empty;

    [JsonPropertyName("SalesUnit")]
    public string SalesUnit { get; set; } = string.Empty;

    [JsonPropertyName("SalesDate")]
    public string SalesDate { get; set; } = string.Empty;

    [JsonPropertyName("Invoice")]
    public string Invoice { get; set; } = string.Empty;

    [JsonPropertyName("InvoiceDate")]
    public string InvoiceDate { get; set; } = string.Empty;

    [JsonPropertyName("Qty")]
    public decimal Qty { get; set; }

    [JsonPropertyName("Batch")]
    public string Batch { get; set; } = string.Empty;

    [JsonPropertyName("ValidityPeriod")]
    public string ValidityPeriod { get; set; } = string.Empty;

    [JsonPropertyName("FixedPrice")]
    public decimal FixedPrice { get; set; }

    [JsonPropertyName("CreditMemo")]
    public string CreditMemo { get; set; } = string.Empty;

    [JsonPropertyName("CustomerCode")]
    public string CustomerCode { get; set; } = string.Empty;

    [JsonPropertyName("BPMOriginNumber")]
    public string BPMOriginNumber { get; set; } = string.Empty;

    [JsonPropertyName("SPNumber")]
    public string SPNumber { get; set; } = string.Empty;
}

/// <summary>
/// SO 同步結果
/// </summary>
public class SOSyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DeletedCount { get; set; }
    public int InsertedCount { get; set; }
    public string TargetTable { get; set; } = string.Empty;
    public List<SOMasterData> Data { get; set; } = [];
}

/// <summary>
/// SO 全部同步結果
/// </summary>
public class SOSyncAllResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalDeletedCount { get; set; }
    public int TotalInsertedCount { get; set; }
    public List<SOSyncResult> Results { get; set; } = [];
}

#endregion

#region BPM 上傳模型 (BpmDataUpload)

/// <summary>
/// BPM 上傳執行完整結果
/// </summary>
public class BpmUploadExecutionResult
{
    public bool Success { get; set; }
    public DataTransformResult? TransformResult { get; set; }
    public List<string> XmlMessages { get; set; } = [];
    public List<DataExchangeResult> UploadResults { get; set; } = [];
}

#endregion

#region 共通結果模型

/// <summary>
/// 檔案交換結果
/// </summary>
public class DataExchangeResult
{
    public string ScenarioName { get; set; } = "";
    public bool Success { get; set; }
    public int ProcessedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public List<string> ProcessedFiles { get; set; } = [];
}

#endregion
