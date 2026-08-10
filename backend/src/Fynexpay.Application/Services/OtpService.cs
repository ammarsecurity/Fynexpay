using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Abstractions.Messaging;
using Fynexpay.Application.DTOs;
using Fynexpay.Application.Security;
using Fynexpay.Domain.Entities;
using Fynexpay.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fynexpay.Application.Services;

public class OtpService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IAppDbContext _db;
    private readonly IUltramsgSettingsService _settings;
    private readonly IUltramsgClient _ultramsg;
    private readonly IEmailSender _email;
    private readonly ISecretProtector _protector;
    private readonly ILogger<OtpService> _logger;
    private readonly bool _isDevelopment;

    public OtpService(
        IAppDbContext db,
        IUltramsgSettingsService settings,
        IUltramsgClient ultramsg,
        IEmailSender email,
        ISecretProtector protector,
        ILogger<OtpService> logger)
    {
        _db = db;
        _settings = settings;
        _ultramsg = ultramsg;
        _email = email;
        _protector = protector;
        _logger = logger;
        _isDevelopment = string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development",
            StringComparison.OrdinalIgnoreCase);
    }

    public string NormalizePhone(string? raw, string defaultCountryCode = "964")
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("رقم الهاتف مطلوب");

        var digits = Regex.Replace(raw.Trim(), @"[^\d+]", "");
        if (digits.StartsWith('+'))
            digits = digits[1..];

        if (digits.StartsWith('0') && digits.Length >= 10)
            digits = defaultCountryCode + digits[1..];

        if (digits.Length is < 10 or > 15)
            throw new ArgumentException("رقم الهاتف غير صالح. استخدم الصيغة الدولية أو 07xxxxxxxxx");

        return "+" + digits;
    }

    public string MaskPhone(string phoneE164)
    {
        var d = phoneE164.TrimStart('+');
        if (d.Length <= 6) return phoneE164;
        return $"+{d[..3]}••••{d[^3..]}";
    }

    public string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***@***";
        var user = email[..at];
        var domain = email[(at + 1)..];
        var visible = user.Length <= 2 ? user[..1] : user[..2];
        return $"{visible}***@{domain}";
    }

    public async Task<OtpSendResult> SendMerchantRegisterOtpAsync(RegisterMerchantRequest request, CancellationToken ct = default)
    {
        var settings = await _settings.GetAsync(ct);
        EnsureOtpEnabled(settings, forCheckout: false);

        PasswordRules.ValidateEmail(request.Email);
        PasswordRules.Validate(request.Password);
        PasswordRules.ValidateRequired(request.FullName, "الاسم الكامل");
        PasswordRules.ValidateRequired(request.BusinessName, "اسم النشاط");

        var useWa = settings.UsesWhatsApp();
        var useEmail = settings.UsesEmail();
        if (useWa)
            PasswordRules.ValidateRequired(request.ContactPhone, "رقم الهاتف");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("البريد الإلكتروني مستخدم مسبقاً");

        string? phone = null;
        if (useWa)
            phone = NormalizePhone(request.ContactPhone, settings.DefaultCountryCode);

        var cooldownKey = phone ?? email;
        await EnforceSendCooldownAsync(OtpPurpose.MerchantRegister, cooldownKey, null, ct);

        var payload = JsonSerializer.Serialize(new PendingMerchantRegistration(
            email,
            _protector.Protect(request.Password),
            request.FullName.Trim(),
            request.BusinessName.Trim(),
            string.IsNullOrWhiteSpace(request.BusinessNameAr) ? null : request.BusinessNameAr.Trim(),
            phone ?? request.ContactPhone?.Trim() ?? "",
            string.IsNullOrWhiteSpace(request.WebsiteUrl) ? null : request.WebsiteUrl.Trim()
        ), JsonOpts);

        var code = GenerateCode();
        var challenge = new OtpChallenge
        {
            Purpose = OtpPurpose.MerchantRegister,
            PhoneE164 = phone ?? "",
            TargetEmail = email,
            CodeHash = HashCode(code),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            LastSentAtUtc = DateTime.UtcNow,
            PayloadJson = payload
        };

        _db.OtpChallenges.Add(challenge);
        await _db.SaveChangesAsync(ct);

        var via = await DeliverAsync(settings, phone, email, code, forCheckout: false, ct);
        var masked = BuildMaskedDestination(phone, email, useWa, useEmail);

        return new OtpSendResult(challenge.Id, masked, 300, DevCode(code), via);
    }

    public async Task ConsumeMerchantRegisterChallengeAsync(Guid challengeId, string code, CancellationToken ct = default)
    {
        var challenge = await LoadActiveChallengeAsync(challengeId, OtpPurpose.MerchantRegister, ct);
        await VerifyCodeAsync(challenge, code, ct);
        challenge.Consumed = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PendingMerchantRegistration> GetPendingRegistrationAsync(Guid challengeId, CancellationToken ct = default)
    {
        var challenge = await _db.OtpChallenges.FirstOrDefaultAsync(x => x.Id == challengeId, ct)
            ?? throw new InvalidOperationException("رمز التحقق غير موجود");
        if (challenge.Purpose != OtpPurpose.MerchantRegister)
            throw new InvalidOperationException("رمز التحقق غير صالح");
        if (!challenge.Consumed)
            throw new InvalidOperationException("يجب تأكيد رمز التحقق أولاً");
        if (string.IsNullOrWhiteSpace(challenge.PayloadJson))
            throw new InvalidOperationException("بيانات التسجيل غير مكتملة");

        var pending = JsonSerializer.Deserialize<PendingMerchantRegistration>(challenge.PayloadJson, JsonOpts)
            ?? throw new InvalidOperationException("بيانات التسجيل غير مكتملة");
        return pending with { Password = _protector.Unprotect(pending.Password) };
    }

    public async Task InvalidateChallengeAsync(Guid challengeId, CancellationToken ct = default)
    {
        var challenge = await _db.OtpChallenges.FirstOrDefaultAsync(x => x.Id == challengeId, ct);
        if (challenge == null) return;
        challenge.PayloadJson = null;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<OtpSendResult> SendCheckoutOtpAsync(Guid paymentId, string? phoneRaw, string? emailRaw, CancellationToken ct = default)
    {
        var settings = await _settings.GetAsync(ct);
        EnsureOtpEnabled(settings, forCheckout: true);

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, ct)
            ?? throw new InvalidOperationException("الدفعة غير موجودة");

        if (payment.Status != PaymentStatus.Pending)
            throw new InvalidOperationException("لا يمكن تأكيد هذه الدفعة");

        var useWa = settings.UsesWhatsApp();
        var useEmail = settings.UsesEmail();

        string? phone = null;
        string? email = null;

        if (useWa)
        {
            phone = NormalizePhone(
                string.IsNullOrWhiteSpace(phoneRaw) ? payment.CustomerPhone : phoneRaw,
                settings.DefaultCountryCode);
            payment.CustomerPhone = phone;
        }

        if (useEmail)
        {
            email = (string.IsNullOrWhiteSpace(emailRaw) ? payment.CustomerEmail : emailRaw)?.Trim().ToLowerInvariant();
            PasswordRules.ValidateEmail(email);
            payment.CustomerEmail = email;
        }

        var cooldownKey = phone ?? email!;
        await EnforceSendCooldownAsync(OtpPurpose.Checkout, cooldownKey, paymentId, ct);

        var code = GenerateCode();
        var challenge = new OtpChallenge
        {
            Purpose = OtpPurpose.Checkout,
            PhoneE164 = phone ?? "",
            TargetEmail = email,
            PaymentId = paymentId,
            CodeHash = HashCode(code),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            LastSentAtUtc = DateTime.UtcNow
        };
        _db.OtpChallenges.Add(challenge);
        await _db.SaveChangesAsync(ct);

        var via = await DeliverAsync(settings, phone, email, code, forCheckout: true, ct);
        var masked = BuildMaskedDestination(phone, email, useWa, useEmail);
        return new OtpSendResult(challenge.Id, masked, 300, DevCode(code), via);
    }

    public async Task VerifyCheckoutOtpAsync(Guid paymentId, Guid challengeId, string code, CancellationToken ct = default)
    {
        var challenge = await LoadActiveChallengeAsync(challengeId, OtpPurpose.Checkout, ct);
        if (challenge.PaymentId != paymentId)
            throw new InvalidOperationException("رمز التحقق غير مرتبط بهذه الدفعة");

        await VerifyCodeAsync(challenge, code, ct);
        challenge.Consumed = true;

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, ct)
            ?? throw new InvalidOperationException("الدفعة غير موجودة");
        if (!string.IsNullOrWhiteSpace(challenge.PhoneE164))
            payment.CustomerPhone = challenge.PhoneE164;
        if (!string.IsNullOrWhiteSpace(challenge.TargetEmail))
            payment.CustomerEmail = challenge.TargetEmail;
        payment.CustomerPhoneVerifiedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> IsCheckoutVerifiedAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        return payment?.CustomerPhoneVerifiedAtUtc != null;
    }

    private async Task<string> DeliverAsync(
        UltramsgSettings settings,
        string? phone,
        string? email,
        string code,
        bool forCheckout,
        CancellationToken ct)
    {
        var sent = new List<string>();
        var errors = new List<string>();

        if (settings.UsesWhatsApp() && !string.IsNullOrWhiteSpace(phone))
        {
            try
            {
                var body = (forCheckout ? settings.CheckoutMessage : settings.MerchantRegisterMessage)
                    .Replace("{code}", code, StringComparison.Ordinal);
                await _ultramsg.SendChatAsync(phone, body, ct);
                sent.Add("WhatsApp");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WhatsApp OTP failed for {Phone}", MaskPhone(phone));
                errors.Add("واتساب");
                if (_isDevelopment)
                    _logger.LogWarning("DEV WhatsApp OTP {Phone}: {Code}", MaskPhone(phone), code);
            }
        }

        if (settings.UsesEmail() && !string.IsNullOrWhiteSpace(email))
        {
            try
            {
                var subject = forCheckout ? settings.EmailCheckoutSubject : settings.EmailRegisterSubject;
                var html = (forCheckout ? settings.EmailCheckoutBody : settings.EmailRegisterBody)
                    .Replace("{code}", code, StringComparison.Ordinal);
                await _email.SendAsync(email, subject, html, ct);
                sent.Add("Email");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email OTP failed for {Email}", MaskEmail(email));
                errors.Add("البريد");
                if (_isDevelopment)
                    _logger.LogWarning("DEV Email OTP {Email}: {Code}", MaskEmail(email), code);
            }
        }

        if (sent.Count == 0)
        {
            if (_isDevelopment)
            {
                _logger.LogWarning("DEV OTP fallback code: {Code}", code);
                return "Dev";
            }
            throw new InvalidOperationException(
                errors.Count > 0
                    ? $"تعذّر إرسال رمز التحقق عبر {string.Join(" و", errors)}. راجع إعدادات القناة."
                    : "لا توجد قناة إرسال مفعّلة");
        }

        if (_isDevelopment)
            _logger.LogInformation("OTP sent via {Via}, code={Code}", string.Join("+", sent), code);

        return string.Join("+", sent);
    }

    private string BuildMaskedDestination(string? phone, string? email, bool useWa, bool useEmail)
    {
        var parts = new List<string>();
        if (useWa && !string.IsNullOrWhiteSpace(phone)) parts.Add(MaskPhone(phone));
        if (useEmail && !string.IsNullOrWhiteSpace(email)) parts.Add(MaskEmail(email));
        return string.Join(" · ", parts);
    }

    private string? DevCode(string code) => _isDevelopment ? code : null;

    private static void EnsureOtpEnabled(UltramsgSettings settings, bool forCheckout)
    {
        if (!settings.Enabled)
            throw new InvalidOperationException("خدمة التحقق غير مفعّلة حالياً");

        if (forCheckout && !settings.RequireCheckoutOtp)
            throw new InvalidOperationException("تأكيد الدفع عبر OTP غير مفعّل");
        if (!forCheckout && !settings.RequireMerchantRegisterOtp)
            throw new InvalidOperationException("تأكيد التسجيل عبر OTP غير مفعّل");

        if (!settings.UsesWhatsApp() && !settings.UsesEmail())
            throw new InvalidOperationException("فعّل واتساب أو البريد (أو الاثنين) من إعدادات التحقق");

        if (settings.UsesWhatsApp() &&
            (string.IsNullOrWhiteSpace(settings.InstanceId) || string.IsNullOrWhiteSpace(settings.Token)))
            throw new InvalidOperationException("إعدادات Ultramsg غير مكتملة");

        if (settings.UsesEmail() &&
            (string.IsNullOrWhiteSpace(settings.SmtpHost) || string.IsNullOrWhiteSpace(settings.FromEmail)))
            throw new InvalidOperationException("إعدادات SMTP غير مكتملة");
    }

    private async Task EnforceSendCooldownAsync(OtpPurpose purpose, string destination, Guid? paymentId, CancellationToken ct)
    {
        var recent = await _db.OtpChallenges
            .Where(x => x.Purpose == purpose && !x.Consumed)
            .Where(x => x.PhoneE164 == destination || x.TargetEmail == destination)
            .Where(x => paymentId == null || x.PaymentId == paymentId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (recent?.LastSentAtUtc is { } last && DateTime.UtcNow - last < TimeSpan.FromSeconds(60))
        {
            var wait = 60 - (int)(DateTime.UtcNow - last).TotalSeconds;
            throw new InvalidOperationException($"انتظر {Math.Max(wait, 1)} ثانية قبل إعادة الإرسال");
        }
    }

    private async Task<OtpChallenge> LoadActiveChallengeAsync(Guid id, OtpPurpose purpose, CancellationToken ct)
    {
        var challenge = await _db.OtpChallenges.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("رمز التحقق غير موجود");

        if (challenge.Purpose != purpose)
            throw new InvalidOperationException("رمز التحقق غير صالح");
        if (challenge.Consumed)
            throw new InvalidOperationException("تم استخدام رمز التحقق مسبقاً");
        if (challenge.ExpiresAtUtc < DateTime.UtcNow)
            throw new InvalidOperationException("انتهت صلاحية رمز التحقق");
        if (challenge.Attempts >= challenge.MaxAttempts)
            throw new InvalidOperationException("تم تجاوز عدد المحاولات المسموح بها");

        return challenge;
    }

    private async Task VerifyCodeAsync(OtpChallenge challenge, string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || !Regex.IsMatch(code.Trim(), @"^\d{6}$"))
            throw new ArgumentException("رمز التحقق يجب أن يكون 6 أرقام");

        challenge.Attempts++;
        var ok = FixedTimeEquals(challenge.CodeHash, HashCode(code.Trim()));
        await _db.SaveChangesAsync(ct);
        if (!ok)
            throw new InvalidOperationException("رمز التحقق غير صحيح");
    }

    private static string GenerateCode()
    {
        var n = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return n.ToString("D6");
    }

    private static string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("fynexpay-otp:" + code));
        return Convert.ToHexString(bytes);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}

public record PendingMerchantRegistration(
    string Email,
    string Password,
    string FullName,
    string BusinessName,
    string? BusinessNameAr,
    string ContactPhone,
    string? WebsiteUrl);

public record OtpSendResult(Guid ChallengeId, string MaskedDestination, int ExpiresInSeconds, string? DevCode, string Via);
