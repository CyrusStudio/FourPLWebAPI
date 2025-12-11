using FourPLWebAPI.Models;
using FourPLWebAPI.Services;
using Hangfire;

namespace FourPLWebAPI.Jobs;

/// <summary>
/// 機櫃匯出排程任務
/// 可透過 App_ScheduleConfig 資料表設定執行時間
/// </summary>
public class CabinetExportJob
{
    private readonly ICabinetExportService _cabinetExportService;
    private readonly ILogger<CabinetExportJob> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    public CabinetExportJob(
        ICabinetExportService cabinetExportService,
        ILogger<CabinetExportJob> logger)
    {
        _cabinetExportService = cabinetExportService;
        _logger = logger;
    }

    /// <summary>
    /// 執行排程任務
    /// </summary>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [Queue("default")]
    public async Task Execute()
    {
        _logger.LogInformation("開始執行機櫃匯出排程任務");

        try
        {
            // 建立請求 (使用排程任務的預設參數)
            var request = new CabinetExportRequest
            {
                RequestId = $"SCHEDULED-{DateTime.UtcNow:yyyyMMddHHmmss}",
                OrganizationCode = "DEFAULT",  // 可從設定檔讀取
                IncludeInactive = false
            };

            var result = await _cabinetExportService.ExportAsync(request);

            if (result.Success)
            {
                _logger.LogInformation(
                    "機櫃匯出排程任務完成. 處理數量: {Count}, 檔案: {File}",
                    result.ProcessedCount,
                    result.XmlFileName);
            }
            else
            {
                _logger.LogWarning(
                    "機櫃匯出排程任務失敗: {Message}",
                    result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "機櫃匯出排程任務發生錯誤");
            throw; // 拋出讓 Hangfire 進行重試
        }
    }
}
