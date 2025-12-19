using System.Reflection;
using FourPLWebAPI.Infrastructure.Abstractions;
using FourPLWebAPI.Infrastructure.SAP;
using FourPLWebAPI.Infrastructure.Persistence;
using FourPLWebAPI.Infrastructure.Files;
using FourPLWebAPI.Infrastructure.Scheduling;
using FourPLWebAPI.Services.Abstractions;
using FourPLWebAPI.Services.Implementations;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Console.Extensions.Serilog;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FourPLWebAPI;

/// <summary>
/// 應用程式進入點
/// </summary>
public class Program
{
    /// <summary>
    /// 主程式進入點
    /// </summary>
    private static readonly string[] HangfireQueues = ["default", "critical"];

    public static void Main(string[] args)
    {
        // 設定 Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Hangfire", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithHangfireContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: @"C:\FourPLLogs\fourplwebapi-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Hangfire()
            .CreateLogger();

        try
        {
            Log.Information("啟動 FourPL Web API");

            var builder = WebApplication.CreateBuilder(args);

            // 使用 Serilog
            builder.Host.UseSerilog();

            // 註冊 Controllers
            builder.Services.AddControllers();

            // 設定 Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                // 1. 外部整合 API
                options.SwaggerDoc("external", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "FourPL Web API - 外部整合",
                    Description = "供 Power Automate 或外部系統呼叫的整合服務",
                    Contact = new OpenApiContact { Name = "FourPL Team", Email = "support@fourpl.com" }
                });

                // 2. 內部管理 API
                options.SwaggerDoc("internal", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "FourPL Web API - 內部管理",
                    Description = "供排程或系統內部管理使用的 API",
                    Contact = new OpenApiContact { Name = "FourPL Team", Email = "support@fourpl.com" }
                });

                // 根據 Namespace 自動過濾 API 歸類
                options.DocInclusionPredicate((docName, apiDesc) =>
                {
                    if (!apiDesc.TryGetMethodInfo(out var methodInfo)) return false;

                    var ns = methodInfo.DeclaringType?.Namespace ?? "";
                    if (docName == "external")
                    {
                        return ns.Contains(".Controllers.External");
                    }
                    if (docName == "internal")
                    {
                        return ns.Contains(".Controllers.Internal");
                    }
                    return true;
                });

                // 載入 XML 註解
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            // 註冊 Infrastructure 服務
            builder.Services.AddScoped<ISqlHelper, SqlHelper>();

            // SAP Helper: 根據設定決定使用 Mock 或實際實作
            var useMockSap = builder.Configuration.GetValue<bool>("UseMockSap");
            if (useMockSap)
            {
                Log.Warning("SAP Mock 模式已啟用 - 使用模擬資料");
                builder.Services.AddScoped<ISapHelper, MockSapHelper>();
            }
            else
            {
                Log.Information("SAP 連線模式 - 使用實際 SAP NCo");
                builder.Services.AddScoped<ISapHelper, SapHelper>();
            }

            // 註冊 Business Services
            builder.Services.AddScoped<ISapSoSyncService, SapSoSyncService>();

            // 註冊 DataExchange 服務 (NetworkDiskHelper 僅支援 Windows)
#pragma warning disable CA1416 // 驗證平台相容性
            builder.Services.AddScoped<INetworkDiskHelper, NetworkDiskHelper>();
#pragma warning restore CA1416
            builder.Services.AddScoped<ISftpConnectionFactory, SftpConnectionFactory>();
            builder.Services.AddScoped<IDataExchangeService, DataExchangeService>();
            builder.Services.AddScoped<ISapMasterDataRepository, SapMasterDataRepository>();
            builder.Services.AddScoped<ISapMasterDataService, SapMasterDataService>();
            builder.Services.AddScoped<IBpmDataUploadService, BpmDataUploadService>();

            // 註冊 Job 服務
            builder.Services.AddScoped<FourPLWebAPI.Jobs.Infrastructure.JobExecutor>();
            builder.Services.AddScoped<FourPLWebAPI.Jobs.Handlers.SapSoSyncJob>();
            builder.Services.AddScoped<FourPLWebAPI.Jobs.Handlers.SapMasterDataJob>();
            builder.Services.AddScoped<FourPLWebAPI.Jobs.Handlers.BpmDataUploadJob>();

            // ===== Hangfire 設定 =====
            var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireConnection")
                ?? builder.Configuration.GetConnectionString("SAPDSConnection");

            if (!string.IsNullOrEmpty(hangfireConnectionString))
            {
                builder.Services.AddHangfire(configuration => configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(hangfireConnectionString, new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true,
                        SchemaName = "HangFire"
                    })
                    .UseConsole());

                // Hangfire Server
                builder.Services.AddHangfireServer(options =>
                {
                    options.ServerName = $"FourPL-{Environment.MachineName}";
                    options.WorkerCount = Environment.ProcessorCount * 2;
                    options.Queues = HangfireQueues;
                });

                // 註冊排程同步服務
                builder.Services.AddHostedService<ScheduleSyncService>();

                Log.Information("Hangfire 已設定完成");
            }
            else
            {
                Log.Warning("未設定 Hangfire 連線字串，排程功能停用");
            }

            var app = builder.Build();

            // 開發環境啟用 Swagger
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/external/swagger.json", "外部整合 API");
                    options.SwaggerEndpoint("/swagger/internal/swagger.json", "內部管理 API");
                    options.RoutePrefix = "swagger";
                });
            }

            // 正式環境也可選擇啟用 Swagger (供測試用)
            if (builder.Configuration.GetValue<bool>("EnableSwaggerInProduction") && !app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            // 加入 Serilog Request Logging
            app.UseSerilogRequestLogging();

            // ===== Hangfire Dashboard =====
            if (!string.IsNullOrEmpty(hangfireConnectionString))
            {
                var dashboardPath = builder.Configuration.GetValue<string>("Hangfire:DashboardPath") ?? "/hangfire";

                app.UseHangfireDashboard(dashboardPath, new DashboardOptions
                {
                    Authorization = [], // 簡化為空集合
                    IgnoreAntiforgeryToken = true,
                    DashboardTitle = "FourPL - 排程管理"
                });

                Log.Information("Hangfire Dashboard 已啟用: {Path}", dashboardPath);
            }

            app.MapControllers();

            Log.Information("FourPL Web API 已啟動");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "應用程式啟動失敗");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

/// <summary>
/// Hangfire Dashboard 授權過濾器
/// 開發環境允許匿名存取
/// </summary>
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    /// <summary>
    /// 授權檢查
    /// </summary>
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // 開發環境允許匿名存取
        // 正式環境建議整合 Windows 驗證或其他認證機制
        return true;
    }
}


