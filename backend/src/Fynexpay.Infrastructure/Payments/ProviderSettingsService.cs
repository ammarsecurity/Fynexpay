using System.Text.Json;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Abstractions.Payments;
using Fynexpay.Domain.Entities;
using Fynexpay.Domain.Enums;
using Microsoft.EntityFrameworkCore;
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
    private ProviderRuntimeSettings? _cache;

    public ProviderSettingsService(IAppDbContext db, IOptions<PaymentProvidersOptions> bootstrap)
    {
        _db = db;
        _bootstrap = bootstrap;
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
        var zainLogo = settings.ZainCash?.LogoUrl;
        var fibLogo = settings.Fib?.LogoUrl;

        settings.Qi = ProviderBundleSettings.DefaultQi();
        settings.Qi.Enabled = true;
        settings.Qi.Priority = 0;
        settings.Qi.LogoUrl = qiLogo;

        settings.SuperQi = ProviderBundleSettings.DefaultSuperQi();
        settings.SuperQi.Enabled = true;
        settings.SuperQi.Priority = 3;
        settings.SuperQi.LogoUrl = superQiLogo;

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
        var env = s.ActiveEnv;
        return provider switch
        {
            PaymentProviderType.Fib => s.Fib.For(env),
            PaymentProviderType.ZainCash => s.ZainCash.For(env),
            PaymentProviderType.Qi => s.Qi.For(env),
            PaymentProviderType.SuperQi => ResolveSuperQiCredentials(s, env),
            _ => throw new ArgumentException("مزود غير صالح")
        };
    }

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
                (PaymentProviderType.SuperQi, s.SuperQi)
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

        return defaults;
    }

    private static void Normalize(ProviderRuntimeSettings s)
    {
        s.ActiveEnvironment = s.ActiveEnv == ProviderEnvironment.Production ? "Production" : "Test";
        s.Fib ??= ProviderBundleSettings.DefaultFib();
        s.ZainCash ??= ProviderBundleSettings.DefaultZainCash();
        s.Qi ??= ProviderBundleSettings.DefaultQi();
        s.SuperQi ??= ProviderBundleSettings.DefaultSuperQi();
        s.Fib.Test ??= new ProviderEnvCredentials();
        s.Fib.Production ??= new ProviderEnvCredentials();
        s.ZainCash.Test ??= new ProviderEnvCredentials();
        s.ZainCash.Production ??= new ProviderEnvCredentials();
        s.Qi.Test ??= new ProviderEnvCredentials();
        s.Qi.Production ??= new ProviderEnvCredentials();
        s.SuperQi.Test ??= new ProviderEnvCredentials();
        s.SuperQi.Production ??= new ProviderEnvCredentials();
        if (string.IsNullOrWhiteSpace(s.Fib.LogoUrl)) s.Fib.LogoUrl = "/providers/fib.svg";
        if (string.IsNullOrWhiteSpace(s.ZainCash.LogoUrl)) s.ZainCash.LogoUrl = "/providers/zaincash.svg";
        if (string.IsNullOrWhiteSpace(s.Qi.LogoUrl)) s.Qi.LogoUrl = "/providers/qi.svg";
        if (string.IsNullOrWhiteSpace(s.SuperQi.LogoUrl)) s.SuperQi.LogoUrl = "/providers/superqi.svg";
    }

    private static ProviderRuntimeSettings Clone(ProviderRuntimeSettings s) =>
        JsonSerializer.Deserialize<ProviderRuntimeSettings>(JsonSerializer.Serialize(s, JsonOpts), JsonOpts)!;
}
