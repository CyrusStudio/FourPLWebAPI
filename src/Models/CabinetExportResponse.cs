namespace FourPLWebAPI.Models;

/// <summary>
/// 機櫃匯出回應模型
/// </summary>
public class CabinetExportResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 請求 ID (與請求對應)
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// 訊息 (成功或錯誤訊息)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 處理的機櫃數量
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// 產生的 XML 檔案名稱
    /// </summary>
    public string? XmlFileName { get; set; }

    /// <summary>
    /// sFTP 上傳路徑
    /// </summary>
    public string? UploadedPath { get; set; }

    /// <summary>
    /// 處理開始時間
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 處理結束時間
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 處理耗時 (毫秒)
    /// </summary>
    public long ElapsedMilliseconds => (long)(EndTime - StartTime).TotalMilliseconds;

    /// <summary>
    /// 詳細錯誤資訊 (若有)
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// 建立成功回應
    /// </summary>
    public static CabinetExportResponse CreateSuccess(
        string requestId, 
        int processedCount, 
        string xmlFileName, 
        string uploadedPath,
        DateTime startTime)
    {
        return new CabinetExportResponse
        {
            Success = true,
            RequestId = requestId,
            Message = "機櫃資料匯出成功",
            ProcessedCount = processedCount,
            XmlFileName = xmlFileName,
            UploadedPath = uploadedPath,
            StartTime = startTime,
            EndTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 建立失敗回應
    /// </summary>
    public static CabinetExportResponse CreateFailure(
        string requestId, 
        string errorMessage, 
        DateTime startTime,
        List<string>? errors = null)
    {
        return new CabinetExportResponse
        {
            Success = false,
            RequestId = requestId,
            Message = errorMessage,
            StartTime = startTime,
            EndTime = DateTime.UtcNow,
            Errors = errors
        };
    }
}
