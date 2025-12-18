namespace FourPLWebAPI.Infrastructure.SAP;

/// <summary>
/// XML 欄位對應標註
/// 用於標註 Model 屬性對應的 XML 元素名稱
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class XmlFieldAttribute(
    string xmlElementName,
    bool isBooleanFlag = false,
    bool skipXmlRead = false,
    bool isNumeric = false,
    bool isDateTime = false) : Attribute
{
    /// <summary>
    /// XML 元素名稱
    /// </summary>
    public string XmlElementName { get; } = xmlElementName;

    /// <summary>
    /// 是否為布林旗標欄位
    /// 如果為 true，則 "X" → "1"，其他值 → "0"
    /// </summary>
    public bool IsBooleanFlag { get; } = isBooleanFlag;

    /// <summary>
    /// 是否跳過 XML 讀取 (保留預設值)
    /// 適用於 ModifyBy, ModifyTime 等程式設定的欄位
    /// </summary>
    public bool SkipXmlRead { get; } = skipXmlRead;

    /// <summary>
    /// 是否為數值欄位 (decimal, int 等)
    /// 如果為 true，空字串會轉為 null
    /// </summary>
    public bool IsNumeric { get; } = isNumeric;

    /// <summary>
    /// 是否為日期時間欄位
    /// 如果為 true，會使用 DateTime 格式
    /// </summary>
    public bool IsDateTime { get; } = isDateTime;
}

/// <summary>
/// SAP 主資料標註
/// 用於標註 Model 對應的資料表和 XML 根元素
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SapMasterDataAttribute(
    string tableName,
    string xmlRootElement,
    params string[] primaryKeyProperties) : Attribute
{
    /// <summary>
    /// 資料表名稱
    /// </summary>
    public string TableName { get; } = tableName;

    /// <summary>
    /// XML 根元素名稱 (例如: CUSTOMER, MATERIAL)
    /// </summary>
    public string XmlRootElement { get; } = xmlRootElement;

    /// <summary>
    /// 主索引欄位屬性名稱 (複合主鍵時為多個)
    /// </summary>
    public string[] PrimaryKeyProperties { get; } = primaryKeyProperties.Length > 0 ? primaryKeyProperties : ["Number"];

    /// <summary>
    /// 取得第一個主鍵欄位 (向下相容)
    /// </summary>
    public string PrimaryKeyProperty => PrimaryKeyProperties.FirstOrDefault() ?? "";
}
