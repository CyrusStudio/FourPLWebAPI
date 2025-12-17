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
/// Export 商品資訊（保持原參數順序）
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
    decimal InvoicePriceWithTax = 0,
    decimal InvoicePrice = 0,
    decimal TotalInvoicePriceWithTax = 0,
    decimal TotalInvoicePrice = 0,
    decimal FixedPriceWithTax = 0,
    string PricingUnit = "1000"
);

/// <summary>
/// Export 退貨/原單資訊
/// </summary>
public record ExportReturnInfo(
    string ItemPurpose = "",
    string ReturnCode = "",
    string SalesDate = "",
    string OriginSONumber = "",
    string OriginSOItem = "",
    string InvoiceNumber = "",
    string InvoiceDate = "",
    string CreditNote = "",
    string CostCenter = ""
);
