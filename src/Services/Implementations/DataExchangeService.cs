using FourPLWebAPI.Infrastructure.Abstractions;

using FourPLWebAPI.Services.Abstractions;

namespace FourPLWebAPI.Services.Implementations;

/// <summary>
/// 檔案交換服務實作
/// 整合網路磁碟、SFTP、郵件通知功能
/// </summary>
public class DataExchangeService(
    IConfiguration configuration,
    ILogger<DataExchangeService> logger,
    INetworkDiskHelper networkDiskHelper,
    ISftpConnectionFactory sftpFactory) : IDataExchangeService
{
    private readonly ILogger<DataExchangeService> _logger = logger;
    private readonly INetworkDiskHelper _networkDiskHelper = networkDiskHelper;
    private readonly ISftpConnectionFactory _sftpFactory = sftpFactory;

    // 設定區段
    private readonly IConfigurationSection _localToSapSection = configuration.GetSection("DataExchange:LocalToSap");
    private readonly IConfigurationSection _zlSection = configuration.GetSection("DataExchange:SftpTargets:ZL");
    private readonly IConfigurationSection _arichSection = configuration.GetSection("DataExchange:SftpTargets:ARICH");


    #region 場景二：BPM → SAP (上傳)

    /// <inheritdoc />
    public async Task<DataExchangeResult> UploadToSapAsync()
    {
        var result = new DataExchangeResult
        {
            ScenarioName = "BPM → SAP (上傳)",
            StartTime = DateTime.Now
        };

        try
        {
            _logger.LogInformation("=== 開始執行: {ScenarioName} ===", result.ScenarioName);

            // 連接網路磁碟
            await _networkDiskHelper.ConnectAllAsync();

            var sourcePath = _localToSapSection["SourcePath"] ?? "";
            var targetPath = _localToSapSection["TargetPath"] ?? "";
            var backupPath = _localToSapSection["BackupPath"] ?? "";

            if (!Directory.Exists(sourcePath))
            {
                _logger.LogWarning("來源目錄不存在: {SourcePath}", sourcePath);
                result.Success = true;
                result.EndTime = DateTime.Now;
                return result;
            }

            var files = Directory.GetFiles(sourcePath);
            _logger.LogInformation("找到 {Count} 個檔案待上傳", files.Length);

            // 確保備份目錄存在
            if (!string.IsNullOrEmpty(backupPath) && !Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }

            var timestamp = DateTime.Now.ToString("_yyyyMMddHHmmss");

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath);

                // 上傳至 SAP
                var destPath = Path.Combine(targetPath, fileName);
                File.Copy(filePath, destPath, true);
                _logger.LogInformation("上傳: {FileName} -> {DestPath}", fileName, destPath);

                // 備份
                if (!string.IsNullOrEmpty(backupPath))
                {
                    var backupFilePath = Path.Combine(backupPath, $"{fileNameWithoutExt}{timestamp}{extension}");
                    File.Copy(filePath, backupFilePath, true);
                    _logger.LogDebug("備份: {FileName} -> {BackupPath}", fileName, backupFilePath);
                }

                // 刪除原始檔案
                File.Delete(filePath);
                _logger.LogDebug("已刪除原始檔案: {FilePath}", filePath);

                result.ProcessedFiles.Add(fileName);
                result.ProcessedCount++;
            }

            result.Success = true;
            _logger.LogInformation("=== 完成: {ScenarioName}, 處理 {Count} 個檔案 ===",
                result.ScenarioName, result.ProcessedCount);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "執行失敗: {ScenarioName}", result.ScenarioName);
        }

        result.EndTime = DateTime.Now;
        return result;
    }

    #endregion

    #region 場景三：BPM → ZL (上傳)

    /// <inheritdoc />
    public async Task<DataExchangeResult> UploadToZLAsync()
    {
        return await UploadToSftpAsync("ZL", _zlSection);
    }

    #endregion

    #region 場景四：BPM → ARICH (上傳)

    /// <inheritdoc />
    public async Task<DataExchangeResult> UploadToARICHAsync()
    {
        return await UploadToSftpAsync("ARICH", _arichSection);
    }

    #endregion

    #region 共用 SFTP 上傳方法

    /// <summary>
    /// 透過 SFTP 上傳檔案的共用方法
    /// </summary>
    private async Task<DataExchangeResult> UploadToSftpAsync(string targetName, IConfigurationSection section)
    {
        var result = new DataExchangeResult
        {
            ScenarioName = $"BPM → {targetName} (sFTP 上傳)",
            StartTime = DateTime.Now
        };

        try
        {
            _logger.LogInformation("=== 開始執行: {ScenarioName} ===", result.ScenarioName);

            var sourcePath = section["SourcePath"] ?? "";
            var targetPath = section["TargetPath"] ?? "";
            var backupPath = section["BackupPath"] ?? "";

            if (!Directory.Exists(sourcePath))
            {
                _logger.LogWarning("來源目錄不存在: {SourcePath}", sourcePath);
                result.Success = true;
                result.EndTime = DateTime.Now;
                return result;
            }

            var files = Directory.GetFiles(sourcePath);
            _logger.LogInformation("找到 {Count} 個檔案待上傳", files.Length);

            // 確保備份目錄存在
            if (!string.IsNullOrEmpty(backupPath) && !Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }

            var timestamp = DateTime.Now.ToString("_yyyyMMddHHmmss");

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath);

                // 上傳至 SFTP
                var remotePath = $"{targetPath}/{fileName}";
                var uploadSuccess = await _sftpFactory.UploadFileAsync(targetName, filePath, remotePath);

                if (uploadSuccess)
                {
                    // 備份
                    if (!string.IsNullOrEmpty(backupPath))
                    {
                        var backupFilePath = Path.Combine(backupPath, $"{fileNameWithoutExt}{timestamp}{extension}");
                        File.Copy(filePath, backupFilePath, true);
                        _logger.LogDebug("備份: {FileName} -> {BackupPath}", fileName, backupFilePath);
                    }

                    // 刪除原始檔案
                    File.Delete(filePath);
                    _logger.LogDebug("已刪除原始檔案: {FilePath}", filePath);

                    result.ProcessedFiles.Add(fileName);
                    result.ProcessedCount++;
                }
            }

            result.Success = true;
            _logger.LogInformation("=== 完成: {ScenarioName}, 處理 {Count} 個檔案 ===",
                result.ScenarioName, result.ProcessedCount);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "執行失敗: {ScenarioName}", result.ScenarioName);
        }

        result.EndTime = DateTime.Now;
        return result;
    }

    #endregion
}
