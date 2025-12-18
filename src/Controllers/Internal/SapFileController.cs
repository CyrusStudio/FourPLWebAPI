using FourPLWebAPI.Models;
using FourPLWebAPI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FourPLWebAPI.Controllers.Internal;

/// <summary>
/// SAP 檔案處理控制器
/// 提供 SAP 主資料檔案的處理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SapFileController(
    ISapFileProcessor sapFileProcessor,
    ILogger<SapFileController> logger) : ControllerBase
{
    private readonly ISapFileProcessor _sapFileProcessor = sapFileProcessor;
    private readonly ILogger<SapFileController> _logger = logger;

    #region SAP 檔案下載


    /// <summary>
    /// 從 SAP 下載檔案後處理全部 (供排程呼叫)
    /// </summary>
    /// <returns>下載和處理結果</returns>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(SapExecuteResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadAndProcessAll()
    {
        _logger.LogInformation("API 呼叫: 從 SAP 下載檔案並處理全部");

        var executeResult = new SapExecuteResult
        {
            // 步驟 1：從 SAP 下載檔案
            DownloadResult = await _sapFileProcessor.DownloadFromSapAsync(),

            // 步驟 2：處理所有檔案
            ProcessResults = await _sapFileProcessor.ProcessAllAsync()
        };

        // 計算總結
        var processList = executeResult.ProcessResults.ToList();
        executeResult.TotalFilesProcessed = processList.Sum(r => r.SuccessCount + r.FailCount);
        executeResult.TotalSuccess = processList.Sum(r => r.SuccessCount);
        executeResult.TotalFailed = processList.Sum(r => r.FailCount);

        _logger.LogInformation("SAP 檔案下載並處理完成: 下載 {DownloadCount} 個, 處理成功 {Success} 個, 失敗 {Fail} 個",
            executeResult.DownloadResult?.ProcessedCount ?? 0,
            executeResult.TotalSuccess,
            executeResult.TotalFailed);

        return Ok(executeResult);
    }

    #endregion

}

/// <summary>
/// SAP 執行結果 (下載 + 處理)
/// </summary>
public class SapExecuteResult
{
    /// <summary>
    /// 下載結果
    /// </summary>
    public SapDownloadResult? DownloadResult { get; set; }

    /// <summary>
    /// 處理結果列表
    /// </summary>
    public IEnumerable<FileProcessingResult> ProcessResults { get; set; } = [];

    /// <summary>
    /// 總處理檔案數
    /// </summary>
    public int TotalFilesProcessed { get; set; }

    /// <summary>
    /// 成功處理數
    /// </summary>
    public int TotalSuccess { get; set; }

    /// <summary>
    /// 失敗處理數
    /// </summary>
    public int TotalFailed { get; set; }
}
