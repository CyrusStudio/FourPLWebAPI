using FourPLWebAPI.Models;
using FourPLWebAPI.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FourPLWebAPI.Controllers.External;

/// <summary>
/// BPM 資料上傳控制器
/// 將 BPM SQL Server To XML 並執行上傳
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
    /// ===== Step 1: 將新資料加入 Queue =====
    /// ===== Step 2: 處理 TWC1D002 訂單（獨立處理）=====
    /// ===== Step 3: 處理 TWC0D003 樣品（獨立處理）=====
    /// ===== Step 4: 處理 TWC0D004 退貨（獨立處理）=====
    /// ===== Step 5: 批次寫入 Export =====
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
