using Fynexpay.Application.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Fynexpay.Api.Cors;

public class DynamicCorsPolicyProvider : ICorsPolicyProvider
{
    private static readonly string[] DevOrigins =
    [
        "http://localhost:5173",
        "http://127.0.0.1:5173",
        "http://localhost:5174",
        "http://127.0.0.1:5174",
        "http://localhost:5080",
        "http://127.0.0.1:5080"
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _lock = new();
    private List<string> _cachedOrigins = new(DevOrigins);
    private DateTime _cachedAtUtc = DateTime.MinValue;

    public DynamicCorsPolicyProvider(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        await RefreshCacheIfNeededAsync(context.RequestAborted);

        var policy = new CorsPolicy
        {
            SupportsCredentials = false
        };
        foreach (var o in _cachedOrigins)
            policy.Origins.Add(o);
        policy.Headers.Add("*");
        policy.Methods.Add("*");
        policy.PreflightMaxAge = TimeSpan.FromMinutes(10);
        return policy;
    }

    private async Task RefreshCacheIfNeededAsync(CancellationToken ct)
    {
        if (DateTime.UtcNow - _cachedAtUtc < TimeSpan.FromSeconds(30))
            return;

        using var scope = _scopeFactory.CreateScope();
        var platforms = scope.ServiceProvider.GetRequiredService<MerchantPlatformService>();
        IReadOnlyList<string> domains;
        try
        {
            domains = await platforms.GetApprovedDomainsAsync(ct);
        }
        catch
        {
            domains = Array.Empty<string>();
        }

        var origins = new HashSet<string>(DevOrigins, StringComparer.OrdinalIgnoreCase);
        foreach (var domain in domains)
        {
            if (domain.StartsWith("localhost", StringComparison.OrdinalIgnoreCase))
            {
                origins.Add($"http://{domain}");
                origins.Add($"https://{domain}");
            }
            else
            {
                origins.Add($"https://{domain}");
                origins.Add($"http://{domain}");
            }
        }

        lock (_lock)
        {
            _cachedOrigins = origins.ToList();
            _cachedAtUtc = DateTime.UtcNow;
        }
    }
}
