using Fynexpay.Domain.Enums;

namespace Fynexpay.Application.Abstractions.Payments;

public enum ProviderEnvironment
{
    Test = 0,
    Production = 1
}

public class ProviderRuntimeSettings
{
    public string ActiveEnvironment { get; set; } = "Test";
    public bool UseMockWhenMissingCredentials { get; set; } = false;
    public ProviderBundleSettings Fib { get; set; } = ProviderBundleSettings.DefaultFib();
    public ProviderBundleSettings ZainCash { get; set; } = ProviderBundleSettings.DefaultZainCash();
    public ProviderBundleSettings Qi { get; set; } = ProviderBundleSettings.DefaultQi();
    /// <summary>Pay with SuperQi (QI Gate ALIPAY method) — https://developers-gate.qi.iq/docs/category/pay-with-superqi</summary>
    public ProviderBundleSettings SuperQi { get; set; } = ProviderBundleSettings.DefaultSuperQi();
    /// <summary>Al Qaseh hosted payment page — https://docs.alqaseh.com/payment-api</summary>
    public ProviderBundleSettings Alqaseh { get; set; } = ProviderBundleSettings.DefaultAlqaseh();

    public ProviderEnvironment ActiveEnv =>
        ActiveEnvironment.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? ProviderEnvironment.Production
            : ProviderEnvironment.Test;
}

public class ProviderBundleSettings
{
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }
    /// <summary>Customer-facing display name (checkout + dashboard).</summary>
    public string DisplayName { get; set; } = "";
    /// <summary>Public relative URL e.g. /uploads/providers/fib.png</summary>
    public string? LogoUrl { get; set; }
    public ProviderEnvCredentials Test { get; set; } = new();
    public ProviderEnvCredentials Production { get; set; } = new();

    public ProviderEnvCredentials For(ProviderEnvironment env) =>
        env == ProviderEnvironment.Production ? Production : Test;

    public string ResolveDisplayName(string fallback) =>
        string.IsNullOrWhiteSpace(DisplayName) ? fallback : DisplayName.Trim();

    public static ProviderBundleSettings DefaultFib() => new()
    {
        Enabled = true,
        Priority = 2,
        DisplayName = "FIB",
        LogoUrl = "/providers/fib.svg",
        Test = new ProviderEnvCredentials
        {
            AuthUrl = "https://fib.stage.fib.iq/auth/realms/fib-online-shop/protocol/openid-connect/token",
            BaseUrl = "https://fib.stage.fib.iq/protected/v1"
        },
        Production = new ProviderEnvCredentials
        {
            AuthUrl = "https://fib.fib.iq/auth/realms/fib-online-shop/protocol/openid-connect/token",
            BaseUrl = "https://fib.fib.iq/protected/v1"
        }
    };

    public static ProviderBundleSettings DefaultZainCash() => new()
    {
        Enabled = true,
        Priority = 1,
        DisplayName = "ZainCash",
        LogoUrl = "/providers/zaincash.svg",
        Test = new ProviderEnvCredentials
        {
            // Official ZainCash PG API v2 UAT (docs.zaincash.iq) — credentials via admin/env only
            BaseUrl = "https://pg-api-uat.zaincash.iq",
            AuthUrl = "https://pg-api-uat.zaincash.iq/oauth2/token"
        },
        Production = new ProviderEnvCredentials
        {
            BaseUrl = "https://pg-api.zaincash.iq",
            AuthUrl = "https://pg-api.zaincash.iq/oauth2/token"
        }
    };

    public static ProviderBundleSettings DefaultQi() => new()
    {
        Enabled = true,
        Priority = 0,
        DisplayName = "QI Card",
        LogoUrl = "/providers/qi.svg",
        Test = new ProviderEnvCredentials
        {
            BaseUrl = "https://uat-sandbox-3ds-api.qi.iq/api/v1"
        },
        Production = new ProviderEnvCredentials
        {
            BaseUrl = "https://api.gate.qi.iq/api/v1"
        }
    };

    /// <summary>
    /// SuperQi wallet via QI Gate (SDK ALIPAY method).
    /// Docs: https://developers-gate.qi.iq/docs/category/pay-with-superqi
    /// </summary>
    public static ProviderBundleSettings DefaultSuperQi() => new()
    {
        Enabled = true,
        Priority = 3,
        DisplayName = "SuperQi",
        LogoUrl = "/providers/superqi.svg",
        Test = new ProviderEnvCredentials
        {
            BaseUrl = "https://uat-sandbox-3ds-api.qi.iq/api/v1"
        },
        Production = new ProviderEnvCredentials
        {
            BaseUrl = "https://api.gate.qi.iq/api/v1"
        }
    };

    /// <summary>
    /// Al Qaseh Payment Gateway (non-PCI hosted page).
    /// Docs: https://docs.alqaseh.com/payment-api
    /// AuthUrl holds the pay-page host (e.g. https://pay-test.alqaseh.com).
    /// </summary>
    public static ProviderBundleSettings DefaultAlqaseh() => new()
    {
        Enabled = true,
        Priority = 4,
        DisplayName = "Alqaseh",
        LogoUrl = "/providers/alqaseh.svg",
        Test = new ProviderEnvCredentials
        {
            BaseUrl = "https://api-test.alqaseh.com/v1",
            AuthUrl = "https://pay-test.alqaseh.com"
        },
        Production = new ProviderEnvCredentials
        {
            BaseUrl = "https://api.alqaseh.com/v1",
            AuthUrl = "https://pay.alqaseh.com"
        }
    };
}

public class ProviderEnvCredentials
{
    public string AuthUrl { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string MerchantId { get; set; } = "";
    public string Secret { get; set; } = "";
    public string Msisdn { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string TerminalId { get; set; } = "";
    /// <summary>Optional shared secret; when set, inbound webhooks must send X-Webhook-Secret.</summary>
    public string WebhookSecret { get; set; } = "";
}

public interface IProviderSettingsService
{
    Task<ProviderRuntimeSettings> GetAsync(CancellationToken ct = default);
    Task<ProviderRuntimeSettings> SaveAsync(ProviderRuntimeSettings settings, CancellationToken ct = default);
    Task<ProviderRuntimeSettings> SetEnvironmentAsync(string environment, CancellationToken ct = default);
    Task<ProviderRuntimeSettings> LoadOfficialSandboxDemoAsync(CancellationToken ct = default);
    Task<ProviderEnvCredentials> GetActiveCredentialsAsync(PaymentProviderType provider, CancellationToken ct = default);
    Task<ProviderEnvCredentials> GetCredentialsAsync(PaymentProviderType provider, ProviderEnvironment environment, CancellationToken ct = default);
    Task<bool> MatchesWebhookSecretAsync(PaymentProviderType provider, IDictionary<string, string> headers, CancellationToken ct = default);
    Task<bool> IsEnabledAsync(PaymentProviderType provider, CancellationToken ct = default);
    Task<bool> UseMockAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PaymentProviderType>> GetEnabledOrderedAsync(CancellationToken ct = default);
}
