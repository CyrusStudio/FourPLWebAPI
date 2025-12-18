namespace FourPLWebAPI.Infrastructure.Abstractions;

/// <summary>
/// 網路磁碟連線管理介面
/// 封裝 Windows 網路磁碟對應功能
/// </summary>
public interface INetworkDiskHelper
{
    /// <summary>
    /// 連接所有設定的網路磁碟
    /// </summary>
    /// <returns>成功連接的磁碟數量</returns>
    Task<int> ConnectAllAsync();

    /// <summary>
    /// 連接單一網路磁碟
    /// </summary>
    /// <param name="remotePath">遠端路徑 (如 \\server\share)</param>
    /// <param name="localDrive">本機磁碟代號 (如 Z:)</param>
    /// <param name="username">使用者名稱</param>
    /// <param name="password">密碼</param>
    /// <returns>是否成功</returns>
    Task<bool> ConnectAsync(string remotePath, string localDrive, string username, string password);

    /// <summary>
    /// 中斷單一網路磁碟連線
    /// </summary>
    /// <param name="localDrive">本機磁碟代號</param>
    /// <returns>是否成功</returns>
    Task<bool> DisconnectAsync(string localDrive);

    /// <summary>
    /// 中斷所有已連接的網路磁碟
    /// </summary>
    /// <returns>成功中斷的磁碟數量</returns>
    Task<int> DisconnectAllAsync();
}
