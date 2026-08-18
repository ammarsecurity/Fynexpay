namespace Fynexpay.Application.Abstractions.Messaging;

public class WhatsAppTemplate
{
    public string Key { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public static class WhatsAppTemplateKeys
{
    public const string OtpRegister = "otp.register";
    public const string OtpCheckout = "otp.checkout";
    public const string OtpReset = "otp.reset";
    public const string OtpProfile = "otp.profile";
    public const string NotifyGeneric = "notify.generic";

    public static string ForNotification(string type)
        => string.IsNullOrWhiteSpace(type) ? NotifyGeneric : "notify." + type.Trim();
}

public static class WhatsAppTemplates
{
    public static readonly IReadOnlyList<string> AllKeys =
    [
        WhatsAppTemplateKeys.OtpRegister,
        WhatsAppTemplateKeys.OtpCheckout,
        WhatsAppTemplateKeys.OtpReset,
        WhatsAppTemplateKeys.OtpProfile,
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.MerchantActivated),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.MerchantSuspended),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.MerchantRejected),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.PaymentPaid),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.PayoutApproved),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.PayoutCompleted),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.PayoutRejected),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.PlatformApproved),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.PlatformRejected),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.PlatformSuspended),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.KycApproved),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.KycRejected),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.MerchantRegistered),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.PlatformSubmitted),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.PayoutRequested),
        WhatsAppTemplateKeys.ForNotification(NotificationTypes.KycSubmitted),
        WhatsAppTemplateKeys.NotifyGeneric
    ];

    public static WhatsAppTemplate Resolve(UltramsgSettings settings, string key)
    {
        var match = settings.Templates?.FirstOrDefault(t =>
            string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase));
        if (match != null && !string.IsNullOrWhiteSpace(match.Body))
            return match;

        if (!string.Equals(key, WhatsAppTemplateKeys.NotifyGeneric, StringComparison.OrdinalIgnoreCase))
        {
            var generic = settings.Templates?.FirstOrDefault(t =>
                string.Equals(t.Key, WhatsAppTemplateKeys.NotifyGeneric, StringComparison.OrdinalIgnoreCase));
            if (generic != null && !string.IsNullOrWhiteSpace(generic.Body))
                return generic;
        }

        return new WhatsAppTemplate { Key = key, Body = DefaultBody(key) };
    }

    public static string Render(string template, IReadOnlyDictionary<string, string?> vars)
    {
        var body = template ?? "";
        foreach (var (name, value) in vars)
            body = body.Replace("{" + name + "}", value ?? "", StringComparison.Ordinal);
        return body.Trim();
    }

    public static void EnsureDefaults(UltramsgSettings s)
    {
        s.Templates ??= [];
        var byKey = s.Templates
            .Where(t => !string.IsNullOrWhiteSpace(t.Key))
            .GroupBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        SeedFromLegacy(s, byKey);

        var next = new List<WhatsAppTemplate>();
        foreach (var key in AllKeys)
        {
            if (byKey.TryGetValue(key, out var existing))
            {
                if (string.IsNullOrWhiteSpace(existing.Body) || IsLegacyBody(existing.Body))
                    existing.Body = DefaultBody(key);
                existing.Key = key;
                next.Add(existing);
            }
            else
            {
                next.Add(new WhatsAppTemplate { Key = key, Body = DefaultBody(key) });
            }
        }

        s.Templates = next;
        SyncLegacyFields(s);
    }

    public static string DefaultBody(string key) => key switch
    {
        WhatsAppTemplateKeys.OtpRegister =>
            "*FynexPay*\n\nرمز التحقق لتسجيل حسابك:\n\n*{code}*\n\nصالح لمدة 5 دقائق فقط.\nلا تشارك هذا الرمز مع أي شخص.\n\n— فريق FynexPay",
        WhatsAppTemplateKeys.OtpCheckout =>
            "*FynexPay*\n\nرمز تأكيد عملية الدفع:\n\n*{code}*\n\nصالح لمدة 5 دقائق.\nلا تشاركه مع أحد.\n\n— فريق FynexPay",
        WhatsAppTemplateKeys.OtpReset =>
            "*FynexPay*\n\nرمز استعادة كلمة المرور:\n\n*{code}*\n\nصالح لمدة 5 دقائق فقط.\nلا تشارك هذا الرمز مع أي شخص.\n\n— فريق FynexPay",
        WhatsAppTemplateKeys.OtpProfile =>
            "*FynexPay*\n\nرمز تأكيد تعديل بيانات الحساب:\n\n*{code}*\n\nصالح لمدة 5 دقائق.\nلا تشاركه مع أحد.\n\n— فريق FynexPay",
        _ when key == WhatsAppTemplateKeys.ForNotification(NotificationTypes.PaymentPaid) =>
            "*FynexPay*\n\n*{title}*\n\n{body}\n\nيمكنك مراجعة التفاصيل من لوحة التاجر.\n\n— فريق FynexPay",
        _ when key == WhatsAppTemplateKeys.ForNotification(NotificationTypes.MerchantActivated) =>
            "*FynexPay*\n\n*{title}*\n\n{body}\n\nأهلاً بك في المنصة — يمكنك الآن قبول المدفوعات.\n\n— فريق FynexPay",
        _ when key == WhatsAppTemplateKeys.ForNotification(NotificationTypes.MerchantSuspended) =>
            "*FynexPay*\n\n*{title}*\n\n{body}\n\nللتوضيح أو إعادة التفعيل تواصل مع الدعم.\n\n— فريق FynexPay",
        _ =>
            "*FynexPay*\n\n*{title}*\n\n{body}\n\n— فريق FynexPay"
    };

    private static void SeedFromLegacy(UltramsgSettings s, Dictionary<string, WhatsAppTemplate> byKey)
    {
        void Seed(string key, string? legacy)
        {
            if (string.IsNullOrWhiteSpace(legacy) || IsLegacyBody(legacy)) return;
            if (byKey.ContainsKey(key)) return;
            byKey[key] = new WhatsAppTemplate { Key = key, Body = legacy };
        }

        Seed(WhatsAppTemplateKeys.OtpRegister, s.MerchantRegisterMessage);
        Seed(WhatsAppTemplateKeys.OtpCheckout, s.CheckoutMessage);
        Seed(WhatsAppTemplateKeys.OtpReset, s.PasswordResetMessage);
        Seed(WhatsAppTemplateKeys.OtpProfile, s.ProfileChangeMessage);
    }

    private static void SyncLegacyFields(UltramsgSettings s)
    {
        s.MerchantRegisterMessage = BodyOf(s, WhatsAppTemplateKeys.OtpRegister);
        s.CheckoutMessage = BodyOf(s, WhatsAppTemplateKeys.OtpCheckout);
        s.PasswordResetMessage = BodyOf(s, WhatsAppTemplateKeys.OtpReset);
        s.ProfileChangeMessage = BodyOf(s, WhatsAppTemplateKeys.OtpProfile);
    }

    private static string BodyOf(UltramsgSettings s, string key)
        => s.Templates.First(t => t.Key == key).Body;

    private static bool IsLegacyBody(string body)
    {
        var t = body.Replace("\r\n", "\n").Trim();
        return t is
            "رمز التحقق من Fynexpay: {code}\nصالح لمدة 5 دقائق. لا تشاركه مع أحد."
            or "رمز تأكيد الدفع عبر Fynexpay: {code}\nصالح لمدة 5 دقائق."
            or "رمز تأكيد تعديل الملف الشخصي في Fynexpay: {code}\nصالح لمدة 5 دقائق. لا تشاركه مع أحد."
            or "رمز استعادة كلمة المرور في Fynexpay: {code}\nصالح لمدة 5 دقائق. لا تشاركه مع أحد.";
    }
}
