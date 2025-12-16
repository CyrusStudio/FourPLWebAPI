namespace FourPLWebAPI.Infrastructure;

/// <summary>
/// XML 欄位對應標註
/// 用於標註 Model 屬性對應的 XML 元素名稱
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class XmlFieldAttribute : Attribute
{
    /// <summary>
    /// XML 元素名稱
    /// </summary>
    public string XmlElementName { get; }

    /// <summary>
    /// 是否為布林旗標欄位
    /// 如果為 true，則 "X" → "1"，其他值 → "0"
    /// </summary>
    public bool IsBooleanFlag { get; }

    /// <summary>
    /// 是否跳過 XML 讀取 (保留預設值)
    /// 適用於 ModifyBy, ModifyTime 等程式設定的欄位
    /// </summary>
    public bool SkipXmlRead { get; }

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="xmlElementName">XML 元素名稱 (同時也是資料庫欄位名稱)</param>
    /// <param name="isBooleanFlag">是否為布林旗標 (X→1, 否則→0)</param>
    /// <param name="skipXmlRead">是否跳過 XML 讀取 (保留預設值)</param>
    public XmlFieldAttribute(string xmlElementName, bool isBooleanFlag = false, bool skipXmlRead = false)
    {
        XmlElementName = xmlElementName;
        IsBooleanFlag = isBooleanFlag;
        SkipXmlRead = skipXmlRead;
    }
}

/// <summary>
/// SAP 主資料標註
/// 用於標註 Model 對應的資料表和 XML 根元素
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SapMasterDataAttribute : Attribute
{
    /// <summary>
    /// 資料表名稱
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// XML 根元素名稱 (例如: CUSTOMER, MATERIAL)
    /// </summary>
    public string XmlRootElement { get; }

    /// <summary>
    /// 主索引欄位屬性名稱
    /// </summary>
    public string PrimaryKeyProperty { get; }

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="tableName">資料表名稱</param>
    /// <param name="xmlRootElement">XML 根元素名稱</param>
    /// <param name="primaryKeyProperty">主索引欄位屬性名稱</param>
    public SapMasterDataAttribute(
        string tableName,
        string xmlRootElement,
        string primaryKeyProperty = "Number")
    {
        TableName = tableName;
        XmlRootElement = xmlRootElement;
        PrimaryKeyProperty = primaryKeyProperty;
    }
}
