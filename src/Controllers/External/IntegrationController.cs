using FourPLWebAPI.Models;
using FourPLWebAPI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FourPLWebAPI.Controllers.External;

/// <summary>
/// 整合 API Controller
/// SAP RFC To BPM SQL Server
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IntegrationController(ISOService soService, ILogger<IntegrationController> logger) : ControllerBase
{
    private readonly ISOService _soService = soService;
    private readonly ILogger<IntegrationController> _logger = logger;

    /// <summary>
    /// 同步 SO 主檔資料至 SQL Server (自動處理兩個表)
    /// </summary>
    /// <remarks>
    /// 從 SAP RFC ZT4PL_BILLING 查詢資料，自動同步至以下兩個資料表：
    /// - Sales_ArichSOMaster (ORDLA IN ('A', 'L'))
    /// - Sales_ZLSOMaster (ORDLA IN ('Z', 'B'))
    /// 
    /// 範例請求：
    /// 
    ///     POST /api/integration/so-sync
    ///     {
    ///         "startDate": "20251201"  // 可選，預設為昨天
    ///     }
    /// 
    /// 或直接呼叫不帶參數：
    /// 
    ///     POST /api/integration/so-sync
    ///     {}
    /// 
    /// </remarks>
    [HttpPost("so-sync")]
    [ProducesResponseType(typeof(SOSyncAllResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SOSyncAllResult>> SyncSOMaster(
        [FromBody] SOSyncRequest? request = null)
    {
        _logger.LogInformation("收到 SO 同步請求 - StartDate: {StartDate}", request?.StartDate ?? "預設(昨天)");

        try
        {
            var result = await _soService.SyncAllSOMasterAsync(request?.StartDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SO 同步發生錯誤");
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}
