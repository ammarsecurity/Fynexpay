using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Abstractions.Messaging;
using Fynexpay.Application.Abstractions.Payments;
using Fynexpay.Application.DTOs;
using Fynexpay.Application.Security;
using Fynexpay.Domain.Entities;
using Fynexpay.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fynexpay.Application.Services;

public class AuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly OtpService _otp;
    private readonly IUltramsgSettingsService _ultramsgSettings;
    private readonly NotificationService _notifications;

    public AuthService(
        IAppDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwt,
        OtpService otp,
        IUltramsgSettingsService ultramsgSettings,
        NotificationService notifications)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _otp = otp;
        _ultramsgSettings = ultramsgSettings;
        _notifications = notifications;
    }

    public async Task<AuthPolicyDto> GetRegisterPolicyAsync(CancellationToken ct = default)
    {
        var s = await _ultramsgSettings.GetAsync(ct);
        var required = s.Enabled && s.RequireMerchantRegisterOtp && (s.UsesWhatsApp() || s.UsesEmail());
        return new AuthPolicyDto(required, s.UsesWhatsApp(), s.UsesEmail(), s.Channel);
    }

    public async Task<OtpSendResultDto> SendRegisterOtpAsync(RegisterMerchantRequest request, CancellationToken ct = default)
    {
        var s = await _ultramsgSettings.GetAsync(ct);
        if (!s.Enabled || !s.RequireMerchantRegisterOtp)
            throw new InvalidOperationException("تأكيد OTP للتسجيل غير مطلوب حالياً");

        var result = await _otp.SendMerchantRegisterOtpAsync(request, ct);
        return new OtpSendResultDto(result.ChallengeId, result.MaskedDestination, result.ExpiresInSeconds, result.DevCode, result.Via);
    }

    public async Task<AuthResponse> RegisterMerchantAsync(RegisterMerchantRequest request, CancellationToken ct = default)
    {
        var s = await _ultramsgSettings.GetAsync(ct);
        if (s.Enabled && s.RequireMerchantRegisterOtp && (s.UsesWhatsApp() || s.UsesEmail()))
            throw new InvalidOperationException("يجب تأكيد رمز التحقق أولاً عبر /api/auth/register/send-otp ثم verify");

        return await CreateMerchantAccountAsync(
            request.Email,
            request.Password,
            request.FullName,
            request.FullNameAr,
            request.BusinessName,
            request.BusinessNameAr,
            request.ContactPhone,
            request.WebsiteUrl,
            ct);
    }

    public async Task<AuthResponse> VerifyRegisterOtpAsync(VerifyRegisterOtpRequest request, CancellationToken ct = default)
    {
        await _otp.ConsumeMerchantRegisterChallengeAsync(request.ChallengeId, request.Code, ct);
        var pending = await _otp.GetPendingRegistrationAsync(request.ChallengeId, ct);
        var auth = await CreateMerchantAccountAsync(
            pending.Email,
            pending.Password,
            pending.FullName,
            pending.FullNameAr,
            pending.BusinessName,
            pending.BusinessNameAr,
            pending.ContactPhone,
            pending.WebsiteUrl,
            ct);
        await _otp.InvalidateChallengeAsync(request.ChallengeId, ct);
        return auth;
    }

    private async Task<AuthResponse> CreateMerchantAccountAsync(
        string emailRaw,
        string password,
        string fullName,
        string fullNameAr,
        string businessName,
        string? businessNameAr,
        string? contactPhone,
        string? websiteUrl,
        CancellationToken ct)
    {
        PasswordRules.ValidateEmail(emailRaw);
        PasswordRules.Validate(password);
        PasswordRules.ValidateRequired(fullName, "الاسم الكامل بالإنجليزية");
        PasswordRules.ValidateRequired(fullNameAr, "الاسم الكامل بالعربية");
        PasswordRules.ValidateRequired(businessName, "اسم النشاط");

        var email = emailRaw.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("البريد الإلكتروني مستخدم مسبقاً");

        var merchant = new Merchant
        {
            BusinessName = businessName.Trim(),
            BusinessNameAr = string.IsNullOrWhiteSpace(businessNameAr) ? null : businessNameAr.Trim(),
            ContactEmail = email,
            ContactPhone = contactPhone?.Trim(),
            WebsiteUrl = websiteUrl?.Trim(),
            Status = MerchantStatus.Pending,
            CommissionPercent = 2.5m,
            FibCommissionPercent = 2.5m,
            ZainCashCommissionPercent = 2.5m,
            QiCommissionPercent = 2.5m,
            SuperQiCommissionPercent = 2.5m,
            AlqasehCommissionPercent = 2.5m,
            WebhookSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant()
        };

        var wallet = new Wallet { Merchant = merchant, Currency = "IQD" };
        var user = new User
        {
            Email = email,
            FullName = fullName.Trim(),
            FullNameAr = fullNameAr.Trim(),
            Phone = contactPhone?.Trim(),
            PasswordHash = _passwordHasher.Hash(password),
            Role = UserRole.MerchantOwner,
            Merchant = merchant
        };

        _db.Merchants.Add(merchant);
        _db.Wallets.Add(wallet);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAdminsSafeAsync(
            NotificationTypes.MerchantRegistered,
            "تاجر جديد بانتظار التفعيل",
            $"سجّل التاجر «{merchant.BusinessName}» ({email}) وهو بانتظار موافقة الإدارة.",
            "/admin/merchants",
            merchant.Id,
            new { merchantId = merchant.Id, businessName = merchant.BusinessName, email },
            ct);

        return IssueAuth(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var s = await _ultramsgSettings.GetAsync(ct);
        if (LoginOtpRequired(s))
            throw new InvalidOperationException("يجب تأكيد رمز التحقق أولاً عبر /api/auth/login/send-otp ثم verify");

        var user = await AuthenticateForLoginAsync(request, ct);
        return IssueAuth(user);
    }

    public async Task<OtpSendResultDto> SendLoginOtpAsync(LoginRequest request, CancellationToken ct = default)
    {
        var s = await _ultramsgSettings.GetAsync(ct);
        if (!LoginOtpRequired(s))
            throw new InvalidOperationException("تأكيد OTP لتسجيل الدخول غير مطلوب حالياً");

        var user = await AuthenticateForLoginAsync(request, ct);
        var result = await _otp.SendLoginOtpAsync(user, ct);
        return new OtpSendResultDto(result.ChallengeId, result.MaskedDestination, result.ExpiresInSeconds, result.DevCode, result.Via);
    }

    public async Task<AuthResponse> VerifyLoginOtpAsync(VerifyLoginOtpRequest request, CancellationToken ct = default)
    {
        var pending = await _otp.ConsumeLoginChallengeAsync(request.ChallengeId, request.Code, ct);
        var user = await _db.Users.Include(u => u.Merchant)
            .FirstOrDefaultAsync(u => u.Id == pending.UserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود");

        if (!user.IsActive)
            throw new InvalidOperationException("الحساب غير نشط");
        if (user.Merchant is { Status: MerchantStatus.Suspended or MerchantStatus.Rejected })
            throw new UnauthorizedAccessException("حساب التاجر موقوف أو مرفوض");

        await _otp.InvalidateChallengeAsync(request.ChallengeId, ct);
        return IssueAuth(user);
    }

    private async Task<User> AuthenticateForLoginAsync(LoginRequest request, CancellationToken ct)
    {
        PasswordRules.ValidateEmail(request.Email);
        var user = await _db.Users.Include(u => u.Merchant)
            .FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException("بيانات الدخول غير صحيحة");

        if (!user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("بيانات الدخول غير صحيحة");

        if (user.Merchant is { Status: MerchantStatus.Suspended or MerchantStatus.Rejected })
            throw new UnauthorizedAccessException("حساب التاجر موقوف أو مرفوض");

        return user;
    }

    private AuthResponse IssueAuth(User user)
    {
        var pending = user.Merchant is { Status: not MerchantStatus.Active };
        var token = pending
            ? ""
            : _jwt.CreateToken(user.Id, user.Email, user.Role.ToString(), user.MerchantId, user.FullName);
        return new AuthResponse(
            token,
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.MerchantId,
            user.Merchant?.Status.ToString(),
            pending);
    }

    private static bool LoginOtpRequired(UltramsgSettings s)
        => s.Enabled && s.RequireMerchantRegisterOtp && (s.UsesWhatsApp() || s.UsesEmail());

    public async Task<OtpSendResultDto> SendForgotPasswordOtpAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        PasswordRules.ValidateRequired(request.Phone, "رقم الهاتف");
        var result = await _otp.SendPasswordResetOtpAsync(request.Phone, ct);
        return new OtpSendResultDto(result.ChallengeId, result.MaskedDestination, result.ExpiresInSeconds, result.DevCode, result.Via);
    }

    public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        PasswordRules.Validate(request.NewPassword);
        var pending = await _otp.ConsumePasswordResetChallengeAsync(request.ChallengeId, request.Code, ct);
        var user = await _db.Users.Include(u => u.Merchant)
            .FirstOrDefaultAsync(u => u.Id == pending.UserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود");

        if (!user.IsActive)
            throw new InvalidOperationException("الحساب غير نشط");
        if (user.Merchant is { Status: MerchantStatus.Suspended or MerchantStatus.Rejected })
            throw new UnauthorizedAccessException("حساب التاجر موقوف أو مرفوض");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _otp.InvalidateChallengeAsync(request.ChallengeId, ct);
        return IssueAuth(user);
    }
}

public class PaymentService
{
    private readonly IAppDbContext _db;
    private readonly IPaymentProviderResolver _resolver;
    private readonly IProviderSettingsService _providerSettings;
    private readonly IMerchantWebhookSender _webhookSender;
    private readonly NotificationService _notifications;
    private readonly ILogger<PaymentService> _logger;
    private readonly string _publicBaseUrl;

    public PaymentService(
        IAppDbContext db,
        IPaymentProviderResolver resolver,
        IProviderSettingsService providerSettings,
        IMerchantWebhookSender webhookSender,
        NotificationService notifications,
        ILogger<PaymentService> logger,
        Microsoft.Extensions.Options.IOptions<AppOptions> options)
    {
        _db = db;
        _resolver = resolver;
        _providerSettings = providerSettings;
        _webhookSender = webhookSender;
        _notifications = notifications;
        _logger = logger;
        _publicBaseUrl = options.Value.PublicBaseUrl.TrimEnd('/');
    }

    public async Task<PaymentDto> CreateAsync(
        Guid merchantId,
        CreatePaymentRequest request,
        string? idempotencyKey,
        CancellationToken ct = default,
        Guid? merchantPlatformId = null,
        bool isTest = false)
    {
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");

        if (merchant.Status != MerchantStatus.Active)
            throw new InvalidOperationException("حساب التاجر غير مفعّل");

        string? platformDomain = null;
        if (merchantPlatformId.HasValue)
        {
            var platform = await _db.MerchantPlatforms.FirstOrDefaultAsync(
                p => p.Id == merchantPlatformId.Value && p.MerchantId == merchantId, ct)
                ?? throw new InvalidOperationException("المنصة غير موجودة");
            if (platform.Status != PlatformStatus.Approved)
                throw new InvalidOperationException("المنصة غير معتمدة");
            platformDomain = platform.Domain;
            UrlSafety.ValidateMerchantUrls(request.SuccessUrl, request.FailureUrl, request.CallbackUrl, platform.Domain);
        }
        else if (!string.IsNullOrWhiteSpace(request.SuccessUrl)
                 || !string.IsNullOrWhiteSpace(request.FailureUrl)
                 || !string.IsNullOrWhiteSpace(request.CallbackUrl))
        {
            throw new ArgumentException("روابط النجاح/الفشل/الإشعار تتطلب منصة معتمدة مربوطة بالمفتاح");
        }

        if (request.Amount < 250)
            throw new ArgumentException("الحد الأدنى للمبلغ 250 دينار");

        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "IQD" : request.Currency.ToUpperInvariant();
        if (currency != "IQD")
            throw new ArgumentException("العملة المدعومة حالياً IQD فقط");

        var serviceType = FirstNonEmpty(request.ServiceType, request.Description);
        if (string.IsNullOrWhiteSpace(serviceType))
            throw new ArgumentException("نوع الخدمة مطلوب");

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await _db.Payments.FirstOrDefaultAsync(
                p => p.MerchantId == merchantId && p.IdempotencyKey == idempotencyKey, ct);
            if (existing != null)
                return await MapAsync(existing, ct);
        }

        var available = await GetEffectiveProvidersAsync(merchant, ct);
        if (available.Count == 0)
            throw new InvalidOperationException("لا يوجد مزود دفع مفعّل لهذا التاجر");

        var fee = Math.Round(request.Amount * merchant.CommissionPercent / 100m, 0, MidpointRounding.AwayFromZero);
        var net = request.Amount - fee;
        var orderId = string.IsNullOrWhiteSpace(request.OrderId)
            ? $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}"
            : request.OrderId.Trim();

        var payment = new Payment
        {
            MerchantId = merchantId,
            MerchantPlatformId = merchantPlatformId,
            MerchantOrderId = orderId,
            Amount = request.Amount,
            Currency = currency,
            Description = serviceType.Trim(),
            Status = PaymentStatus.Pending,
            Provider = PaymentProviderType.Auto, // يختاره الزبون لاحقاً في صفحة الدفع
            SuccessUrl = request.SuccessUrl?.Trim(),
            FailureUrl = request.FailureUrl?.Trim(),
            CallbackUrl = request.CallbackUrl?.Trim(),
            IdempotencyKey = idempotencyKey,
            // مؤقت حتى اختيار المزود — يُعاد الحساب في Initiate حسب عمولة ذلك المزود
            PlatformFee = fee,
            NetAmount = net,
            ExpiredAtUtc = DateTime.UtcNow.AddHours(1),
            CustomerPhone = string.IsNullOrWhiteSpace(request.CustomerPhone) ? null : request.CustomerPhone.Trim(),
            IsTest = isTest
        };

        _db.Payments.Add(payment);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var raced = await _db.Payments.FirstOrDefaultAsync(
                p => p.MerchantId == merchantId && p.IdempotencyKey == idempotencyKey, ct);
            if (raced != null)
                return await MapAsync(raced, ct);
            throw;
        }

        _ = platformDomain; // validated above
        payment.CheckoutUrl = $"{_publicBaseUrl}/checkout/{payment.Id}";
        payment.UpdatedAtUtc = DateTime.UtcNow;

        _db.PaymentEvents.Add(new PaymentEvent
        {
            PaymentId = payment.Id,
            Source = "Fynexpay",
            EventType = "CheckoutCreated",
            Payload = JsonSerializer.Serialize(new
            {
                availableProviders = available.Select(p => p.ToString()).ToArray(),
                serviceType = payment.Description,
                mode = payment.IsTest ? "test" : "live",
                providerEnvironment = payment.IsTest ? "Test" : "Production"
            })
        });

        await _db.SaveChangesAsync(ct);
        return await MapAsync(payment, available, ct);
    }

    public async Task<PaymentDto> InitiateAsync(Guid paymentId, string providerName, CancellationToken ct = default)
    {
        var payment = await _db.Payments.Include(p => p.Merchant)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct)
            ?? throw new InvalidOperationException("الدفعة غير موجودة");

        if (payment.Status != PaymentStatus.Pending)
            throw new InvalidOperationException("لا يمكن متابعة هذه الدفعة");

        if (await TryPurgeExpiredIncompleteCheckoutAsync(payment.Id, ct))
            throw new InvalidOperationException("انتهت صلاحية رابط الدفع وتم إغلاق الجلسة");

        // إعادة تحميل بعد فحص المهلة
        payment = await _db.Payments.Include(p => p.Merchant)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct)
            ?? throw new InvalidOperationException("انتهت صلاحية رابط الدفع وتم إغلاق الجلسة");

        // إذا سبق اختيار مزود، أعد توجيه الزبون لنفس الرابط
        if (payment.Provider != PaymentProviderType.Auto && !string.IsNullOrWhiteSpace(payment.ProviderCheckoutUrl))
            return await MapAsync(payment, ct);

        var providerType = EnumMaps.ParseProvider(providerName);
        if (providerType == PaymentProviderType.Auto)
            throw new ArgumentException("يجب اختيار مزود دفع");

        var available = await GetEffectiveProvidersAsync(payment.Merchant, ct);
        if (!available.Contains(providerType))
            throw new InvalidOperationException("هذا المزود غير متاح لهذه الدفعة");

        var provider = _resolver.Resolve(providerType);
        var callbackUrl = $"{_publicBaseUrl}/api/webhooks/{providerType.ToString().ToLowerInvariant()}";
        // المزود يرجع دائماً للمنصة أولاً، ثم المنصة تحوّل لرابط التاجر
        var platformSuccess = $"{_publicBaseUrl}/checkout/{payment.Id}/return?result=success";
        var platformFailure = $"{_publicBaseUrl}/checkout/{payment.Id}/return?result=failure";
        ProviderPaymentResult result;
        using (ProviderEnvironmentScope.Use(payment.IsTest))
        {
            result = await provider.CreatePaymentAsync(new CreateProviderPaymentRequest
            {
                PaymentId = payment.Id,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Description = payment.Description ?? $"Order {payment.MerchantOrderId}",
                StatusCallbackUrl = callbackUrl,
                SuccessUrl = platformSuccess,
                FailureUrl = platformFailure,
                MerchantOrderId = payment.MerchantOrderId
            }, ct);
        }

        if (!result.Success)
        {
            payment.FailureReason = result.ErrorMessage;
            payment.ProviderRawResponse = result.RawResponse;
            payment.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            throw new InvalidOperationException(result.ErrorMessage ?? "فشل إنشاء الدفعة لدى المزود");
        }

        ApplyProviderCommission(payment, payment.Merchant, providerType);

        payment.Provider = providerType;
        payment.ProviderPaymentId = result.ProviderPaymentId;
        payment.ProviderCheckoutUrl = result.CheckoutUrl;
        payment.CheckoutUrl = $"{_publicBaseUrl}/checkout/{payment.Id}";
        payment.QrCode = result.QrCode;
        payment.ReadableCode = result.ReadableCode;
        // لا نمدّد مهلة صفحة الدفع المستضافة عن ساعة الإنشاء
        if (result.ValidUntilUtc.HasValue && payment.ExpiredAtUtc.HasValue
            && result.ValidUntilUtc.Value < payment.ExpiredAtUtc.Value)
            payment.ExpiredAtUtc = result.ValidUntilUtc;
        payment.ProviderRawResponse = result.RawResponse;
        payment.UpdatedAtUtc = DateTime.UtcNow;
        payment.FailureReason = null;

        _db.PaymentEvents.Add(new PaymentEvent
        {
            PaymentId = payment.Id,
            Source = providerType.ToString(),
            EventType = "ProviderSelected",
            Payload = result.RawResponse ?? "{}"
        });

        await _db.SaveChangesAsync(ct);
        return await MapAsync(payment, available, ct);
    }

    public async Task<IReadOnlyList<PaymentProviderType>> GetAvailableProvidersForPaymentAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _db.Payments.Include(p => p.Merchant)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct)
            ?? throw new InvalidOperationException("الدفعة غير موجودة");
        return await GetEffectiveProvidersAsync(payment.Merchant, ct);
    }

    public async Task<MerchantPaymentMethodsDto> GetPaymentMethodsAsync(Guid merchantId, CancellationToken ct = default)
    {
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");
        var platform = await _resolver.GetEnabledProvidersAsync(ct);
        var effective = await GetEffectiveProvidersAsync(merchant, ct);
        var catalog = await BuildProviderCatalogAsync(ct);
        return new MerchantPaymentMethodsDto(
            merchant.FibEnabled,
            merchant.ZainCashEnabled,
            merchant.QiEnabled,
            merchant.SuperQiEnabled,
            merchant.AlqasehEnabled,
            platform.Select(p => p.ToString()).ToList(),
            effective.Select(p => p.ToString()).ToList(),
            catalog);
    }

    public async Task<IReadOnlyList<ProviderCatalogItemDto>> BuildProviderCatalogAsync(CancellationToken ct = default)
    {
        var s = await _providerSettings.GetAsync(ct);
        return new[]
        {
            Item(PaymentProviderType.Fib, s.Fib),
            Item(PaymentProviderType.ZainCash, s.ZainCash),
            Item(PaymentProviderType.Qi, s.Qi),
            Item(PaymentProviderType.SuperQi, s.SuperQi),
            Item(PaymentProviderType.Alqaseh, s.Alqaseh)
        };

        static ProviderCatalogItemDto Item(PaymentProviderType type, ProviderBundleSettings b)
        {
            var logo = string.IsNullOrWhiteSpace(b.LogoUrl)
                ? DefaultLogo(type)
                : b.LogoUrl;
            var name = b.ResolveDisplayName(DefaultName(type));
            return new(type.ToString(), name, logo, b.Enabled, b.Priority);
        }

        static string DefaultName(PaymentProviderType type) => type switch
        {
            PaymentProviderType.Fib => "FIB",
            PaymentProviderType.ZainCash => "ZainCash",
            PaymentProviderType.Qi => "QI Card",
            PaymentProviderType.SuperQi => "SuperQi",
            PaymentProviderType.Alqaseh => "Alqaseh",
            _ => type.ToString()
        };

        static string DefaultLogo(PaymentProviderType type) => type switch
        {
            PaymentProviderType.Fib => "/providers/fib.svg",
            PaymentProviderType.ZainCash => "/providers/zaincash.svg",
            PaymentProviderType.Qi => "/providers/qi.svg",
            PaymentProviderType.SuperQi => "/providers/superqi.svg",
            PaymentProviderType.Alqaseh => "/providers/alqaseh.svg",
            _ => ""
        };
    }

    public async Task<MerchantPaymentMethodsDto> UpdatePaymentMethodsAsync(
        Guid merchantId,
        UpdateMerchantPaymentMethodsRequest request,
        CancellationToken ct = default)
    {
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");

        if (request.FibEnabled.HasValue) merchant.FibEnabled = request.FibEnabled.Value;
        if (request.ZainCashEnabled.HasValue) merchant.ZainCashEnabled = request.ZainCashEnabled.Value;
        if (request.QiEnabled.HasValue) merchant.QiEnabled = request.QiEnabled.Value;
        if (request.SuperQiEnabled.HasValue) merchant.SuperQiEnabled = request.SuperQiEnabled.Value;
        if (request.AlqasehEnabled.HasValue) merchant.AlqasehEnabled = request.AlqasehEnabled.Value;

        if (!merchant.FibEnabled && !merchant.ZainCashEnabled && !merchant.QiEnabled
            && !merchant.SuperQiEnabled && !merchant.AlqasehEnabled)
            throw new ArgumentException("يجب تفعيل مزود دفع واحد على الأقل");

        merchant.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetPaymentMethodsAsync(merchantId, ct);
    }

    private async Task<IReadOnlyList<PaymentProviderType>> GetEffectiveProvidersAsync(Merchant merchant, CancellationToken ct)
    {
        var platform = await _resolver.GetEnabledProvidersAsync(ct);
        return platform.Where(p => p switch
        {
            PaymentProviderType.Fib => merchant.FibEnabled,
            PaymentProviderType.ZainCash => merchant.ZainCashEnabled,
            PaymentProviderType.Qi => merchant.QiEnabled,
            PaymentProviderType.SuperQi => merchant.SuperQiEnabled,
            PaymentProviderType.Alqaseh => merchant.AlqasehEnabled,
            _ => false
        }).ToList();
    }

    /// <summary>
    /// جلسة الدفع المستضافة صالحة ساعة واحدة. إن انتهت دون دفع ناجح تُحذف الدفعة وبياناتها.
    /// </summary>
    /// <returns>true إذا حُذفت الجلسة لانتهاء المهلة.</returns>
    public async Task<bool> TryPurgeExpiredIncompleteCheckoutAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment == null)
            return true; // تعتبر منتهية/غير موجودة

        if (payment.Status is not PaymentStatus.Pending and not PaymentStatus.Expired)
            return false;

        var deadline = AsUtc(payment.ExpiredAtUtc ?? payment.CreatedAtUtc.AddHours(1));
        if (deadline >= DateTime.UtcNow)
            return false;

        await DeletePaymentGraphAsync(payment, ct);
        return true;
    }

    /// <summary>
    /// يحذف كل دفعات Pending/Expired التي تجاوزت مهلة الساعة دون اكتمال الدفع.
    /// </summary>
    public async Task<int> PurgeExpiredIncompleteCheckoutsAsync(CancellationToken ct = default, int take = 200)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(-1);

        var expired = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Expired)
            .Where(p =>
                (p.ExpiredAtUtc != null && p.ExpiredAtUtc < now)
                || (p.ExpiredAtUtc == null && p.CreatedAtUtc < cutoff))
            .OrderBy(p => p.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(ct);

        if (expired.Count == 0)
            return 0;

        // Double-check Kind/UTC wall-clock for rows that EF may treat as Unspecified.
        var toDelete = expired
            .Where(p => AsUtc(p.ExpiredAtUtc ?? p.CreatedAtUtc.AddHours(1)) < now)
            .ToList();
        if (toDelete.Count == 0)
            return 0;

        var ids = toDelete.Select(p => p.Id).ToList();

        var events = await _db.PaymentEvents.Where(e => ids.Contains(e.PaymentId)).ToListAsync(ct);
        if (events.Count > 0)
            _db.PaymentEvents.RemoveRange(events);

        var otps = await _db.OtpChallenges.Where(o => o.PaymentId != null && ids.Contains(o.PaymentId.Value)).ToListAsync(ct);
        if (otps.Count > 0)
            _db.OtpChallenges.RemoveRange(otps);

        var ledger = await _db.WalletLedgerEntries.Where(l => l.PaymentId != null && ids.Contains(l.PaymentId.Value)).ToListAsync(ct);
        if (ledger.Count > 0)
            _db.WalletLedgerEntries.RemoveRange(ledger);

        _db.Payments.RemoveRange(toDelete);
        await _db.SaveChangesAsync(ct);
        return toDelete.Count;
    }

    private async Task DeletePaymentGraphAsync(Payment payment, CancellationToken ct)
    {
        var paymentId = payment.Id;
        var events = await _db.PaymentEvents.Where(e => e.PaymentId == paymentId).ToListAsync(ct);
        if (events.Count > 0)
            _db.PaymentEvents.RemoveRange(events);

        var otps = await _db.OtpChallenges.Where(o => o.PaymentId == paymentId).ToListAsync(ct);
        if (otps.Count > 0)
            _db.OtpChallenges.RemoveRange(otps);

        var ledger = await _db.WalletLedgerEntries.Where(l => l.PaymentId == paymentId).ToListAsync(ct);
        if (ledger.Count > 0)
            _db.WalletLedgerEntries.RemoveRange(ledger);

        _db.Payments.Remove(payment);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<DateTime?> GetCheckoutDeadlineUtcAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment == null) return null;
        return AsUtc(payment.ExpiredAtUtc ?? payment.CreatedAtUtc.AddHours(1));
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static void ApplyProviderCommission(Payment payment, Merchant merchant, PaymentProviderType provider)
    {
        var rate = ResolveCommissionPercent(merchant, provider);
        var fee = Math.Round(payment.Amount * rate / 100m, 0, MidpointRounding.AwayFromZero);
        if (fee < 0) fee = 0;
        if (fee > payment.Amount) fee = payment.Amount;
        payment.PlatformFee = fee;
        payment.NetAmount = payment.Amount - fee;
    }

    public static decimal ResolveCommissionPercent(Merchant merchant, PaymentProviderType provider) => provider switch
    {
        PaymentProviderType.Fib => merchant.FibCommissionPercent,
        PaymentProviderType.ZainCash => merchant.ZainCashCommissionPercent,
        PaymentProviderType.Qi => merchant.QiCommissionPercent,
        PaymentProviderType.SuperQi => merchant.SuperQiCommissionPercent,
        PaymentProviderType.Alqaseh => merchant.AlqasehCommissionPercent,
        _ => merchant.CommissionPercent
    };

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    public async Task<PaymentDto?> GetAsync(Guid merchantId, Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId && p.MerchantId == merchantId, ct);
        return payment == null ? null : await MapAsync(payment, ct);
    }

    public async Task<PaymentDto?> CancelAsync(Guid merchantId, Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId && p.MerchantId == merchantId, ct)
            ?? throw new InvalidOperationException("الدفعة غير موجودة");

        if (payment.Status != PaymentStatus.Pending)
            throw new InvalidOperationException("لا يمكن إلغاء هذه الدفعة");

        if (!string.IsNullOrEmpty(payment.ProviderPaymentId))
        {
            var provider = _resolver.Resolve(payment.Provider);
            using (ProviderEnvironmentScope.Use(payment.IsTest))
                await provider.CancelAsync(payment.ProviderPaymentId, ct);
        }

        payment.Status = PaymentStatus.Cancelled;
        payment.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await MapAsync(payment, ct);
    }

    public async Task ApplyProviderStatusAsync(Guid paymentId, PaymentStatus status, string source, string payload, string? failureReason = null, CancellationToken ct = default)
    {
        await using var tx = await _db.BeginTransactionAsync(ct);

        var payment = await _db.Payments.Include(p => p.Merchant).ThenInclude(m => m!.Wallet)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment == null)
        {
            await tx.RollbackAsync(ct);
            return;
        }

        _db.PaymentEvents.Add(new PaymentEvent
        {
            PaymentId = payment.Id,
            Source = source,
            EventType = status.ToString(),
            Payload = payload
        });

        if (!TryTransitionPaymentStatus(payment.Status, status, out var effective))
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return;
        }

        var previous = payment.Status;
        payment.Status = effective;
        payment.UpdatedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(failureReason))
            payment.FailureReason = failureReason;

        if (effective == PaymentStatus.Paid && !payment.LedgerApplied)
        {
            payment.PaidAtUtc = DateTime.UtcNow;
            // Sandbox / dashboard test payments never credit the live merchant wallet.
            if (!payment.IsTest)
                await CreditWalletAsync(payment, ct);
            payment.LedgerApplied = true;
        }
        else if (previous == PaymentStatus.Paid
                 && effective == PaymentStatus.Refunded
                 && payment.LedgerApplied
                 && !payment.RefundLedgerApplied)
        {
            if (!payment.IsTest)
                await DebitWalletForRefundAsync(payment, ct);
            payment.RefundLedgerApplied = true;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        if (effective == PaymentStatus.Paid && previous != PaymentStatus.Paid)
        {
            await _notifications.NotifyMerchantUsersSafeAsync(
                payment.MerchantId,
                NotificationTypes.PaymentPaid,
                "تم استلام دفعة جديدة",
                $"دفعة بمبلغ {payment.Amount:N0} {payment.Currency} عبر {payment.Provider}.",
                "/merchant/payments",
                new { paymentId = payment.Id, amount = payment.Amount, currency = payment.Currency, provider = payment.Provider.ToString() },
                ct);
        }

        try
        {
            await _webhookSender.SendPaymentUpdateAsync(payment.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send merchant webhook for payment {PaymentId}", payment.Id);
        }
    }

    /// <summary>
    /// One-way money-safe transitions. Paid cannot move to Failed/Cancelled; only Refunded is allowed from Paid.
    /// </summary>
    private static bool TryTransitionPaymentStatus(PaymentStatus current, PaymentStatus incoming, out PaymentStatus effective)
    {
        effective = current;

        if (current == incoming)
            return false;

        if (current == PaymentStatus.Refunded)
            return false;

        if (current == PaymentStatus.Paid)
        {
            if (incoming == PaymentStatus.Refunded)
            {
                effective = PaymentStatus.Refunded;
                return true;
            }
            // Ignore downgrades from Paid (forged/late Failed webhooks).
            return false;
        }

        // Terminal non-paid states are sticky.
        if (current is PaymentStatus.Failed or PaymentStatus.Declined or PaymentStatus.Expired or PaymentStatus.Cancelled)
            return false;

        // Pending → any terminal / paid
        if (current == PaymentStatus.Pending)
        {
            effective = incoming;
            return true;
        }

        return false;
    }

    public async Task ApplyByProviderPaymentIdAsync(string providerPaymentId, PaymentProviderType provider, PaymentStatus status, string payload, string? failureReason = null, CancellationToken ct = default)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.ProviderPaymentId == providerPaymentId && p.Provider == provider, ct);
        if (payment == null) return;
        await ApplyProviderStatusAsync(payment.Id, status, provider.ToString(), payload, failureReason, ct);
    }

    public async Task SyncFromProviderAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, ct)
            ?? throw new InvalidOperationException("الدفعة غير موجودة");

        if (payment.Status != PaymentStatus.Pending)
            return;
        if (payment.Provider == PaymentProviderType.Auto || string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
            return;

        var provider = _resolver.Resolve(payment.Provider);
        ProviderStatusResult status;
        using (ProviderEnvironmentScope.Use(payment.IsTest))
            status = await provider.GetStatusAsync(payment.ProviderPaymentId, ct);
        if (status.Status == PaymentStatus.Pending)
            return;

        await ApplyProviderStatusAsync(
            payment.Id,
            status.Status,
            payment.Provider.ToString(),
            status.RawResponse ?? "{}",
            status.FailureReason,
            ct);
    }

    private async Task CreditWalletAsync(Payment payment, CancellationToken ct)
    {
        var wallet = await _db.Wallets.FirstAsync(w => w.MerchantId == payment.MerchantId, ct);
        wallet.AvailableBalance += payment.NetAmount;
        wallet.LifetimeGross += payment.Amount;
        wallet.LifetimeFees += payment.PlatformFee;
        wallet.UpdatedAtUtc = DateTime.UtcNow;

        var paymentRef = payment.Id.ToString("N")[..8].ToUpperInvariant();
        _db.WalletLedgerEntries.Add(new WalletLedgerEntry
        {
            WalletId = wallet.Id,
            Type = LedgerEntryType.PaymentCredit,
            Amount = payment.NetAmount,
            BalanceAfter = wallet.AvailableBalance,
            Description = $"صافي دفعة #{paymentRef}",
            PaymentId = payment.Id
        });

        _db.WalletLedgerEntries.Add(new WalletLedgerEntry
        {
            WalletId = wallet.Id,
            Type = LedgerEntryType.PlatformFee,
            Amount = -payment.PlatformFee,
            BalanceAfter = wallet.AvailableBalance,
            Description = $"عمولة المنصة #{paymentRef}",
            PaymentId = payment.Id
        });
    }

    private async Task DebitWalletForRefundAsync(Payment payment, CancellationToken ct)
    {
        var wallet = await _db.Wallets.FirstAsync(w => w.MerchantId == payment.MerchantId, ct);
        wallet.AvailableBalance -= payment.NetAmount;
        wallet.LifetimeGross -= payment.Amount;
        wallet.LifetimeFees -= payment.PlatformFee;
        wallet.UpdatedAtUtc = DateTime.UtcNow;

        var paymentRef = payment.Id.ToString("N")[..8].ToUpperInvariant();
        _db.WalletLedgerEntries.Add(new WalletLedgerEntry
        {
            WalletId = wallet.Id,
            Type = LedgerEntryType.PaymentRefund,
            Amount = -payment.NetAmount,
            BalanceAfter = wallet.AvailableBalance,
            Description = $"استرجاع دفعة #{paymentRef}",
            PaymentId = payment.Id
        });
    }

    public PaymentDto Map(Payment p, IReadOnlyList<string>? availableProviders = null, bool includeEvents = false)
    {
        var checkout = string.IsNullOrWhiteSpace(p.CheckoutUrl) ? null : p.CheckoutUrl;
        var returnUrl = $"{_publicBaseUrl}/checkout/{p.Id:D}/return";
        IReadOnlyList<PaymentEventDto>? events = null;
        if (includeEvents && p.Events != null)
        {
            events = p.Events
                .OrderByDescending(e => e.CreatedAtUtc)
                .Select(e => new PaymentEventDto(e.Id, e.Source, e.EventType, e.Payload, e.CreatedAtUtc))
                .ToList();
        }

        return new PaymentDto(
            p.Id,
            p.MerchantId,
            p.MerchantPlatformId,
            p.Merchant?.BusinessName,
            p.MerchantOrderId,
            p.Amount,
            p.Currency,
            p.Status.ToString(),
            p.Provider == PaymentProviderType.Auto ? "PendingSelection" : p.Provider.ToString(),
            p.Description,
            checkout,
            p.ProviderCheckoutUrl,
            returnUrl,
            p.QrCode,
            p.ReadableCode,
            p.SuccessUrl,
            p.FailureUrl,
            p.CallbackUrl,
            p.ProviderPaymentId,
            p.IdempotencyKey,
            p.PlatformFee,
            p.NetAmount,
            p.LedgerApplied,
            p.IsTest,
            p.CreatedAtUtc,
            p.UpdatedAtUtc,
            p.PaidAtUtc,
            p.ExpiredAtUtc,
            p.FailureReason,
            p.ProviderRawResponse,
            availableProviders,
            events,
            p.CustomerPhone,
            p.CustomerEmail,
            p.CustomerPhoneVerifiedAtUtc);
    }

    public PublicPaymentDto MapPublic(Payment p) =>
        new(
            p.Id,
            p.MerchantOrderId,
            p.Amount,
            p.Currency,
            p.Status.ToString(),
            p.Provider == PaymentProviderType.Auto ? "PendingSelection" : p.Provider.ToString(),
            p.Description,
            string.IsNullOrWhiteSpace(p.CheckoutUrl) ? null : p.CheckoutUrl,
            p.IsTest ? "test" : "live",
            p.CreatedAtUtc,
            p.PaidAtUtc,
            p.ExpiredAtUtc,
            p.FailureReason,
            p.CustomerPhone,
            p.CustomerPhoneVerifiedAtUtc != null);

    public PublicPaymentDto ToPublic(PaymentDto dto) =>
        new(
            dto.Id,
            dto.OrderId,
            dto.Amount,
            dto.Currency,
            dto.Status,
            dto.Provider,
            dto.Description,
            dto.CheckoutUrl,
            dto.IsTest ? "test" : "live",
            dto.CreatedAtUtc,
            dto.PaidAtUtc,
            dto.ExpiredAtUtc,
            dto.FailureReason,
            dto.CustomerPhone,
            dto.CustomerPhoneVerifiedAtUtc != null);

    public async Task<PaymentDto?> GetDetailAsync(Guid paymentId, Guid? merchantId = null, CancellationToken ct = default)
    {
        var q = _db.Payments.Include(p => p.Merchant).Include(p => p.Events).AsQueryable();
        q = q.Where(p => p.Id == paymentId);
        if (merchantId.HasValue)
            q = q.Where(p => p.MerchantId == merchantId.Value);

        var payment = await q.FirstOrDefaultAsync(ct);
        if (payment == null) return null;
        return Map(payment, includeEvents: true);
    }

    private async Task<PaymentDto> MapAsync(Payment payment, CancellationToken ct)
    {
        IReadOnlyList<PaymentProviderType>? available = null;
        if (payment.Status == PaymentStatus.Pending && payment.Provider == PaymentProviderType.Auto)
        {
            if (payment.Merchant != null)
                available = await GetEffectiveProvidersAsync(payment.Merchant, ct);
            else
            {
                var merchant = await _db.Merchants.FirstAsync(m => m.Id == payment.MerchantId, ct);
                available = await GetEffectiveProvidersAsync(merchant, ct);
            }
        }

        return Map(payment, available?.Select(p => p.ToString()).ToList());
    }

    private Task<PaymentDto> MapAsync(Payment payment, IReadOnlyList<PaymentProviderType> available, CancellationToken ct)
        => Task.FromResult(Map(payment, available.Select(p => p.ToString()).ToList()));
}

