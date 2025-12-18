using FourPLWebAPI.Models;

namespace FourPLWebAPI.Services;

/// <summary>
/// 資料轉換服務介面
/// 將 BPM 表單資料轉換為 SAP 匯出格式
/// </summary>
public interface IDataTransformService
{
    /// <summary>
    /// 批次處理所有待處理資料
    /// </summary>
    /// <returns>處理結果</returns>
    Task<DataTransformResult> ProcessPendingAsync();
}
