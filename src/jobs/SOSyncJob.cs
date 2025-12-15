using FourPLWebAPI.Services;
using Hangfire;

namespace FourPLWebAPI.Jobs;

/// <summary>
/// SO 主檔同步排程任務
/// 定期從 SAP 同步 SO 資料至 SQL Server
/// </summary>
public class SOSyncJob
{
    private readonly ISOService _soService;
    private readonly ILogger<SOSyncJob> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    public SOSyncJob(ISOService soService, ILogger<SOSyncJob> logger)
    {
        _soService = soService;
        _logger = logger;
    }

    /// <summary>
    /// 執行同步任務
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 600 })]
    public async Task Execute()
    {
        var currentHour = DateTime.Now.Hour;

        // 僅在 8:00 ~ 23:00 執行 (排程每小時觸發，但可在此做額外檢查)
        if (currentHour < 8 || currentHour > 23)
        {
            _logger.LogInformation("目前時間 {Hour}:00 不在執行區間 (8:00-23:00)，跳過執行", currentHour);
            return;
        }

        _logger.LogInformation("開始執行 SO 同步排程任務 - 時間: {Time}", DateTime.Now);

        try
        {
            // 使用預設日期 (昨天)
            var result = await _soService.SyncAllSOMasterAsync();

            if (result.Success)
            {
                _logger.LogInformation(
                    "SO 同步排程任務完成 - 刪除: {Deleted}, 新增: {Inserted}",
                    result.TotalDeletedCount,
                    result.TotalInsertedCount);
            }
            else
            {
                _logger.LogWarning("SO 同步排程任務部分失敗 - {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SO 同步排程任務執行失敗");
            throw; // 讓 Hangfire 進行重試
        }
    }
}
