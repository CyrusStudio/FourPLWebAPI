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
        // 讀取資料庫設定 (使用 HangfireConnection)
        var configs = await GetScheduleConfigsAsync();
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
    /// 從資料庫讀取排程設定 (使用 HangfireConnection)
    /// </summary>
    private async Task<IEnumerable<ScheduleConfig>> GetScheduleConfigsAsync()
    {
        var connectionString = _configuration.GetConnectionString("HangfireConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("未設定 HangfireConnection，無法讀取排程設定");
            return Enumerable.Empty<ScheduleConfig>();
        }

        const string sql = @"
            SELECT JobId, CronExpression, JobType, IsActive, Description, LastModified
            FROM App_ScheduleConfig
            WHERE IsActive = 1";

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync();

        return await Dapper.SqlMapper.QueryAsync<ScheduleConfig>(connection, sql);
    }

    /// <summary>
    /// 註冊或更新排程
    /// </summary>
    private void RegisterOrUpdateJob(ScheduleConfig config)
    {
        try
        {
            // 取得環境名稱，開發環境忽略錯過的執行
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

            // 使用 Hangfire 註冊排程，呼叫 JobExecutor 執行 Job
            RecurringJob.AddOrUpdate<FourPLWebAPI.Jobs.Infrastructure.JobExecutor>(
                config.JobId,
                executor => executor.ExecuteAsync(config.JobType),
                config.CronExpression,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Local,
                    // 開發環境忽略錯過的執行，避免啟動時立即補跑
                    MisfireHandling = isDevelopment
                        ? MisfireHandlingMode.Ignorable
                        : MisfireHandlingMode.Relaxed
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
    /// 釋放資源
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
    }
}

