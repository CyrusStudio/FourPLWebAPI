namespace FourPLWebAPI.Services.Abstractions;

/// <summary>
/// SAP 檔案服務介面
/// 處理從 SAP 下載檔案以及後續處理
/// </summary>
public interface ISapFileProcessor
{
    #region SAP 檔案下載

    /// <summary>
    /// 從 SAP 下載檔案並分類存放
    /// </summary>
    /// <returns>下載結果</returns>
    Task<SapDownloadResult> DownloadFromSapAsync();

    #endregion

    #region SAP 檔案後處理

    /// <summary>
    /// 處理所有類型的檔案
    /// </summary>
    /// <returns>處理結果清單</returns>
    Task<IEnumerable<FileProcessingResult>> ProcessAllAsync();

    /// <summary>
    /// 處理指定類型的檔案
    /// </summary>
    /// <param name="fileType">檔案類型 (Customer, Material, Price, Sales)</param>
    /// <returns>處理結果</returns>
    Task<FileProcessingResult> ProcessByTypeAsync(string fileType);

    /// <summary>
    /// 處理單一檔案
    /// </summary>
    /// <param name="fileType">檔案類型</param>
    /// <param name="filePath">檔案路徑</param>
    /// <returns>處理結果</returns>
    Task<SingleFileResult> ProcessFileAsync(string fileType, string filePath);

    #endregion
}

/// <summary>
/// 檔案處理結果
/// </summary>
public class FileProcessingResult
{
    /// <summary>
    /// 檔案類型
    /// </summary>
    public string FileType { get; set; } = "";

    /// <summary>
    /// 處理的檔案總數
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 成功數量
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失敗數量
    /// </summary>
    public int FailCount { get; set; }

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
    /// 各檔案處理結果
    /// </summary>
    public List<SingleFileResult> FileResults { get; set; } = [];
}

/// <summary>
/// 單一檔案處理結果
/// </summary>
public class SingleFileResult
{
    /// <summary>
    /// 檔案名稱
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 錯誤訊息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 目標路徑 (成功或失敗資料夾)
    /// </summary>
    public string? DestinationPath { get; set; }
}

/// <summary>
/// SAP 下載結果
/// </summary>
public class SapDownloadResult
{
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
    public List<SapDownloadFileInfo> ProcessedFiles { get; set; } = [];
}

/// <summary>
/// SAP 下載檔案資訊
/// </summary>
public class SapDownloadFileInfo
{
    /// <summary>
    /// 原始檔案名稱
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    /// 檔案類型 (Customer, Material, Price, Sales)
    /// </summary>
    public string FileType { get; set; } = "";

    /// <summary>
    /// 目標路徑
    /// </summary>
    public string DestinationPath { get; set; } = "";
}

