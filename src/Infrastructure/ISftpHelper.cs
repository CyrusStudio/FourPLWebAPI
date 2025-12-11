namespace FourPLWebAPI.Infrastructure;

/// <summary>
/// sFTP 操作的通用介面
/// 封裝 SSH.NET，提供檔案上傳功能
/// </summary>
public interface ISftpHelper
{
    /// <summary>
    /// 上傳檔案至 sFTP 伺服器
    /// </summary>
    /// <param name="remotePath">遠端路徑 (包含檔名)</param>
    /// <param name="fileStream">檔案串流</param>
    /// <returns>上傳成功與否</returns>
    Task<bool> UploadFileAsync(string remotePath, Stream fileStream);

    /// <summary>
    /// 上傳檔案至 sFTP 伺服器 (從本機檔案)
    /// </summary>
    /// <param name="localPath">本機檔案路徑</param>
    /// <param name="remotePath">遠端路徑 (包含檔名)</param>
    /// <returns>上傳成功與否</returns>
    Task<bool> UploadFileAsync(string localPath, string remotePath);

    /// <summary>
    /// 檢查遠端檔案是否存在
    /// </summary>
    /// <param name="remotePath">遠端路徑</param>
    /// <returns>是否存在</returns>
    Task<bool> FileExistsAsync(string remotePath);

    /// <summary>
    /// 建立遠端目錄
    /// </summary>
    /// <param name="remotePath">遠端目錄路徑</param>
    /// <returns>建立成功與否</returns>
    Task<bool> CreateDirectoryAsync(string remotePath);

    /// <summary>
    /// 下載檔案
    /// </summary>
    /// <param name="remotePath">遠端路徑</param>
    /// <returns>檔案串流</returns>
    Task<Stream?> DownloadFileAsync(string remotePath);
}
