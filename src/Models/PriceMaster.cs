using FourPLWebAPI.Infrastructure;

namespace FourPLWebAPI.Models;

/// <summary>
/// Price 主資料模型 (價格)
/// 寫入 SAPDS.Sales_PriceMaster 資料表
/// </summary>
[SapMasterData("Sales_PriceMaster", "PRICE", "SalesOrg")]
public class PriceMaster
{
    /// <summary>
    /// 銷售組織 (主索引欄位)
    /// </summary>
    [XmlField("VKORG")]
    public string SalesOrg { get; set; } = "";

    /// <summary>
    /// 配銷通路
    /// </summary>
    [XmlField("VTWEG")]
    public string DistributionChannel { get; set; } = "";

    /// <summary>
    /// 價格類型
    /// </summary>
    [XmlField("PRCTYP")]
    public string PricingType { get; set; } = "";

    /// <summary>
    /// 價格群組
    /// </summary>
    [XmlField("PLTYP_KUNNR")]
    public string PricingGroup { get; set; } = "";

    /// <summary>
    /// 文件幣別
    /// </summary>
    [XmlField("WAERK")]
    public string DocCurrency { get; set; } = "";

    /// <summary>
    /// 物料代碼
    /// </summary>
    [XmlField("MATNR")]
    public string MaterialCode { get; set; } = "";

    /// <summary>
    /// 生效日期
    /// </summary>
    [XmlField("VALID")]
    public string ValidOn { get; set; } = "";

    /// <summary>
    /// 發票價格
    /// </summary>
    [XmlField("KBETR_PR00")]
    public string InvoicePrice { get; set; } = "";

    /// <summary>
    /// 幣別
    /// </summary>
    [XmlField("KONWA")]
    public string Currency { get; set; } = "";

    /// <summary>
    /// 條件價格單位
    /// </summary>
    [XmlField("KPEIN")]
    public string ConditionPriceUnit { get; set; } = "";

    /// <summary>
    /// 條件單位
    /// </summary>
    [XmlField("KMEIN")]
    public string ConditionUnit { get; set; } = "";

    /// <summary>
    /// 條件有效起始日
    /// </summary>
    [XmlField("DATAB")]
    public string ConditionValidFrom { get; set; } = "";

    /// <summary>
    /// 條件有效結束日
    /// </summary>
    [XmlField("DATBI")]
    public string ConditionValidTo { get; set; } = "";

    /// <summary>
    /// 固定價格
    /// </summary>
    [XmlField("KBETR_ZTW2")]
    public string FixedPrice { get; set; } = "";

    /// <summary>
    /// 最小數量
    /// </summary>
    [XmlField("KNRMM")]
    public string MinQty { get; set; } = "";

    /// <summary>
    /// 贈品數量
    /// </summary>
    [XmlField("KNRNM")]
    public string FreeGoodsQty { get; set; } = "";

    /// <summary>
    /// 額外贈品數量
    /// </summary>
    [XmlField("KNRZM")]
    public string AdditionFreeGoodsQty { get; set; } = "";

    /// <summary>
    /// 額外贈品物料代碼
    /// </summary>
    [XmlField("KNRMAT")]
    public string AdditionFreeGoodsMaterialCode { get; set; } = "";

    /// <summary>
    /// 額外贈品單位
    /// </summary>
    [XmlField("KNREZ")]
    public string AdditionFreeGoodsUnit { get; set; } = "";

    /// <summary>
    /// 級距數量
    /// </summary>
    [XmlField("SCALE_QUANTITY")]
    public string ScaleQty { get; set; } = "";

    /// <summary>
    /// 級距金額
    /// </summary>
    [XmlField("SCALE_AMOUNT")]
    public string ScaleAmount { get; set; } = "";

    /// <summary>
    /// 責任歸屬
    /// </summary>
    [XmlField("KZUST")]
    public string Responsibility { get; set; } = "";

    /// <summary>
    /// BPM 表單編號
    /// </summary>
    [XmlField("FORM_NO")]
    public string BPMFormNo { get; set; } = "";

    /// <summary>
    /// 修改者 (固定值 AutoEDI)
    /// </summary>
    [XmlField("ModifyBy")]
    public string ModifyBy { get; set; } = "AutoEDI";
}
