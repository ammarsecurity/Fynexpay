using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Abstractions.Payments;
using Fynexpay.Application.DTOs;
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
    private readonly IApiKeyService _apiKeys;

    public AuthService(IAppDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwt, IApiKeyService apiKeys)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _apiKeys = apiKeys;
    }

    public async Task<AuthResponse> RegisterMerchantAsync(RegisterMerchantRequest request, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), ct))
            throw new InvalidOperationException("البريد الإلكتروني مستخدم مسبقاً");

        var merchant = new Merchant
        {
            BusinessName = request.BusinessName,
            BusinessNameAr = request.BusinessNameAr,
            ContactEmail = request.Email.ToLowerInvariant(),
            ContactPhone = request.ContactPhone,
            WebsiteUrl = request.WebsiteUrl,
            Status = MerchantStatus.Pending,
            CommissionPercent = 2.5m,
            WebhookSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant()
        };

        var wallet = new Wallet { Merchant = merchant, Currency = "IQD" };
        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            FullName = request.FullName,
            Phone = request.ContactPhone,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.MerchantOwner,
            Merchant = merchant
        };

        var (plain, prefix, hash) = _apiKeys.Generate();
        var apiKey = new ApiKey
        {
            Merchant = merchant,
            Name = "Default",
            KeyPrefix = prefix,
            KeyHash = hash
        };

        _db.Merchants.Add(merchant);
        _db.Wallets.Add(wallet);
        _db.Users.Add(user);
        _db.ApiKeys.Add(apiKey);
        await _db.SaveChangesAsync(ct);

        // Store plain key once in memory return path via notes? Better return separately.
        // For register response we only return JWT; API key created but merchant regenerates in dashboard.
        // Actually include first key in a temporary way - we'll return CreateApiKey in register enhanced response.
        _ = plain; // generated for seed; merchant creates visible key from dashboard after activation

        var token = _jwt.CreateToken(user.Id, user.Email, user.Role.ToString(), merchant.Id, user.FullName);
        return new AuthResponse(token, user.Id, user.Email, user.FullName, user.Role.ToString(), merchant.Id, merchant.Status.ToString());
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.Merchant)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException("بيانات الدخول غير صحيحة");

        if (!user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("بيانات الدخول غير صحيحة");

        var token = _jwt.CreateToken(user.Id, user.Email, user.Role.ToString(), user.MerchantId, user.FullName);
        return new AuthResponse(token, user.Id, user.Email, user.FullName, user.Role.ToString(), user.MerchantId, user.Merchant?.Status.ToString());
    }
}

public class PaymentService
{
    private readonly IAppDbContext _db;
    private readonly IPaymentProviderResolver _resolver;
    private readonly IMerchantWebhookSender _webhookSender;
    private readonly ILogger<PaymentService> _logger;
    private readonly string _publicBaseUrl;

    public PaymentService(
        IAppDbContext db,
        IPaymentProviderResolver resolver,
        IMerchantWebhookSender webhookSender,
        ILogger<PaymentService> logger,
        Microsoft.Extensions.Options.IOptions<AppOptions> options)
    {
        _db = db;
        _resolver = resolver;
        _webhookSender = webhookSender;
        _logger = logger;
        _publicBaseUrl = options.Value.PublicBaseUrl.TrimEnd('/');
    }

