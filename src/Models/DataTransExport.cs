using System.ComponentModel.DataAnnotations.Schema;

namespace FourPLWebAPI.Models;

/// <summary>
/// FourPL_DataTrans_Export / FourPL_DataTrans_Export_Verify 資料表實體
/// 包含 44 個原始欄位 + 額外狀態欄位
/// </summary>
public class DataTransExport
{
    /// <summary>匯出資料識別碼（自動產生）</summary>
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ExportID { get; set; }

    /// <summary>簽核單號</summary>
    public string RequisitionID { get; set; } = string.Empty;

    /// <summary>表單編號</summary>
    public string FormNo { get; set; } = string.Empty;

    /// <summary>表單項次</summary>
    public string FormItem { get; set; } = string.Empty;

    /// <summary>表單參考項次</summary>
    public string FormRefItem { get; set; } = string.Empty;

    /// <summary>申請人代碼</summary>
    public string ApplicantID { get; set; } = string.Empty;

    /// <summary>銷售組織</summary>
    public string SalesOrg { get; set; } = string.Empty;

    /// <summary>配銷通路</summary>
    public string DistributionChannel { get; set; } = string.Empty;

    /// <summary>產品群組</summary>
    public string Division { get; set; } = string.Empty;

    /// <summary>收貨方 (L:經銷商, B:醫院)</summary>
    public string ReceivingParty { get; set; } = string.Empty;

    /// <summary>客戶代碼</summary>
    public string CustomerNumber { get; set; } = string.Empty;

    /// <summary>客戶名稱</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>特約商代碼</summary>
    public string SPNumber { get; set; } = string.Empty;

    /// <summary>核准日期 (yyyyMMdd)</summary>
    public string ApprovalDate { get; set; } = string.Empty;

    /// <summary>備註</summary>
    public string? Remark { get; set; }

    /// <summary>項目類別 (ZTW1~ZTW8, ZTWB)</summary>
    public string ItemCategory { get; set; } = string.Empty;

    /// <summary>報價類型</summary>
    public string? PricingType { get; set; }

    /// <summary>價格群組</summary>
    public string? PricingGroup { get; set; }

    /// <summary>物料代碼</summary>
    public string MaterialCode { get; set; } = string.Empty;

    /// <summary>批次</summary>
    public string? Batch { get; set; }

    /// <summary>銷售通路</summary>
    public string? SalesChannel { get; set; }

    /// <summary>數量</summary>
    public decimal Qty { get; set; }

    /// <summary>銷售單位</summary>
    public string SalesUnit { get; set; } = string.Empty;

    /// <summary>借貸類型 (D:借, C:貸)</summary>
    public string DebitCreditType { get; set; } = string.Empty;

    /// <summary>幣別</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>含稅發票價（每千單位）</summary>
    public decimal InvoicePriceWithTax { get; set; }

    /// <summary>未稅發票價（每千單位）</summary>
    public decimal InvoicePrice { get; set; }

    /// <summary>含稅發票總價</summary>
    public decimal TotalInvoicePriceWithTax { get; set; }

    /// <summary>未稅發票總價</summary>
    public decimal TotalInvoicePrice { get; set; }

    /// <summary>含稅固定價（每千單位）</summary>
    public decimal FixedPriceWithTax { get; set; }

    /// <summary>計價單位</summary>
    public string? PricingUnit { get; set; }

    /// <summary>項目用途</summary>
    public string? ItemPurpose { get; set; }

    /// <summary>退貨原因代碼</summary>
    public string? ReturnCode { get; set; }

    /// <summary>銷售日期</summary>
    public string? SalesDate { get; set; }

    /// <summary>原銷售訂單號</summary>
    public string? OriginSONumber { get; set; }

    /// <summary>原銷售訂單項次</summary>
    public string? OriginSOItem { get; set; }

    /// <summary>新銷售訂單號</summary>
    public string? NewSONumber { get; set; }

    /// <summary>新銷售訂單項次</summary>
    public string? NewSOItem { get; set; }

    /// <summary>發票號碼</summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>發票日期</summary>
    public string? InvoiceDate { get; set; }

    /// <summary>折讓單號</summary>
    public string? CreditNote { get; set; }

    /// <summary>有效期間</summary>
    public string? ValidityPeriod { get; set; }

    /// <summary>儲存位置</summary>
    public string? Sloc { get; set; }

    /// <summary>成本中心</summary>
    public string? CostCenter { get; set; }

    // ========== 額外狀態欄位（用於 Export_Verify 表）==========

    /// <summary>匯出狀態 (0:待處理, 1:待轉XML, 2:已完成)</summary>
    public int ExportStatus { get; set; }

    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>匯出完成時間</summary>
    public DateTime? ExportedAt { get; set; }

    /// <summary>錯誤訊息</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 資料轉換結果
/// </summary>
public class DataTransformResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>處理訊息</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>處理筆數</summary>
    public int ProcessedCount { get; set; }

    /// <summary>處理的 RequisitionID 清單</summary>
    public List<string> RequisitionIds { get; set; } = new();

    /// <summary>錯誤清單</summary>
    public List<string> Errors { get; set; } = new();
}
