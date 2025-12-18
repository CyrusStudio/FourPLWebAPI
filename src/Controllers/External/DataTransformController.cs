using FourPLWebAPI.Models;
using FourPLWebAPI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FourPLWebAPI.Controllers.External;

/// <summary>
/// 資料轉換 API
/// 將 BPM SQL Server To XML
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DataTransformController(
    IDataTransformService dataTransformService,
    ILogger<DataTransformController> logger) : ControllerBase
{
    private readonly IDataTransformService _dataTransformService = dataTransformService;
    private readonly ILogger<DataTransformController> _logger = logger;

    /// <summary>
    /// ===== Step 1: 將新資料加入 Queue =====
    /// ===== Step 2: 處理 TWC1D002 訂單（獨立處理）=====
    /// ===== Step 3: 處理 TWC0D003 樣品（獨立處理）=====
    /// ===== Step 4: 處理 TWC0D004 退貨（獨立處理）=====
    /// ===== Step 5: 批次寫入 Export =====
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
