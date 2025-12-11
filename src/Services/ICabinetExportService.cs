using FourPLWebAPI.Models;

namespace FourPLWebAPI.Services;

/// <summary>
/// 機櫃匯出服務介面
/// 負責編排完整的匯出流程
/// </summary>
public interface ICabinetExportService
{
    /// <summary>
    /// 執行機櫃資料匯出
    /// 流程：SQL 查詢參數 → SAP RFC 呼叫 → XML 轉換 → sFTP 上傳
    /// </summary>
    /// <param name="request">匯出請求</param>
    /// <returns>匯出結果</returns>
    Task<CabinetExportResponse> ExportAsync(CabinetExportRequest request);
}
