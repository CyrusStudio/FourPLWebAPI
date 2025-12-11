using System.ComponentModel.DataAnnotations;

namespace FourPLWebAPI.Models;

/// <summary>
/// 機櫃匯出請求模型
/// 用於 POST /api/integration/cabinet-export 端點
/// </summary>
public class CabinetExportRequest
{
    /// <summary>
    /// 請求 ID (用於追蹤與日誌)
    /// </summary>
    [Required]
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// 組織代碼
    /// </summary>
    [Required]
    public string OrganizationCode { get; set; } = string.Empty;

    /// <summary>
    /// 機櫃類型篩選 (可選)
    /// </summary>
    public string? CabinetType { get; set; }

    /// <summary>
    /// 資料中心代碼 (可選)
    /// </summary>
    public string? DataCenterCode { get; set; }

    /// <summary>
    /// 起始日期 (yyyyMMdd 格式)
    /// </summary>
    public string? StartDate { get; set; }

    /// <summary>
    /// 結束日期 (yyyyMMdd 格式)
    /// </summary>
    public string? EndDate { get; set; }

    /// <summary>
    /// 是否包含已停用的機櫃
    /// </summary>
    public bool IncludeInactive { get; set; } = false;

    /// <summary>
    /// 自訂 sFTP 上傳路徑 (可選，若未指定則使用預設路徑)
    /// </summary>
    public string? CustomUploadPath { get; set; }
}
