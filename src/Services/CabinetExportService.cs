using System.Text;
using System.Xml.Linq;
using FourPLWebAPI.Infrastructure;
using FourPLWebAPI.Models;

namespace FourPLWebAPI.Services;

/// <summary>
/// 機櫃匯出服務實作
/// 整合 SQL、SAP、XML、sFTP 的完整流程編排
/// </summary>
public class CabinetExportService : ICabinetExportService
{
    private readonly ISqlHelper _sqlHelper;
    private readonly ISapHelper _sapHelper;
    private readonly ISftpHelper _sftpHelper;
    private readonly ILogger<CabinetExportService> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 建構函式
    /// </summary>
    public CabinetExportService(
        ISqlHelper sqlHelper,
        ISapHelper sapHelper,
        ISftpHelper sftpHelper,
        ILogger<CabinetExportService> logger,
        IConfiguration configuration)
    {
        _sqlHelper = sqlHelper;
        _sapHelper = sapHelper;
        _sftpHelper = sftpHelper;
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<CabinetExportResponse> ExportAsync(CabinetExportRequest request)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("開始處理機櫃匯出請求: {RequestId}", request.RequestId);

        try
        {
            // 步驟 1: 從 SQL Server 查詢 SAP 呼叫參數
            _logger.LogDebug("步驟 1: 查詢 SAP 參數");
            var sapParams = await GetSapParametersAsync(request);
            if (sapParams == null)
            {
                return CabinetExportResponse.CreateFailure(
                    request.RequestId, 
                    "找不到對應的 SAP 參數設定", 
                    startTime);
            }

            // 步驟 2: 呼叫 SAP RFC 取得機櫃資料
            _logger.LogDebug("步驟 2: 呼叫 SAP RFC");
            var cabinetList = await GetCabinetDataFromSapAsync(request, sapParams);
            if (cabinetList.Count == 0)
            {
                return CabinetExportResponse.CreateFailure(
                    request.RequestId, 
                    "SAP 未回傳任何機櫃資料", 
                    startTime);
            }

            _logger.LogInformation("SAP 回傳 {Count} 筆機櫃資料", cabinetList.Count);

            // 步驟 3: 轉換為 XML
            _logger.LogDebug("步驟 3: 轉換為 XML");
            var xmlDocument = ConvertToXml(cabinetList, request);
            var xmlFileName = GenerateXmlFileName(request);

            // 步驟 4: 上傳至 sFTP
            _logger.LogDebug("步驟 4: 上傳至 sFTP");
            var uploadPath = request.CustomUploadPath 
                ?? $"{sapParams.SftpDirectory}/{xmlFileName}";

            using var xmlStream = new MemoryStream(Encoding.UTF8.GetBytes(xmlDocument.ToString()));
            var uploadSuccess = await _sftpHelper.UploadFileAsync(uploadPath, xmlStream);

            if (!uploadSuccess)
            {
                return CabinetExportResponse.CreateFailure(
                    request.RequestId, 
                    "sFTP 上傳失敗", 
                    startTime);
            }

            _logger.LogInformation("機櫃匯出完成: {FileName} -> {Path}", xmlFileName, uploadPath);

            return CabinetExportResponse.CreateSuccess(
                request.RequestId,
                cabinetList.Count,
                xmlFileName,
                uploadPath,
                startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "機櫃匯出失敗: {RequestId}", request.RequestId);
            return CabinetExportResponse.CreateFailure(
                request.RequestId, 
                $"處理過程發生錯誤: {ex.Message}", 
                startTime,
                new List<string> { ex.ToString() });
        }
    }

    /// <summary>
    /// 從 SQL Server 查詢 SAP 參數
    /// </summary>
    private async Task<SapParameterData?> GetSapParametersAsync(CabinetExportRequest request)
    {
        const string sql = @"
            SELECT 
                Id, RfcName, SapSystemCode, CompanyCode, PlantCode,
                AdditionalParams, OutputTableName, SftpDirectory,
                XmlTemplateCode, IsActive
            FROM SapParameters
            WHERE OrganizationCode = @OrganizationCode
              AND IsActive = 1
            ORDER BY Id
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY";

        return await _sqlHelper.QueryFirstOrDefaultAsync<SapParameterData>(
            sql, 
            new { request.OrganizationCode });
    }

