namespace FourPLWebAPI.Models;

// ========== 批次查詢合併模型 ==========

/// <summary>
/// 訂單批次查詢結果 (Master + Detail + Requisition JOIN)
/// </summary>
public class OrderBatchItem
{
    // Requisition 資訊
    public string RequisitionID { get; set; } = string.Empty;
    public string SerialID { get; set; } = string.Empty;
    public DateTime TimeLastAction { get; set; }

    // Master 資訊
    public string ApplicantID { get; set; } = string.Empty;
    public int Invoice { get; set; }
    public int RequestType { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerSPCode { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string QuotationType { get; set; } = string.Empty;
    public string? PriceGroup { get; set; }

    // Detail 資訊
    public int ItemNo { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string UOM { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public string? PriceType { get; set; }
    public decimal OldInvoicePriceWithTax { get; set; }
    public decimal OldSalePriceWithTax { get; set; }
    public decimal NewInvoicePriceWithTax { get; set; }
    public decimal NewSalePriceWithTax { get; set; }
}

/// <summary>
/// 訂單贈品批次查詢結果
/// </summary>
public class OrderFreeGoodsBatchItem
{
    public string RequisitionID { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string FreeMaterialCode { get; set; } = string.Empty;
    public decimal FreeQty { get; set; }
    public string UOM { get; set; } = string.Empty;
    public string? Purpose { get; set; }
}

/// <summary>
/// 訂單加購品批次查詢結果（包含 M 表資訊，可獨立產生 Export）
/// </summary>
public class OrderAddOnBatchItem
{
    // 基本資訊
    public string RequisitionID { get; set; } = string.Empty;
    public string SerialID { get; set; } = string.Empty;
    public DateTime TimeLastAction { get; set; }

    // D3 加購品資訊
    public int ItemNo { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public decimal AddQty { get; set; }
    public string UOM { get; set; } = string.Empty;
    public string? Purpose { get; set; }

    // M 主表資訊（用於獨立產生 Export）
    public string ApplicantID { get; set; } = string.Empty;
    public int Invoice { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerSPCode { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

/// <summary>
/// 樣品批次查詢結果 (Master + Detail + Requisition JOIN)
/// </summary>
public class SampleBatchItem
{
    // Requisition 資訊
    public string RequisitionID { get; set; } = string.Empty;
    public string SerialID { get; set; } = string.Empty;
    public DateTime TimeLastAction { get; set; }

    // Master 資訊
    public string ApplicantID { get; set; } = string.Empty;
    public int Invoice { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerSPCode { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string? FundingSource { get; set; }

    // Detail 資訊
    public int DNo { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string UOM { get; set; } = string.Empty;
    public string? Purpose { get; set; }
}

/// <summary>
/// 退貨批次查詢結果 (Master + Detail + Requisition JOIN)
/// </summary>
public class ReturnBatchItem
{
    // Requisition 資訊
    public string RequisitionID { get; set; } = string.Empty;
    public string SerialID { get; set; } = string.Empty;
    public DateTime TimeLastAction { get; set; }

    // Master 資訊
    public string ApplicantID { get; set; } = string.Empty;
    public int Invoice { get; set; }
    public int RequestType { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? Remark { get; set; }

    // Detail 資訊
    public int ItemNo { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string? Batch { get; set; }
    public decimal Qty { get; set; }
    public decimal ExchangeOut { get; set; }
    public string UOM { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? ReturnCode { get; set; }
    public string? SalesDate { get; set; }
    public string SalesOrderNumber { get; set; } = string.Empty;
    public string SOItem { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public string? InvoiceDate { get; set; }
    public string? DiscountOrderNumber { get; set; }
    public decimal OldUnitPriceInTax { get; set; }
    public decimal TotalUnitPriceInTax { get; set; }
    public decimal NewUnitPriceInTax { get; set; }
    public decimal NewTotalUnitPriceInTax { get; set; }

    // SPNumber 從 SO 表查詢
    public string? SPNumber { get; set; }
}

/// <summary>
/// 佇列表資料模型（用於 BulkInsert）
/// </summary>
public class QueueEntry
{
    public string RequisitionID { get; set; } = string.Empty;
    public string DiagramID { get; set; } = string.Empty;
    public string? SerialID { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ProcessedAt { get; set; }  // NULL = 待處理
    public int ExportCount { get; set; }
}
