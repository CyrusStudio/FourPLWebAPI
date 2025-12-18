namespace FourPLWebAPI.Services.Abstractions;

/// <summary>
/// 檔案交換服務介面
/// 定義 sFTP 相關檔案交換場景的操作
/// </summary>
public interface IDataExchangeService
{
    #region 場景二：BPM → SAP (上傳)

    /// <summary>
    /// 上傳檔案至 SAP 資料夾
    /// </summary>
    /// <returns>處理結果</returns>
    Task<DataExchangeResult> UploadToSapAsync();

    #endregion

    #region 場景三：BPM → ZL (上傳)

    /// <summary>
    /// 透過 sFTP 上傳檔案至 ZL
    /// </summary>
    /// <returns>處理結果</returns>
    Task<DataExchangeResult> UploadToZLAsync();

    #endregion

    #region 場景四：BPM → ARICH (上傳)

    /// <summary>
    /// 透過 sFTP 上傳檔案至 ARICH
    /// </summary>
    /// <returns>處理結果</returns>
    Task<DataExchangeResult> UploadToARICHAsync();

    #endregion
}

/// <summary>
/// 檔案交換結果
/// </summary>
public class DataExchangeResult
{
    /// <summary>
    /// 場景名稱
    /// </summary>
    public string ScenarioName { get; set; } = "";

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 處理的檔案數量
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// 錯誤訊息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 開始時間
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 結束時間
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 處理時長
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// 處理的檔案清單
    /// </summary>
    public List<string> ProcessedFiles { get; set; } = [];
}
