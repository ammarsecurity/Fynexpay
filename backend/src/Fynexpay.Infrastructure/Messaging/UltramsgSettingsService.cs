using System.Text.Json;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Abstractions.Messaging;
using Fynexpay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fynexpay.Infrastructure.Messaging;

public class UltramsgSettingsService : IUltramsgSettingsService
{
    public const string SettingsKey = "ultramsg_runtime";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IAppDbContext _db;
    private UltramsgSettings? _cache;

    public UltramsgSettingsService(IAppDbContext db) => _db = db;

    public async Task<UltramsgSettings> GetAsync(CancellationToken ct = default)
    {
        if (_cache != null) return Clone(_cache);

        var row = await _db.PlatformSettings.FirstOrDefaultAsync(s => s.Key == SettingsKey, ct);
        UltramsgSettings settings;
        if (row == null || string.IsNullOrWhiteSpace(row.Value))
        {
            settings = new UltramsgSettings();
            await PersistAsync(settings, ct);
        }
        else
        {
            settings = JsonSerializer.Deserialize<UltramsgSettings>(row.Value, JsonOpts) ?? new UltramsgSettings();
        }

        Normalize(settings);
        _cache = settings;
        return Clone(settings);
    }

    public async Task<UltramsgSettings> SaveAsync(UltramsgSettings settings, CancellationToken ct = default)
    {
        var current = await GetAsync(ct);
        if (string.IsNullOrWhiteSpace(settings.Token) || settings.Token.Contains('•', StringComparison.Ordinal))
            settings.Token = current.Token;
        if (string.IsNullOrWhiteSpace(settings.SmtpPassword) || settings.SmtpPassword.Contains('•', StringComparison.Ordinal))
            settings.SmtpPassword = current.SmtpPassword;

        Normalize(settings);
        await PersistAsync(settings, ct);
        _cache = settings;
        return MaskSecrets(Clone(settings));
    }

    public UltramsgSettings MaskSecrets(UltramsgSettings settings)
    {
        var clone = Clone(settings);
        clone.Token = Mask(clone.Token);
        clone.SmtpPassword = Mask(clone.SmtpPassword);
        return clone;
    }

    private static string Mask(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Length > 8) return value[..4] + new string('•', Math.Min(12, value.Length - 4));
        return "••••••••";
    }

    private async Task PersistAsync(UltramsgSettings settings, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(settings, JsonOpts);
        var row = await _db.PlatformSettings.FirstOrDefaultAsync(s => s.Key == SettingsKey, ct);
        if (row == null)
        {
            _db.PlatformSettings.Add(new PlatformSetting { Key = SettingsKey, Value = json });
        }
        else
        {
            row.Value = json;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    private static void Normalize(UltramsgSettings s)
    {
        s.Channel = (s.Channel ?? OtpChannels.WhatsApp).Trim() switch
        {
            var c when c.Equals(OtpChannels.Email, StringComparison.OrdinalIgnoreCase) => OtpChannels.Email,
            var c when c.Equals(OtpChannels.Both, StringComparison.OrdinalIgnoreCase) => OtpChannels.Both,
            _ => OtpChannels.WhatsApp
        };

        // Backward compat: older rows only had Enabled for WhatsApp
        if (!s.WhatsAppEnabled && s.Enabled && s.Channel != OtpChannels.Email)
            s.WhatsAppEnabled = true;

        s.InstanceId = (s.InstanceId ?? "").Trim();
        s.Token = (s.Token ?? "").Trim();
        s.DefaultCountryCode = string.IsNullOrWhiteSpace(s.DefaultCountryCode)
            ? "964"
            : s.DefaultCountryCode.Trim().TrimStart('+');
        s.AdminAlertPhone = string.IsNullOrWhiteSpace(s.AdminAlertPhone)
            ? null
            : s.AdminAlertPhone.Trim();
        s.SmtpHost = (s.SmtpHost ?? "").Trim();
        s.SmtpUsername = (s.SmtpUsername ?? "").Trim();
        s.SmtpPassword = (s.SmtpPassword ?? "").Trim();
        s.FromEmail = (s.FromEmail ?? "").Trim();
        s.FromName = string.IsNullOrWhiteSpace(s.FromName) ? "Fynexpay" : s.FromName.Trim();
        if (s.SmtpPort <= 0) s.SmtpPort = 587;

        if (string.IsNullOrWhiteSpace(s.MerchantRegisterMessage))
            s.MerchantRegisterMessage = "رمز التحقق من Fynexpay: {code}\nصالح لمدة 5 دقائق. لا تشاركه مع أحد.";
        if (string.IsNullOrWhiteSpace(s.CheckoutMessage))
            s.CheckoutMessage = "رمز تأكيد الدفع عبر Fynexpay: {code}\nصالح لمدة 5 دقائق.";
        if (string.IsNullOrWhiteSpace(s.ProfileChangeMessage))
            s.ProfileChangeMessage = "رمز تأكيد تعديل الملف الشخصي في Fynexpay: {code}\nصالح لمدة 5 دقائق. لا تشاركه مع أحد.";
        if (string.IsNullOrWhiteSpace(s.PasswordResetMessage))
            s.PasswordResetMessage = "رمز استعادة كلمة المرور في Fynexpay: {code}\nصالح لمدة 5 دقائق. لا تشاركه مع أحد.";
        if (string.IsNullOrWhiteSpace(s.EmailRegisterSubject))
            s.EmailRegisterSubject = "رمز التحقق — Fynexpay";
        if (string.IsNullOrWhiteSpace(s.EmailCheckoutSubject))
            s.EmailCheckoutSubject = "تأكيد الدفع — Fynexpay";
        if (string.IsNullOrWhiteSpace(s.EmailRegisterBody))
            s.EmailRegisterBody = "<p>رمز التحقق: <strong>{code}</strong></p>";
        if (string.IsNullOrWhiteSpace(s.EmailCheckoutBody))
            s.EmailCheckoutBody = "<p>رمز تأكيد الدفع: <strong>{code}</strong></p>";
    }

    private static UltramsgSettings Clone(UltramsgSettings s) => new()
    {
        Enabled = s.Enabled,
        Channel = s.Channel,
        RequireMerchantRegisterOtp = s.RequireMerchantRegisterOtp,
        RequireCheckoutOtp = s.RequireCheckoutOtp,
        WhatsAppEnabled = s.WhatsAppEnabled,
        InstanceId = s.InstanceId,
        Token = s.Token,
        DefaultCountryCode = s.DefaultCountryCode,
        AdminAlertPhone = s.AdminAlertPhone,
        MerchantRegisterMessage = s.MerchantRegisterMessage,
        CheckoutMessage = s.CheckoutMessage,
        ProfileChangeMessage = s.ProfileChangeMessage,
        PasswordResetMessage = s.PasswordResetMessage,
        EmailEnabled = s.EmailEnabled,
        SmtpHost = s.SmtpHost,
        SmtpPort = s.SmtpPort,
        SmtpUseSsl = s.SmtpUseSsl,
        SmtpUsername = s.SmtpUsername,
        SmtpPassword = s.SmtpPassword,
        FromEmail = s.FromEmail,
        FromName = s.FromName,
        EmailRegisterSubject = s.EmailRegisterSubject,
        EmailRegisterBody = s.EmailRegisterBody,
        EmailCheckoutSubject = s.EmailCheckoutSubject,
        EmailCheckoutBody = s.EmailCheckoutBody
    };
}
