using FourPLWebAPI.Models;

namespace FourPLWebAPI.Services.Abstractions;

/// <summary>
/// BPM 資料上傳服務介面
/// </summary>
public interface IBpmDataUploadService
{
    /// <summary>
    /// 處理待處理資料 (BPM -> Staging Table)
    /// </summary>
    /// <returns>處理結果</returns>
    Task<DataTransformResult> ProcessPendingAsync();

    /// <summary>
    /// 執行完整上傳流程 (轉換 -> XML -> 上傳)
    /// </summary>
    /// <returns>執行結果</returns>
    Task<BpmUploadExecutionResult> ExecuteFullUploadProcessAsync();
}