    public async Task<PaymentDto> CreateAsync(Guid merchantId, CreatePaymentRequest request, string? idempotencyKey, CancellationToken ct = default)
    {
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");

        if (merchant.Status != MerchantStatus.Active)
            throw new InvalidOperationException("حساب التاجر غير مفعّل");

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
            MerchantOrderId = orderId,
            Amount = request.Amount,
            Currency = currency,
            Description = serviceType.Trim(),
            Status = PaymentStatus.Pending,
            Provider = PaymentProviderType.Auto, // يختاره الزبون لاحقاً في صفحة الدفع
            SuccessUrl = request.SuccessUrl,
            FailureUrl = request.FailureUrl,
            CallbackUrl = request.CallbackUrl,
            IdempotencyKey = idempotencyKey,
            PlatformFee = fee,
            NetAmount = net,
            ExpiredAtUtc = DateTime.UtcNow.AddHours(2)
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

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
                serviceType = payment.Description
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

        if (payment.ExpiredAtUtc.HasValue && payment.ExpiredAtUtc < DateTime.UtcNow)
        {
            payment.Status = PaymentStatus.Expired;
            payment.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            throw new InvalidOperationException("انتهت صلاحية رابط الدفع");
        }

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
        var result = await provider.CreatePaymentAsync(new CreateProviderPaymentRequest
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

        if (!result.Success)
        {
            payment.FailureReason = result.ErrorMessage;
            payment.ProviderRawResponse = result.RawResponse;
            payment.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            throw new InvalidOperationException(result.ErrorMessage ?? "فشل إنشاء الدفعة لدى المزود");
        }

        payment.Provider = providerType;
        payment.ProviderPaymentId = result.ProviderPaymentId;
        payment.ProviderCheckoutUrl = result.CheckoutUrl;
        payment.CheckoutUrl = $"{_publicBaseUrl}/checkout/{payment.Id}";
        payment.QrCode = result.QrCode;
        payment.ReadableCode = result.ReadableCode;
        payment.ExpiredAtUtc = result.ValidUntilUtc ?? payment.ExpiredAtUtc;
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
        return new MerchantPaymentMethodsDto(
            merchant.FibEnabled,
            merchant.ZainCashEnabled,
            merchant.QiEnabled,
            merchant.SuperQiEnabled,
            platform.Select(p => p.ToString()).ToList(),
            effective.Select(p => p.ToString()).ToList());
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

        if (!merchant.FibEnabled && !merchant.ZainCashEnabled && !merchant.QiEnabled && !merchant.SuperQiEnabled)
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
            _ => false
        }).ToList();
    }

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
            await provider.CancelAsync(payment.ProviderPaymentId, ct);
        }

        payment.Status = PaymentStatus.Cancelled;
        payment.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await MapAsync(payment, ct);
    }

    public async Task ApplyProviderStatusAsync(Guid paymentId, PaymentStatus status, string source, string payload, string? failureReason = null, CancellationToken ct = default)
    {
        var payment = await _db.Payments.Include(p => p.Merchant).ThenInclude(m => m!.Wallet)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment == null) return;

        _db.PaymentEvents.Add(new PaymentEvent
        {
            PaymentId = payment.Id,
            Source = source,
            EventType = status.ToString(),
            Payload = payload
        });

        if (payment.Status == PaymentStatus.Paid && status == PaymentStatus.Paid)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        payment.Status = status;
        payment.UpdatedAtUtc = DateTime.UtcNow;
        payment.FailureReason = failureReason;

        if (status == PaymentStatus.Paid && !payment.LedgerApplied)
        {
            payment.PaidAtUtc = DateTime.UtcNow;
            await CreditWalletAsync(payment, ct);
            payment.LedgerApplied = true;
        }

        await _db.SaveChangesAsync(ct);

        try
        {
            await _webhookSender.SendPaymentUpdateAsync(payment.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send merchant webhook for payment {PaymentId}", payment.Id);
        }
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
        var status = await provider.GetStatusAsync(payment.ProviderPaymentId, ct);
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

        _db.WalletLedgerEntries.Add(new WalletLedgerEntry
        {
            WalletId = wallet.Id,
            Type = LedgerEntryType.PaymentCredit,
            Amount = payment.NetAmount,
            BalanceAfter = wallet.AvailableBalance,
            Description = $"صافي دفعة {payment.Id}",
            PaymentId = payment.Id
        });

        _db.WalletLedgerEntries.Add(new WalletLedgerEntry
        {
            WalletId = wallet.Id,
            Type = LedgerEntryType.PlatformFee,
            Amount = -payment.PlatformFee,
            BalanceAfter = wallet.AvailableBalance,
            Description = $"عمولة المنصة على دفعة {payment.Id}",
            PaymentId = payment.Id
        });
    }

    public static PaymentDto Map(Payment p, IReadOnlyList<string>? availableProviders = null) => new(
        p.Id,
        p.MerchantOrderId,
        p.Amount,
        p.Currency,
        p.Status.ToString(),
        p.Provider == PaymentProviderType.Auto ? "PendingSelection" : p.Provider.ToString(),
        p.Description,
        string.IsNullOrWhiteSpace(p.CheckoutUrl) ? null : p.CheckoutUrl,
        p.ProviderCheckoutUrl,
        p.QrCode,
        p.ReadableCode,
        p.PlatformFee,
        p.NetAmount,
        p.CreatedAtUtc,
        p.PaidAtUtc,
        p.FailureReason,
        availableProviders);

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

    public PayoutService(IAppDbContext db) => _db = db;

    public async Task<PayoutDto> CreateAsync(Guid merchantId, CreatePayoutRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("المبلغ غير صالح");

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
            DestinationType = request.DestinationType,
            DestinationDetails = request.DestinationDetails,
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
        return Map(payout);
    }

    public async Task<IReadOnlyList<PayoutDto>> ListForMerchantAsync(Guid merchantId, CancellationToken ct = default)
    {
        var list = await _db.PayoutRequests.Where(p => p.MerchantId == merchantId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<PayoutDto> ReviewAsync(Guid payoutId, Guid adminUserId, ReviewPayoutRequest request, CancellationToken ct = default)
    {
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
        return Map(payout);
    }

    private static PayoutDto Map(PayoutRequest p) => new(
        p.Id, p.Amount, p.Currency, p.Status.ToString(), p.DestinationType, p.DestinationDetails,
        p.AdminNote, p.CreatedAtUtc, p.ReviewedAtUtc, p.CompletedAtUtc);
}

public class MerchantAdminService
{
    private readonly IAppDbContext _db;
    private readonly IApiKeyService _apiKeys;

    public MerchantAdminService(IAppDbContext db, IApiKeyService apiKeys)
    {
        _db = db;
        _apiKeys = apiKeys;
    }

    public async Task<IReadOnlyList<MerchantDto>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Merchants
            .Include(m => m.Wallet)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Select(m => new MerchantDto(
                m.Id, m.BusinessName, m.BusinessNameAr, m.ContactEmail, m.ContactPhone,
                m.Status.ToString(), m.CommissionPercent, m.WebsiteUrl, m.CreatedAtUtc,
                m.Wallet != null ? m.Wallet.AvailableBalance : 0))
            .ToListAsync(ct);
    }

    public async Task<MerchantDto> UpdateAsync(Guid merchantId, UpdateMerchantAdminRequest request, CancellationToken ct = default)
    {
        var merchant = await _db.Merchants.Include(m => m.Wallet).FirstOrDefaultAsync(m => m.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<MerchantStatus>(request.Status, true, out var status))
            merchant.Status = status;

        if (request.CommissionPercent.HasValue)
        {
            if (request.CommissionPercent < 0 || request.CommissionPercent > 100)
                throw new ArgumentException("نسبة العمولة غير صالحة");
            merchant.CommissionPercent = request.CommissionPercent.Value;
        }

        if (request.Notes != null)
            merchant.Notes = request.Notes;

        merchant.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new MerchantDto(
            merchant.Id, merchant.BusinessName, merchant.BusinessNameAr, merchant.ContactEmail, merchant.ContactPhone,
            merchant.Status.ToString(), merchant.CommissionPercent, merchant.WebsiteUrl, merchant.CreatedAtUtc,
            merchant.Wallet?.AvailableBalance ?? 0);
    }

    public async Task<CreateApiKeyResponse> CreateApiKeyAsync(Guid merchantId, string name, CancellationToken ct = default)
    {
        var (plain, prefix, hash) = _apiKeys.Generate();
        var entity = new ApiKey
        {
            MerchantId = merchantId,
            Name = string.IsNullOrWhiteSpace(name) ? "Key" : name,
            KeyPrefix = prefix,
            KeyHash = hash
        };
        _db.ApiKeys.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new CreateApiKeyResponse(entity.Id, entity.Name, entity.KeyPrefix, plain, entity.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<ApiKeyDto>> ListApiKeysAsync(Guid merchantId, CancellationToken ct = default)
    {
        return await _db.ApiKeys.Where(k => k.MerchantId == merchantId)
            .OrderByDescending(k => k.CreatedAtUtc)
            .Select(k => new ApiKeyDto(k.Id, k.Name, k.KeyPrefix, k.IsActive, k.CreatedAtUtc, k.LastUsedAtUtc))
            .ToListAsync(ct);
    }

    public async Task RevokeApiKeyAsync(Guid merchantId, Guid keyId, CancellationToken ct = default)
    {
        var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == keyId && k.MerchantId == merchantId, ct)
            ?? throw new InvalidOperationException("المفتاح غير موجود");
        key.IsActive = false;
        key.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<MerchantDto> GetMerchantAsync(Guid merchantId, CancellationToken ct = default)
    {
        var m = await _db.Merchants.Include(x => x.Wallet).FirstOrDefaultAsync(x => x.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");
        return new MerchantDto(m.Id, m.BusinessName, m.BusinessNameAr, m.ContactEmail, m.ContactPhone,
            m.Status.ToString(), m.CommissionPercent, m.WebsiteUrl, m.CreatedAtUtc, m.Wallet?.AvailableBalance ?? 0);
    }

    public async Task<string> GetWebhookSecretAsync(Guid merchantId, CancellationToken ct = default)
    {
        var m = await _db.Merchants.FirstAsync(x => x.Id == merchantId, ct);
        return m.WebhookSecret;
    }

    public async Task<PlatformStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var merchants = await _db.Merchants.CountAsync(ct);
        var active = await _db.Merchants.CountAsync(m => m.Status == MerchantStatus.Active, ct);
        var payments = await _db.Payments.CountAsync(ct);
        var gross = await _db.Payments.Where(p => p.Status == PaymentStatus.Paid).SumAsync(p => (decimal?)p.Amount, ct) ?? 0;
        var fees = await _db.Payments.Where(p => p.Status == PaymentStatus.Paid).SumAsync(p => (decimal?)p.PlatformFee, ct) ?? 0;
        var pendingPayouts = await _db.PayoutRequests.CountAsync(p => p.Status == PayoutStatus.Pending, ct);
        return new PlatformStatsDto(merchants, active, payments, gross, fees, pendingPayouts);
    }
}

public class AppOptions
{
    public string PublicBaseUrl { get; set; } = "https://localhost:7100";
    public decimal DefaultCommissionPercent { get; set; } = 2.5m;
    public string[] ProviderPriority { get; set; } = ["Fib", "ZainCash", "Qi"];
}
