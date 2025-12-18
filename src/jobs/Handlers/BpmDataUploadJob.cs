using FourPLWebAPI.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace FourPLWebAPI.Jobs.Handlers;

/// <summary>
/// BPM 資料上傳任務
/// </summary>
public class BpmDataUploadJob(IBpmDataUploadService uploadService, ILogger<BpmDataUploadJob> logger)
{
    private readonly IBpmDataUploadService _uploadService = uploadService;
    private readonly ILogger<BpmDataUploadJob> _logger = logger;

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("開始執行 BPM 資料上傳任務");
        var result = await _uploadService.ExecuteFullUploadProcessAsync();
        _logger.LogInformation("BPM 資料上傳任務執行完成, Success: {Success}", result.Success);
    }
}
