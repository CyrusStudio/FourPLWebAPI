using FourPLWebAPI.Models;
using FourPLWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FourPLWebAPI.Controllers;

/// <summary>
/// 資料轉換 API
/// 將 BPM 表單資料轉換為 SAP 匯出格式
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DataTransformController : ControllerBase
{
    private readonly IDataTransformService _dataTransformService;
    private readonly ILogger<DataTransformController> _logger;

    public DataTransformController(
        IDataTransformService dataTransformService,
        ILogger<DataTransformController> logger)
    {
        _dataTransformService = dataTransformService;
        _logger = logger;
    }

    /// <summary>
    /// 批次處理待處理資料
    /// </summary>
    /// <returns>處理結果</returns>
    [HttpPost("pending")]
    public async Task<ActionResult<DataTransformResult>> ProcessPending()
    {
        _logger.LogInformation("收到批次處理請求");
        var result = await _dataTransformService.ProcessPendingAsync();

        return result.Success ? Ok(result) : BadRequest(result);
    }
}
