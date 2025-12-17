namespace FourPLWebAPI.Models;

/// <summary>
/// Export 主表資訊（共用欄位）
/// </summary>
public record ExportMasterInfo(
    string RequisitionID,
    string FormNo,
    string ApprovalDate,
    string ApplicantID,
    int Invoice,
    string CustomerCode,
    string CustomerName,
    string SPNumber,
    string Remark
);

/// <summary>
/// Export 項目資訊（訂單類）
/// </summary>
public record ExportOrderItemInfo(
    string ItemCategory,
    string PricingType,
    string PricingGroup,
    string MaterialCode,
    string Batch,
    decimal Qty,
    string SalesUnit,
    string DebitCreditType,
    decimal InvoicePriceWithTax,
    decimal InvoicePrice,
    decimal TotalInvoicePriceWithTax,
    decimal TotalInvoicePrice,
    decimal FixedPriceWithTax,
    string PricingUnit,
    string ItemPurpose,
    string ReturnCode
);

/// <summary>
/// Export 項目資訊（退貨類）
/// </summary>
public record ExportReturnItemInfo(
    string ItemCategory,
    string MaterialCode,
    string Batch,
    decimal Qty,
    string SalesUnit,
    string DebitCreditType,
    decimal InvoicePriceWithTax,
    decimal InvoicePrice,
    decimal TotalInvoicePriceWithTax,
    decimal TotalInvoicePrice,
    string ItemPurpose,
    string ReturnCode,
    string SalesDate,
    string OriginSONumber,
    string OriginSOItem,
    string InvoiceNumber,
    string InvoiceDate,
    string CreditNote
);

/// <summary>
/// Export 格式化資訊
/// </summary>
public record ExportFormInfo(
    int ItemNo,
    string FormRefItem
);
