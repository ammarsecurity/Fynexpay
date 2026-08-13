using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Services;
using Fynexpay.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Fynexpay.Api.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IAppDbContext db, IApiKeyService apiKeys)
    {
        if (!context.Request.Path.StartsWithSegments("/v1"))
        {
            await _next(context);
            return;
        }

        var merchantKey = await AuthenticateMerchantBearerAsync(context, db, apiKeys);
        if (merchantKey == null)
            return;

        var isPaymentRoute = context.Request.Path.StartsWithSegments("/v1/payments");
        if (isPaymentRoute)
        {
            var platformOk = await AuthenticatePlatformApiKeyAsync(context, db, apiKeys, merchantKey.MerchantId);
            if (!platformOk)
                return;
        }

        merchantKey.LastUsedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(context.RequestAborted);
        await _next(context);
    }

    private static async Task<Domain.Entities.ApiKey?> AuthenticateMerchantBearerAsync(
        HttpContext context, IAppDbContext db, IApiKeyService apiKeys)
    {
        var header = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "مطلوب Authorization: Bearer بمفتاح التاجر (fx_merch_)" });
            return null;
        }

        var plain = header["Bearer ".Length..].Trim();
        if (LooksLikeJwt(plain) || !plain.StartsWith("fx_merch_", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "لا تستخدم توكن تسجيل الدخول. استخدم مفتاح التاجر fx_merch_ من صفحة المفاتيح."
            });
            return null;
        }

        var prefix = plain.Length >= 12 ? plain[..12] : plain;
        var candidates = await db.ApiKeys
            .Include(k => k.Merchant)
            .Where(k => k.IsActive && k.MerchantPlatformId == null && k.KeyPrefix == prefix)
            .ToListAsync(context.RequestAborted);

        var match = candidates.FirstOrDefault(k => apiKeys.Verify(plain, k.KeyHash));
        if (match == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "مفتاح التاجر غير صالح" });
            return null;
        }

        if (match.Merchant.Status != MerchantStatus.Active)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "حساب التاجر غير مفعّل" });
            return null;
        }

        context.Items["MerchantId"] = match.MerchantId;
        context.Items["MerchantApiKeyId"] = match.Id;
        return match;
    }

    private static async Task<bool> AuthenticatePlatformApiKeyAsync(
        HttpContext context, IAppDbContext db, IApiKeyService apiKeys, Guid merchantId)
    {
        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var keyValues) || string.IsNullOrWhiteSpace(keyValues))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "مسارات الدفع تتطلب مفتاح المنصة في X-Api-Key" });
            return false;
        }

        var plain = keyValues.ToString().Trim();
        var prefix = plain.Length >= 12 ? plain[..12] : plain;
        var candidates = await db.ApiKeys
            .Include(k => k.Merchant)
            .Include(k => k.MerchantPlatform)
            .Where(k => k.IsActive && k.KeyPrefix == prefix)
            .ToListAsync(context.RequestAborted);

        var match = candidates.FirstOrDefault(k => apiKeys.Verify(plain, k.KeyHash));
        if (match == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "مفتاح API غير صالح" });
            return false;
        }

        if (match.MerchantId != merchantId)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "مفتاح المنصة لا يعود لنفس التاجر" });
            return false;
        }

        if (match.MerchantPlatformId == null || match.MerchantPlatform == null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "هذا المفتاح غير مربوط بمنصة معتمدة. أضف منصة واطلب موافقة الإدارة."
            });
            return false;
        }

        if (match.MerchantPlatform.Status != PlatformStatus.Approved)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "منصة هذا المفتاح غير معتمدة أو معلّقة" });
            return false;
        }

        var origin = context.Request.Headers.Origin.FirstOrDefault();
        var referer = context.Request.Headers.Referer.FirstOrDefault();
        var browserOrigin = !string.IsNullOrWhiteSpace(origin) ? origin : null;
        if (browserOrigin == null && !string.IsNullOrWhiteSpace(referer))
            browserOrigin = referer;

        if (!string.IsNullOrWhiteSpace(browserOrigin) &&
            !MerchantPlatformService.OriginMatchesDomain(browserOrigin, match.MerchantPlatform.Domain))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                message = $"طلب مرفوض: الدومين غير مسموح لهذه المنصة ({match.MerchantPlatform.Domain})"
            });
            return false;
        }

        match.LastUsedAtUtc = DateTime.UtcNow;

        var isTest = match.IsTest
                     || match.KeyPrefix.StartsWith("fx_test_", StringComparison.OrdinalIgnoreCase)
                     || plain.StartsWith("fx_test_", StringComparison.OrdinalIgnoreCase);

        context.Items["ApiKeyId"] = match.Id;
        context.Items["MerchantPlatformId"] = match.MerchantPlatformId.Value;
        context.Items["ApiKeyIsTest"] = isTest;
        return true;
    }

    private static bool LooksLikeJwt(string token)
    {
        var parts = token.Split('.');
        return parts.Length == 3 && parts.All(p => p.Length > 0);
    }
}
