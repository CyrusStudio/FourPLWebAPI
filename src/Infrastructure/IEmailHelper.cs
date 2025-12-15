namespace FourPLWebAPI.Infrastructure;

/// <summary>
/// 郵件通知服務介面
/// 用於發送系統通知郵件
/// </summary>
public interface IEmailHelper
{
    /// <summary>
    /// 發送通知郵件
    /// </summary>
    /// <param name="subject">主旨</param>
    /// <param name="body">內容</param>
    /// <param name="isHtml">是否為 HTML 格式</param>
    /// <returns>是否成功</returns>
    Task<bool> SendNotificationAsync(string subject, string body, bool isHtml = true);

    /// <summary>
    /// 發送通知郵件至指定收件者
    /// </summary>
    /// <param name="recipients">收件者清單</param>
    /// <param name="subject">主旨</param>
    /// <param name="body">內容</param>
    /// <param name="isHtml">是否為 HTML 格式</param>
    /// <returns>是否成功</returns>
    Task<bool> SendNotificationAsync(IEnumerable<string> recipients, string subject, string body, bool isHtml = true);
}
