using FourPLWebAPI.Models;

namespace FourPLWebAPI.Extensions;

/// <summary>
/// BatchItem 擴充方法 - 簡化 Export DTO 建立
/// </summary>
public static class BatchItemExtensions
{
    /// <summary>
    /// 從 OrderBatchItem 建立 ExportFormInfo
    /// </summary>
    public static ExportFormInfo ToFormInfo(this OrderBatchItem i, string formNo, int itemNo, int refItem, string approvalDate)
        => new(i.RequisitionID, formNo, itemNo,
               refItem > 0 ? FormatFormItem(refItem) : "",
               approvalDate);

    /// <summary>
    /// 從 OrderBatchItem 建立 ExportCustomerInfo
    /// </summary>
    public static ExportCustomerInfo ToCustomerInfo(this OrderBatchItem i)
        => new(i.ApplicantID, i.Invoice, i.CustomerCode, i.CustomerName,
               i.CustomerSPCode.OrEmpty(), i.Remark.OrEmpty());

    /// <summary>
    /// 從 SampleBatchItem 建立 ExportFormInfo
    /// </summary>
    public static ExportFormInfo ToFormInfo(this SampleBatchItem i, string formNo, string approvalDate)
        => new(i.RequisitionID, formNo, i.DNo, "",
               approvalDate);

    /// <summary>
    /// 從 SampleBatchItem 建立 ExportCustomerInfo（支援自訂 Remark）
    /// </summary>
    public static ExportCustomerInfo ToCustomerInfo(this SampleBatchItem i, string? customRemark = null)
        => new(i.ApplicantID, i.Invoice, i.CustomerCode, i.CustomerName,
               i.CustomerSPCode.OrEmpty(), customRemark ?? i.Remark.OrEmpty());

    /// <summary>
    /// 從 ReturnBatchItem 建立 ExportFormInfo
    /// </summary>
    public static ExportFormInfo ToFormInfo(this ReturnBatchItem i, string formNo, int itemNo, int refItem, string approvalDate)
        => new(i.RequisitionID, formNo, itemNo,
               FormatFormItem(refItem),
               approvalDate);

    /// <summary>
    /// 從 ReturnBatchItem 建立 ExportCustomerInfo
    /// </summary>
    public static ExportCustomerInfo ToCustomerInfo(this ReturnBatchItem i)
        => new(i.ApplicantID, i.Invoice, i.CustomerCode, i.CustomerName,
               i.SPNumber.OrEmpty(), i.Remark.OrEmpty());

    /// <summary>
    /// 從 OrderAddOnBatchItem 建立 ExportFormInfo
    /// </summary>
    public static ExportFormInfo ToFormInfo(this OrderAddOnBatchItem i, string formNo, int itemNo, string approvalDate)
        => new(i.RequisitionID, formNo, itemNo,
               FormatFormItem(itemNo),
               approvalDate);

    /// <summary>
    /// 從 OrderAddOnBatchItem 建立 ExportCustomerInfo
    /// </summary>
    public static ExportCustomerInfo ToCustomerInfo(this OrderAddOnBatchItem i)
        => new(i.ApplicantID, i.Invoice, i.CustomerCode, i.CustomerName,
               i.CustomerSPCode.OrEmpty(), i.Remark.OrEmpty());

    // ===== ToItemInfo 擴充方法 =====

    /// <summary>
    /// 從 OrderBatchItem 建立 ExportItemInfo（ZTW1/ZTW2 用）
    /// </summary>
    public static ExportItemInfo ToItemInfo(this OrderBatchItem i, string category, string debitCreditType)
        => new(category, i.QuotationType.OrEmpty(),
               i.QuotationType == "1" ? i.CustomerCode : i.PriceGroup.OrEmpty(),
               i.MaterialCode, "", i.Qty, i.UOM, debitCreditType);

    // ===== ToPriceInfo 擴充方法 =====

    /// <summary>
    /// 從 OrderBatchItem 建立 ExportPriceInfo（ZTW1 用）
    /// </summary>
    public static ExportPriceInfo ToPriceInfo(this OrderBatchItem i)
    {
        var isOldPrice = i.RequestType == 1 || i.RequestType == 4;
        var invoicePrice = isOldPrice ? i.OldInvoicePriceWithTax : i.NewInvoicePriceWithTax;
        var fixedPrice = isOldPrice ? i.OldSalePriceWithTax : i.NewSalePriceWithTax;
        return new ExportPriceInfo(
            invoicePrice * 1000, invoicePrice / 1.05m * 1000,
            invoicePrice * i.Qty, invoicePrice / 1.05m * i.Qty,
            fixedPrice * 1000, "1000");
    }

