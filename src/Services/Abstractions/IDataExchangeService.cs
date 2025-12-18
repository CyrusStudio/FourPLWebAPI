using FourPLWebAPI.Models;

namespace FourPLWebAPI.Services.Abstractions;

/// <summary>
/// 檔案交換服務介面
/// 定義 sFTP 相關檔案交換場景的操作
/// </summary>
public interface IDataExchangeService
{
    #region 場景二：BPM → SAP (上傳)

    /// <summary>
    /// 上傳檔案至 SAP 資料夾
    /// </summary>
    /// <returns>處理結果</returns>
    Task<DataExchangeResult> UploadToSapAsync();

    #endregion

    #region 場景三：BPM → ZL (上傳)

    /// <summary>
    /// 透過 sFTP 上傳檔案至 ZL
    /// </summary>
    /// <returns>處理結果</returns>
    Task<DataExchangeResult> UploadToZLAsync();

    #endregion

    #region 場景四：BPM → ARICH (上傳)

    /// <summary>
    /// 透過 sFTP 上傳檔案至 ARICH
    /// </summary>
    /// <returns>處理結果</returns>
    Task<DataExchangeResult> UploadToARICHAsync();

    #endregion
}
