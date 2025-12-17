namespace FourPLWebAPI.Models;

// ========== TWC1D002 訂單相關 ==========

/// <summary>
/// 訂單主檔 (FM7T_TWF1D002_M)
/// </summary>
public class OrderMaster
{
    public string RequisitionID { get; set; } = string.Empty;
    public string ApplicantID { get; set; } = string.Empty;
    public int Invoice { get; set; }
    public int RequestType { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerSPCode { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string QuotationType { get; set; } = string.Empty;
    public string? PriceGroup { get; set; }
}

/// <summary>
/// 訂單明細 (FM7T_TWF1D002_D)
/// </summary>
public class OrderDetail
{
    public string RequisitionID { get; set; } = string.Empty;
    public int ItemNo { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string UOM { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public string? PriceType { get; set; }
    public string? PARENT { get; set; }
    public decimal OldInvoicePriceWithTax { get; set; }
    public decimal OldSalePriceWithTax { get; set; }
    public decimal NewInvoicePriceWithTax { get; set; }
    public decimal NewSalePriceWithTax { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>
/// 訂單贈品明細 (FM7T_TWF1D002_D2)
/// </summary>
public class OrderFreeGoods
{
    public string RequisitionID { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string FreeMaterialCode { get; set; } = string.Empty;
    public decimal FreeQty { get; set; }
    public string UOM { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public string? CHILD { get; set; }
}

/// <summary>
/// 訂單加購品明細 (FM7T_TWF1D002_D3)
/// </summary>
public class OrderAddOn
{
    public string RequisitionID { get; set; } = string.Empty;
    public int ItemNo { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public decimal AddQty { get; set; }
    public string UOM { get; set; } = string.Empty;
    public string? Purpose { get; set; }
}

// ========== TWC0D003 樣品相關 ==========

/// <summary>
/// 樣品主檔 (FM7T_TWF0D003_M)
/// </summary>
public class SampleMaster
{
    public string RequisitionID { get; set; } = string.Empty;
    public string ApplicantID { get; set; } = string.Empty;
    public int Invoice { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerSPCode { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string? FundingSource { get; set; }
}

/// <summary>
/// 樣品明細 (FM7T_TWF0D003_D)
/// </summary>
public class SampleDetail
{
    public string RequisitionID { get; set; } = string.Empty;
    public int DNo { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string UOM { get; set; } = string.Empty;
    public string? Purpose { get; set; }
}

// ========== TWC0D004 退貨相關 ==========

/// <summary>
/// 退貨主檔 (FM7T_TWF0D004_M)
/// </summary>
public class ReturnMaster
{
    public string RequisitionID { get; set; } = string.Empty;
    public string ApplicantID { get; set; } = string.Empty;
    public int Invoice { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

/// <summary>
/// 退貨明細 (FM7T_TWF0D004_D)
/// </summary>
public class ReturnDetail
{
    public string RequisitionID { get; set; } = string.Empty;
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
}

// ========== 系統簽核資訊 ==========

/// <summary>
/// BPM 簽核資訊 (FSe7en_Sys_Requisition)
/// </summary>
public class BpmRequisition
{
    public string RequisitionID { get; set; } = string.Empty;
    public string DiagramID { get; set; } = string.Empty;
    public string SerialID { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime TimeLastAction { get; set; }
}

/// <summary>
/// 銷售訂單主檔（用於查詢 SPNumber）
/// </summary>
public class SalesSOMaster
{
    public string SONumber { get; set; } = string.Empty;
    public string SOItem { get; set; } = string.Empty;
    public string SPNumber { get; set; } = string.Empty;
}
