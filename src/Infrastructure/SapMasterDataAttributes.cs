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
    /// 建構函式
    /// </summary>
    /// <param name="xmlElementName">XML 元素名稱</param>
    public XmlFieldAttribute(string xmlElementName)
    {
        XmlElementName = xmlElementName;
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
