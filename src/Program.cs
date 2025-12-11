using System.Reflection;
using FourPLWebAPI.Infrastructure;
using FourPLWebAPI.Infrastructure.Scheduling;
using FourPLWebAPI.Jobs;
using FourPLWebAPI.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.OpenApi.Models;
using Serilog;

namespace FourPLWebAPI;

/// <summary>
/// 應用程式進入點
/// </summary>
public class Program
{
    /// <summary>
    /// 主程式進入點
    /// </summary>
    public static void Main(string[] args)
    {
        // 設定 Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Hangfire", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/fourplwebapi-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
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
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "FourPL Web API",
                    Description = "SAP 整合中間件 API - 供 Power Automate 呼叫的整合服務，支援 Hangfire 背景排程",
                    Contact = new OpenApiContact
                    {
                        Name = "FourPL Team",
                        Email = "support@fourpl.com"
                    }
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
            builder.Services.AddScoped<ISftpHelper, SftpHelper>();

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
            builder.Services.AddScoped<ICabinetExportService, CabinetExportService>();

            // 註冊 Jobs
            builder.Services.AddScoped<CabinetExportJob>();

            // ===== Hangfire 設定 =====
            var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireConnection")
                ?? builder.Configuration.GetConnectionString("DefaultConnection");

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
                    }));

                // Hangfire Server
                builder.Services.AddHangfireServer(options =>
                {
                    options.ServerName = $"FourPL-{Environment.MachineName}";
                    options.WorkerCount = Environment.ProcessorCount * 2;
                    options.Queues = new[] { "default", "critical" };
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
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "FourPL Web API v1");
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
                    // 開發環境允許匿名存取，正式環境應設定授權
                    Authorization = app.Environment.IsDevelopment()
                        ? new[] { new HangfireAuthorizationFilter() }
                        : new[] { new HangfireAuthorizationFilter() },
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


