using System.Text.Json.Serialization;

namespace FourPLWebAPI.Models;

/// <summary>
/// SO 同步請求 (簡化版，自動處理兩個表)
/// </summary>
public class SOSyncRequest
{
    /// <summary>
    /// 查詢起始日 (YYYYMMDD) - 預設為昨天
    /// </summary>
    public string? StartDate { get; set; }
}

/// <summary>
/// SO 主檔查詢請求 (舊版，保留向下相容)
/// </summary>
public class SOQueryRequest
{
    /// <summary>
    /// 業務模組 (例如 "AR" 或其他值)
    /// AR: ORDLA IN ('A', 'L') -> Sales_ArichSOMaster
    /// 其他: ORDLA IN ('Z', 'B') -> Sales_ZLSOMaster
    /// </summary>
    public string Vm { get; set; } = string.Empty;

    /// <summary>
    /// 查詢起始日 (YYYYMMDD) - 預設為昨天
    /// </summary>
    public string? StartDate { get; set; }
}

/// <summary>
/// SO 主檔資料 DTO
/// 對應 SAP Table ZT4PL_BILLING 轉換後的欄位
/// </summary>
public class SOMasterData
{
    /// <summary>
    /// 銷售訂單編號
    /// </summary>
    [JsonPropertyName("SONumber")]
    public string SONumber { get; set; } = string.Empty;

    /// <summary>
    /// 銷售訂單項次
    /// </summary>
    [JsonPropertyName("SOItem")]
    public string SOItem { get; set; } = string.Empty;

    /// <summary>
    /// 物料代碼
    /// </summary>
    [JsonPropertyName("MaterialCode")]
    public string MaterialCode { get; set; } = string.Empty;

    /// <summary>
    /// 物料描述 (透過 JOIN 更新)
    /// </summary>
    [JsonPropertyName("MaterialDesc")]
    public string MaterialDesc { get; set; } = string.Empty;

    /// <summary>
    /// 銷售單位 (BOT 會轉換為 BT)
    /// </summary>
    [JsonPropertyName("SalesUnit")]
    public string SalesUnit { get; set; } = string.Empty;

    /// <summary>
    /// 銷售日期
    /// </summary>
    [JsonPropertyName("SalesDate")]
    public string SalesDate { get; set; } = string.Empty;

    /// <summary>
    /// 發票號碼
    /// </summary>
    [JsonPropertyName("Invoice")]
    public string Invoice { get; set; } = string.Empty;

    /// <summary>
    /// 發票日期 (0 會轉換為空字串)
    /// </summary>
    [JsonPropertyName("InvoiceDate")]
    public string InvoiceDate { get; set; } = string.Empty;

    /// <summary>
    /// 數量
    /// </summary>
    [JsonPropertyName("Qty")]
    public decimal Qty { get; set; }

    /// <summary>
    /// 批號
    /// </summary>
    [JsonPropertyName("Batch")]
    public string Batch { get; set; } = string.Empty;

    /// <summary>
    /// 有效期限
    /// </summary>
    [JsonPropertyName("ValidityPeriod")]
    public string ValidityPeriod { get; set; } = string.Empty;

    /// <summary>
    /// 固定價格 (經過 *100/1000 計算)
    /// </summary>
    [JsonPropertyName("FixedPrice")]
    public decimal FixedPrice { get; set; }

    /// <summary>
    /// 貸項通知單
    /// </summary>
    [JsonPropertyName("CreditMemo")]
    public string CreditMemo { get; set; } = string.Empty;

    /// <summary>
    /// 客戶代碼
    /// </summary>
    [JsonPropertyName("CustomerCode")]
    public string CustomerCode { get; set; } = string.Empty;

    /// <summary>
    /// BPM 原始單號
    /// </summary>
    [JsonPropertyName("BPMOriginNumber")]
    public string BPMOriginNumber { get; set; } = string.Empty;

    /// <summary>
    /// SP 編號
    /// </summary>
    [JsonPropertyName("SPNumber")]
    public string SPNumber { get; set; } = string.Empty;
}

/// <summary>
/// SO 同步結果
/// </summary>
public class SOSyncResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 結果訊息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 刪除的資料筆數
    /// </summary>
    public int DeletedCount { get; set; }

    /// <summary>
    /// 新增的資料筆數
    /// </summary>
    public int InsertedCount { get; set; }

    /// <summary>
    /// 目標資料表名稱
    /// </summary>
    public string TargetTable { get; set; } = string.Empty;

    /// <summary>
    /// 同步的資料列表
    /// </summary>
    public List<SOMasterData> Data { get; set; } = new();
}

/// <summary>
/// SO 全部同步結果 (包含多個表的結果)
/// </summary>
public class SOSyncAllResult
{
    /// <summary>
    /// 是否全部成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 結果訊息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 總刪除筆數
    /// </summary>
    public int TotalDeletedCount { get; set; }

    /// <summary>
    /// 總新增筆數
    /// </summary>
    public int TotalInsertedCount { get; set; }

    /// <summary>
    /// 各表同步結果
    /// </summary>
    public List<SOSyncResult> Results { get; set; } = new();
}

