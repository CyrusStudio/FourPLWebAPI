using FourPLWebAPI.Infrastructure.SAP;

namespace FourPLWebAPI.Models;

/// <summary>
/// Sales 主資料模型 (業務人員)
/// 寫入 SAPDS.Sales_SalesMaster 資料表
/// </summary>
[SapMasterData("Sales_SalesMaster", "SALES", "ADAccount")]
public class SalesMaster
{
    /// <summary>
    /// AD 帳號 (主索引欄位)
    /// </summary>
    [XmlField("SALES_AD")]
    public string ADAccount { get; set; } = "";

    /// <summary>
    /// 姓名
    /// </summary>
    [XmlField("ENAME")]
    public string Name { get; set; } = "";

    /// <summary>
    /// 區域
    /// </summary>
    [XmlField("BZIRK")]
    public string District { get; set; } = "";

    /// <summary>
    /// 辦公室
    /// </summary>
    [XmlField("VKBUR")]
    public string Office { get; set; } = "";

    /// <summary>
    /// 群組代碼
    /// </summary>
    [XmlField("VKGRP")]
    public string GroupCode { get; set; } = "";

    /// <summary>
    /// 帳號
    /// </summary>
    [XmlField("SALESA")]
    public string Account { get; set; } = "";

    /// <summary>
    /// 員工編號
    /// </summary>
    [XmlField("PERNR")]
    public string EmployeeID { get; set; } = "";

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
