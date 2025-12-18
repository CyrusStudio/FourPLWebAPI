using FourPLWebAPI.Models;
using FourPLWebAPI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FourPLWebAPI.Controllers.External;

/// <summary>
/// BPM 資料上傳控制器
/// 將 BPM SQL Server資料整理後 To XML 並執行上傳到 SAP ZP
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BpmDataUploadController(
    IBpmDataUploadService uploadService,
    ILogger<BpmDataUploadController> logger) : ControllerBase
{
    private readonly IBpmDataUploadService _uploadService = uploadService;
    private readonly ILogger<BpmDataUploadController> _logger = logger;

    /// <summary>
    /// 將 BPM SQL Server資料整理後 To XML 並執行上傳到 SAP ZP (測試環境存本地)
    /// </summary>
    /// <returns>處理結果</returns>
    [HttpPost("execute")]
    public async Task<ActionResult<BpmUploadExecutionResult>> ExecuteFullProcess()
    {
        _logger.LogInformation("收到完整上傳流程請求");
        var result = await _uploadService.ExecuteFullUploadProcessAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
