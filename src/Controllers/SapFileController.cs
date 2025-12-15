using FourPLWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FourPLWebAPI.Controllers;

/// <summary>
/// SAP 檔案處理控制器
/// 提供 SAP 主資料檔案的處理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SapFileController : ControllerBase
{
    private readonly ISapFileProcessor _sapFileProcessor;
    private readonly ILogger<SapFileController> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    public SapFileController(
        ISapFileProcessor sapFileProcessor,
        ILogger<SapFileController> logger)
    {
        _sapFileProcessor = sapFileProcessor;
        _logger = logger;
    }

    #region SAP 檔案下載

    /// <summary>
    /// 從 SAP 下載檔案並分類存放
    /// </summary>
    /// <returns>下載結果</returns>
    [HttpPost("download")]
    [ProducesResponseType(typeof(SapDownloadResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadFromSap()
    {
        _logger.LogInformation("API 呼叫: 從 SAP 下載檔案");
        var result = await _sapFileProcessor.DownloadFromSapAsync();
        return Ok(result);
    }

    #endregion

    #region 處理所有類型

    /// <summary>
    /// 處理所有 SAP 檔案類型
    /// </summary>
    /// <returns>各類型處理結果</returns>
    [HttpPost("process/all")]
    [ProducesResponseType(typeof(IEnumerable<FileProcessingResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessAll()
    {
        _logger.LogInformation("API 呼叫: 處理所有 SAP 檔案");
        var results = await _sapFileProcessor.ProcessAllAsync();
        return Ok(results);
    }

    #endregion

    #region 處理指定類型

    /// <summary>
    /// 處理 Customer 類型檔案
    /// </summary>
    /// <returns>處理結果</returns>
    [HttpPost("process/customer")]
    [ProducesResponseType(typeof(FileProcessingResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessCustomer()
    {
        _logger.LogInformation("API 呼叫: 處理 Customer 檔案");
        var result = await _sapFileProcessor.ProcessByTypeAsync("Customer");
        return Ok(result);
    }

    /// <summary>
    /// 處理 Material 類型檔案
    /// </summary>
    /// <returns>處理結果</returns>
    [HttpPost("process/material")]
    [ProducesResponseType(typeof(FileProcessingResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessMaterial()
    {
        _logger.LogInformation("API 呼叫: 處理 Material 檔案");
        var result = await _sapFileProcessor.ProcessByTypeAsync("Material");
        return Ok(result);
    }

    /// <summary>
    /// 處理 Price 類型檔案
    /// </summary>
    /// <returns>處理結果</returns>
    [HttpPost("process/price")]
    [ProducesResponseType(typeof(FileProcessingResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessPrice()
    {
        _logger.LogInformation("API 呼叫: 處理 Price 檔案");
        var result = await _sapFileProcessor.ProcessByTypeAsync("Price");
        return Ok(result);
    }

    /// <summary>
    /// 處理 Sales 類型檔案
    /// </summary>
    /// <returns>處理結果</returns>
    [HttpPost("process/sales")]
    [ProducesResponseType(typeof(FileProcessingResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProcessSales()
    {
        _logger.LogInformation("API 呼叫: 處理 Sales 檔案");
        var result = await _sapFileProcessor.ProcessByTypeAsync("Sales");
        return Ok(result);
    }

    #endregion

    #region 查詢待處理檔案

    /// <summary>
    /// 取得各類型待處理檔案數量
    /// </summary>
    /// <returns>各類型檔案數量</returns>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(PendingFilesInfo), StatusCodes.Status200OK)]
    public IActionResult GetPendingFiles()
    {
        _logger.LogInformation("API 呼叫: 查詢待處理檔案");

        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var section = config.GetSection("DataExchange:SapFileProcessing:FileTypes");

        var info = new PendingFilesInfo
        {
            Customer = GetFileCount(section.GetSection("Customer")["SourcePath"]),
            Material = GetFileCount(section.GetSection("Material")["SourcePath"]),
            Price = GetFileCount(section.GetSection("Price")["SourcePath"]),
            Sales = GetFileCount(section.GetSection("Sales")["SourcePath"])
        };

        return Ok(info);
    }

    private int GetFileCount(string? path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return 0;
        return Directory.GetFiles(path).Length;
    }

    #endregion
}

/// <summary>
/// 待處理檔案資訊
/// </summary>
public class PendingFilesInfo
{
    /// <summary>
    /// Customer 類型待處理數量
    /// </summary>
    public int Customer { get; set; }

    /// <summary>
    /// Material 類型待處理數量
    /// </summary>
    public int Material { get; set; }

    /// <summary>
    /// Price 類型待處理數量
    /// </summary>
    public int Price { get; set; }

    /// <summary>
    /// Sales 類型待處理數量
    /// </summary>
    public int Sales { get; set; }

    /// <summary>
    /// 總待處理數量
    /// </summary>
    public int Total => Customer + Material + Price + Sales;
}
