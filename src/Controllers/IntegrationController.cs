using FourPLWebAPI.Models;
using FourPLWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FourPLWebAPI.Controllers;

/// <summary>
/// 整合 API Controller
/// 提供 Power Automate 呼叫的整合端點
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class IntegrationController : ControllerBase
{
    private readonly IBillingService _billingService;
    private readonly ISOService _soService;
    private readonly ILogger<IntegrationController> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    public IntegrationController(
        IBillingService billingService,
        ISOService soService,
        ILogger<IntegrationController> logger)
    {
        _billingService = billingService;
        _soService = soService;
        _logger = logger;
    }

    /// <summary>
    /// 查詢 ZT4PL_BILLING 帳單資料
    /// </summary>
    /// <remarks>
    /// 根據業務需求查詢 SAP 帳單資料
    /// 
    /// 範例請求：
    /// 
    ///     POST /api/integration/billing-query
    ///     {
    ///         "vm": "AR",
    ///         "startDate": "20251201"
    ///     }
    /// 
    /// </remarks>
    [HttpPost("billing-query")]
    [ProducesResponseType(typeof(List<BillingData>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<BillingData>>> QueryBilling(
        [FromBody] BillingQueryRequest request)
    {
        _logger.LogInformation("收到帳單查詢請求 - Vm: {Vm}", request.Vm);

        try
        {
            var result = await _billingService.GetBillingDataAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "帳單查詢發生錯誤");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// 同步 SO 主檔資料至 SQL Server (自動處理兩個表)
    /// </summary>
    /// <remarks>
    /// 從 SAP ZT4PL_BILLING 查詢資料，自動同步至以下兩個資料表：
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
