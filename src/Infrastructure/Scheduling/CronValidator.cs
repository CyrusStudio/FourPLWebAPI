using Cronos;

namespace FourPLWebAPI.Infrastructure.Scheduling;

/// <summary>
/// Cron 表達式驗證器
/// 使用 Cronos 函式庫驗證 Cron 格式
/// </summary>
public static class CronValidator
{
    /// <summary>
    /// 驗證 Cron 表達式是否有效
    /// </summary>
    /// <param name="cronExpression">Cron 表達式</param>
    /// <returns>是否有效</returns>
    public static bool IsValid(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return false;
        }

        try
        {
            // 使用 Cronos 解析 Cron 表達式
            // CronFormat.Standard 支援 5 欄位格式 (分 時 日 月 週)
            CronExpression.Parse(cronExpression, CronFormat.Standard);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 驗證 Cron 表達式並取得錯誤訊息
    /// </summary>
    /// <param name="cronExpression">Cron 表達式</param>
    /// <param name="errorMessage">錯誤訊息 (若有效則為 null)</param>
    /// <returns>是否有效</returns>
    public static bool TryValidate(string cronExpression, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            errorMessage = "Cron 表達式不可為空";
            return false;
        }

        try
        {
            CronExpression.Parse(cronExpression, CronFormat.Standard);
            errorMessage = null;
            return true;
        }
        catch (CronFormatException ex)
        {
            errorMessage = $"無效的 Cron 格式: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"Cron 驗證失敗: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// 取得下次執行時間
    /// </summary>
    /// <param name="cronExpression">Cron 表達式</param>
    /// <param name="timeZone">時區 (預設 UTC)</param>
    /// <returns>下次執行時間 (若無效則回傳 null)</returns>
    public static DateTime? GetNextOccurrence(string cronExpression, TimeZoneInfo? timeZone = null)
    {
        try
        {
            var cron = CronExpression.Parse(cronExpression, CronFormat.Standard);
            var tz = timeZone ?? TimeZoneInfo.Utc;
            return cron.GetNextOccurrence(DateTime.UtcNow, tz);
        }
        catch
        {
            return null;
        }
    }
}
