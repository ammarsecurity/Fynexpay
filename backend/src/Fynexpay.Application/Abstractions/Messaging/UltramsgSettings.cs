namespace Fynexpay.Application.Abstractions.Messaging;

public static class OtpChannels
{
    public const string WhatsApp = "WhatsApp";
    public const string Email = "Email";
    public const string Both = "Both";
}

public class UltramsgSettings
{
    /// <summary>Master switch for OTP verification flows.</summary>
    public bool Enabled { get; set; }

    /// <summary>WhatsApp | Email | Both</summary>
    public string Channel { get; set; } = OtpChannels.WhatsApp;

    public bool RequireMerchantRegisterOtp { get; set; } = true;
    public bool RequireCheckoutOtp { get; set; } = true;

    // —— WhatsApp (Ultramsg) ——
    public bool WhatsAppEnabled { get; set; } = true;
    public string InstanceId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string DefaultCountryCode { get; set; } = "964";
    public string MerchantRegisterMessage { get; set; } =
        "رمز التحقق من Fynexpay: {code}\nصالح لمدة 5 دقائق. لا تشاركه مع أحد.";
    public string CheckoutMessage { get; set; } =
        "رمز تأكيد الدفع عبر Fynexpay: {code}\nصالح لمدة 5 دقائق.";

    // —— Email (SMTP) ——
    public bool EmailEnabled { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool SmtpUseSsl { get; set; } = true;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Fynexpay";
    public string EmailRegisterSubject { get; set; } = "رمز التحقق — Fynexpay";
    public string EmailRegisterBody { get; set; } =
        "<div style=\"font-family:Tahoma,Arial,sans-serif;direction:rtl;text-align:right\">" +
        "<h2>رمز التحقق</h2><p>رمزك هو:</p>" +
        "<p style=\"font-size:28px;font-weight:bold;letter-spacing:4px\">{code}</p>" +
        "<p>صالح لمدة 5 دقائق.</p></div>";
    public string EmailCheckoutSubject { get; set; } = "تأكيد الدفع — Fynexpay";
    public string EmailCheckoutBody { get; set; } =
        "<div style=\"font-family:Tahoma,Arial,sans-serif;direction:rtl;text-align:right\">" +
        "<h2>تأكيد عملية الدفع</h2><p>رمز التحقق:</p>" +
        "<p style=\"font-size:28px;font-weight:bold;letter-spacing:4px\">{code}</p>" +
        "<p>صالح لمدة 5 دقائق.</p></div>";

    public bool UsesWhatsApp()
    {
        var ch = (Channel ?? OtpChannels.WhatsApp).Trim();
        return WhatsAppEnabled && (
            ch.Equals(OtpChannels.WhatsApp, StringComparison.OrdinalIgnoreCase) ||
            ch.Equals(OtpChannels.Both, StringComparison.OrdinalIgnoreCase));
    }

    public bool UsesEmail()
    {
        var ch = (Channel ?? OtpChannels.WhatsApp).Trim();
        return EmailEnabled && (
            ch.Equals(OtpChannels.Email, StringComparison.OrdinalIgnoreCase) ||
            ch.Equals(OtpChannels.Both, StringComparison.OrdinalIgnoreCase));
    }
}

public interface IUltramsgSettingsService
{
    Task<UltramsgSettings> GetAsync(CancellationToken ct = default);
    Task<UltramsgSettings> SaveAsync(UltramsgSettings settings, CancellationToken ct = default);
    UltramsgSettings MaskSecrets(UltramsgSettings settings);
}

public interface IUltramsgClient
{
    Task<UltramsgStatusResult> GetStatusAsync(CancellationToken ct = default);
    Task<byte[]?> GetQrImageAsync(CancellationToken ct = default);
    Task SendChatAsync(string phoneE164, string body, CancellationToken ct = default);
}

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
    Task<(bool Ok, string Message)> TestConnectionAsync(CancellationToken ct = default);
}

public record UltramsgStatusResult(
    bool Configured,
    bool Enabled,
    string AccountStatus,
    string? SubStatus,
    bool IsReady,
    string? Raw,
    string? Error);
