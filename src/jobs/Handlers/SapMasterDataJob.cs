using FourPLWebAPI.Models;
using FourPLWebAPI.Services.Abstractions;
using Hangfire;

namespace FourPLWebAPI.Jobs.Handlers;

/// <summary>
/// SAP 檔案處理排程任務
/// 定期從 SAP 下載檔案並處理
/// </summary>
public class SapMasterDataJob(ISapMasterDataService masterDataService, ILogger<SapMasterDataJob> logger)
{
    private readonly ISapMasterDataService _masterDataService = masterDataService;
    private readonly ILogger<SapMasterDataJob> _logger = logger;

    /// <summary>
    /// 執行 SAP 檔案下載並處理
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 600])]
    public async Task Execute()
    {
        _logger.LogInformation("開始執行 SAP 檔案下載與處理任務 - 時間: {Time}", DateTime.Now);

        try
        {
            var hasError = false;
            var errorMessages = new List<string>();

            // 1. 下載檔案
            _logger.LogInformation("步驟 1/2: 從 SAP 下載檔案");
            var downloadResult = await _masterDataService.DownloadFromSapAsync();

            if (!downloadResult.Success)
            {
                _logger.LogWarning("SAP 檔案下載失敗: {Message}", downloadResult.ErrorMessage);
                hasError = true;
                errorMessages.Add($"下載失敗: {downloadResult.ErrorMessage}");
            }
            else
            {
                _logger.LogInformation("SAP 檔案下載完成，處理 {Count} 個檔案", downloadResult.ProcessedCount);
            }

            // 2. 處理檔案
            _logger.LogInformation("步驟 2/2: 開始處理解析 SAP 檔案並更新資料庫");
            var processResults = await _masterDataService.ProcessAllAsync();
            var enumerable = processResults as FileProcessingResult[] ?? [.. processResults];

            var totalCount = enumerable.Sum(r => r.TotalCount);
            var successCount = enumerable.Sum(r => r.SuccessCount);
            var failCount = enumerable.Sum(r => r.FailCount);

            if (failCount == 0)
            {
                _logger.LogInformation(
                    "SAP 檔案處理任務完成 - 總計: {Total}, 成功: {Success}, 失敗: {Fail}",
                    totalCount, successCount, failCount);
            }
            else
            {
                _logger.LogWarning(
                    "SAP 檔案處理任務完成，但有部分失敗 - 總計: {Total}, 成功: {Success}, 失敗: {Fail}",
                    totalCount, successCount, failCount);
                hasError = true;
                errorMessages.Add($"檔案處理有 {failCount} 個失敗");
            }

            // 如果有任何錯誤，拋出例外讓 Hangfire 知道
            if (hasError)
            {
                throw new InvalidOperationException($"SAP 檔案處理任務未完全成功: {string.Join(", ", errorMessages)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAP 檔案處理排程任務執行失敗");
            throw; // 讓 Hangfire 進行重試
        }
    }
}
