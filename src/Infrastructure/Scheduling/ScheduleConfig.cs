namespace FourPLWebAPI.Infrastructure.Scheduling;

/// <summary>
/// 排程設定模型
/// 對應資料庫 App_ScheduleConfig 資料表
/// </summary>
public class ScheduleConfig
{
    /// <summary>
    /// 排程 ID (唯一識別碼)
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Cron 表達式
    /// 格式: 分 時 日 月 週
    /// 範例: "0 8 * * *" = 每天早上 8:00
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Job 類型 (完整類別名稱)
    /// 範例: "FourPLWebAPI.Jobs.CabinetExportJob"
    /// </summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// 是否啟用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 描述說明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 最後修改時間
    /// </summary>
    public DateTime LastModified { get; set; }
}
