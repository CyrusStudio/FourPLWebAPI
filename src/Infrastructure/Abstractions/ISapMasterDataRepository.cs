namespace FourPLWebAPI.Infrastructure.Abstractions;

/// <summary>
/// 通用 SAP 主資料 Repository 介面
/// </summary>
public interface ISapMasterDataRepository
{
    /// <summary>
    /// 從 XML 檔案讀取資料
    /// </summary>
    /// <typeparam name="T">Model 類型 (需標註 SapMasterDataAttribute)</typeparam>
    /// <param name="filePath">XML 檔案路徑</param>
    /// <returns>資料清單</returns>
    Task<IEnumerable<T>> ReadFromXmlAsync<T>(string filePath) where T : class, new();

    /// <summary>
    /// 清空資料表後批次 Insert (適用於全量資料匯入)
    /// </summary>
    /// <typeparam name="T">Model 類型 (需標註 SapMasterDataAttribute)</typeparam>
    /// <param name="data">資料清單</param>
    /// <returns>處理結果</returns>
    Task<UpsertBatchResult> TruncateAndBulkInsertAsync<T>(IEnumerable<T> data) where T : class, new();
}

/// <summary>
/// 批次處理結果
/// </summary>
public class UpsertBatchResult
{
    /// <summary>
    /// 是否全部成功
    /// </summary>
    public bool Success => FailedItems.Count == 0;

    /// <summary>
    /// 成功筆數
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失敗筆數
    /// </summary>
    public int FailedCount => FailedItems.Count;

    /// <summary>
    /// 總筆數
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 失敗項目清單
    /// </summary>
    public List<FailedItem> FailedItems { get; set; } = [];

    /// <summary>
    /// 錯誤摘要訊息
    /// </summary>
    public string? ErrorSummary { get; set; }
}

/// <summary>
/// 失敗項目資訊
/// </summary>
public class FailedItem
{
    /// <summary>
    /// 主鍵值
    /// </summary>
    public string PrimaryKey { get; set; } = "";

    /// <summary>
    /// 錯誤訊息
    /// </summary>
    public string ErrorMessage { get; set; } = "";

    /// <summary>
    /// 原始資料 (JSON 格式)
    /// </summary>
    public string? RawData { get; set; }
}