public class WalletService
{
    private readonly IAppDbContext _db;

    public WalletService(IAppDbContext db) => _db = db;

    public async Task<WalletDto> GetAsync(Guid merchantId, CancellationToken ct = default)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.MerchantId == merchantId, ct)
            ?? throw new InvalidOperationException("المحفظة غير موجودة");

        var entries = await _db.WalletLedgerEntries
            .Where(e => e.WalletId == wallet.Id)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(50)
            .Select(e => new LedgerEntryDto(
                e.Id, e.Type.ToString(), e.Amount, e.BalanceAfter, e.Description, e.CreatedAtUtc, e.PaymentId, e.PayoutRequestId))
            .ToListAsync(ct);

        return new WalletDto(
            wallet.AvailableBalance,
            wallet.PendingBalance,
            wallet.LifetimeGross,
            wallet.LifetimeFees,
            wallet.LifetimePayouts,
            wallet.Currency,
            entries);
    }
}

public class PayoutService
{
    private readonly IAppDbContext _db;
    private readonly NotificationService _notifications;

    public PayoutService(IAppDbContext db, NotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<PayoutDto> CreateAsync(Guid merchantId, CreatePayoutRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("المبلغ غير صالح");

        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");
        if (merchant.Status != MerchantStatus.Active)
            throw new InvalidOperationException("حساب التاجر غير مفعّل");

        var destinationType = string.IsNullOrWhiteSpace(request.DestinationType)
            ? "BankTransfer"
            : request.DestinationType.Trim();
        var destinationDetails = string.IsNullOrWhiteSpace(request.DestinationDetails)
            ? null
            : request.DestinationDetails.Trim();

        if (string.IsNullOrWhiteSpace(destinationDetails))
        {
            if (!MerchantBankAccount.IsComplete(merchant))
                throw new InvalidOperationException("أضف رقم الحساب البنكي في الملف الشخصي قبل طلب السحب");
            destinationType = "BankTransfer";
            destinationDetails = MerchantBankAccount.FormatDetails(merchant);
        }

        await using var tx = await _db.BeginTransactionAsync(ct);

        var wallet = await _db.Wallets.FirstAsync(w => w.MerchantId == merchantId, ct);
        if (wallet.AvailableBalance < request.Amount)
            throw new InvalidOperationException("الرصيد غير كافٍ");

        wallet.AvailableBalance -= request.Amount;
        wallet.PendingBalance += request.Amount;
        wallet.UpdatedAtUtc = DateTime.UtcNow;

        var payout = new PayoutRequest
        {
            MerchantId = merchantId,
            Amount = request.Amount,
            DestinationType = destinationType,
            DestinationDetails = destinationDetails,
            Status = PayoutStatus.Pending
        };
        _db.PayoutRequests.Add(payout);

        _db.WalletLedgerEntries.Add(new WalletLedgerEntry
        {
            WalletId = wallet.Id,
            Type = LedgerEntryType.PayoutHold,
            Amount = -request.Amount,
            BalanceAfter = wallet.AvailableBalance,
            Description = "حجز مبلغ سحب",
            PayoutRequest = payout
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var merchantLabel = merchant.BusinessNameAr ?? merchant.BusinessName;
        var waBody =
            $"طلب سحب جديد من التاجر «{merchantLabel}».\n" +
            $"المبلغ: {payout.Amount:N0} {payout.Currency}\n" +
            (MerchantBankAccount.IsComplete(merchant)
                ? MerchantBankAccount.FormatWhatsAppBlock(merchant)
                : $"تفاصيل التحويل: {destinationDetails}");

        await _notifications.NotifyAdminsSafeAsync(
            NotificationTypes.PayoutRequested,
            "طلب سحب جديد",
            waBody,
            "/admin/payouts",
            merchantId,
            new
            {
                payoutId = payout.Id,
                amount = payout.Amount,
                merchantId,
                merchantName = merchantLabel,
                bankName = merchant.BankName,
                accountNumber = merchant.BankAccountNumber
            },
            ct);

        return Map(payout, merchant);
    }

    public async Task<IReadOnlyList<PayoutDto>> ListForMerchantAsync(Guid merchantId, CancellationToken ct = default)
    {
        var list = await _db.PayoutRequests.Where(p => p.MerchantId == merchantId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct);
        return list.Select(p => Map(p, merchant: null)).ToList();
    }

    public async Task<PayoutDto> ReviewAsync(Guid payoutId, Guid adminUserId, ReviewPayoutRequest request, CancellationToken ct = default)
    {
        await using var tx = await _db.BeginTransactionAsync(ct);

        var payout = await _db.PayoutRequests.FirstOrDefaultAsync(p => p.Id == payoutId, ct)
            ?? throw new InvalidOperationException("طلب السحب غير موجود");

        var wallet = await _db.Wallets.FirstAsync(w => w.MerchantId == payout.MerchantId, ct);
        var action = request.Action.ToLowerInvariant();

        if (action == "approve")
        {
            if (payout.Status != PayoutStatus.Pending)
                throw new InvalidOperationException("لا يمكن الموافقة على هذا الطلب");
            payout.Status = PayoutStatus.Approved;
        }
        else if (action == "complete")
        {
            if (payout.Status is not (PayoutStatus.Pending or PayoutStatus.Approved))
                throw new InvalidOperationException("لا يمكن إتمام هذا الطلب");
            if (payout.Status == PayoutStatus.Completed)
                throw new InvalidOperationException("تم إتمام هذا الطلب مسبقاً");
            payout.Status = PayoutStatus.Completed;
            wallet.PendingBalance -= payout.Amount;
            wallet.LifetimePayouts += payout.Amount;
            payout.CompletedAtUtc = DateTime.UtcNow;
            _db.WalletLedgerEntries.Add(new WalletLedgerEntry
            {
                WalletId = wallet.Id,
                Type = LedgerEntryType.PayoutDebit,
                Amount = -payout.Amount,
                BalanceAfter = wallet.AvailableBalance,
                Description = "إتمام سحب",
                PayoutRequestId = payout.Id
            });
        }
        else if (action == "reject")
        {
            if (payout.Status is not (PayoutStatus.Pending or PayoutStatus.Approved))
                throw new InvalidOperationException("لا يمكن رفض هذا الطلب");
            payout.Status = PayoutStatus.Rejected;
            wallet.PendingBalance -= payout.Amount;
            wallet.AvailableBalance += payout.Amount;
            _db.WalletLedgerEntries.Add(new WalletLedgerEntry
            {
                WalletId = wallet.Id,
                Type = LedgerEntryType.PayoutRelease,
                Amount = payout.Amount,
                BalanceAfter = wallet.AvailableBalance,
                Description = "إرجاع مبلغ سحب مرفوض",
                PayoutRequestId = payout.Id
            });
        }
        else
        {
            throw new ArgumentException("الإجراء يجب أن يكون approve أو complete أو reject");
        }

        payout.AdminNote = request.AdminNote;
        payout.ReviewedByUserId = adminUserId;
        payout.ReviewedAtUtc = DateTime.UtcNow;
        payout.UpdatedAtUtc = DateTime.UtcNow;
        wallet.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var (type, title, body) = payout.Status switch
        {
            PayoutStatus.Approved => (NotificationTypes.PayoutApproved, "تمت الموافقة على طلب السحب", $"تمت الموافقة على سحب {payout.Amount:N0} {payout.Currency}."),
            PayoutStatus.Completed => (NotificationTypes.PayoutCompleted, "تم إتمام السحب", $"تم تحويل مبلغ {payout.Amount:N0} {payout.Currency} بنجاح."),
            PayoutStatus.Rejected => (NotificationTypes.PayoutRejected, "تم رفض طلب السحب", $"تم رفض سحب {payout.Amount:N0} {payout.Currency}." + (string.IsNullOrWhiteSpace(payout.AdminNote) ? "" : $" ملاحظة: {payout.AdminNote}")),
            _ => (NotificationTypes.PayoutApproved, "تحديث طلب السحب", $"تم تحديث حالة طلب السحب إلى {payout.Status}.")
        };
        await _notifications.NotifyMerchantUsersSafeAsync(
            payout.MerchantId, type, title, body, "/merchant/payouts",
            new { payoutId = payout.Id, status = payout.Status.ToString(), amount = payout.Amount }, ct);

        return Map(payout, payout.Merchant);
    }

    public static PayoutDto Map(PayoutRequest p, Merchant? merchant = null) => new(
        p.Id, p.Amount, p.Currency, p.Status.ToString(), p.DestinationType, p.DestinationDetails,
        p.AdminNote, p.CreatedAtUtc, p.ReviewedAtUtc, p.CompletedAtUtc,
        merchant?.Id ?? p.MerchantId,
        merchant?.BusinessNameAr ?? merchant?.BusinessName ?? p.Merchant?.BusinessNameAr ?? p.Merchant?.BusinessName);
}

public class MerchantAdminService
{
    private readonly IAppDbContext _db;
    private readonly IApiKeyService _apiKeys;
    private readonly IPasswordHasher _passwordHasher;
    private readonly NotificationService _notifications;

    public MerchantAdminService(IAppDbContext db, IApiKeyService apiKeys, IPasswordHasher passwordHasher, NotificationService notifications)
    {
        _db = db;
        _apiKeys = apiKeys;
        _passwordHasher = passwordHasher;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<MerchantDto>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Merchants
            .Include(m => m.Wallet)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Select(m => new MerchantDto(
                m.Id, m.BusinessName, m.BusinessNameAr, m.ContactEmail, m.ContactPhone,
                m.Status.ToString(),
                m.CommissionPercent,
                m.FibCommissionPercent,
                m.ZainCashCommissionPercent,
                m.QiCommissionPercent,
                m.SuperQiCommissionPercent,
                m.AlqasehCommissionPercent,
                m.WebsiteUrl, m.CreatedAtUtc,
                m.Wallet != null ? m.Wallet.AvailableBalance : 0))
            .ToListAsync(ct);
    }

    public async Task<MerchantDetailDto> GetDetailAsync(Guid merchantId, CancellationToken ct = default)
    {
        var m = await _db.Merchants
            .Include(x => x.Wallet)
            .Include(x => x.Users)
            .FirstOrDefaultAsync(x => x.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");

        var paymentsCount = await _db.Payments.CountAsync(p => p.MerchantId == merchantId, ct);
        var apiKeysCount = await _db.ApiKeys.CountAsync(k => k.MerchantId == merchantId, ct);
        var owners = m.Users
            .OrderBy(u => u.CreatedAtUtc)
            .Select(u => new MerchantOwnerDto(u.Id, u.Email, u.FullName, u.FullNameAr, u.Phone, u.IsActive, u.CreatedAtUtc))
            .ToList();

        var maskedSecret = string.IsNullOrEmpty(m.WebhookSecret) || m.WebhookSecret.Length < 8
            ? "********"
            : $"{m.WebhookSecret[..4]}…{m.WebhookSecret[^4..]}";

        return new MerchantDetailDto(
            m.Id, m.BusinessName, m.BusinessNameAr, m.ContactEmail, m.ContactPhone,
            m.Status.ToString(),
            m.CommissionPercent,
            m.FibCommissionPercent,
            m.ZainCashCommissionPercent,
            m.QiCommissionPercent,
            m.SuperQiCommissionPercent,
            m.AlqasehCommissionPercent,
            m.WebsiteUrl, m.Notes, maskedSecret,
            m.FibEnabled, m.ZainCashEnabled, m.QiEnabled, m.SuperQiEnabled, m.AlqasehEnabled,
            m.CreatedAtUtc, m.UpdatedAtUtc,
            m.Wallet?.AvailableBalance ?? 0,
            m.Wallet?.PendingBalance ?? 0,
            m.Wallet?.LifetimeGross ?? 0,
            m.Wallet?.LifetimeFees ?? 0,
            paymentsCount, apiKeysCount, owners,
            m.KycStatus.ToString(),
            m.KycIdFrontUrl,
            m.KycIdBackUrl,
            m.KycPassportUrl,
            m.KycAdminNotes,
            m.KycSubmittedAtUtc,
            m.KycReviewedAtUtc,
            m.BankName,
            m.BankAccountHolder,
            m.BankAccountNumber,
            m.BankIban);
    }

    public async Task<MerchantDto> UpdateAsync(Guid merchantId, UpdateMerchantAdminRequest request, CancellationToken ct = default)
    {
        var merchant = await _db.Merchants
            .Include(m => m.Wallet)
            .Include(m => m.Users)
            .FirstOrDefaultAsync(m => m.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");

        var previousStatus = merchant.Status;
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<MerchantStatus>(request.Status, true, out var status))
            merchant.Status = status;

        if (request.CommissionPercent.HasValue)
        {
            ValidateCommission(request.CommissionPercent.Value);
            merchant.CommissionPercent = request.CommissionPercent.Value;
        }
        if (request.FibCommissionPercent.HasValue)
        {
            ValidateCommission(request.FibCommissionPercent.Value);
            merchant.FibCommissionPercent = request.FibCommissionPercent.Value;
        }
        if (request.ZainCashCommissionPercent.HasValue)
        {
            ValidateCommission(request.ZainCashCommissionPercent.Value);
            merchant.ZainCashCommissionPercent = request.ZainCashCommissionPercent.Value;
        }
        if (request.QiCommissionPercent.HasValue)
        {
            ValidateCommission(request.QiCommissionPercent.Value);
            merchant.QiCommissionPercent = request.QiCommissionPercent.Value;
        }
        if (request.SuperQiCommissionPercent.HasValue)
        {
            ValidateCommission(request.SuperQiCommissionPercent.Value);
            merchant.SuperQiCommissionPercent = request.SuperQiCommissionPercent.Value;
        }
        if (request.AlqasehCommissionPercent.HasValue)
        {
            ValidateCommission(request.AlqasehCommissionPercent.Value);
            merchant.AlqasehCommissionPercent = request.AlqasehCommissionPercent.Value;
        }

        if (request.Notes != null)
            merchant.Notes = request.Notes;
        if (request.BusinessName != null)
        {
            if (string.IsNullOrWhiteSpace(request.BusinessName))
                throw new ArgumentException("اسم النشاط مطلوب");
            merchant.BusinessName = request.BusinessName.Trim();
        }
        if (request.BusinessNameAr != null)
            merchant.BusinessNameAr = string.IsNullOrWhiteSpace(request.BusinessNameAr) ? null : request.BusinessNameAr.Trim();
        if (request.ContactEmail != null)
        {
            if (string.IsNullOrWhiteSpace(request.ContactEmail))
                throw new ArgumentException("البريد مطلوب");
            merchant.ContactEmail = request.ContactEmail.Trim().ToLowerInvariant();
        }
        if (request.ContactPhone != null)
            merchant.ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone) ? null : request.ContactPhone.Trim();
        if (request.WebsiteUrl != null)
            merchant.WebsiteUrl = string.IsNullOrWhiteSpace(request.WebsiteUrl) ? null : request.WebsiteUrl.Trim();
        if (request.FibEnabled.HasValue) merchant.FibEnabled = request.FibEnabled.Value;
        if (request.ZainCashEnabled.HasValue) merchant.ZainCashEnabled = request.ZainCashEnabled.Value;
        if (request.QiEnabled.HasValue) merchant.QiEnabled = request.QiEnabled.Value;
        if (request.SuperQiEnabled.HasValue) merchant.SuperQiEnabled = request.SuperQiEnabled.Value;
        if (request.AlqasehEnabled.HasValue) merchant.AlqasehEnabled = request.AlqasehEnabled.Value;
        if (request.BankName != null
            || request.BankAccountHolder != null
            || request.BankAccountNumber != null
            || request.BankIban != null)
        {
            ProfileService.ApplyPayoutAccount(merchant, new UpdateMerchantPayoutAccountRequest(
                request.BankName ?? merchant.BankName ?? "",
                request.BankAccountHolder ?? merchant.BankAccountHolder ?? "",
                request.BankAccountNumber ?? merchant.BankAccountNumber ?? "",
                request.BankIban ?? merchant.BankIban));
        }

        var owner = merchant.Users.OrderBy(u => u.CreatedAtUtc).FirstOrDefault();
        if (owner != null)
        {
            if (request.OwnerFullName != null)
            {
                if (string.IsNullOrWhiteSpace(request.OwnerFullName))
                    throw new ArgumentException("اسم المسؤول بالإنجليزية مطلوب");
                owner.FullName = request.OwnerFullName.Trim();
            }
            if (request.OwnerFullNameAr != null)
            {
                if (string.IsNullOrWhiteSpace(request.OwnerFullNameAr))
                    throw new ArgumentException("اسم المسؤول بالعربية مطلوب");
                owner.FullNameAr = request.OwnerFullNameAr.Trim();
            }
            if (request.OwnerPhone != null)
                owner.Phone = string.IsNullOrWhiteSpace(request.OwnerPhone) ? null : request.OwnerPhone.Trim();
            if (request.OwnerEmail != null)
            {
                var email = request.OwnerEmail.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email))
                    throw new ArgumentException("بريد المسؤول مطلوب");
                if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != owner.Id, ct))
                    throw new InvalidOperationException("البريد مستخدم لحساب آخر");
                owner.Email = email;
                merchant.ContactEmail = email;
            }
            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                PasswordRules.Validate(request.NewPassword);
                owner.PasswordHash = _passwordHasher.Hash(request.NewPassword);
                owner.UpdatedAtUtc = DateTime.UtcNow;
            }
            owner.UpdatedAtUtc = DateTime.UtcNow;
        }
        else if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new InvalidOperationException("لا يوجد مستخدم مسؤول لتعيين كلمة المرور");
        }

        merchant.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        if (previousStatus != merchant.Status)
        {
            var (type, title, body) = merchant.Status switch
            {
                MerchantStatus.Active => (NotificationTypes.MerchantActivated, "تم تفعيل حسابك", "تمت الموافقة على حساب التاجر ويمكنك الآن قبول المدفوعات."),
                MerchantStatus.Suspended => (NotificationTypes.MerchantSuspended, "تم تعليق حسابك", "تم تعليق حساب التاجر مؤقتاً. تواصل مع الدعم لمزيد من التفاصيل."),
                MerchantStatus.Rejected => (NotificationTypes.MerchantRejected, "تم رفض طلب التسجيل", "لم تتم الموافقة على حساب التاجر. راجع بياناتك أو تواصل مع الإدارة."),
                _ => (NotificationTypes.MerchantActivated, "تحديث حالة الحساب", $"تم تحديث حالة حسابك إلى {merchant.Status}.")
            };
            await _notifications.NotifyMerchantUsersSafeAsync(
                merchant.Id, type, title, body, "/merchant",
                new { merchantId = merchant.Id, status = merchant.Status.ToString() }, ct);
        }

        return new MerchantDto(
            merchant.Id, merchant.BusinessName, merchant.BusinessNameAr, merchant.ContactEmail, merchant.ContactPhone,
            merchant.Status.ToString(),
            merchant.CommissionPercent,
            merchant.FibCommissionPercent,
            merchant.ZainCashCommissionPercent,
            merchant.QiCommissionPercent,
            merchant.SuperQiCommissionPercent,
            merchant.AlqasehCommissionPercent,
            merchant.WebsiteUrl, merchant.CreatedAtUtc,
            merchant.Wallet?.AvailableBalance ?? 0);
    }

    private static void ValidateCommission(decimal value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentException("نسبة العمولة غير صالحة");
    }

    public async Task DeleteAsync(Guid merchantId, CancellationToken ct = default)
    {
        var merchant = await _db.Merchants
            .Include(m => m.Wallet)
            .Include(m => m.Users)
            .FirstOrDefaultAsync(m => m.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");

        var paymentIds = await _db.Payments.Where(p => p.MerchantId == merchantId).Select(p => p.Id).ToListAsync(ct);
        if (paymentIds.Count > 0)
        {
            var events = await _db.PaymentEvents.Where(e => paymentIds.Contains(e.PaymentId)).ToListAsync(ct);
            _db.PaymentEvents.RemoveRange(events);
            var payments = await _db.Payments.Where(p => p.MerchantId == merchantId).ToListAsync(ct);
            _db.Payments.RemoveRange(payments);
        }

        var payouts = await _db.PayoutRequests.Where(p => p.MerchantId == merchantId).ToListAsync(ct);
        _db.PayoutRequests.RemoveRange(payouts);

        var keys = await _db.ApiKeys.Where(k => k.MerchantId == merchantId).ToListAsync(ct);
        _db.ApiKeys.RemoveRange(keys);

        var platforms = await _db.MerchantPlatforms.Where(p => p.MerchantId == merchantId).ToListAsync(ct);
        _db.MerchantPlatforms.RemoveRange(platforms);

        if (merchant.Wallet != null)
        {
            var ledger = await _db.WalletLedgerEntries.Where(l => l.WalletId == merchant.Wallet.Id).ToListAsync(ct);
            _db.WalletLedgerEntries.RemoveRange(ledger);
            _db.Wallets.Remove(merchant.Wallet);
        }

        if (merchant.Users.Count > 0)
        {
            var userIds = merchant.Users.Select(u => u.Id).ToList();
            var reviewed = await _db.PayoutRequests
                .Where(p => p.ReviewedByUserId != null && userIds.Contains(p.ReviewedByUserId.Value))
                .ToListAsync(ct);
            foreach (var p in reviewed)
                p.ReviewedByUserId = null;

            _db.Users.RemoveRange(merchant.Users);
        }

        _db.Merchants.Remove(merchant);
        await _db.SaveChangesAsync(ct);
    }

    public Task<CreateApiKeyResponse> CreateApiKeyAsync(Guid merchantId, string name, CancellationToken ct = default)
        => throw new InvalidOperationException("إنشاء المفاتيح الحرّة متوقف. أضف منصة معتمدة لإصدار مفتاح API مربوط بالدومين.");

    public async Task<IReadOnlyList<ApiKeyDto>> ListApiKeysAsync(Guid merchantId, CancellationToken ct = default)
    {
        return await _db.ApiKeys
            .Include(k => k.MerchantPlatform)
            .Where(k => k.MerchantId == merchantId)
            .OrderByDescending(k => k.CreatedAtUtc)
            .Select(k => new ApiKeyDto(
                k.Id, k.Name, k.KeyPrefix, k.IsActive, k.IsTest, k.CreatedAtUtc, k.LastUsedAtUtc,
                k.MerchantPlatformId,
                k.MerchantPlatform != null ? k.MerchantPlatform.Name : null,
                k.MerchantPlatform != null ? k.MerchantPlatform.Domain : null))
            .ToListAsync(ct);
    }

    public async Task RevokeApiKeyAsync(Guid merchantId, Guid keyId, CancellationToken ct = default)
    {
        var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == keyId && k.MerchantId == merchantId, ct)
            ?? throw new InvalidOperationException("المفتاح غير موجود");
        if (IsMerchantBearer(key))
            throw new InvalidOperationException("مفتاح التاجر يُعاد توليده من صفحة المفاتيح، ولا يُلغى من مفاتيح المنصات");
        key.IsActive = false;
        key.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<MerchantBearerDto> GetMerchantBearerAsync(Guid merchantId, CancellationToken ct = default)
    {
        var key = await FindMerchantBearerAsync(merchantId, ct);
        return new MerchantBearerDto(
            key?.Id,
            key?.KeyPrefix,
            key?.IsActive == true,
            key == null || !key.IsActive,
            key?.CreatedAtUtc,
            key?.LastUsedAtUtc);
    }

    public async Task<CreateApiKeyResponse> ClaimMerchantBearerAsync(Guid merchantId, CancellationToken ct = default)
    {
        var existing = await FindMerchantBearerAsync(merchantId, ct);
        if (existing is { IsActive: true })
            throw new InvalidOperationException("مفتاح التاجر مُستلم مسبقاً. استخدم إعادة التوليد إن ضاع منك.");
        return await IssueMerchantBearerAsync(merchantId, ct);
    }

    public Task<CreateApiKeyResponse> RegenerateMerchantBearerAsync(Guid merchantId, CancellationToken ct = default)
        => IssueMerchantBearerAsync(merchantId, ct);

    private async Task<CreateApiKeyResponse> IssueMerchantBearerAsync(Guid merchantId, CancellationToken ct)
    {
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");
        if (merchant.Status != MerchantStatus.Active)
            throw new InvalidOperationException("حساب التاجر غير مفعّل بعد");

        var previous = await _db.ApiKeys
            .Where(k => k.MerchantId == merchantId && k.MerchantPlatformId == null)
            .Where(k => k.KeyPrefix.StartsWith("fx_merch_"))
            .ToListAsync(ct);
        foreach (var old in previous)
        {
            old.IsActive = false;
            old.UpdatedAtUtc = DateTime.UtcNow;
        }

        var (plain, prefix, hash) = _apiKeys.GenerateMerchant();
        var key = new ApiKey
        {
            MerchantId = merchantId,
            MerchantPlatformId = null,
            Name = "Merchant",
            KeyPrefix = prefix,
            KeyHash = hash,
            IsActive = true,
            IsTest = false
        };
        _db.ApiKeys.Add(key);
        await _db.SaveChangesAsync(ct);
        return new CreateApiKeyResponse(key.Id, key.Name, key.KeyPrefix, plain, key.CreatedAtUtc);
    }

    private Task<ApiKey?> FindMerchantBearerAsync(Guid merchantId, CancellationToken ct)
        => _db.ApiKeys
            .Where(k => k.MerchantId == merchantId && k.MerchantPlatformId == null && k.IsActive)
            .Where(k => k.KeyPrefix.StartsWith("fx_merch_"))
            .OrderByDescending(k => k.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

    private static bool IsMerchantBearer(ApiKey key)
        => key.MerchantPlatformId == null && key.KeyPrefix.StartsWith("fx_merch_", StringComparison.OrdinalIgnoreCase);

    public async Task<MerchantDto> GetMerchantAsync(Guid merchantId, CancellationToken ct = default)
    {
        var m = await _db.Merchants.Include(x => x.Wallet).FirstOrDefaultAsync(x => x.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");
        return new MerchantDto(m.Id, m.BusinessName, m.BusinessNameAr, m.ContactEmail, m.ContactPhone,
            m.Status.ToString(),
            m.CommissionPercent,
            m.FibCommissionPercent,
            m.ZainCashCommissionPercent,
            m.QiCommissionPercent,
            m.SuperQiCommissionPercent,
            m.AlqasehCommissionPercent,
            m.WebsiteUrl, m.CreatedAtUtc, m.Wallet?.AvailableBalance ?? 0);
    }

    public async Task<string> GetWebhookSecretAsync(Guid merchantId, CancellationToken ct = default)
    {
        var m = await _db.Merchants.FirstAsync(x => x.Id == merchantId, ct);
        return m.WebhookSecret;
    }

    public async Task<PlatformStatsDto> GetStatsAsync(string? mode = "live", CancellationToken ct = default)
    {
        var merchants = await _db.Merchants.CountAsync(ct);
        var active = await _db.Merchants.CountAsync(m => m.Status == MerchantStatus.Active, ct);
        var pendingMerchants = await _db.Merchants.CountAsync(m => m.Status == MerchantStatus.Pending, ct);

        var paymentsQuery = ApplyPaymentModeFilter(_db.Payments.AsQueryable(), mode);

        var payments = await paymentsQuery.CountAsync(ct);
        var paidCount = await paymentsQuery.CountAsync(p => p.Status == PaymentStatus.Paid, ct);
        var pendingPayments = await paymentsQuery.CountAsync(p => p.Status == PaymentStatus.Pending, ct);
        var failedPayments = await paymentsQuery.CountAsync(p =>
            p.Status == PaymentStatus.Failed
            || p.Status == PaymentStatus.Declined
            || p.Status == PaymentStatus.Expired
            || p.Status == PaymentStatus.Cancelled, ct);

        var paidQuery = paymentsQuery.Where(p => p.Status == PaymentStatus.Paid);
        var gross = await paidQuery.SumAsync(p => (decimal?)p.Amount, ct) ?? 0;
        var fees = await paidQuery.SumAsync(p => (decimal?)p.PlatformFee, ct) ?? 0;
        var net = await paidQuery.SumAsync(p => (decimal?)p.NetAmount, ct) ?? 0;
        var avgTicket = paidCount > 0 ? Math.Round(gross / paidCount, 0) : 0m;
        var pendingPayouts = await _db.PayoutRequests.CountAsync(p => p.Status == PayoutStatus.Pending, ct);

        var fromUtc = DateTime.UtcNow.Date.AddDays(-13);
        var recentPaid = await paymentsQuery
            .Where(p => p.Status == PaymentStatus.Paid
                        && (p.PaidAtUtc ?? p.CreatedAtUtc) >= fromUtc)
            .Select(p => new { Day = p.PaidAtUtc ?? p.CreatedAtUtc, p.Amount, p.PlatformFee })
            .ToListAsync(ct);

        var byDay = recentPaid
            .GroupBy(p => p.Day.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var last14 = new List<DailyVolumePointDto>(14);
        for (var i = 0; i < 14; i++)
        {
            var day = fromUtc.AddDays(i);
            byDay.TryGetValue(day, out var rows);
            last14.Add(new DailyVolumePointDto(
                day.ToString("yyyy-MM-dd"),
                rows?.Count ?? 0,
                rows?.Sum(x => x.Amount) ?? 0,
                rows?.Sum(x => x.PlatformFee) ?? 0));
        }

        var statusRows = await paymentsQuery
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(x => x.Amount) })
            .ToListAsync(ct);
        var byStatus = statusRows
            .OrderByDescending(x => x.Count)
            .Select(x => new NamedCountDto(x.Status.ToString(), x.Count, x.Amount))
            .ToList();

        var providerRows = await paymentsQuery
            .Where(p => p.Provider != PaymentProviderType.Auto)
            .GroupBy(p => p.Provider)
            .Select(g => new { Provider = g.Key, Count = g.Count(), Amount = g.Sum(x => x.Amount) })
            .ToListAsync(ct);
        var byProvider = providerRows
            .OrderByDescending(x => x.Amount)
            .Select(x => new NamedCountDto(x.Provider.ToString(), x.Count, x.Amount))
            .ToList();

        return new PlatformStatsDto(
            merchants, active, pendingMerchants,
            payments, paidCount, pendingPayments, failedPayments,
            gross, fees, net, avgTicket, pendingPayouts,
            last14, byStatus, byProvider);
    }

    private static IQueryable<Payment> ApplyPaymentModeFilter(IQueryable<Payment> query, string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return query.Where(p => !p.IsTest);

        var m = mode.Trim();
        if (m.Equals("test", StringComparison.OrdinalIgnoreCase))
            return query.Where(p => p.IsTest);
        if (m.Equals("all", StringComparison.OrdinalIgnoreCase)
            || m.Equals("any", StringComparison.OrdinalIgnoreCase))
            return query;
        // live / production / prod (default)
        return query.Where(p => !p.IsTest);
    }
}

public class AppOptions
{
    public string PublicBaseUrl { get; set; } = "https://localhost:7100";
    public decimal DefaultCommissionPercent { get; set; } = 2.5m;
    public string[] ProviderPriority { get; set; } = ["Fib", "ZainCash", "Qi"];
}
