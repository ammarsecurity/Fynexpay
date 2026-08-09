using Fynexpay.Application.Abstractions;
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

        match.LastUsedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(context.RequestAborted);

        context.Items["MerchantId"] = match.MerchantId;
        context.Items["ApiKeyId"] = match.Id;
        await _next(context);
    }
}
