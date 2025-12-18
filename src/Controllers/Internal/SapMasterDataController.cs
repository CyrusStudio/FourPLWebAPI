using FourPLWebAPI.Models;
using FourPLWebAPI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FourPLWebAPI.Controllers.Internal;

/// <summary>
/// SAP 主資料處理控制器 (內部測試)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SapMasterDataController(
    ISapMasterDataService masterDataService,
    ILogger<SapMasterDataController> logger) : ControllerBase
{
    private readonly ISapMasterDataService _masterDataService = masterDataService;
    private readonly ILogger<SapMasterDataController> _logger = logger;

    // 內部測試專用 API (個別觸發)

    /// <summary>
    /// 下載所有檔案
    /// </summary>
    [HttpPost("download")]
    public async Task<IActionResult> Download() => Ok(await _masterDataService.DownloadFromSapAsync());

    /// <summary>
    /// 處理所有檔案
    /// </summary>
    [HttpPost("process/all")]
    public async Task<IActionResult> ProcessAll() => Ok(await _masterDataService.ProcessAllAsync());
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
