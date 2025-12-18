using FourPLWebAPI.Models;

namespace FourPLWebAPI.Services.Abstractions;

/// <summary>
/// SO 主檔同步服務介面
/// </summary>
public interface ISapSoSyncService
{
    /// <summary>
    /// 同步所有 SO 主檔資料
    /// </summary>
    /// <param name="startDate">開始日期 (yyyyMMdd)，若為 null 則預設為昨天</param>
    /// <returns>同步結果</returns>
    Task<SOSyncAllResult> SyncAllSOMasterAsync(string? startDate = null);
}