    /// <summary>
    /// 從 OrderBatchItem 建立折讓 ExportPriceInfo（ZTW2 用，回傳 null 表示無需折讓）
    /// </summary>
    public static (ExportPriceInfo? Price, decimal PriceDiff) ToDiscountPriceInfo(this OrderBatchItem i)
    {
        var isOldPrice = i.RequestType == 1 || i.RequestType == 4;
        var invoicePrice = isOldPrice ? i.OldInvoicePriceWithTax : i.NewInvoicePriceWithTax;
        var salePrice = isOldPrice ? i.OldSalePriceWithTax : i.NewSalePriceWithTax;
        var priceDiff = (invoicePrice - salePrice) * i.Qty;
        if (priceDiff == 0) return (null, 0);
        return (new ExportPriceInfo(0, 0, priceDiff, priceDiff / 1.05m, 0, "1000"), priceDiff);
    }

    /// <summary>
    /// 從 SampleBatchItem 建立 ExportItemInfo（ZTW6 用）
    /// </summary>
    public static ExportItemInfo ToItemInfo(this SampleBatchItem i)
        => new("ZTW6", "", "", i.MaterialCode, "", i.Qty, i.UOM, "D");

    /// <summary>
    /// 從 OrderAddOnBatchItem 建立 ExportItemInfo（ZTW5 加購用）
    /// </summary>
    public static ExportItemInfo ToItemInfo(this OrderAddOnBatchItem i)
        => new("ZTW5", "", "", i.MaterialCode, "", i.AddQty, i.UOM, "D");

    /// <summary>
    /// 從 OrderFreeGoodsBatchItem 建立 ExportItemInfo（ZTW5 贈品用，需 OrderBatchItem 提供報價類型）
    /// </summary>
    public static ExportItemInfo ToItemInfo(this OrderFreeGoodsBatchItem fg, OrderBatchItem m)
        => new("ZTW5", m.QuotationType.OrEmpty(),
               m.QuotationType == "1" ? m.CustomerCode : m.PriceGroup.OrEmpty(),
               fg.FreeMaterialCode, "", fg.FreeQty, fg.UOM, "D");

    /// <summary>
    /// 從 ReturnBatchItem 建立 ExportItemInfo
    /// </summary>
    public static ExportItemInfo ToItemInfo(this ReturnBatchItem i, string category, string debitCreditType, decimal qty, string batch = "")
        => new(category, "", "", i.MaterialCode, batch, qty, i.UOM, debitCreditType);

    /// <summary>
    /// 從 ReturnBatchItem 建立 ExportPriceInfo
    /// </summary>
    public static ExportPriceInfo ToPriceInfo(this ReturnBatchItem i, decimal invoicePriceWithTax, decimal totalPriceWithTax)
        => new(invoicePriceWithTax * 1000, invoicePriceWithTax / 1.05m * 1000,
               totalPriceWithTax, totalPriceWithTax / 1.05m, 0, "1000");

    /// <summary>
    /// 從 ReturnBatchItem 建立 ExportReturnInfo
    /// </summary>
    public static ExportReturnInfo ToReturnInfo(this ReturnBatchItem i, string returnCode, string creditNote = "", bool includeInvoice = true)
        => new(i.Notes.OrEmpty(), returnCode,
               i.SalesDate.OrEmpty(), i.SalesOrderNumber.OrEmpty(), i.SOItem.OrEmpty(),
               includeInvoice ? i.InvoiceNumber.OrEmpty() : "",
               includeInvoice ? i.InvoiceDate.OrEmpty() : "",
               creditNote, "");

    /// <summary>
    /// 格式化表單項次為 6 位數字串（原始 SQL 邏輯）
    /// RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@n as NVARCHAR) + REPLICATE('0', len(@n) + 1), len(@n) + 1) as NVARCHAR), 6)
    /// 例如：1 → "000010", 2 → "000020", 10 → "000100"
    /// </summary>
    private static string FormatFormItem(int itemNo)
    {
        var itemStr = itemNo.ToString();
        var padLength = itemStr.Length + 1;
        var paddedItem = itemStr.PadRight(padLength, '0');
        return paddedItem.PadLeft(6, '0');
    }
}
