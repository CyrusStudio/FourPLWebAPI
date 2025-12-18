using FourPLWebAPI.Infrastructure.Abstractions;
using FourPLWebAPI.Models;
using FourPLWebAPI.Services.Abstractions;

namespace FourPLWebAPI.Services.Implementations;

/// <summary>
/// SAP 檔案服務實作
/// 處理從 SAP 下載檔案和後續處理 (Customer, Material, Price, Sales)
/// </summary>
public class SapMasterDataService(
    IConfiguration configuration,
    ILogger<SapMasterDataService> logger,
    INetworkDiskHelper networkDiskHelper,
    ISapMasterDataRepository masterDataRepository) : ISapMasterDataService
{
    private readonly ILogger<SapMasterDataService> _logger = logger;
    private readonly INetworkDiskHelper _networkDiskHelper = networkDiskHelper;
    private readonly ISapMasterDataRepository _masterDataRepository = masterDataRepository;
    private readonly IConfigurationSection _masterDataSection = configuration.GetSection("SapMasterData");
    private readonly IConfigurationSection _processingSection = configuration.GetSection("SapMasterData:ProcessPaths");
    private readonly IConfigurationSection _safetySection = configuration.GetSection("SafetySystem");

    private bool IsProdMode => _safetySection.GetValue<bool>("IsProdMode", false);

    // 支援的檔案類型
    private static readonly string[] SupportedFileTypes = ["Customer", "Material", "Price", "Sales"];

    #region SAP 檔案下載

    /// <inheritdoc />
    public async Task<SapDownloadResult> DownloadFromSapAsync()
    {
        var result = new SapDownloadResult
        {
            StartTime = DateTime.Now
        };

        try
        {
            _logger.LogInformation("=== 開始執行: SAP → BPM (下載) ===");

            // 連接網路磁碟
            await _networkDiskHelper.ConnectAllAsync();

            var sourcePath = _masterDataSection["SapSourcePath"] ?? "";
            var targetBasePath = _masterDataSection["LocalTargetPath"] ?? "";

            if (!Directory.Exists(sourcePath))
            {
                throw new DirectoryNotFoundException($"來源目錄不存在: {sourcePath}");
            }

            var files = Directory.GetFiles(sourcePath);
            _logger.LogInformation("找到 {Count} 個檔案待處理", files.Length);

            var timestamp = DateTime.Now.ToString("_yyyyMMddHHmmss");

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath);
                var prefix = fileName.Split('_')[0];

                // 根據檔名前綴決定目標資料夾
                var targetFolder = GetTargetFolder(prefix);
                if (string.IsNullOrEmpty(targetFolder))
                {
                    _logger.LogWarning("無法識別檔案前綴: {FileName}", fileName);
                    continue;
                }

                var targetDir = Path.Combine(targetBasePath, targetFolder);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                var targetPath = Path.Combine(targetDir, $"{fileName}{timestamp}{extension}");

                // 複製檔案
                File.Copy(filePath, targetPath, true);
                _logger.LogInformation("複製: {Source} -> {Target}", Path.GetFileName(filePath), targetPath);

                // 刪除原始檔案 (僅在生產模式執行，避免影響舊系統驗證)
                if (IsProdMode)
                {
                    File.Delete(filePath);
                    _logger.LogDebug("已刪除正式環境原始檔案: {FilePath}", filePath);
                }
                else
                {
                    _logger.LogInformation("【SAFETY KEEP】非生產模式，保留原始檔案: {FileName}", Path.GetFileName(filePath));
                }

                result.ProcessedFiles.Add(new SapDownloadFileInfo
                {
                    FileName = Path.GetFileName(filePath),
                    FileType = targetFolder,
                    DestinationPath = targetPath
                });
                result.ProcessedCount++;
            }

            result.Success = true;
            _logger.LogInformation("=== 完成: SAP → BPM (下載), 處理 {Count} 個檔案 ===", result.ProcessedCount);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "執行失敗: SAP → BPM (下載)");
        }

        result.EndTime = DateTime.Now;
        return result;
    }

    /// <summary>
    /// 根據檔案前綴取得目標資料夾名稱
    /// </summary>
    private static string GetTargetFolder(string prefix)
    {
        return prefix.ToUpper() switch
        {
            "CSTM" => "Customer",
            "MARA" => "Material",
            "SALES" => "Sales",
            "PRICE" => "Price",
            _ => ""
        };
    }

    #endregion

    #region SAP 檔案後處理

    /// <inheritdoc />
    public async Task<IEnumerable<FileProcessingResult>> ProcessAllAsync()
    {
        var results = new List<FileProcessingResult>();

        _logger.LogInformation("========== 開始處理所有 SAP 檔案 ==========");

        foreach (var fileType in SupportedFileTypes)
        {
            var result = await ProcessByTypeAsync(fileType);
            results.Add(result);
        }

        _logger.LogInformation("========== 所有 SAP 檔案處理完畢 ==========");

        return results;
    }

    /// <inheritdoc />
    public async Task<FileProcessingResult> ProcessByTypeAsync(string fileType)
    {
        var result = new FileProcessingResult
        {
            FileType = fileType,
            StartTime = DateTime.Now
        };

        try
        {
            _logger.LogInformation("=== 開始處理 {FileType} 類型檔案 ===", fileType);

            var section = _processingSection.GetSection(fileType);
            var sourcePath = section["Source"] ?? "";
            var successPath = section["Success"] ?? "";
            var failPath = section["Fail"] ?? "";

            if (string.IsNullOrEmpty(sourcePath))
            {
                _logger.LogWarning("未設定 {FileType} 的來源路徑", fileType);
                result.EndTime = DateTime.Now;
                return result;
            }

            // 確保成功/失敗資料夾存在
            EnsureDirectoryExists(successPath);
            EnsureDirectoryExists(failPath);

            // 取得待處理檔案
            if (!Directory.Exists(sourcePath))
            {
                _logger.LogWarning("來源目錄不存在: {SourcePath}", sourcePath);
                result.EndTime = DateTime.Now;
                return result;
            }

            var files = Directory.GetFiles(sourcePath);
            result.TotalCount = files.Length;

            _logger.LogInformation("找到 {Count} 個 {FileType} 檔案待處理", files.Length, fileType);

            foreach (var filePath in files)
            {
                var fileResult = await ProcessFileAsync(fileType, filePath);
                result.FileResults.Add(fileResult);

                if (fileResult.Success)
                    result.SuccessCount++;
                else
                    result.FailCount++;
            }

            _logger.LogInformation("=== {FileType} 處理完成: 成功 {Success}/{Total}, 失敗 {Fail}/{Total} ===",
                fileType, result.SuccessCount, result.TotalCount, result.FailCount, result.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "處理 {FileType} 類型檔案時發生錯誤", fileType);
        }

        result.EndTime = DateTime.Now;
        return result;
    }

    /// <inheritdoc />
    public async Task<SingleFileResult> ProcessFileAsync(string fileType, string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var result = new SingleFileResult { FileName = fileName };

        try
        {
            var section = _processingSection.GetSection(fileType);
            var successPath = section["Success"] ?? "";
            var failPath = section["Fail"] ?? "";

            _logger.LogDebug("開始處理檔案: {FileName}", fileName);

            // 執行實際的檔案處理邏輯
            var processSuccess = await ExecuteFileProcessingAsync(fileType, filePath);

            if (processSuccess)
            {
                // 處理成功，移動到成功資料夾
                var destPath = Path.Combine(successPath, fileName);
                await MoveFileAsync(filePath, destPath);

                result.Success = true;
                result.DestinationPath = destPath;
                _logger.LogInformation("檔案處理成功: {FileName} -> {DestPath}", fileName, successPath);
            }
            else
            {
                // 處理失敗，移動到失敗資料夾
                var destPath = Path.Combine(failPath, fileName);
                await MoveFileAsync(filePath, destPath);

                result.Success = false;
                result.ErrorMessage = "檔案處理失敗";
                result.DestinationPath = destPath;
                _logger.LogWarning("檔案處理失敗: {FileName} -> {DestPath}", fileName, failPath);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "處理檔案時發生例外: {FileName}", fileName);

            // 嘗試移動到失敗資料夾
            try
            {
                var section = _processingSection.GetSection(fileType);
                var failPath = section["Fail"] ?? "";
                if (!string.IsNullOrEmpty(failPath))
                {
                    var destPath = Path.Combine(failPath, fileName);
                    await MoveFileAsync(filePath, destPath);
                    result.DestinationPath = destPath;
                }
            }
            catch
            {
                // 移動失敗時忽略
            }
        }

        return result;
    }

    /// <summary>
    /// 執行實際的檔案處理邏輯
    /// 根據檔案類型調用對應的泛型處理方法
    /// </summary>
    protected virtual async Task<bool> ExecuteFileProcessingAsync(string fileType, string filePath)
    {
        return fileType switch
        {
            "Customer" => await ProcessFileAsync<Models.CustomerMaster>(fileType, filePath, "Customer"),
            "Material" => await ProcessFileAsync<Models.MaterialMaster>(fileType, filePath, "Material"),
            "Price" => await ProcessFileAsync<Models.PriceMaster>(fileType, filePath, "Price"),
            "Sales" => await ProcessFileAsync<Models.SalesMaster>(fileType, filePath, "Sales"),
            _ => LogUnsupportedTypeAndReturnFalse(fileType)
        };
    }

    /// <summary>
    /// 記錄不支援的類型並回傳 false
    /// </summary>
    private bool LogUnsupportedTypeAndReturnFalse(string fileType)
    {
        _logger.LogWarning("不支援的檔案類型: {FileType}", fileType);
        return false;
    }

    #endregion

    #region 泛型檔案處理方法

    /// <summary>
    /// 泛型檔案處理方法
    /// 讀取 XML 後寫入對應的資料庫資料表
    /// 失敗時產生錯誤 Log 檔案
    /// </summary>
    /// <typeparam name="T">Model 類型 (需標註 SapMasterDataAttribute)</typeparam>
    /// <param name="fileType">檔案類型 (用於取得失敗資料夾路徑)</param>
    /// <param name="filePath">XML 檔案路徑</param>
    /// <param name="typeName">類型名稱 (用於記錄)</param>
    /// <returns>處理是否成功</returns>
    protected virtual async Task<bool> ProcessFileAsync<T>(string fileType, string filePath, string typeName) where T : class, new()
    {
        try
        {
            _logger.LogInformation("開始處理 {TypeName} 檔案: {FilePath}", typeName, filePath);

            // 讀取 XML 檔案
            var data = await _masterDataRepository.ReadFromXmlAsync<T>(filePath);
            var dataList = data.ToList();

            if (dataList.Count == 0)
            {
                _logger.LogWarning("{TypeName} 檔案無有效資料: {FilePath}", typeName, filePath);
                return true; // 空檔案視為成功
            }

            // 使用 TRUNCATE + Bulk Insert 方法 (全量資料匯入)
            var result = await _masterDataRepository.TruncateAndBulkInsertAsync(dataList);

            _logger.LogInformation("{TypeName} 檔案處理完成: {FilePath}, 成功 {SuccessCount} 筆, 失敗 {FailedCount} 筆",
                typeName, filePath, result.SuccessCount, result.FailedCount);

            // 如果有錯誤，產生錯誤 Log 檔案 (放到失敗資料夾)
            if (result.FailedItems.Count > 0)
            {
                var section = _processingSection.GetSection(fileType);
                var failPath = section["Fail"] ?? "";
                await WriteErrorLogAsync(filePath, typeName, result, failPath);
                return false; // 有錯誤則視為失敗
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{TypeName} 檔案處理失敗: {FilePath}", typeName, filePath);
            return false;
        }
    }

    /// <summary>
    /// 產生錯誤 Log 檔案
    /// </summary>
    /// <param name="originalFilePath">原始 XML 檔案路徑</param>
    /// <param name="typeName">類型名稱</param>
    /// <param name="result">處理結果</param>
    /// <param name="failPath">失敗資料夾路徑</param>
    private async Task WriteErrorLogAsync(string originalFilePath, string typeName,
        FourPLWebAPI.Infrastructure.Abstractions.UpsertBatchResult result, string failPath)
    {
        try
        {
            // 使用失敗資料夾路徑，如果沒有則使用原始檔案目錄
            var directory = !string.IsNullOrEmpty(failPath)
                ? failPath
                : Path.GetDirectoryName(originalFilePath) ?? "";
            EnsureDirectoryExists(directory);

            var originalFileName = Path.GetFileNameWithoutExtension(originalFilePath);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var logFileName = $"{originalFileName}_Error_{timestamp}.log";
            var logFilePath = Path.Combine(directory, logFileName);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== {typeName} 處理錯誤報告 ===");
            sb.AppendLine($"原始檔案: {originalFilePath}");
            sb.AppendLine($"處理時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"總筆數: {result.TotalCount}");
            sb.AppendLine($"成功筆數: {result.SuccessCount}");
            sb.AppendLine($"失敗筆數: {result.FailedCount}");
            sb.AppendLine();
            sb.AppendLine("=== 失敗項目明細 ===");
            sb.AppendLine();

            foreach (var item in result.FailedItems)
            {
                sb.AppendLine($"主鍵: {item.PrimaryKey}");
                sb.AppendLine($"錯誤訊息: {item.ErrorMessage}");
                sb.AppendLine($"原始資料: {item.RawData}");
                sb.AppendLine("---");
            }

            await File.WriteAllTextAsync(logFilePath, sb.ToString(), System.Text.Encoding.UTF8);
            _logger.LogWarning("已產生錯誤 Log 檔案: {LogFilePath}", logFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "產生錯誤 Log 檔案失敗");
        }
    }

    #endregion

    #region 輔助方法

    /// <summary>
    /// 確保目錄存在
    /// </summary>
    private void EnsureDirectoryExists(string path)
    {
        if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            _logger.LogDebug("建立目錄: {Path}", path);
        }
    }

    /// <summary>
    /// 移動檔案 (靜態方法)
    /// </summary>
    private static async Task MoveFileAsync(string source, string destination)
    {
        await Task.Run(() =>
        {
            // 如果目標檔案已存在，先刪除
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }
            File.Move(source, destination);
        });
    }

    #endregion
}
