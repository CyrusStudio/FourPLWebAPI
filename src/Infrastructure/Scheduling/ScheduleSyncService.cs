using Hangfire;

namespace FourPLWebAPI.Infrastructure.Scheduling;

/// <summary>
/// 排程同步服務
/// 定期讀取資料庫設定，同步至 Hangfire RecurringJob
/// </summary>
public class ScheduleSyncService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduleSyncService> _logger;
    private readonly IConfiguration _configuration;
    private Timer? _timer;
    
    // 記錄目前已註冊的排程 (用於比對變更)
    private readonly Dictionary<string, string> _registeredJobs = new();

    /// <summary>
    /// 建構函式
    /// </summary>
    public ScheduleSyncService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduleSyncService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// 服務啟動
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("排程同步服務啟動");

        // 取得同步間隔 (預設 5 分鐘)
        var intervalMinutes = _configuration.GetValue<int>("Hangfire:ScheduleSyncIntervalMinutes", 5);
        
        // 啟動後立即執行一次，之後定期執行
        _timer = new Timer(
            DoSync, 
            null, 
            TimeSpan.Zero, 
            TimeSpan.FromMinutes(intervalMinutes));

        return Task.CompletedTask;
    }

    /// <summary>
    /// 服務停止
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("排程同步服務停止");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 執行同步
    /// </summary>
    private void DoSync(object? state)
    {
        try
        {
            _logger.LogDebug("開始同步排程設定...");
            SyncSchedulesAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // 記錄錯誤但不中斷服務
            _logger.LogError(ex, "排程同步失敗");
        }
    }

    /// <summary>
    /// 同步排程設定
    /// </summary>
    private async Task SyncSchedulesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var sqlHelper = scope.ServiceProvider.GetRequiredService<ISqlHelper>();

        // 讀取資料庫設定
        var configs = await GetScheduleConfigsAsync(sqlHelper);
        var activeJobIds = new HashSet<string>();

        foreach (var config in configs)
        {
            activeJobIds.Add(config.JobId);

            // 驗證 Cron 表達式
            if (!CronValidator.TryValidate(config.CronExpression, out var errorMessage))
            {
                _logger.LogWarning(
                    "跳過無效的排程設定 [{JobId}]: {Error}", 
                    config.JobId, 
                    errorMessage);
                continue;
            }

            // 檢查是否需要更新
            var needUpdate = !_registeredJobs.TryGetValue(config.JobId, out var existingCron)
                || existingCron != config.CronExpression;

            if (needUpdate)
            {
                RegisterOrUpdateJob(config);
                _registeredJobs[config.JobId] = config.CronExpression;
            }
        }

        // 移除已停用或刪除的排程
        var jobsToRemove = _registeredJobs.Keys
            .Where(jobId => !activeJobIds.Contains(jobId))
            .ToList();

        foreach (var jobId in jobsToRemove)
        {
            RecurringJob.RemoveIfExists(jobId);
            _registeredJobs.Remove(jobId);
            _logger.LogInformation("已移除排程: {JobId}", jobId);
        }

        _logger.LogDebug(
            "排程同步完成. 總數: {Total}, 已註冊: {Registered}", 
            configs.Count(), 
            _registeredJobs.Count);
    }

    /// <summary>
    /// 從資料庫讀取排程設定
    /// </summary>
    private async Task<IEnumerable<ScheduleConfig>> GetScheduleConfigsAsync(ISqlHelper sqlHelper)
    {
        const string sql = @"
            SELECT JobId, CronExpression, JobType, IsActive, Description, LastModified
            FROM App_ScheduleConfig
            WHERE IsActive = 1";

        return await sqlHelper.QueryAsync<ScheduleConfig>(sql);
    }

    /// <summary>
    /// 註冊或更新排程
    /// </summary>
    private void RegisterOrUpdateJob(ScheduleConfig config)
    {
        try
        {
            // 根據 JobType 動態建立排程
            // 這裡使用反射取得 Job 類型
            var jobType = Type.GetType(config.JobType);
            
            if (jobType == null)
            {
                _logger.LogWarning(
                    "找不到 Job 類型: {JobType} (排程: {JobId})", 
                    config.JobType, 
                    config.JobId);
                return;
            }

            // 使用 Hangfire 的泛型方法註冊 RecurringJob
            // 呼叫 Execute 方法
            var executeMethod = jobType.GetMethod("Execute");
            if (executeMethod == null)
            {
                _logger.LogWarning(
                    "Job 類型缺少 Execute 方法: {JobType}", 
                    config.JobType);
                return;
            }

            // 使用表達式樹動態建立 Job
            RecurringJob.AddOrUpdate(
                config.JobId,
                () => ExecuteJobByType(config.JobType),
                config.CronExpression,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Local
                });

            _logger.LogInformation(
                "已註冊/更新排程: {JobId} ({Cron}) -> {JobType}", 
                config.JobId, 
                config.CronExpression, 
                config.JobType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "註冊排程失敗: {JobId}", 
                config.JobId);
        }
    }

    /// <summary>
    /// 透過類型名稱執行 Job
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public static void ExecuteJobByType(string jobTypeName)
    {
        // 這個方法會在 Hangfire 執行時被呼叫
        // 實際的 Job 執行邏輯由 JobExecutor 處理
        var jobType = Type.GetType(jobTypeName);
        if (jobType == null)
        {
            throw new InvalidOperationException($"找不到 Job 類型: {jobTypeName}");
        }

        // Job 的實際執行在 Hangfire 環境中透過 DI 處理
        // 這裡只是一個橋接方法
    }

    /// <summary>
    /// 釋放資源
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
    }
}
