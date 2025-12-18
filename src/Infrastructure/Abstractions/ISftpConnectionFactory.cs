using Renci.SshNet;

namespace FourPLWebAPI.Infrastructure.Abstractions;

/// <summary>
/// 多伺服器 SFTP 連線管理介面
/// 支援連接至不同 SFTP 伺服器
/// </summary>
public interface ISftpConnectionFactory
{
    /// <summary>
    /// 建立 SFTP 連線
    /// </summary>
    /// <param name="configKey">設定檔 Key (如 "ZL", "ARICH")</param>
    /// <returns>已連接的 SftpClient</returns>
    SftpClient CreateConnection(string configKey);

    /// <summary>
    /// 上傳檔案至指定伺服器
    /// </summary>
    /// <param name="configKey">設定檔 Key</param>
    /// <param name="localPath">本機檔案路徑</param>
    /// <param name="remotePath">遠端路徑</param>
    /// <returns>是否成功</returns>
    Task<bool> UploadFileAsync(string configKey, string localPath, string remotePath);

    /// <summary>
    /// 上傳目錄下所有檔案
    /// </summary>
    /// <param name="configKey">設定檔 Key</param>
    /// <param name="sourceDirectory">來源目錄</param>
    /// <param name="targetDirectory">目標遠端目錄</param>
    /// <returns>成功上傳的檔案數</returns>
    Task<int> UploadDirectoryAsync(string configKey, string sourceDirectory, string targetDirectory);

    /// <summary>
    /// 下載檔案
    /// </summary>
    /// <param name="configKey">設定檔 Key</param>
    /// <param name="remotePath">遠端路徑</param>
    /// <param name="localPath">本機檔案路徑</param>
    /// <returns>是否成功</returns>
    Task<bool> DownloadFileAsync(string configKey, string remotePath, string localPath);

    /// <summary>
    /// 列出遠端目錄檔案
    /// </summary>
    /// <param name="configKey">設定檔 Key</param>
    /// <param name="remotePath">遠端目錄路徑</param>
    /// <returns>檔案名稱清單</returns>
    Task<IEnumerable<string>> ListFilesAsync(string configKey, string remotePath);
}
