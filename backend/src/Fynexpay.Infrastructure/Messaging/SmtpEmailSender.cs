using System.Net;
using System.Net.Mail;
using Fynexpay.Application.Abstractions.Messaging;
using Microsoft.Extensions.Logging;

namespace Fynexpay.Infrastructure.Messaging;

public class SmtpEmailSender : IEmailSender
{
    private readonly IUltramsgSettingsService _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IUltramsgSettingsService settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        EnsureConfigured(s);

        using var message = new MailMessage
        {
            From = new MailAddress(s.FromEmail, string.IsNullOrWhiteSpace(s.FromName) ? "Fynexpay" : s.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = CreateClient(s);
        await client.SendMailAsync(message, ct);
    }

    public async Task<(bool Ok, string Message)> TestConnectionAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        try
        {
            EnsureConfigured(s);
            // Send a lightweight self-test to FromEmail
            await SendAsync(s.FromEmail, "Fynexpay SMTP test",
                "<p style=\"font-family:Tahoma,Arial,sans-serif\">اختبار اتصال البريد من Fynexpay ناجح.</p>", ct);
            return (true, "تم إرسال رسالة اختبار إلى " + s.FromEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP test failed");
            return (false, ex.Message);
        }
    }

    private static SmtpClient CreateClient(UltramsgSettings s)
    {
        var client = new SmtpClient(s.SmtpHost, s.SmtpPort <= 0 ? 587 : s.SmtpPort)
        {
            EnableSsl = s.SmtpUseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 20000
        };

        if (!string.IsNullOrWhiteSpace(s.SmtpUsername))
        {
            client.Credentials = new NetworkCredential(s.SmtpUsername, s.SmtpPassword);
        }

        return client;
    }

    private static void EnsureConfigured(UltramsgSettings s)
    {
        if (!s.EmailEnabled)
            throw new InvalidOperationException("خدمة البريد غير مفعّلة");
        if (string.IsNullOrWhiteSpace(s.SmtpHost))
            throw new InvalidOperationException("Smtp Host مطلوب");
        if (string.IsNullOrWhiteSpace(s.FromEmail) || !s.FromEmail.Contains('@'))
            throw new InvalidOperationException("From Email غير صالح");
    }
}
