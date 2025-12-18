using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using FourPLWebAPI.Infrastructure.Abstractions;

namespace FourPLWebAPI.Infrastructure.Files;

/// <summary>
/// 網路磁碟連線管理實作
/// 使用 P/Invoke 呼叫 Windows API
/// </summary>
[SupportedOSPlatform("windows")]
public class NetworkDiskHelper : INetworkDiskHelper
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NetworkDiskHelper> _logger;

    // 網路磁碟設定
    private readonly string[] _remotePaths;
    private readonly string[] _localDrives;
    private readonly string _username;
    private readonly string _password;

    #region P/Invoke 宣告

    [DllImport("mpr.dll", EntryPoint = "WNetAddConnection2W", CharSet = CharSet.Unicode)]
    private static extern uint WNetAddConnection2(
        in NETRESOURCE lpNetResource,
        string? lpPassword,
        string? lpUsername,
        uint dwFlags);

    [DllImport("mpr.dll", EntryPoint = "WNetCancelConnection2W", CharSet = CharSet.Unicode)]
    private static extern uint WNetCancelConnection2(
        string lpName,
        uint dwFlags,
        bool fForce);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NETRESOURCE
    {
        public int dwScope;
        public int dwType;
        public int dwDisplayType;
        public int dwUsage;
        public string? LocalName;
        public string? RemoteName;
        public string? Comment;
        public string? Provider;
    }

    // 常數定義
    private const int RESOURCETYPE_DISK = 1;
    private const int RESOURCE_GLOBALNET = 2;
    private const int RESOURCEDISPLAYTYPE_GENERIC = 3;
    private const int RESOURCEUSAGE_CONNECTABLE = 1;

    #endregion

    /// <summary>
    /// 建構函式
    /// </summary>
    public NetworkDiskHelper(IConfiguration configuration, ILogger<NetworkDiskHelper> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // 從設定檔讀取網路磁碟設定
        var section = configuration.GetSection("DataExchange:NetworkDisk");
        var remotePathStr = section["RemotePaths"] ?? "";
        var localDriveStr = section["LocalDrives"] ?? "";

        _remotePaths = remotePathStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
        _localDrives = localDriveStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
        _username = section["Username"] ?? "";
        _password = section["Password"] ?? "";
    }

    /// <inheritdoc />
    public async Task<int> ConnectAllAsync()
    {
        var successCount = 0;

        for (int i = 0; i < _remotePaths.Length && i < _localDrives.Length; i++)
        {
            var result = await ConnectAsync(_remotePaths[i], _localDrives[i], _username, _password);
            if (result) successCount++;
        }

        _logger.LogInformation("網路磁碟連線完成，成功 {SuccessCount}/{TotalCount} 個",
            successCount, Math.Min(_remotePaths.Length, _localDrives.Length));

        return successCount;
    }

    /// <inheritdoc />
    public Task<bool> ConnectAsync(string remotePath, string localDrive, string username, string password)
    {
        return Task.Run(() =>
        {
            try
            {
                _logger.LogDebug("嘗試連接網路磁碟: {RemotePath} -> {LocalDrive}", remotePath, localDrive);

                var resource = new NETRESOURCE
                {
                    dwScope = RESOURCE_GLOBALNET,
                    dwType = RESOURCETYPE_DISK,
                    dwDisplayType = RESOURCEDISPLAYTYPE_GENERIC,
                    dwUsage = RESOURCEUSAGE_CONNECTABLE,
                    LocalName = localDrive,
                    RemoteName = remotePath,
                    Provider = null,
                    Comment = null
                };

                var result = WNetAddConnection2(in resource, password, username, 0);

                if (result == 0)
                {
                    _logger.LogInformation("成功連接網路磁碟: {LocalDrive} -> {RemotePath}", localDrive, remotePath);
                    return true;
                }

                // 如果已連接，嘗試中斷後重連
                if (result == 85) // ERROR_ALREADY_ASSIGNED
                {
                    _logger.LogWarning("磁碟代號 {LocalDrive} 已被使用，嘗試重新連接", localDrive);

                    var disconnectResult = WNetCancelConnection2(localDrive, 0, true);
                    if (disconnectResult == 0)
                    {
                        result = WNetAddConnection2(in resource, password, username, 0);
                        if (result == 0)
                        {
                            _logger.LogInformation("重新連接成功: {LocalDrive} -> {RemotePath}", localDrive, remotePath);
                            return true;
                        }
                    }
                }

                _logger.LogError("連接網路磁碟失敗: {LocalDrive} -> {RemotePath}, 錯誤碼: {ErrorCode}",
                    localDrive, remotePath, result);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "連接網路磁碟時發生例外: {LocalDrive} -> {RemotePath}", localDrive, remotePath);
                return false;
            }
        });
    }

    /// <inheritdoc />
    public Task<bool> DisconnectAsync(string localDrive)
    {
        return Task.Run(() =>
        {
            try
            {
                var result = WNetCancelConnection2(localDrive, 0, true);

                if (result == 0)
                {
                    _logger.LogInformation("成功中斷網路磁碟連線: {LocalDrive}", localDrive);
                    return true;
                }

                _logger.LogWarning("中斷網路磁碟連線失敗: {LocalDrive}, 錯誤碼: {ErrorCode}", localDrive, result);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "中斷網路磁碟連線時發生例外: {LocalDrive}", localDrive);
                return false;
            }
        });
    }

    /// <inheritdoc />
    public async Task<int> DisconnectAllAsync()
    {
        var successCount = 0;

        foreach (var drive in _localDrives)
        {
            if (await DisconnectAsync(drive)) successCount++;
        }

        _logger.LogInformation("網路磁碟中斷連線完成，成功 {SuccessCount}/{TotalCount} 個",
            successCount, _localDrives.Length);

        return successCount;
    }
}
