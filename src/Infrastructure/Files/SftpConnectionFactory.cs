using Renci.SshNet;
using FourPLWebAPI.Infrastructure.Abstractions;

namespace FourPLWebAPI.Infrastructure.Files;

/// <summary>
/// 多伺服器 SFTP 連線管理實作
/// 支援從設定檔讀取多組 SFTP 伺服器設定
/// </summary>
public class SftpConnectionFactory(IConfiguration configuration, ILogger<SftpConnectionFactory> logger) : ISftpConnectionFactory
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<SftpConnectionFactory> _logger = logger;

    /// <inheritdoc />
    public SftpClient CreateConnection(string configKey)
    {
        var section = _configuration.GetSection($"DataExchange:SftpTargets:{configKey}");

        var host = section["Host"] ?? throw new InvalidOperationException($"未設定 SFTP Host: {configKey}");
        var port = int.TryParse(section["Port"], out var p) ? p : 22;
        var username = section["Username"] ?? throw new InvalidOperationException($"未設定 SFTP Username: {configKey}");
        var password = section["Password"] ?? "";

        _logger.LogDebug("建立 SFTP 連線: {ConfigKey} @ {Host}:{Port}", configKey, host, port);

        var client = new SftpClient(host, port, username, password);
        client.Connect();

        return client;
    }

    /// <inheritdoc />
    public async Task<bool> UploadFileAsync(string configKey, string localPath, string remotePath)
    {
        try
        {
            if (!File.Exists(localPath))
            {
                _logger.LogError("本機檔案不存在: {LocalPath}", localPath);
                return false;
            }

            using var client = CreateConnection(configKey);

            await Task.Run(() =>
            {
                using var fileStream = File.OpenRead(localPath);
                client.UploadFile(fileStream, remotePath, true);
            });

            _logger.LogInformation("[{ConfigKey}] 上傳成功: {LocalPath} -> {RemotePath}",
                configKey, Path.GetFileName(localPath), remotePath);

            client.Disconnect();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ConfigKey}] 上傳失敗: {LocalPath}", configKey, localPath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<int> UploadDirectoryAsync(string configKey, string sourceDirectory, string targetDirectory)
    {
        var successCount = 0;

        try
        {
            if (!Directory.Exists(sourceDirectory))
            {
                _logger.LogWarning("來源目錄不存在: {SourceDirectory}", sourceDirectory);
                return 0;
            }

            var files = Directory.GetFiles(sourceDirectory);
            if (files.Length == 0)
            {
                _logger.LogDebug("[{ConfigKey}] 來源目錄無檔案: {SourceDirectory}", configKey, sourceDirectory);
                return 0;
            }

            using var client = CreateConnection(configKey);

            await Task.Run(() =>
            {
                foreach (var filePath in files)
                {
                    try
                    {
                        var fileName = Path.GetFileName(filePath);
                        var remotePath = $"{targetDirectory}/{fileName}";

                        using var fileStream = File.OpenRead(filePath);
                        client.UploadFile(fileStream, remotePath, true);

                        _logger.LogInformation("[{ConfigKey}] 上傳成功: {FileName} -> {RemotePath}",
                            configKey, fileName, remotePath);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[{ConfigKey}] 上傳失敗: {FilePath}", configKey, filePath);
                    }
                }
            });

            client.Disconnect();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ConfigKey}] 批次上傳失敗: {SourceDirectory}", configKey, sourceDirectory);
        }

        return successCount;
    }

    /// <inheritdoc />
    public async Task<bool> DownloadFileAsync(string configKey, string remotePath, string localPath)
    {
        try
        {
            // 確保本機目錄存在
            var directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var client = CreateConnection(configKey);

            await Task.Run(() =>
            {
                using var fileStream = File.Create(localPath);
                client.DownloadFile(remotePath, fileStream);
            });

            _logger.LogInformation("[{ConfigKey}] 下載成功: {RemotePath} -> {LocalPath}",
                configKey, remotePath, Path.GetFileName(localPath));

            client.Disconnect();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ConfigKey}] 下載失敗: {RemotePath}", configKey, remotePath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> ListFilesAsync(string configKey, string remotePath)
    {
        try
        {
            using var client = CreateConnection(configKey);

            var files = await Task.Run(() =>
            {
                return client.ListDirectory(remotePath)
                    .Where(f => !f.IsDirectory && f.Name != "." && f.Name != "..")
                    .Select(f => f.Name)
                    .ToList();
            });

            client.Disconnect();

            _logger.LogDebug("[{ConfigKey}] 列出 {Count} 個檔案: {RemotePath}",
                configKey, files.Count, remotePath);

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ConfigKey}] 列出檔案失敗: {RemotePath}", configKey, remotePath);
            return [];
        }
    }
}
