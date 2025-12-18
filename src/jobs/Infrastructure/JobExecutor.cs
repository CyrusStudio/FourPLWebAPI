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

    /// <summary>
    /// 透過類型名稱執行 Job
    /// </summary>
    /// <param name="jobTypeName">完整類型名稱</param>
    public async Task ExecuteAsync(string jobTypeName)
    {
        _logger.LogInformation("開始執行 Job: {JobType}", jobTypeName);

        // 解析 Job 類型
        var jobType = Type.GetType(jobTypeName);
        if (jobType == null)
        {
            // 嘗試從目前組件搜尋
            jobType = Assembly.GetExecutingAssembly()
                .GetTypes()
                .FirstOrDefault(t => t.FullName == jobTypeName || t.Name == jobTypeName.Split('.').Last());

            if (jobType == null)
            {
                throw new InvalidOperationException($"找不到 Job 類型: {jobTypeName}");
            }
        }

        // 透過 DI 建立 Job 實例
        using var scope = _serviceProvider.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService(jobType);

        // 取得並呼叫 Execute 方法
        var executeMethod = jobType.GetMethod("Execute");
        if (executeMethod == null)
        {
            throw new InvalidOperationException($"Job 類型 {jobTypeName} 缺少 Execute 方法");
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
