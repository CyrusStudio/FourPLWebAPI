namespace FourPLWebAPI.Services.Abstractions;

/// <summary>
/// SO 主檔同步服務介面
/// </summary>
public interface ISOService
{
    /// <summary>
    /// 同步所有 SO 主檔資料 (同時處理 ZL 和 Arich)
    /// </summary>
    /// <param name="startDate">查詢起始日 (YYYYMMDD)，預設為昨天</param>
    /// <returns>同步結果 (包含兩個表的結果)</returns>
    Task<FourPLWebAPI.Models.SOSyncAllResult> SyncAllSOMasterAsync(string? startDate = null);
}
