using FourPLWebAPI.Services;
using Hangfire;

namespace FourPLWebAPI.Jobs;

/// <summary>
/// SAP 檔案處理排程任務
/// 定期從 SAP 下載檔案並處理
/// </summary>
public class SapFileProcessJob
{
    private readonly ISapFileProcessor _sapFileProcessor;
    private readonly ILogger<SapFileProcessJob> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    public SapFileProcessJob(ISapFileProcessor sapFileProcessor, ILogger<SapFileProcessJob> logger)
    {
        _sapFileProcessor = sapFileProcessor;
        _logger = logger;
    }

    /// <summary>
    /// 執行 SAP 檔案下載並處理
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 600 })]
    public async Task Execute()
    {
        _logger.LogInformation("開始執行 SAP 檔案處理排程任務 - 時間: {Time}", DateTime.Now);

        try
        {
            // 步驟 1：從 SAP 下載檔案
            _logger.LogInformation("步驟 1: 從 SAP 下載檔案");
            var downloadResult = await _sapFileProcessor.DownloadFromSapAsync();

            if (!downloadResult.Success)
            {
                _logger.LogWarning("SAP 檔案下載失敗: {Message}", downloadResult.ErrorMessage);
                // 下載失敗仍繼續處理已存在的檔案
            }
            else
            {
                _logger.LogInformation("SAP 檔案下載完成 - 下載 {Count} 個檔案", downloadResult.ProcessedCount);
            }

            // 步驟 2：處理所有檔案
            _logger.LogInformation("步驟 2: 處理所有 SAP 檔案");
            var processResults = await _sapFileProcessor.ProcessAllAsync();

            var resultsList = processResults.ToList();
            var totalSuccess = resultsList.Sum(r => r.SuccessCount);
            var totalFailed = resultsList.Sum(r => r.FailCount);

            _logger.LogInformation(
                "SAP 檔案處理排程任務完成 - 成功: {Success}, 失敗: {Failed}",
                totalSuccess,
                totalFailed);

            // 記錄各類型詳細結果
            foreach (var result in resultsList)
            {
                _logger.LogInformation(
                    "類型 {Type}: 處理 {Total} 個, 成功 {Success}, 失敗 {Fail}",
                    result.FileType,
                    result.TotalCount,
                    result.SuccessCount,
                    result.FailCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAP 檔案處理排程任務執行失敗");
            throw; // 讓 Hangfire 進行重試
        }
    }
}
