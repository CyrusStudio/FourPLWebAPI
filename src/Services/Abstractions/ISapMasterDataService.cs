using FourPLWebAPI.Models;

namespace FourPLWebAPI.Services.Abstractions;

/// <summary>
/// SAP 主資料處理服務介面
/// </summary>
public interface ISapMasterDataService
{
    /// <summary>
    /// 從 SAP 下載檔案並分類存放
    /// </summary>
    /// <returns>下載結果</returns>
    Task<SapDownloadResult> DownloadFromSapAsync();

    /// <summary>
    /// 處理所有資料夾下的檔案
    /// </summary>
    /// <returns>各類型的處理結果</returns>
    Task<IEnumerable<FileProcessingResult>> ProcessAllAsync();

    /// <summary>
    /// 處理特定類型的檔案
    /// </summary>
    /// <param name="fileType">檔案類型</param>
    /// <returns>該類型的處理結果</returns>
    Task<FileProcessingResult> ProcessByTypeAsync(string fileType);

    /// <summary>
    /// 處理單一檔案
    /// </summary>
    /// <param name="fileType">檔案類型</param>
    /// <param name="filePath">檔案路徑</param>
    /// <returns>單一檔案處理結果</returns>
    Task<SingleFileResult> ProcessFileAsync(string fileType, string filePath);
}
