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
    private readonly ILogger<IntegrationController> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    public IntegrationController(
        IBillingService billingService,
        ILogger<IntegrationController> logger)
    {
        _billingService = billingService;
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
    ///         "startDate": "20251101"
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
}
