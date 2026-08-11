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
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly object _lock = new();
    private List<string> _cachedOrigins = new(DevOrigins);
    private DateTime _cachedAtUtc = DateTime.MinValue;

    public DynamicCorsPolicyProvider(IServiceScopeFactory scopeFactory, IHostEnvironment env, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _env = env;
        _config = config;
    }

    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        await RefreshCacheIfNeededAsync(context.RequestAborted);

        var policy = new CorsPolicy
        {
            SupportsCredentials = false
        };
        foreach (var o in _cachedOrigins)
            policy.Origins.Add(o);
        policy.Headers.Add("Authorization");
        policy.Headers.Add("Content-Type");
        policy.Headers.Add("X-Api-Key");
        policy.Headers.Add("X-Idempotency-Key");
        policy.Headers.Add("Accept");
        policy.Headers.Add("Origin");
        policy.Methods.Add("GET");
        policy.Methods.Add("POST");
        policy.Methods.Add("PUT");
        policy.Methods.Add("PATCH");
        policy.Methods.Add("DELETE");
        policy.Methods.Add("OPTIONS");
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

        var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_env.IsDevelopment())
        {
            foreach (var o in DevOrigins)
                origins.Add(o);
        }

        var allowed = _config["App:AllowedOrigins"];
        if (!string.IsNullOrWhiteSpace(allowed))
        {
            foreach (var part in allowed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                origins.Add(part);
        }

        foreach (var domain in domains)
        {
            if (domain.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
                || domain.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                origins.Add($"http://{domain}");
                origins.Add($"https://{domain}");
            }
            else
            {
                // Production hosts: HTTPS only (mitigate MITM via cleartext origins).
                origins.Add($"https://{domain}");
            }
        }

        lock (_lock)
        {
            _cachedOrigins = origins.Count > 0 ? origins.ToList() : (_env.IsDevelopment() ? DevOrigins.ToList() : new List<string>());
            _cachedAtUtc = DateTime.UtcNow;
        }
    }
}
