using FourPLWebAPI.Models;
using FourPLWebAPI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FourPLWebAPI.Controllers.External;

/// <summary>
/// SAP 主資料整合控制器 (排程專用)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SapMasterDataController(
    ISapMasterDataService masterDataService,
    ILogger<SapMasterDataController> logger) : ControllerBase
{
    private readonly ISapMasterDataService _masterDataService = masterDataService;
    private readonly ILogger<SapMasterDataController> _logger = logger;

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
            DownloadResult = await _masterDataService.DownloadFromSapAsync(),
            ProcessResults = await _masterDataService.ProcessAllAsync()
        };

        var processList = executeResult.ProcessResults.ToList();
        executeResult.TotalFilesProcessed = processList.Sum(r => r.SuccessCount + r.FailCount);
        executeResult.TotalSuccess = processList.Sum(r => r.SuccessCount);
        executeResult.TotalFailed = processList.Sum(r => r.FailCount);

        return Ok(executeResult);
    }
}
