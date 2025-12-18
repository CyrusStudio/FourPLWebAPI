using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace FourPLWebAPI.Jobs.Infrastructure;

/// <summary>
/// Job 執行器
/// 透過 DI 動態建立並執行 Job
/// </summary>
public class JobExecutor(IServiceProvider serviceProvider, ILogger<JobExecutor> logger)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<JobExecutor> _logger = logger;
    private static readonly Dictionary<string, string> JobTypeMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "SOSyncJob", "FourPLWebAPI.Jobs.Handlers.SapSoSyncJob" },
        { "SapFileProcessJob", "FourPLWebAPI.Jobs.Handlers.SapMasterDataJob" },
        { "DataTransformJob", "FourPLWebAPI.Jobs.Handlers.BpmDataUploadJob" }
    };

    /// <summary>
    /// 透過類型名稱執行 Job
    /// </summary>
    /// <param name="jobTypeName">完整類型名稱或別名</param>
    public async Task ExecuteAsync(string jobTypeName)
    {
        _logger.LogInformation("開始執行 Job: {JobType}", jobTypeName);

        // 處理由舊資料庫傳來的舊名稱 (相容性映射)
        var mappedTypeName = jobTypeName;
        var simpleName = jobTypeName.Split('.').Last();
        if (JobTypeMapping.TryGetValue(simpleName, out var newFullName))
        {
            _logger.LogInformation("發現舊版 Job 名稱 {OldName}，已自動導向新版 {NewName}", jobTypeName, newFullName);
            mappedTypeName = newFullName;
        }

        // 解析 Job 類型
        var jobType = Type.GetType(mappedTypeName);
        if (jobType == null)
        {
            // 嘗試從目前組件搜尋
            jobType = Assembly.GetExecutingAssembly()
                .GetTypes()
                .FirstOrDefault(t => t.FullName == mappedTypeName || t.Name == mappedTypeName.Split('.').Last() || t.Name == simpleName);

            if (jobType == null)
            {
                throw new InvalidOperationException($"找不到 Job 類型: {jobTypeName} (映射後: {mappedTypeName})");
            }
        }

        // 透過 DI 建立 Job 實例
        using var scope = _serviceProvider.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService(jobType);

        // 取得並呼叫 Execute 方法 (優先找 Execute，備援找 ExecuteAsync)
        var executeMethod = jobType.GetMethod("Execute") ?? jobType.GetMethod("ExecuteAsync");
        if (executeMethod == null)
        {
            throw new InvalidOperationException($"Job 類型 {jobTypeName} 缺少 Execute 或 ExecuteAsync 方法");
        }

        // 執行 Job
        var result = executeMethod.Invoke(job, null);

        // 如果是非同步方法，等待執行完成
        if (result is Task task)
        {
            await task;
        }

        _logger.LogInformation("Job 執行完成: {JobType}", jobTypeName);
    }
}
