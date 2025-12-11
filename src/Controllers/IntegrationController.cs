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
    private readonly ICabinetExportService _cabinetExportService;
    private readonly ILogger<IntegrationController> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    public IntegrationController(
        ICabinetExportService cabinetExportService, 
        ILogger<IntegrationController> logger)
    {
        _cabinetExportService = cabinetExportService;
        _logger = logger;
    }

    /// <summary>
    /// 機櫃資料匯出
    /// </summary>
    /// <remarks>
    /// 執行完整的機櫃資料匯出流程：
    /// 1. 從 SQL Server 查詢 SAP 呼叫參數
    /// 2. 呼叫 SAP RFC 取得機櫃資料
    /// 3. 轉換為 XML 格式
    /// 4. 上傳至 sFTP 伺服器
    /// 
    /// 範例請求：
    /// 
    ///     POST /api/integration/cabinet-export
    ///     {
    ///         "requestId": "REQ-2024-001",
    ///         "organizationCode": "ORG001",
    ///         "cabinetType": "RACK",
    ///         "dataCenterCode": "DC-TPE",
    ///         "startDate": "20240101",
    ///         "endDate": "20241231",
    ///         "includeInactive": false
    ///     }
    /// 
    /// </remarks>
    /// <param name="request">匯出請求參數</param>
    /// <returns>匯出結果</returns>
    /// <response code="200">匯出成功</response>
    /// <response code="400">請求參數錯誤</response>
    /// <response code="500">處理過程發生錯誤</response>
    [HttpPost("cabinet-export")]
    [ProducesResponseType(typeof(CabinetExportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CabinetExportResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(CabinetExportResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CabinetExportResponse>> ExportCabinets(
        [FromBody] CabinetExportRequest request)
    {
        _logger.LogInformation(
            "收到機櫃匯出請求 - RequestId: {RequestId}, OrgCode: {OrgCode}",
            request.RequestId,
            request.OrganizationCode);

        try
        {
            var result = await _cabinetExportService.ExportAsync(request);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "機櫃匯出發生未預期錯誤");
            
            return StatusCode(500, CabinetExportResponse.CreateFailure(
                request.RequestId,
                "伺服器內部錯誤",
                DateTime.UtcNow,
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 健康檢查端點
    /// </summary>
    /// <returns>健康狀態</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthCheckResponse> HealthCheck()
    {
        return Ok(new HealthCheckResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}

/// <summary>
/// 健康檢查回應模型
/// </summary>
public class HealthCheckResponse
{
    /// <summary>
    /// 狀態
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 時間戳記
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// API 版本
    /// </summary>
    public string Version { get; set; } = string.Empty;
}
