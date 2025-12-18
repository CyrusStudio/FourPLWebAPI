using FourPLWebAPI.Models;
using FourPLWebAPI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FourPLWebAPI.Controllers.Internal;

/// <summary>
/// 檔案交換控制器
/// 提供手動觸發各種檔案交換場景的 API (網路磁碟、sFTP)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DataExchangeController(
    IDataExchangeService dataExchangeService,
    ILogger<DataExchangeController> logger) : ControllerBase
{
    private readonly IDataExchangeService _dataExchangeService = dataExchangeService;
    private readonly ILogger<DataExchangeController> _logger = logger;

    /// <summary>
    /// 上傳：傳送檔案至 SAP
    /// </summary>
    /// <returns>執行結果</returns>
    [HttpPost("upload/sap")]
    [ProducesResponseType(typeof(DataExchangeResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadToSap()
    {
        _logger.LogInformation("API 呼叫: 上傳檔案至 SAP");
        var result = await _dataExchangeService.UploadToSapAsync();
        return Ok(result);
    }

    /// <summary>
    /// 上傳：傳送檔案至 ZL (透過 sFTP)
    /// </summary>
    /// <returns>執行結果</returns>
    [HttpPost("upload/zl")]
    [ProducesResponseType(typeof(DataExchangeResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadToZL()
    {
        _logger.LogInformation("API 呼叫: 上傳檔案至 ZL");
        var result = await _dataExchangeService.UploadToZLAsync();
        return Ok(result);
    }

    /// <summary>
    /// 上傳：傳送檔案至 ARICH (透過 sFTP)
    /// </summary>
    /// <returns>執行結果</returns>
    [HttpPost("upload/arich")]
    [ProducesResponseType(typeof(DataExchangeResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadToARICH()
    {
        _logger.LogInformation("API 呼叫: 上傳檔案至 ARICH");
        var result = await _dataExchangeService.UploadToARICHAsync();
        return Ok(result);
    }
}
