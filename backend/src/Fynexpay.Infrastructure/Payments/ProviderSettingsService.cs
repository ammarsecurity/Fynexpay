using System.Text.Json;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Abstractions.Payments;
using Fynexpay.Domain.Entities;
using Fynexpay.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Fynexpay.Infrastructure.Payments;

public class ProviderSettingsService : IProviderSettingsService
{
    public const string SettingsKey = "payment_providers_runtime";
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IAppDbContext _db;
    private readonly IOptions<PaymentProvidersOptions> _bootstrap;
    private readonly IHostEnvironment _env;
    private ProviderRuntimeSettings? _cache;

    public ProviderSettingsService(
        IAppDbContext db,
        IOptions<PaymentProvidersOptions> bootstrap,
        IHostEnvironment env)
    {
        _db = db;
        _bootstrap = bootstrap;
        _env = env;
    }

    public async Task<ProviderRuntimeSettings> GetAsync(CancellationToken ct = default)
    {
        if (_cache != null) return Clone(_cache);

        var row = await _db.PlatformSettings.FirstOrDefaultAsync(s => s.Key == SettingsKey, ct);
        if (row == null || string.IsNullOrWhiteSpace(row.Value))
        {
            var seeded = FromBootstrap(_bootstrap.Value);
            await PersistAsync(seeded, ct);
            _cache = seeded;
            return Clone(seeded);
        }

        var settings = JsonSerializer.Deserialize<ProviderRuntimeSettings>(row.Value, JsonOpts)
                       ?? FromBootstrap(_bootstrap.Value);

        // ترقية تلقائية لإعدادات التيست الفارغة إلى الديمو الرسمي
        if (string.IsNullOrWhiteSpace(settings.Qi?.Test?.Username))
        {
            ApplyOfficialSandboxDemo(settings);
            await PersistAsync(settings, ct);
        }

        Normalize(settings);
        _cache = settings;
        return Clone(settings);
    }

    public async Task<ProviderRuntimeSettings> SaveAsync(ProviderRuntimeSettings settings, CancellationToken ct = default)
    {
        Normalize(settings);
        await PersistAsync(settings, ct);
        _cache = settings;
        return Clone(settings);
    }

