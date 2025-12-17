namespace FourPLWebAPI.Extensions;

/// <summary>
/// 字串擴充方法
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// 若為 null 則回傳空字串
    /// </summary>
    public static string OrEmpty(this string? value) => value ?? "";
}
