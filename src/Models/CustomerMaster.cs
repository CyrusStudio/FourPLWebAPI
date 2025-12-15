using FourPLWebAPI.Infrastructure;

namespace FourPLWebAPI.Models;

/// <summary>
/// Customer 主資料模型
/// 寫入 SAPDS.Sales_CustomerMaster 資料表
/// </summary>
[SapMasterData("Sales_CustomerMaster", "CUSTOMER", "Number")]
public class CustomerMaster
{
    /// <summary>
    /// 客戶編號 (主索引欄位)
    /// </summary>
    [XmlField("KUNNR")]
    public string Number { get; set; } = "";

    /// <summary>
    /// 銷售組織
    /// </summary>
    [XmlField("VKORG")]
    public string SalesOrg { get; set; } = "";

    /// <summary>
    /// 配銷通路
    /// </summary>
    [XmlField("VTWEG")]
    public string DistributionChannel { get; set; } = "";

    /// <summary>
    /// 產品群組
    /// </summary>
    [XmlField("SPART")]
    public string Division { get; set; } = "";

    /// <summary>
    /// 夥伴功能
    /// </summary>
    [XmlField("PARVW")]
    public string PartnerFunction { get; set; } = "";

    /// <summary>
    /// SP 編號
    /// </summary>
    [XmlField("KUNNR_SP")]
    public string SPNumber { get; set; } = "";

    /// <summary>
    /// 客戶名稱
    /// </summary>
    [XmlField("NAME1")]
    public string Name { get; set; } = "";

    /// <summary>
    /// 帳戶群組
    /// </summary>
    [XmlField("KTOKD")]
    public string AccountGroup { get; set; } = "";

    /// <summary>
    /// 地址
    /// </summary>
    [XmlField("STRAS")]
    public string Address { get; set; } = "";

    /// <summary>
    /// 國家
    /// </summary>
    [XmlField("LAND1")]
    public string Country { get; set; } = "";

    /// <summary>
    /// 郵遞區號
    /// </summary>
    [XmlField("PSTLZ")]
    public string PostalCode { get; set; } = "";

    /// <summary>
    /// 城市
    /// </summary>
    [XmlField("ORT01")]
    public string City { get; set; } = "";

    /// <summary>
    /// 負責人名稱
    /// </summary>
    [XmlField("SORT2")]
    public string OwnerName { get; set; } = "";

    /// <summary>
    /// 電話
    /// </summary>
    [XmlField("TEL_NUMBER")]
    public string TEL { get; set; } = "";

    /// <summary>
    /// 傳真
    /// </summary>
    [XmlField("FAX_NUMBER")]
    public string FAX { get; set; } = "";

    /// <summary>
    /// 信用額度
    /// </summary>
    [XmlField("KLIMK")]
    public string CreditLimit { get; set; } = "";

    /// <summary>
    /// 下次檢閱日期
    /// </summary>
    [XmlField("NXTRV")]
    public string NextReviewDate { get; set; } = "";

    /// <summary>
    /// 外部單位編號
    /// </summary>
    [XmlField("ZEXGEN_UNIT")]
    public string ExternalUnitNumber { get; set; } = "";

    /// <summary>
    /// 外部執照編號
    /// </summary>
    [XmlField("ZEXGEN_LCNS")]
    public string ExternalLicenseNumber { get; set; } = "";

    /// <summary>
    /// 統一編號
    /// </summary>
    [XmlField("ZSTCD1")]
    public string GUINumber { get; set; } = "";

    /// <summary>
    /// 發票聯
    /// </summary>
    [XmlField("ZSTCD2")]
    public string InvoiceCoupon { get; set; } = "";

    /// <summary>
    /// 備註
    /// </summary>
    [XmlField("ZCSMEMO")]
    public string Remark { get; set; } = "";

    /// <summary>
    /// 付款條件天數
    /// </summary>
    [XmlField("ZTERM")]
    public string PaymentTermsDays { get; set; } = "";

    /// <summary>
    /// 付款條件
    /// </summary>
    [XmlField("ZWELS")]
    public string PaymentTerms { get; set; } = "";

    /// <summary>
    /// 價格表類型
    /// </summary>
    [XmlField("PLTYP")]
    public string PriceListType { get; set; } = "";

    /// <summary>
    /// 銷售類型
    /// </summary>
    [XmlField("KLABC")]
    public string SalesType { get; set; } = "";

    /// <summary>
    /// 修改者 (固定值 AutoEDI)
    /// </summary>
    [XmlField("ModifyBy")]
    public string ModifyBy { get; set; } = "AutoEDI";

    /// <summary>
    /// 是否刪除
    /// </summary>
    [XmlField("DEL")]
    public string IsDelete { get; set; } = "";
}
