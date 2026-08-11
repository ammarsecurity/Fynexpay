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

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var keyValues) || string.IsNullOrWhiteSpace(keyValues))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "مطلوب مفتاح X-Api-Key" });
            return;
        }

        var plain = keyValues.ToString();
        var prefix = plain.Length >= 10 ? plain[..10] : plain;
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
            return;
        }

        if (match.Merchant.Status != MerchantStatus.Active)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "حساب التاجر غير مفعّل" });
            return;
        }

        if (match.MerchantPlatformId == null || match.MerchantPlatform == null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "هذا المفتاح غير مربوط بمنصة معتمدة. أضف منصة واطلب موافقة الإدارة."
            });
            return;
        }

        if (match.MerchantPlatform.Status != PlatformStatus.Approved)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "منصة هذا المفتاح غير معتمدة أو معلّقة" });
            return;
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
            return;
        }

        match.LastUsedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(context.RequestAborted);

        // Legacy fx_ keys (without live/test prefix) default to live/production.
        var isTest = match.IsTest
                     || match.KeyPrefix.StartsWith("fx_test_", StringComparison.OrdinalIgnoreCase)
                     || plain.StartsWith("fx_test_", StringComparison.OrdinalIgnoreCase);

        context.Items["MerchantId"] = match.MerchantId;
        context.Items["ApiKeyId"] = match.Id;
        context.Items["MerchantPlatformId"] = match.MerchantPlatformId.Value;
        context.Items["ApiKeyIsTest"] = isTest;
        await _next(context);
    }
}
