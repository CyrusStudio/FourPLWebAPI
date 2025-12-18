using Renci.SshNet;

using FourPLWebAPI.Infrastructure.Abstractions;

namespace FourPLWebAPI.Infrastructure.Files;

/// <summary>
/// sFTP Helper 實作
/// 封裝 SSH.NET，支援 MemoryStream 上傳
/// </summary>
public class SftpHelper : ISftpHelper
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SftpHelper> _logger;

    // sFTP 連線參數
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string? _privateKeyPath;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="configuration">設定檔</param>
    /// <param name="logger">日誌記錄器</param>
    public SftpHelper(IConfiguration configuration, ILogger<SftpHelper> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // 從設定檔讀取 sFTP 連線參數
        var sftpSection = configuration.GetSection("Sftp");
        _host = sftpSection["Host"] ?? throw new InvalidOperationException("未設定 SFTP Host");
        _port = int.TryParse(sftpSection["Port"], out var port) ? port : 22;
        _username = sftpSection["Username"] ?? throw new InvalidOperationException("未設定 SFTP Username");
        _password = sftpSection["Password"] ?? "";
        _privateKeyPath = sftpSection["PrivateKeyPath"];
    }

    /// <summary>
    /// 建立 SFTP 連線
    /// </summary>
    private SftpClient CreateClient()
    {
        if (!string.IsNullOrEmpty(_privateKeyPath) && File.Exists(_privateKeyPath))
        {
            // 使用私鑰認證
            var keyFile = new PrivateKeyFile(_privateKeyPath);
            var keyFiles = new[] { keyFile };
            var authMethod = new PrivateKeyAuthenticationMethod(_username, keyFiles);
            var connectionInfo = new Renci.SshNet.ConnectionInfo(_host, _port, _username, authMethod);
            return new SftpClient(connectionInfo);
        }
        else
        {
            // 使用密碼認證
            return new SftpClient(_host, _port, _username, _password);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UploadFileAsync(string remotePath, Stream fileStream)
    {
        try
        {
            _logger.LogInformation("開始上傳檔案至 sFTP: {RemotePath}", remotePath);

            using var client = CreateClient();
            await Task.Run(() =>
            {
                client.Connect();

                // 確保目錄存在
                var directory = Path.GetDirectoryName(remotePath)?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(directory))
                {
                    EnsureDirectoryExists(client, directory);
                }

                // 上傳檔案
                client.UploadFile(fileStream, remotePath, true);

                client.Disconnect();
            });

            _logger.LogInformation("檔案上傳成功: {RemotePath}", remotePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "sFTP 上傳失敗: {RemotePath}", remotePath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UploadFileAsync(string localPath, string remotePath)
    {
        if (!File.Exists(localPath))
        {
            _logger.LogError("本機檔案不存在: {LocalPath}", localPath);
            return false;
        }

        await using var fileStream = File.OpenRead(localPath);
        return await UploadFileAsync(remotePath, fileStream);
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string remotePath)
    {
        try
        {
            using var client = CreateClient();
            return await Task.Run(() =>
            {
                client.Connect();
                var exists = client.Exists(remotePath);
                client.Disconnect();
                return exists;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "檢查 sFTP 檔案存在失敗: {RemotePath}", remotePath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CreateDirectoryAsync(string remotePath)
    {
        try
        {
            using var client = CreateClient();
            await Task.Run(() =>
            {
                client.Connect();
                EnsureDirectoryExists(client, remotePath);
                client.Disconnect();
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立 sFTP 目錄失敗: {RemotePath}", remotePath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Stream?> DownloadFileAsync(string remotePath)
    {
        try
        {
            _logger.LogInformation("開始從 sFTP 下載檔案: {RemotePath}", remotePath);

            using var client = CreateClient();
            var memoryStream = new MemoryStream();

            await Task.Run(() =>
            {
                client.Connect();
                client.DownloadFile(remotePath, memoryStream);
                client.Disconnect();
            });

            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "sFTP 下載失敗: {RemotePath}", remotePath);
            return null;
        }
    }

    /// <summary>
    /// 確保目錄存在，若不存在則建立
    /// </summary>
    private void EnsureDirectoryExists(SftpClient client, string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var currentPath = "";

        foreach (var part in parts)
        {
            currentPath = string.IsNullOrEmpty(currentPath) ? $"/{part}" : $"{currentPath}/{part}";

            if (!client.Exists(currentPath))
            {
                _logger.LogDebug("建立 sFTP 目錄: {Path}", currentPath);
                client.CreateDirectory(currentPath);
            }
        }
    }
}
