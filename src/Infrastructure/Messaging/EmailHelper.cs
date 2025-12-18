using System.Net;
using System.Net.Mail;

using FourPLWebAPI.Infrastructure.Abstractions;

namespace FourPLWebAPI.Infrastructure.Messaging;

/// <summary>
/// 郵件通知服務實作
/// 使用 SMTP 發送郵件
/// </summary>
public class EmailHelper : IEmailHelper
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailHelper> _logger;

    // SMTP 設定
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly bool _enableSsl;
    private readonly string _username;
    private readonly string _password;
    private readonly string _fromAddress;
    private readonly string[] _defaultRecipients;

    /// <summary>
    /// 建構函式
    /// </summary>
    public EmailHelper(IConfiguration configuration, ILogger<EmailHelper> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // 從設定檔讀取 SMTP 設定
        var section = configuration.GetSection("Smtp");
        _smtpHost = section["Host"] ?? "smtp.office365.com";
        _smtpPort = int.TryParse(section["Port"], out var port) ? port : 587;
        _enableSsl = !bool.TryParse(section["EnableSsl"], out var ssl) || ssl;
        _username = section["Username"] ?? "";
        _password = section["Password"] ?? "";
        _fromAddress = section["FromAddress"] ?? _username;

        var recipientsStr = section["NotifyRecipients"] ?? "";
        _defaultRecipients = recipientsStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <inheritdoc />
    public Task<bool> SendNotificationAsync(string subject, string body, bool isHtml = true)
    {
        return SendNotificationAsync(_defaultRecipients, subject, body, isHtml);
    }

    /// <inheritdoc />
    public async Task<bool> SendNotificationAsync(IEnumerable<string> recipients, string subject, string body, bool isHtml = true)
    {
        try
        {
            var recipientList = recipients.ToList();
            if (recipientList.Count == 0)
            {
                _logger.LogWarning("未指定收件者，郵件未發送");
                return false;
            }

            _logger.LogInformation("開始發送郵件通知: {Subject}, 收件者: {Recipients}",
                subject, string.Join(", ", recipientList));

            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = _enableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_username, _password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_fromAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            foreach (var recipient in recipientList)
            {
                message.To.Add(recipient.Trim());
            }

            await client.SendMailAsync(message);

            _logger.LogInformation("郵件發送成功: {Subject}", subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "郵件發送失敗: {Subject}", subject);
            return false;
        }
    }
}
