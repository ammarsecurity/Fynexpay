using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Abstractions.Messaging;
using Fynexpay.Application.DTOs;
using Fynexpay.Application.Services;
using Fynexpay.Domain.Entities;
using Fynexpay.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Fynexpay.Application.Services;

public class MerchantPlatformService
{
    private readonly IAppDbContext _db;
    private readonly IApiKeyService _apiKeys;
    private readonly ISecretProtector _protector;
    private readonly NotificationService _notifications;

    public MerchantPlatformService(IAppDbContext db, IApiKeyService apiKeys, ISecretProtector protector, NotificationService notifications)
    {
        _db = db;
        _apiKeys = apiKeys;
        _protector = protector;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<MerchantPlatformDto>> ListForMerchantAsync(Guid merchantId, CancellationToken ct = default)
    {
        var list = await _db.MerchantPlatforms
            .Include(p => p.ApiKey)
            .Where(p => p.MerchantId == merchantId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct);
        return list.Select(p => Map(p)).ToList();
    }

    public async Task<IReadOnlyList<MerchantPlatformDto>> ListAdminAsync(string? status, string? q, CancellationToken ct = default)
    {
        var query = _db.MerchantPlatforms
            .Include(p => p.Merchant)
            .Include(p => p.ApiKey)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PlatformStatus>(status, true, out var st))
            query = query.Where(p => p.Status == st);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.Domain.Contains(term) ||
                (p.Merchant != null && (p.Merchant.BusinessName.Contains(term) || p.Merchant.ContactEmail.Contains(term))));
        }

        var list = await query.OrderByDescending(p => p.CreatedAtUtc).Take(200).ToListAsync(ct);
        return list.Select(p => Map(p, includeMerchantName: true)).ToList();
    }

    public async Task<MerchantPlatformDetailDto> GetAdminDetailAsync(Guid platformId, CancellationToken ct = default)
    {
        var platform = await _db.MerchantPlatforms
            .Include(p => p.Merchant)
            .Include(p => p.ApiKey)
            .FirstOrDefaultAsync(p => p.Id == platformId, ct)
            ?? throw new InvalidOperationException("المنصة غير موجودة");

        string? reviewerName = null;
        if (platform.ReviewedByUserId.HasValue)
        {
            reviewerName = await _db.Users
                .Where(u => u.Id == platform.ReviewedByUserId.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);
        }

        var paymentsQuery = _db.Payments.Where(p => p.MerchantPlatformId == platformId);
        var paymentsCount = await paymentsQuery.CountAsync(ct);
        var paymentsVolume = await paymentsQuery
            .Where(p => p.Status == PaymentStatus.Paid)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0;

        return new MerchantPlatformDetailDto(
            platform.Id,
            platform.MerchantId,
            platform.Merchant?.BusinessName,
            platform.Merchant?.ContactEmail,
            platform.Merchant?.ContactPhone,
            platform.Merchant?.Status.ToString(),
            platform.Name,
            platform.Domain,
            platform.LogoUrl,
            platform.Status.ToString(),
            platform.AdminNotes,
            platform.CreatedAtUtc,
            platform.UpdatedAtUtc,
            platform.ReviewedAtUtc,
            platform.ReviewedByUserId,
            reviewerName,
            platform.ApiKey?.Id,
            platform.ApiKey is { IsActive: true } ? platform.ApiKey.KeyPrefix : platform.ApiKey?.KeyPrefix,
            platform.ApiKey?.IsActive == true,
            platform.ApiKey?.CreatedAtUtc,
            !string.IsNullOrWhiteSpace(platform.OneTimeApiKey),
            paymentsCount,
            paymentsVolume);
    }

    public async Task<MerchantPlatformDto> RequestAsync(Guid merchantId, CreateMerchantPlatformRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("اسم المنصة مطلوب");

        var domain = NormalizeDomain(request.Domain);
        if (await _db.MerchantPlatforms.AnyAsync(p => p.MerchantId == merchantId && p.Domain == domain, ct))
            throw new InvalidOperationException("هذا الدومين مسجّل مسبقاً لهذا التاجر");

        var entity = new MerchantPlatform
        {
            MerchantId = merchantId,
            Name = name,
            Domain = domain,
            Status = PlatformStatus.Pending
        };
        _db.MerchantPlatforms.Add(entity);
        await _db.SaveChangesAsync(ct);

        var merchantName = await _db.Merchants.Where(m => m.Id == merchantId).Select(m => m.BusinessName).FirstOrDefaultAsync(ct) ?? "";
        await _notifications.NotifyAdminsSafeAsync(
            NotificationTypes.PlatformSubmitted,
            "طلب منصة جديد",
            $"التاجر «{merchantName}» طلب اعتماد منصة «{name}» ({domain}).",
            "/admin/platforms",
            merchantId,
            new { platformId = entity.Id, domain, name },
            ct);

        return Map(entity);
    }

    public async Task<MerchantPlatformDto> UpdateAsync(Guid merchantId, Guid platformId, UpdateMerchantPlatformRequest request, CancellationToken ct = default)
    {
        var platform = await _db.MerchantPlatforms
            .Include(p => p.ApiKey)
            .Include(p => p.Merchant)
            .FirstOrDefaultAsync(p => p.Id == platformId && p.MerchantId == merchantId, ct)
            ?? throw new InvalidOperationException("المنصة غير موجودة");

        var previousStatus = platform.Status;
        var nameChanged = false;
        var domainChanged = false;

        if (request.Name != null)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("اسم المنصة مطلوب");
            if (!string.Equals(platform.Name, name, StringComparison.Ordinal))
            {
                platform.Name = name;
                nameChanged = true;
            }
        }

        if (request.Domain != null)
        {
            var domain = NormalizeDomain(request.Domain);
            if (domain != platform.Domain)
            {
                if (await _db.MerchantPlatforms.AnyAsync(p => p.MerchantId == merchantId && p.Domain == domain && p.Id != platformId, ct))
                    throw new InvalidOperationException("هذا الدومين مسجّل مسبقاً لهذا التاجر");
                platform.Domain = domain;
                domainChanged = true;
            }
        }

        if (!nameChanged && !domainChanged)
            return Map(platform);

        var requiresReview = previousStatus is PlatformStatus.Approved or PlatformStatus.Suspended or PlatformStatus.Rejected;
        if (requiresReview)
        {
            platform.Status = PlatformStatus.Pending;
            platform.ReviewedAtUtc = null;
            platform.ReviewedByUserId = null;
            platform.AdminNotes = domainChanged
                ? "أُعيد الطلب للمراجعة بسبب تعديل الدومين/الاسم"
                : "أُعيد الطلب للمراجعة بسبب تعديل بيانات المنصة";
            if (platform.ApiKey != null)
            {
                platform.ApiKey.IsActive = false;
                platform.ApiKey.UpdatedAtUtc = DateTime.UtcNow;
            }
            platform.OneTimeApiKey = null;
        }
        else if (platform.Status == PlatformStatus.Pending)
        {
            platform.AdminNotes = null;
        }

        platform.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var merchantName = platform.Merchant?.BusinessName
            ?? await _db.Merchants.Where(m => m.Id == merchantId).Select(m => m.BusinessName).FirstOrDefaultAsync(ct)
            ?? "";
        await _notifications.NotifyAdminsSafeAsync(
            NotificationTypes.PlatformSubmitted,
            requiresReview ? "تعديل منصة بانتظار الموافقة" : "تحديث طلب منصة",
            $"التاجر «{merchantName}» عدّل منصة «{platform.Name}» ({platform.Domain}) وتحتاج مراجعة.",
            "/admin/platforms",
            merchantId,
            new { platformId = platform.Id, domain = platform.Domain, name = platform.Name, requiresReview },
            ct);

        return Map(platform);
    }

    public async Task<MerchantPlatformDto> ReviewAsync(Guid platformId, Guid adminUserId, ReviewMerchantPlatformRequest request, CancellationToken ct = default)
    {
        var platform = await _db.MerchantPlatforms
            .Include(p => p.ApiKey)
            .Include(p => p.Merchant)
            .FirstOrDefaultAsync(p => p.Id == platformId, ct)
            ?? throw new InvalidOperationException("المنصة غير موجودة");

        var action = (request.Action ?? "").Trim().ToLowerInvariant();
        platform.AdminNotes = string.IsNullOrWhiteSpace(request.AdminNotes) ? platform.AdminNotes : request.AdminNotes.Trim();
        platform.ReviewedAtUtc = DateTime.UtcNow;
        platform.ReviewedByUserId = adminUserId;
        platform.UpdatedAtUtc = DateTime.UtcNow;

        if (action is "approve" or "approved")
        {
            platform.Status = PlatformStatus.Approved;
            var plain = await IssueKeyAsync(platform, ct);

            // Disable legacy unbound keys for this merchant
            var legacy = await _db.ApiKeys
                .Where(k => k.MerchantId == platform.MerchantId && k.MerchantPlatformId == null && k.IsActive)
                .ToListAsync(ct);
            foreach (var k in legacy)
            {
                k.IsActive = false;
                k.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            await _notifications.NotifyMerchantUsersSafeAsync(
                platform.MerchantId,
                NotificationTypes.PlatformApproved,
                "تم اعتماد المنصة",
                $"تم اعتماد منصة «{platform.Name}» ({platform.Domain}) وإصدار مفتاح API.",
                "/merchant/platforms",
                new { platformId = platform.Id, status = platform.Status.ToString() },
                ct);
            var dto = Map(platform, includeMerchantName: true);
            return dto with { OneTimeApiKey = plain, HasOneTimeApiKey = true };
        }

        if (action is "reject" or "rejected")
        {
            platform.Status = PlatformStatus.Rejected;
            if (platform.ApiKey != null)
            {
                platform.ApiKey.IsActive = false;
                platform.ApiKey.UpdatedAtUtc = DateTime.UtcNow;
            }
            platform.OneTimeApiKey = null;
            await _db.SaveChangesAsync(ct);
            await _notifications.NotifyMerchantUsersSafeAsync(
                platform.MerchantId,
                NotificationTypes.PlatformRejected,
                "تم رفض المنصة",
                $"تم رفض منصة «{platform.Name}» ({platform.Domain}).",
                "/merchant/platforms",
                new { platformId = platform.Id, status = platform.Status.ToString() },
                ct);
            return Map(platform, includeMerchantName: true);
        }

        if (action is "suspend" or "suspended")
        {
            platform.Status = PlatformStatus.Suspended;
            if (platform.ApiKey != null)
            {
                platform.ApiKey.IsActive = false;
                platform.ApiKey.UpdatedAtUtc = DateTime.UtcNow;
            }
            platform.OneTimeApiKey = null;
            await _db.SaveChangesAsync(ct);
            await _notifications.NotifyMerchantUsersSafeAsync(
                platform.MerchantId,
                NotificationTypes.PlatformSuspended,
                "تم تعليق المنصة",
                $"تم تعليق منصة «{platform.Name}» ({platform.Domain}).",
                "/merchant/platforms",
                new { platformId = platform.Id, status = platform.Status.ToString() },
                ct);
            return Map(platform, includeMerchantName: true);
        }

        throw new ArgumentException("الإجراء يجب أن يكون approve أو reject أو suspend");
    }

    public async Task<MerchantPlatformDto> RegenerateKeyAsync(Guid merchantId, Guid platformId, CancellationToken ct = default)
    {
        var platform = await _db.MerchantPlatforms
            .Include(p => p.ApiKey)
            .FirstOrDefaultAsync(p => p.Id == platformId && p.MerchantId == merchantId, ct)
            ?? throw new InvalidOperationException("المنصة غير موجودة");

        if (platform.Status != PlatformStatus.Approved)
            throw new InvalidOperationException("يمكن توليد المفتاح للمنصات المعتمدة فقط");

        var plain = await IssueKeyAsync(platform, ct);
        await _db.SaveChangesAsync(ct);
        var dto = Map(platform);
        return dto with { OneTimeApiKey = plain, HasOneTimeApiKey = true };
    }

    public async Task<MerchantPlatformDto> SetLogoAsync(
        Guid merchantId,
        Guid platformId,
        string logoUrl,
        CancellationToken ct = default)
    {
        var platform = await _db.MerchantPlatforms
            .Include(p => p.ApiKey)
            .FirstOrDefaultAsync(p => p.Id == platformId && p.MerchantId == merchantId, ct)
            ?? throw new InvalidOperationException("المنصة غير موجودة");

        platform.LogoUrl = logoUrl;
        platform.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(platform);
    }

    public async Task<MerchantPlatformDto> ClearLogoAsync(Guid merchantId, Guid platformId, CancellationToken ct = default)
    {
        var platform = await _db.MerchantPlatforms
            .Include(p => p.ApiKey)
            .FirstOrDefaultAsync(p => p.Id == platformId && p.MerchantId == merchantId, ct)
            ?? throw new InvalidOperationException("المنصة غير موجودة");

        platform.LogoUrl = null;
        platform.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(platform);
    }

    public async Task<string> ClaimKeyAsync(Guid merchantId, Guid platformId, CancellationToken ct = default)
    {
        var platform = await _db.MerchantPlatforms
            .FirstOrDefaultAsync(p => p.Id == platformId && p.MerchantId == merchantId, ct)
            ?? throw new InvalidOperationException("المنصة غير موجودة");

        if (string.IsNullOrWhiteSpace(platform.OneTimeApiKey))
            throw new InvalidOperationException("لا يوجد مفتاح جاهز للاستلام. أعد توليد المفتاح إن لزم.");

        var key = _protector.Unprotect(platform.OneTimeApiKey);
        platform.OneTimeApiKey = null;
        platform.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return key;
    }

    public async Task<IReadOnlyList<string>> GetApprovedDomainsAsync(CancellationToken ct = default)
    {
        return await _db.MerchantPlatforms
            .Where(p => p.Status == PlatformStatus.Approved)
            .Select(p => p.Domain)
            .Distinct()
            .ToListAsync(ct);
    }

    private Task<string> IssueKeyAsync(MerchantPlatform platform, CancellationToken ct)
    {
        if (platform.ApiKey != null)
        {
            platform.ApiKey.IsActive = false;
            platform.ApiKey.MerchantPlatformId = null;
            platform.ApiKey.UpdatedAtUtc = DateTime.UtcNow;
            platform.ApiKey = null;
        }

        var (plain, prefix, hash) = _apiKeys.Generate();
        var entity = new ApiKey
        {
            MerchantId = platform.MerchantId,
            MerchantPlatformId = platform.Id,
            Name = $"Platform:{platform.Name}",
            KeyPrefix = prefix,
            KeyHash = hash,
            IsActive = true
        };
        _db.ApiKeys.Add(entity);
        platform.ApiKey = entity;
        platform.OneTimeApiKey = _protector.Protect(plain);
        return Task.FromResult(plain);
    }

    private static MerchantPlatformDto Map(MerchantPlatform p, bool includeMerchantName = false) => new(
        p.Id,
        p.MerchantId,
        includeMerchantName ? p.Merchant?.BusinessName : null,
        p.Name,
        p.Domain,
        p.LogoUrl,
        p.Status.ToString(),
        p.AdminNotes,
        p.CreatedAtUtc,
        p.UpdatedAtUtc,
        p.ReviewedAtUtc,
        p.ApiKey?.Id,
        p.ApiKey is { IsActive: true } ? p.ApiKey.KeyPrefix : null,
        !string.IsNullOrWhiteSpace(p.OneTimeApiKey),
        null);

    public static string NormalizeDomain(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("الدومين مطلوب");

        var raw = input.Trim().ToLowerInvariant();
        if (!raw.Contains("://", StringComparison.Ordinal))
            raw = "https://" + raw;

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
            throw new ArgumentException("صيغة الدومين غير صالحة");

        var host = uri.Host.Trim().TrimEnd('.');
        if (host.StartsWith("www.", StringComparison.Ordinal))
            host = host[4..];

        if (host is "localhost" || host.EndsWith(".localhost", StringComparison.Ordinal))
        {
            if (uri.IsDefaultPort) return host;
            return $"{host}:{uri.Port}";
        }

        if (host.Contains(':'))
            throw new ArgumentException("لا يُسمح برقم المنفذ إلا لـ localhost");

        if (host.Length < 3 || !host.Contains('.'))
            throw new ArgumentException("الدومين غير صالح");

        return host;
    }

    public static bool OriginMatchesDomain(string? originOrReferer, string platformDomain)
    {
        if (string.IsNullOrWhiteSpace(originOrReferer) || string.IsNullOrWhiteSpace(platformDomain))
            return false;

        if (!Uri.TryCreate(originOrReferer.Trim(), UriKind.Absolute, out var uri))
            return false;

        try
        {
            var host = NormalizeDomain(uri.GetLeftPart(UriPartial.Authority));
            return string.Equals(host, platformDomain, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