    public async Task<ProviderRuntimeSettings> SetEnvironmentAsync(string environment, CancellationToken ct = default)
    {
        if (!environment.Equals("Test", StringComparison.OrdinalIgnoreCase) &&
            !environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("البيئة يجب أن تكون Test أو Production");

        var settings = await GetAsync(ct);
        settings.ActiveEnvironment = environment.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? "Production"
            : "Test";
        return await SaveAsync(settings, ct);
    }

    public async Task<ProviderRuntimeSettings> LoadOfficialSandboxDemoAsync(CancellationToken ct = default)
    {
        if (!_env.IsDevelopment())
            throw new InvalidOperationException("تحميل بيانات الـ sandbox مسموح في بيئة التطوير فقط");

        var settings = await GetAsync(ct);
        ApplyOfficialSandboxDemo(settings);
        return await SaveAsync(settings, ct);
    }

    private static void ApplyOfficialSandboxDemo(ProviderRuntimeSettings settings)
    {
        settings.ActiveEnvironment = "Test";
        settings.UseMockWhenMissingCredentials = true;

        var qiLogo = settings.Qi?.LogoUrl;
        var superQiLogo = settings.SuperQi?.LogoUrl;
        var alqasehLogo = settings.Alqaseh?.LogoUrl;
        var zainLogo = settings.ZainCash?.LogoUrl;
        var fibLogo = settings.Fib?.LogoUrl;

        settings.Qi = ProviderBundleSettings.DefaultQi();
        settings.Qi.Enabled = true;
        settings.Qi.Priority = 0;
        settings.Qi.LogoUrl = qiLogo;
        // Public Qi Gate UAT credentials (official docs) — Development only via this action.
        settings.Qi.Test.Username = "paymentgatewaytest";
        settings.Qi.Test.Password = "WHaNFE5C3qlChqNbAzH4";
        settings.Qi.Test.TerminalId = "237984";

        settings.SuperQi = ProviderBundleSettings.DefaultSuperQi();
        settings.SuperQi.Enabled = true;
        settings.SuperQi.Priority = 3;
        settings.SuperQi.LogoUrl = superQiLogo;
        settings.SuperQi.Test.Username = "paymentgatewaytest";
        settings.SuperQi.Test.Password = "WHaNFE5C3qlChqNbAzH4";
        settings.SuperQi.Test.TerminalId = "237984";

        settings.Alqaseh = ProviderBundleSettings.DefaultAlqaseh();
        settings.Alqaseh.Enabled = true;
        settings.Alqaseh.Priority = 4;
        settings.Alqaseh.LogoUrl = alqasehLogo;
        // Public Alqaseh sandbox credentials (official docs) — Development only via this action.
        settings.Alqaseh.Test.ClientId = "public_test";
        settings.Alqaseh.Test.ClientSecret = "Lr10yWWmm1dXLoI7VgXCrQVnlq13c1G0";

        settings.ZainCash = ProviderBundleSettings.DefaultZainCash();
        settings.ZainCash.Enabled = true;
        settings.ZainCash.Priority = 1;
        settings.ZainCash.LogoUrl = zainLogo;

        settings.Fib ??= ProviderBundleSettings.DefaultFib();
        settings.Fib.Enabled = true;
        settings.Fib.Priority = 2;
        settings.Fib.LogoUrl = fibLogo;
        settings.Fib.Test ??= new ProviderEnvCredentials();
        settings.Fib.Test.AuthUrl = "https://fib.stage.fib.iq/auth/realms/fib-online-shop/protocol/openid-connect/token";
        settings.Fib.Test.BaseUrl = "https://fib.stage.fib.iq/protected/v1";
    }

    public async Task<ProviderEnvCredentials> GetActiveCredentialsAsync(PaymentProviderType provider, CancellationToken ct = default)
    {
        var s = await GetAsync(ct);
        var env = ProviderEnvironmentScope.Current ?? s.ActiveEnv;
        return ResolveCredentials(s, provider, env);
    }

    public async Task<ProviderEnvCredentials> GetCredentialsAsync(
        PaymentProviderType provider,
        ProviderEnvironment environment,
        CancellationToken ct = default)
    {
        var s = await GetAsync(ct);
        return ResolveCredentials(s, provider, environment);
    }

    public async Task<bool> MatchesWebhookSecretAsync(
        PaymentProviderType provider,
        IDictionary<string, string> headers,
        CancellationToken ct = default)
    {
        var s = await GetAsync(ct);
        var secrets = new[]
            {
                ResolveCredentials(s, provider, ProviderEnvironment.Test).WebhookSecret,
                ResolveCredentials(s, provider, ProviderEnvironment.Production).WebhookSecret
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (secrets.Count == 0)
            return true;

        if (!headers.TryGetValue("X-Webhook-Secret", out var provided)
            && !headers.TryGetValue("X-Fynexpay-Provider-Secret", out provided))
            return false;

        var b = System.Text.Encoding.UTF8.GetBytes(provided.Trim());
        foreach (var secret in secrets)
        {
            var a = System.Text.Encoding.UTF8.GetBytes(secret);
            if (a.Length == b.Length && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, b))
                return true;
        }

        return false;
    }

    private static ProviderEnvCredentials ResolveCredentials(
        ProviderRuntimeSettings s,
        PaymentProviderType provider,
        ProviderEnvironment env) =>
        provider switch
        {
            PaymentProviderType.Fib => s.Fib.For(env),
            PaymentProviderType.ZainCash => s.ZainCash.For(env),
            PaymentProviderType.Qi => s.Qi.For(env),
            PaymentProviderType.SuperQi => ResolveSuperQiCredentials(s, env),
            PaymentProviderType.Alqaseh => s.Alqaseh.For(env),
            _ => throw new ArgumentException("مزود غير صالح")
        };

    private static ProviderEnvCredentials ResolveSuperQiCredentials(ProviderRuntimeSettings s, ProviderEnvironment env)
    {
        var creds = s.SuperQi.For(env);
        // إن لم تُضبط credentials خاصة بـ SuperQi، استخدم بيانات QI Gate
        if (string.IsNullOrWhiteSpace(creds.Username) || string.IsNullOrWhiteSpace(creds.Password))
        {
            var qi = s.Qi.For(env);
            return new ProviderEnvCredentials
            {
                BaseUrl = string.IsNullOrWhiteSpace(creds.BaseUrl) ? qi.BaseUrl : creds.BaseUrl,
                Username = string.IsNullOrWhiteSpace(creds.Username) ? qi.Username : creds.Username,
                Password = string.IsNullOrWhiteSpace(creds.Password) ? qi.Password : creds.Password,
                TerminalId = string.IsNullOrWhiteSpace(creds.TerminalId) ? qi.TerminalId : creds.TerminalId
            };
        }

        return creds;
    }

    public async Task<bool> IsEnabledAsync(PaymentProviderType provider, CancellationToken ct = default)
    {
        var s = await GetAsync(ct);
        return provider switch
        {
            PaymentProviderType.Fib => s.Fib.Enabled,
            PaymentProviderType.ZainCash => s.ZainCash.Enabled,
            PaymentProviderType.Qi => s.Qi.Enabled,
            PaymentProviderType.SuperQi => s.SuperQi.Enabled,
            PaymentProviderType.Alqaseh => s.Alqaseh.Enabled,
            _ => false
        };
    }

    public async Task<bool> UseMockAsync(CancellationToken ct = default)
    {
        var s = await GetAsync(ct);
        return s.UseMockWhenMissingCredentials;
    }

    public async Task<IReadOnlyList<PaymentProviderType>> GetEnabledOrderedAsync(CancellationToken ct = default)
    {
        var s = await GetAsync(ct);
        return new[]
            {
                (PaymentProviderType.Fib, s.Fib),
                (PaymentProviderType.ZainCash, s.ZainCash),
                (PaymentProviderType.Qi, s.Qi),
                (PaymentProviderType.SuperQi, s.SuperQi),
                (PaymentProviderType.Alqaseh, s.Alqaseh)
            }
            .Where(x => x.Item2.Enabled)
            .OrderBy(x => x.Item2.Priority)
            .Select(x => x.Item1)
            .ToList();
    }

    private async Task PersistAsync(ProviderRuntimeSettings settings, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(settings, JsonOpts);
        var row = await _db.PlatformSettings.FirstOrDefaultAsync(s => s.Key == SettingsKey, ct);
        if (row == null)
        {
            _db.PlatformSettings.Add(new PlatformSetting
            {
                Key = SettingsKey,
                Value = json
            });
        }
        else
        {
            row.Value = json;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static ProviderRuntimeSettings FromBootstrap(PaymentProvidersOptions o)
    {
        var defaults = new ProviderRuntimeSettings
        {
            ActiveEnvironment = "Test",
            UseMockWhenMissingCredentials = o.UseMockWhenMissingCredentials
        };

        defaults.Fib.Enabled = o.Fib.Enabled;
        defaults.Fib.Priority = 0;
        defaults.Fib.Test.AuthUrl = o.Fib.AuthUrl;
        defaults.Fib.Test.BaseUrl = o.Fib.BaseUrl;
        defaults.Fib.Test.ClientId = o.Fib.ClientId;
        defaults.Fib.Test.ClientSecret = o.Fib.ClientSecret;

        defaults.ZainCash.Enabled = o.ZainCash.Enabled;
        defaults.ZainCash.Priority = 1;
        defaults.ZainCash.Test.BaseUrl = o.ZainCash.BaseUrl;
        defaults.ZainCash.Test.AuthUrl = o.ZainCash.AuthUrl;
        defaults.ZainCash.Test.ClientId = o.ZainCash.ClientId;
        defaults.ZainCash.Test.ClientSecret = o.ZainCash.ClientSecret;
        defaults.ZainCash.Test.MerchantId = o.ZainCash.MerchantId;
        defaults.ZainCash.Test.Secret = o.ZainCash.Secret;
        defaults.ZainCash.Test.Msisdn = o.ZainCash.Msisdn;

        defaults.Qi.Enabled = o.Qi.Enabled;
        defaults.Qi.Priority = 2;
        defaults.Qi.Test.BaseUrl = o.Qi.BaseUrl;
        defaults.Qi.Test.Username = o.Qi.Username;
        defaults.Qi.Test.Password = o.Qi.Password;
        defaults.Qi.Test.TerminalId = o.Qi.TerminalId;

        defaults.SuperQi = ProviderBundleSettings.DefaultSuperQi();
        defaults.SuperQi.Priority = 3;

        defaults.Alqaseh = ProviderBundleSettings.DefaultAlqaseh();
        defaults.Alqaseh.Priority = 4;

        return defaults;
    }

    private static void Normalize(ProviderRuntimeSettings s)
    {
        s.ActiveEnvironment = s.ActiveEnv == ProviderEnvironment.Production ? "Production" : "Test";
        s.Fib ??= ProviderBundleSettings.DefaultFib();
        s.ZainCash ??= ProviderBundleSettings.DefaultZainCash();
        s.Qi ??= ProviderBundleSettings.DefaultQi();
        s.SuperQi ??= ProviderBundleSettings.DefaultSuperQi();
        s.Alqaseh ??= ProviderBundleSettings.DefaultAlqaseh();
        s.Fib.Test ??= new ProviderEnvCredentials();
        s.Fib.Production ??= new ProviderEnvCredentials();
        s.ZainCash.Test ??= new ProviderEnvCredentials();
        s.ZainCash.Production ??= new ProviderEnvCredentials();
        s.Qi.Test ??= new ProviderEnvCredentials();
        s.Qi.Production ??= new ProviderEnvCredentials();
        s.SuperQi.Test ??= new ProviderEnvCredentials();
        s.SuperQi.Production ??= new ProviderEnvCredentials();
        s.Alqaseh.Test ??= new ProviderEnvCredentials();
        s.Alqaseh.Production ??= new ProviderEnvCredentials();
        if (string.IsNullOrWhiteSpace(s.Fib.LogoUrl)) s.Fib.LogoUrl = "/providers/fib.svg";
        if (string.IsNullOrWhiteSpace(s.ZainCash.LogoUrl)) s.ZainCash.LogoUrl = "/providers/zaincash.svg";
        if (string.IsNullOrWhiteSpace(s.Qi.LogoUrl)) s.Qi.LogoUrl = "/providers/qi.svg";
        if (string.IsNullOrWhiteSpace(s.SuperQi.LogoUrl)) s.SuperQi.LogoUrl = "/providers/superqi.svg";
        if (string.IsNullOrWhiteSpace(s.Alqaseh.LogoUrl)) s.Alqaseh.LogoUrl = "/providers/alqaseh.svg";
        s.Fib.DisplayName = ClampName(s.Fib.DisplayName, "FIB");
        s.ZainCash.DisplayName = ClampName(s.ZainCash.DisplayName, "ZainCash");
        s.Qi.DisplayName = ClampName(s.Qi.DisplayName, "QI Card");
        s.SuperQi.DisplayName = ClampName(s.SuperQi.DisplayName, "SuperQi");
        s.Alqaseh.DisplayName = ClampName(s.Alqaseh.DisplayName, "Alqaseh");
    }

    private static string ClampName(string? value, string fallback)
    {
        var name = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return name.Length > 64 ? name[..64].Trim() : name;
    }

    private static ProviderRuntimeSettings Clone(ProviderRuntimeSettings s) =>
        JsonSerializer.Deserialize<ProviderRuntimeSettings>(JsonSerializer.Serialize(s, JsonOpts), JsonOpts)!;
}
