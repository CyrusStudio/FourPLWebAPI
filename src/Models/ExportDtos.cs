namespace FourPLWebAPI.Models;

/// <summary>
/// Export 表單資訊
/// </summary>
public record ExportFormInfo(
    string RequisitionID,
    string FormNo,
    int ItemNo,
    string FormRefItem,
    string ApprovalDate
);

/// <summary>
/// Export 客戶資訊
/// </summary>
public record ExportCustomerInfo(
    string ApplicantID,
    int Invoice,
    string CustomerCode,
    string CustomerName,
    string SPNumber,
    string Remark
);

/// <summary>
/// Export 商品資訊
/// </summary>
public record ExportItemInfo(
    string ItemCategory,
    string PricingType,
    string PricingGroup,
    string MaterialCode,
    string Batch,
    decimal Qty,
    string SalesUnit,
    string DebitCreditType
);

/// <summary>
/// Export 價格資訊
/// </summary>
public record ExportPriceInfo(
    decimal InvoicePriceWithTax,
    decimal InvoicePrice,
    decimal TotalInvoicePriceWithTax,
    decimal TotalInvoicePrice,
    decimal FixedPriceWithTax,
    string PricingUnit
);

/// <summary>
/// Export 退貨/原單資訊
/// </summary>
public record ExportReturnInfo(
    string ItemPurpose,
    string ReturnCode,
    string SalesDate,
    string OriginSONumber,
    string OriginSOItem,
    string InvoiceNumber,
    string InvoiceDate,
    string CreditNote,
    string CostCenter
);
