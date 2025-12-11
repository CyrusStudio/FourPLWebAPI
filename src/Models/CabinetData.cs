namespace FourPLWebAPI.Models;

/// <summary>
/// 機櫃資料模型
/// 對應 SAP RFC 回傳的 Table 資料
/// </summary>
public class CabinetData
{
    /// <summary>
    /// 機櫃 ID
    /// </summary>
    public string Cabinet_Id { get; set; } = string.Empty;

    /// <summary>
    /// 機櫃名稱
    /// </summary>
    public string Cabinet_Name { get; set; } = string.Empty;

    /// <summary>
    /// 所在位置
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// 總容量 (U)
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// 已使用槽位
    /// </summary>
    public int Used_Slots { get; set; }

    /// <summary>
    /// 狀態
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 建立日期 (yyyyMMdd)
    /// </summary>
    public string Created_Date { get; set; } = string.Empty;

    /// <summary>
    /// 機櫃類型
    /// </summary>
    public string? Cabinet_Type { get; set; }

    /// <summary>
    /// 資料中心代碼
    /// </summary>
    public string? DataCenter_Code { get; set; }

    /// <summary>
    /// 負責人
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// 備註
    /// </summary>
    public string? Remarks { get; set; }
}
