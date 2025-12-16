using FourPLWebAPI.Infrastructure;

namespace FourPLWebAPI.Models;

/// <summary>
/// Material 主資料模型 (物料)
/// 寫入 SAPDS.Sales_MaterialMaster 資料表
/// </summary>
[SapMasterData("Sales_MaterialMaster", "MATERIAL", "MaterialCode", "SalesOrg", "DistributionChannel", "Division")]
public class MaterialMaster
{
    /// <summary>
    /// 物料代碼 (主索引欄位)
    /// </summary>
    [XmlField("MATNR")]
    public string MaterialCode { get; set; } = "";

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
    /// 物料說明
    /// </summary>
    [XmlField("MAKTX")]
    public string MaterialDesc { get; set; } = "";

    /// <summary>
    /// 基本單位
    /// </summary>
    [XmlField("MEINS")]
    public string BaseUOM { get; set; } = "";

    /// <summary>
    /// 採購文字
    /// </summary>
    [XmlField("ZLEGCTL")]
    public string PurchaseText { get; set; } = "";

    /// <summary>
    /// 法規管制
    /// </summary>
    [XmlField("Z_TW_NHIBX")]
    public string LegalControl { get; set; } = "";

    /// <summary>
    /// 健保代碼
    /// </summary>
    [XmlField("Z_TW_NHIB")]
    public string NHIBCode { get; set; } = "";

    /// <summary>
    /// 儲存條件
    /// </summary>
    [XmlField("RAUBE")]
    public string StorageConditions { get; set; } = "";

    /// <summary>
    /// 銷售單位
    /// </summary>
    [XmlField("VRKME")]
    public string SalesUnit { get; set; } = "";

    /// <summary>
    /// 物料群組
    /// </summary>
    [XmlField("MVGR5")]
    public string MaterialGroup { get; set; } = "";

    /// <summary>
    /// 產品規格
    /// </summary>
    [XmlField("Z_TW_SPEC")]
    public string ProductSpec { get; set; } = "";

    /// <summary>
    /// 藥品許可證
    /// </summary>
    [XmlField("Z_TW_DRUG_LICENSE")]
    public string DrugPermitLicense { get; set; } = "";

    /// <summary>
    /// 最小單位
    /// </summary>
    [XmlField("Z_MINIMUM_UOM")]
    public string MinUnit { get; set; } = "";

    /// <summary>
    /// 包裝單位
    /// </summary>
    [XmlField("Z_PACK_UOM")]
    public string PackageUnit { get; set; } = "";

    /// <summary>
    /// 適應症 1
    /// </summary>
    [XmlField("Z_INDIC_1")]
    public string Indication1 { get; set; } = "";

    /// <summary>
    /// 適應症 2
    /// </summary>
    [XmlField("Z_INDIC_2")]
    public string Indication2 { get; set; } = "";

    /// <summary>
    /// 適應症 3
    /// </summary>
    [XmlField("Z_INDIC_3")]
    public string Indication3 { get; set; } = "";

    /// <summary>
    /// 劑型
    /// </summary>
    [XmlField("Z_DOSAGE_FORM")]
    public string DosageForm { get; set; } = "";

    /// <summary>
    /// 劑量
    /// </summary>
    [XmlField("Z_STRENGHT")]
    public string Strength { get; set; } = "";

    /// <summary>
    /// 包裝大小
    /// </summary>
    [XmlField("Z_PACK_SIZE")]
    public string PackSize { get; set; } = "";

    /// <summary>
    /// 修改者 (固定值 4PLAPI，不從 XML 讀取)
    /// </summary>
    [XmlField("ModifyBy", skipXmlRead: true)]
    public string ModifyBy { get; set; } = "4PLAPI";

    /// <summary>
    /// 修改時間 (自動填入處理時間，不從 XML 讀取)
    /// </summary>
    [XmlField("ModifyTime", skipXmlRead: true, isDateTime: true)]
    public DateTime ModifyTime { get; set; } = DateTime.Now;

}