    /// <summary>
    /// 呼叫 SAP RFC 取得機櫃資料
    /// </summary>
    private async Task<List<CabinetData>> GetCabinetDataFromSapAsync(
        CabinetExportRequest request, 
        SapParameterData sapParams)
    {
        return await _sapHelper.ExecuteRfcAndParseTableAsync<CabinetData>(
            sapParams.RfcName,
            sapParams.OutputTableName,
            builder =>
            {
                // 設定 SAP RFC Import 參數
                builder.SetImport("I_COMPANY_CODE", sapParams.CompanyCode);
                
                if (!string.IsNullOrEmpty(sapParams.PlantCode))
                {
                    builder.SetImport("I_PLANT", sapParams.PlantCode);
                }

                if (!string.IsNullOrEmpty(request.CabinetType))
                {
                    builder.SetImport("I_CABINET_TYPE", request.CabinetType);
                }

                if (!string.IsNullOrEmpty(request.DataCenterCode))
                {
                    builder.SetImport("I_DATACENTER", request.DataCenterCode);
                }

                if (!string.IsNullOrEmpty(request.StartDate))
                {
                    builder.SetImport("I_DATE_FROM", request.StartDate);
                }

                if (!string.IsNullOrEmpty(request.EndDate))
                {
                    builder.SetImport("I_DATE_TO", request.EndDate);
                }

                builder.SetImport("I_INCLUDE_INACTIVE", request.IncludeInactive ? "X" : "");
            });
    }

    /// <summary>
    /// 將機櫃資料轉換為 XML
    /// </summary>
    private XDocument ConvertToXml(List<CabinetData> cabinetList, CabinetExportRequest request)
    {
        var root = new XElement("CabinetExport",
            new XAttribute("RequestId", request.RequestId),
            new XAttribute("ExportDate", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
            new XAttribute("OrganizationCode", request.OrganizationCode),
            new XElement("Summary",
                new XElement("TotalCount", cabinetList.Count),
                new XElement("ActiveCount", cabinetList.Count(c => c.Status == "ACTIVE")),
                new XElement("MaintenanceCount", cabinetList.Count(c => c.Status == "MAINTENANCE"))
            ),
            new XElement("Cabinets",
                cabinetList.Select(cabinet => new XElement("Cabinet",
                    new XElement("Id", cabinet.Cabinet_Id),
                    new XElement("Name", cabinet.Cabinet_Name),
                    new XElement("Location", cabinet.Location),
                    new XElement("Capacity", cabinet.Capacity),
                    new XElement("UsedSlots", cabinet.Used_Slots),
                    new XElement("AvailableSlots", cabinet.Capacity - cabinet.Used_Slots),
                    new XElement("UtilizationRate", 
                        cabinet.Capacity > 0 
                            ? Math.Round((double)cabinet.Used_Slots / cabinet.Capacity * 100, 2) 
                            : 0),
                    new XElement("Status", cabinet.Status),
                    new XElement("CreatedDate", cabinet.Created_Date),
                    !string.IsNullOrEmpty(cabinet.Cabinet_Type) 
                        ? new XElement("Type", cabinet.Cabinet_Type) 
                        : null,
                    !string.IsNullOrEmpty(cabinet.DataCenter_Code) 
                        ? new XElement("DataCenter", cabinet.DataCenter_Code) 
                        : null,
                    !string.IsNullOrEmpty(cabinet.Owner) 
                        ? new XElement("Owner", cabinet.Owner) 
                        : null,
                    !string.IsNullOrEmpty(cabinet.Remarks) 
                        ? new XElement("Remarks", cabinet.Remarks) 
                        : null
                ))
            )
        );

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            root);
    }

    /// <summary>
    /// 產生 XML 檔案名稱
    /// </summary>
    private string GenerateXmlFileName(CabinetExportRequest request)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"CabinetExport_{request.OrganizationCode}_{timestamp}.xml";
    }
}
