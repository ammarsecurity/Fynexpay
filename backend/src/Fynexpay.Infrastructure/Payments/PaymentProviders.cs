using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Fynexpay.Application.Abstractions.Payments;
using Fynexpay.Application.Services;
using Fynexpay.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fynexpay.Infrastructure.Payments;

public class PaymentProvidersOptions
{
    public FibOptions Fib { get; set; } = new();
    public ZainCashOptions ZainCash { get; set; } = new();
    public QiOptions Qi { get; set; } = new();
    public bool UseMockWhenMissingCredentials { get; set; } = false;
}

public class FibOptions
{
    public bool Enabled { get; set; } = true;
    public string AuthUrl { get; set; } = "https://fib.stage.fib.iq/auth/realms/fib-online-shop/protocol/openid-connect/token";
    public string BaseUrl { get; set; } = "https://fib.stage.fib.iq/protected/v1";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}

public class ZainCashOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "https://test.zaincash.iq";
    public string AuthUrl { get; set; } = "https://test.zaincash.iq/auth/token";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Msisdn { get; set; } = "";
    public string MerchantId { get; set; } = "";
    public string Secret { get; set; } = "";
    public bool UseLegacyJwtInit { get; set; } = true;
}

public class QiOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "https://api.gate.qi.iq";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string TerminalId { get; set; } = "";
}

public class PaymentProviderResolver : IPaymentProviderResolver
{
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly IProviderSettingsService _settings;

    public PaymentProviderResolver(
        IEnumerable<IPaymentProvider> providers,
        IProviderSettingsService settings)
    {
        _providers = providers;
        _settings = settings;
    }

    public IPaymentProvider Resolve(PaymentProviderType type)
    {
        if (type == PaymentProviderType.Auto)
            type = ResolveAuto();

        return _providers.FirstOrDefault(p => p.ProviderType == type)
            ?? throw new InvalidOperationException($"مزود الدفع غير متاح: {type}");
    }

    public PaymentProviderType ResolveAuto()
    {
        var enabled = GetEnabledProviders();
        if (enabled.Count == 0)
            throw new InvalidOperationException("لا يوجد مزود دفع مفعّل");
        return enabled[0];
    }

    public IReadOnlyList<PaymentProviderType> GetEnabledProviders()
        => GetEnabledProvidersAsync().GetAwaiter().GetResult();

    public Task<IReadOnlyList<PaymentProviderType>> GetEnabledProvidersAsync(CancellationToken ct = default)
        => _settings.GetEnabledOrderedAsync(ct);
}

public abstract class HttpPaymentProviderBase
{
    protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly ILogger Logger;

    protected HttpPaymentProviderBase(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        HttpClientFactory = httpClientFactory;
        Logger = logger;
    }

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

public class MockPaymentProvider : IPaymentProvider
{
    private readonly PaymentProviderType _type;
    private readonly string _checkoutBaseUrl;

    public MockPaymentProvider(PaymentProviderType type, string checkoutBaseUrl)
    {
        _type = type;
        _checkoutBaseUrl = string.IsNullOrWhiteSpace(checkoutBaseUrl)
            ? "http://localhost:5080"
            : checkoutBaseUrl.TrimEnd('/');
    }

    public PaymentProviderType ProviderType => _type;

    public Task<ProviderPaymentResult> CreatePaymentAsync(CreateProviderPaymentRequest request, CancellationToken ct = default)
    {
        var id = $"mock-{_type}-{request.PaymentId:N}".ToLowerInvariant();
        return Task.FromResult(new ProviderPaymentResult
        {
            Success = true,
            ProviderPaymentId = id,
            CheckoutUrl = $"{_checkoutBaseUrl}/mock-checkout/{request.PaymentId}",
            ReadableCode = id[..Math.Min(12, id.Length)].ToUpperInvariant(),
            QrCode = null,
            ValidUntilUtc = DateTime.UtcNow.AddMinutes(15),
            RawResponse = JsonSerializer.Serialize(new { mock = true, provider = _type.ToString(), id, checkoutUrl = $"{_checkoutBaseUrl}/mock-checkout/{request.PaymentId}" })
        });
    }

    public Task<ProviderStatusResult> GetStatusAsync(string providerPaymentId, CancellationToken ct = default)
        => Task.FromResult(new ProviderStatusResult { Status = PaymentStatus.Pending, RawResponse = "{}" });

    public Task<bool> CancelAsync(string providerPaymentId, CancellationToken ct = default) => Task.FromResult(true);

    public Task<ProviderWebhookResult?> HandleWebhookAsync(string payload, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        // Mock inbound webhooks are disabled — use Development mock-checkout only.
        return Task.FromResult<ProviderWebhookResult?>(null);
    }
}

public class FibPaymentProvider : HttpPaymentProviderBase, IPaymentProvider
{
    private readonly IProviderSettingsService _settings;
    private readonly string _publicBaseUrl;
    private string? _cachedToken;
    private DateTime _tokenExpires = DateTime.MinValue;
    private string? _cachedForKey;

    public FibPaymentProvider(
        IHttpClientFactory factory,
        IProviderSettingsService settings,
        IOptions<AppOptions> appOptions,
        ILogger<FibPaymentProvider> logger)
        : base(factory, logger)
    {
        _settings = settings;
        _publicBaseUrl = appOptions.Value.PublicBaseUrl;
    }

    public PaymentProviderType ProviderType => PaymentProviderType.Fib;

    private async Task<(ProviderEnvCredentials Creds, bool UseMock)> ResolveAsync(CancellationToken ct)
    {
        var creds = await _settings.GetActiveCredentialsAsync(PaymentProviderType.Fib, ct);
        var useMock = await _settings.UseMockAsync(ct) &&
                      (string.IsNullOrWhiteSpace(creds.ClientId) || string.IsNullOrWhiteSpace(creds.ClientSecret));
        return (creds, useMock);
    }

    public async Task<ProviderPaymentResult> CreatePaymentAsync(CreateProviderPaymentRequest request, CancellationToken ct = default)
    {
        var (creds, useMock) = await ResolveAsync(ct);
        if (useMock)
            return await new MockPaymentProvider(PaymentProviderType.Fib, _publicBaseUrl).CreatePaymentAsync(request, ct);

        try
        {
            var token = await GetTokenAsync(creds, ct);
            var client = HttpClientFactory.CreateClient("fib");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var body = new
            {
                monetaryValue = new { amount = request.Amount.ToString("0.00"), currency = request.Currency },
                statusCallbackUrl = request.StatusCallbackUrl,
                description = request.Description.Length > 50 ? request.Description[..50] : request.Description
            };

            var response = await client.PostAsync($"{creds.BaseUrl.TrimEnd('/')}/payments",
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"), ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return new ProviderPaymentResult { Success = false, ErrorMessage = $"FIB error: {response.StatusCode}", RawResponse = raw };

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            return new ProviderPaymentResult
            {
                Success = true,
                ProviderPaymentId = root.GetProperty("paymentId").GetString(),
                QrCode = root.TryGetProperty("qrCode", out var qr) ? qr.GetString() : null,
                ReadableCode = root.TryGetProperty("readableCode", out var rc) ? rc.GetString() : null,
                CheckoutUrl = root.TryGetProperty("personalAppLink", out var link) ? link.GetString() : null,
                ValidUntilUtc = root.TryGetProperty("validUntil", out var vu) && DateTime.TryParse(vu.GetString(), out var dt) ? dt.ToUniversalTime() : null,
                RawResponse = raw
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "FIB create payment failed");
            return new ProviderPaymentResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ProviderStatusResult> GetStatusAsync(string providerPaymentId, CancellationToken ct = default)
    {
        var (creds, useMock) = await ResolveAsync(ct);
        if (useMock)
            return await new MockPaymentProvider(PaymentProviderType.Fib, _publicBaseUrl).GetStatusAsync(providerPaymentId, ct);

        var token = await GetTokenAsync(creds, ct);
        var client = HttpClientFactory.CreateClient("fib");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync($"{creds.BaseUrl.TrimEnd('/')}/payments/{providerPaymentId}/status", ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(raw);
        var statusText = doc.RootElement.GetProperty("status").GetString();
        var status = statusText?.ToUpperInvariant() switch
        {
            "PAID" => PaymentStatus.Paid,
            "DECLINED" => PaymentStatus.Declined,
            _ => PaymentStatus.Pending
        };
        return new ProviderStatusResult { Status = status, RawResponse = raw };
    }

    public async Task<bool> CancelAsync(string providerPaymentId, CancellationToken ct = default)
    {
        var (creds, useMock) = await ResolveAsync(ct);
        if (useMock) return true;
        var token = await GetTokenAsync(creds, ct);
        var client = HttpClientFactory.CreateClient("fib");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.PostAsync($"{creds.BaseUrl.TrimEnd('/')}/payments/{providerPaymentId}/cancel", null, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<ProviderWebhookResult?> HandleWebhookAsync(string payload, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        if (!await _settings.MatchesWebhookSecretAsync(PaymentProviderType.Fib, headers, ct))
            return null;

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var providerId = root.TryGetProperty("id", out var id) ? id.GetString() : null;
        var statusText = root.TryGetProperty("status", out var st) ? st.GetString() : "UNPAID";
        var status = statusText?.ToUpperInvariant() switch
        {
            "PAID" => PaymentStatus.Paid,
            "DECLINED" => PaymentStatus.Declined,
            _ => PaymentStatus.Pending
        };
        return new ProviderWebhookResult
        {
            ProviderPaymentId = providerId,
            Status = status,
            RawPayload = payload
        };
    }

    private async Task<string> GetTokenAsync(ProviderEnvCredentials options, CancellationToken ct)
    {
        var key = $"{options.AuthUrl}|{options.ClientId}|{options.ClientSecret}";
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpires && _cachedForKey == key)
            return _cachedToken!;

        var client = HttpClientFactory.CreateClient("fib-auth");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret
        });
        var response = await client.PostAsync(options.AuthUrl, content, ct);
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(raw);
        _cachedToken = doc.RootElement.GetProperty("access_token").GetString();
        _cachedForKey = key;
        var expires = doc.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 60;
        _tokenExpires = DateTime.UtcNow.AddSeconds(Math.Max(10, expires - 10));
        return _cachedToken!;
    }
}

public class ZainCashPaymentProvider : HttpPaymentProviderBase, IPaymentProvider
{
    private readonly IProviderSettingsService _settings;
    private readonly string _publicBaseUrl;

    public ZainCashPaymentProvider(
        IHttpClientFactory factory,
        IProviderSettingsService settings,
        IOptions<AppOptions> appOptions,
        ILogger<ZainCashPaymentProvider> logger)
        : base(factory, logger)
    {
        _settings = settings;
        _publicBaseUrl = appOptions.Value.PublicBaseUrl;
    }

    public PaymentProviderType ProviderType => PaymentProviderType.ZainCash;

    private async Task<(ProviderEnvCredentials Creds, bool UseMock)> ResolveAsync(CancellationToken ct)
    {
        var creds = await _settings.GetActiveCredentialsAsync(PaymentProviderType.ZainCash, ct);
        var useMock = await _settings.UseMockAsync(ct) &&
                      string.IsNullOrWhiteSpace(creds.Secret) &&
                      string.IsNullOrWhiteSpace(creds.ClientSecret);
        return (creds, useMock);
    }

    public async Task<ProviderPaymentResult> CreatePaymentAsync(CreateProviderPaymentRequest request, CancellationToken ct = default)
    {
        var (options, useMock) = await ResolveAsync(ct);
        if (useMock)
            return await new MockPaymentProvider(PaymentProviderType.ZainCash, _publicBaseUrl).CreatePaymentAsync(request, ct);

        try
        {
            if (!string.IsNullOrWhiteSpace(options.ClientId) && !string.IsNullOrWhiteSpace(options.ClientSecret))
                return await CreateV2Async(options, request, ct);
            return await CreateLegacyAsync(options, request, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ZainCash create payment failed");
            return new ProviderPaymentResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<ProviderPaymentResult> CreateV2Async(ProviderEnvCredentials options, CreateProviderPaymentRequest request, CancellationToken ct)
    {
        var token = await GetZainCashTokenAsync(options, ct);
        var client = HttpClientFactory.CreateClient("zaincash");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var success = request.SuccessUrl ?? $"{_publicBaseUrl.TrimEnd('/')}/mock-checkout/{request.PaymentId}";
        var failure = request.FailureUrl ?? $"{_publicBaseUrl.TrimEnd('/')}/mock-checkout/{request.PaymentId}";

        var body = new
        {
            language = "ar",
            externalReferenceId = request.PaymentId.ToString(),
            orderId = request.MerchantOrderId,
            serviceType = string.IsNullOrWhiteSpace(request.Description) ? "Fynexpay" : request.Description,
            amount = new { value = request.Amount.ToString("0"), currency = "IQD" },
            redirectUrls = new { successUrl = success, failureUrl = failure }
        };

        var initUrl = $"{options.BaseUrl.TrimEnd('/')}/api/v2/payment-gateway/transaction/init";
        var response = await client.PostAsync(initUrl,
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"), ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            return new ProviderPaymentResult { Success = false, ErrorMessage = $"ZainCash v2 error: {response.StatusCode}", RawResponse = raw };

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var id = root.TryGetProperty("transactionId", out var tid) ? tid.GetString()
            : root.TryGetProperty("id", out var id2) ? id2.GetString() : null;
        var redirect = root.TryGetProperty("redirectUrl", out var ru) ? ru.GetString() : null;

        return new ProviderPaymentResult
        {
            Success = !string.IsNullOrWhiteSpace(redirect),
            ProviderPaymentId = id,
            CheckoutUrl = redirect,
            RawResponse = raw,
            ValidUntilUtc = DateTime.UtcNow.AddHours(4),
            ErrorMessage = string.IsNullOrWhiteSpace(redirect) ? "لم يُرجع ZainCash رابط دفع" : null
        };
    }

    private async Task<ProviderPaymentResult> CreateLegacyAsync(ProviderEnvCredentials options, CreateProviderPaymentRequest request, CancellationToken ct)
    {
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = new Dictionary<string, object>
        {
            ["amount"] = (long)request.Amount,
            ["serviceType"] = request.Description,
            ["msisdn"] = options.Msisdn,
            ["orderId"] = request.MerchantOrderId,
            ["redirectUrl"] = request.SuccessUrl ?? request.StatusCallbackUrl ?? "",
            ["iat"] = iat,
            ["exp"] = iat + 60 * 60 * 4
        };

        var token = CreateHs256Jwt(payload, options.Secret);
        var client = HttpClientFactory.CreateClient("zaincash");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = token,
            ["merchantId"] = options.MerchantId,
            ["lang"] = "ar"
        });

        var response = await client.PostAsync($"{options.BaseUrl.TrimEnd('/')}/transaction/init", form, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            return new ProviderPaymentResult { Success = false, ErrorMessage = $"ZainCash error: {response.StatusCode}", RawResponse = raw };

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        var redirect = $"{options.BaseUrl.TrimEnd('/')}/transaction/pay?id={id}";
        if (root.TryGetProperty("redirectUrl", out var ru))
            redirect = ru.GetString() ?? redirect;

        return new ProviderPaymentResult
        {
            Success = true,
            ProviderPaymentId = id,
            CheckoutUrl = redirect,
            RawResponse = raw,
            ValidUntilUtc = DateTime.UtcNow.AddHours(4)
        };
    }

    private string? _zcToken;
    private DateTime _zcTokenExpires = DateTime.MinValue;
    private string? _zcTokenKey;

    private async Task<string> GetZainCashTokenAsync(ProviderEnvCredentials options, CancellationToken ct)
    {
        var key = $"{options.AuthUrl}|{options.ClientId}|{options.ClientSecret}";
        if (!string.IsNullOrEmpty(_zcToken) && DateTime.UtcNow < _zcTokenExpires && _zcTokenKey == key)
            return _zcToken!;

        var authUrl = string.IsNullOrWhiteSpace(options.AuthUrl)
            ? $"{options.BaseUrl.TrimEnd('/')}/oauth2/token"
            : options.AuthUrl;

        var client = HttpClientFactory.CreateClient("zaincash");
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
            ["scope"] = "payment:read payment:write"
        });
        var response = await client.PostAsync(authUrl, content, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(raw);
        _zcToken = doc.RootElement.GetProperty("access_token").GetString();
        _zcTokenKey = key;
        var expires = doc.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;
        _zcTokenExpires = DateTime.UtcNow.AddSeconds(Math.Max(30, expires - 30));
        return _zcToken!;
    }

    public async Task<ProviderStatusResult> GetStatusAsync(string providerPaymentId, CancellationToken ct = default)
    {
        var (options, useMock) = await ResolveAsync(ct);
        if (useMock || string.IsNullOrWhiteSpace(options.ClientId))
            return new ProviderStatusResult { Status = PaymentStatus.Pending };

        try
        {
            var token = await GetZainCashTokenAsync(options, ct);
            var client = HttpClientFactory.CreateClient("zaincash");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var url = $"{options.BaseUrl.TrimEnd('/')}/api/v2/payment-gateway/transaction/inquiry/{providerPaymentId}";
            var response = await client.GetAsync(url, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(raw);
            var st = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
            var status = st?.ToLowerInvariant() switch
            {
                "success" or "completed" or "paid" => PaymentStatus.Paid,
                "failed" or "fail" => PaymentStatus.Failed,
                "expired" => PaymentStatus.Expired,
                "refunded" => PaymentStatus.Refunded,
                _ => PaymentStatus.Pending
            };
            return new ProviderStatusResult { Status = status, RawResponse = raw };
        }
        catch
        {
            return new ProviderStatusResult { Status = PaymentStatus.Pending };
        }
    }

    public Task<bool> CancelAsync(string providerPaymentId, CancellationToken ct = default) => Task.FromResult(false);

    public async Task<ProviderWebhookResult?> HandleWebhookAsync(string payload, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        try
        {
            var test = await _settings.GetCredentialsAsync(PaymentProviderType.ZainCash, ProviderEnvironment.Test, ct);
            var prod = await _settings.GetCredentialsAsync(PaymentProviderType.ZainCash, ProviderEnvironment.Production, ct);
            if (!await _settings.MatchesWebhookSecretAsync(PaymentProviderType.ZainCash, headers, ct))
                return null;

            string json;
            if (payload.Trim().StartsWith('{'))
            {
                // JSON bodies require an explicit webhook secret when configured; without JWT there is no HMAC.
                if (string.IsNullOrWhiteSpace(test.WebhookSecret) && string.IsNullOrWhiteSpace(prod.WebhookSecret)
                    && string.IsNullOrWhiteSpace(test.Secret) && string.IsNullOrWhiteSpace(prod.Secret))
                    return null;
                json = payload;
            }
            else
            {
                var secrets = new[]
                    {
                        test.Secret, test.ClientSecret, test.WebhookSecret,
                        prod.Secret, prod.ClientSecret, prod.WebhookSecret
                    }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                string? verifiedJson = null;
                foreach (var secret in secrets)
                {
                    if (TryVerifyHs256Jwt(payload, secret!, out var candidate))
                    {
                        verifiedJson = candidate;
                        break;
                    }
                }
                if (verifiedJson == null)
                    return null;
                json = verifiedJson;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var providerId = root.TryGetProperty("id", out var id) ? id.GetString() : null;
            var st = root.TryGetProperty("status", out var s) ? s.GetString() : null;
            var status = st?.ToLowerInvariant() switch
            {
                "success" or "completed" or "paid" => PaymentStatus.Paid,
                "failed" or "fail" => PaymentStatus.Failed,
                "pending" => PaymentStatus.Pending,
                _ => PaymentStatus.Failed
            };
            return new ProviderWebhookResult
            {
                ProviderPaymentId = providerId,
                Status = status,
                RawPayload = json
            };
        }
        catch
        {
            return null;
        }
    }

    private static string CreateHs256Jwt(Dictionary<string, object> payload, string secret)
    {
        string B64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var header = B64(Encoding.UTF8.GetBytes("""{"alg":"HS256","typ":"JWT"}"""));
        var body = B64(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        var data = $"{header}.{body}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var sig = B64(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
        return $"{data}.{sig}";
    }

    private static bool TryVerifyHs256Jwt(string jwt, string secret, out string payloadJson)
    {
        payloadJson = "";
        var parts = jwt.Split('.');
        if (parts.Length != 3) return false;

        string B64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var data = $"{parts[0]}.{parts[1]}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = B64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
        if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(parts[2])))
            return false;

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
        payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        return true;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
            if (!string.IsNullOrWhiteSpace(v)) return v;
        return null;
    }
}

public class QiPaymentProvider : QiGatePaymentProviderBase
{
    public QiPaymentProvider(
        IHttpClientFactory factory,
        IProviderSettingsService settings,
        IOptions<AppOptions> appOptions,
        ILogger<QiPaymentProvider> logger)
        : base(factory, settings, appOptions, logger, PaymentProviderType.Qi, paymentMethod: "CARD", appChannel: false)
    {
    }
}

/// <summary>
/// Pay with SuperQi via QI Gate (maps to SDK ALIPAY method).
/// https://developers-gate.qi.iq/docs/category/pay-with-superqi
/// </summary>
public class SuperQiPaymentProvider : QiGatePaymentProviderBase
{
    public SuperQiPaymentProvider(
        IHttpClientFactory factory,
        IProviderSettingsService settings,
        IOptions<AppOptions> appOptions,
        ILogger<SuperQiPaymentProvider> logger)
        : base(factory, settings, appOptions, logger, PaymentProviderType.SuperQi, paymentMethod: "ALIPAY", appChannel: false)
    {
    }
}

public abstract class QiGatePaymentProviderBase : HttpPaymentProviderBase, IPaymentProvider
{
    private readonly IProviderSettingsService _settings;
    private readonly string _publicBaseUrl;
    private readonly string _paymentMethod;
    private readonly bool _appChannel;

    protected QiGatePaymentProviderBase(
        IHttpClientFactory factory,
        IProviderSettingsService settings,
        IOptions<AppOptions> appOptions,
        ILogger logger,
        PaymentProviderType providerType,
        string paymentMethod,
        bool appChannel)
        : base(factory, logger)
    {
        _settings = settings;
        _publicBaseUrl = appOptions.Value.PublicBaseUrl;
        ProviderType = providerType;
        _paymentMethod = paymentMethod;
        _appChannel = appChannel;
    }

    public PaymentProviderType ProviderType { get; }

    private async Task<(ProviderEnvCredentials Creds, bool UseMock)> ResolveAsync(CancellationToken ct)
    {
        var creds = await _settings.GetActiveCredentialsAsync(ProviderType, ct);
        var useMock = await _settings.UseMockAsync(ct) &&
                      (string.IsNullOrWhiteSpace(creds.Username) || string.IsNullOrWhiteSpace(creds.Password));
        return (creds, useMock);
    }

    public async Task<ProviderPaymentResult> CreatePaymentAsync(CreateProviderPaymentRequest request, CancellationToken ct = default)
    {
        var (options, useMock) = await ResolveAsync(ct);
        if (useMock)
            return await new MockPaymentProvider(ProviderType, _publicBaseUrl).CreatePaymentAsync(request, ct);

        try
        {
            var client = CreateQiClient(options);
            var finish = request.SuccessUrl ?? $"{_publicBaseUrl.TrimEnd('/')}/checkout/{request.PaymentId}/return?result=success";
            var notify = request.StatusCallbackUrl
                ?? $"{_publicBaseUrl.TrimEnd('/')}/api/webhooks/{ProviderType.ToString().ToLowerInvariant()}";

            var body = new
            {
                requestId = Guid.NewGuid().ToString(),
                amount = request.Amount,
                currency = string.IsNullOrWhiteSpace(request.Currency) ? "IQD" : request.Currency,
                locale = "ar_IQ",
                finishPaymentUrl = finish,
                notificationUrl = notify,
                additionalInfo = new
                {
                    orderId = request.MerchantOrderId,
                    description = request.Description ?? "Fynexpay",
                    paymentMethod = _paymentMethod,
                    channel = ProviderType == PaymentProviderType.SuperQi ? "SuperQi" : "QiGate"
                },
                appChannel = _appChannel
            };

            var response = await client.PostAsync($"{options.BaseUrl.TrimEnd('/')}/payment",
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"), ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return new ProviderPaymentResult
                {
                    Success = false,
                    ErrorMessage = $"{ProviderType} error: {response.StatusCode}",
                    RawResponse = raw
                };

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var id = root.TryGetProperty("paymentId", out var pid) ? pid.GetString()
                : root.TryGetProperty("id", out var id2) ? id2.GetString() : null;
            var formUrl = root.TryGetProperty("formUrl", out var fu) ? fu.GetString() : null;

            return new ProviderPaymentResult
            {
                Success = !string.IsNullOrWhiteSpace(formUrl),
                ProviderPaymentId = id,
                CheckoutUrl = formUrl,
                RawResponse = raw,
                ValidUntilUtc = DateTime.UtcNow.AddMinutes(30),
                ErrorMessage = string.IsNullOrWhiteSpace(formUrl) ? $"لم يُرجع {ProviderType} رابط دفع" : null
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Provider} create payment failed", ProviderType);
            return new ProviderPaymentResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ProviderStatusResult> GetStatusAsync(string providerPaymentId, CancellationToken ct = default)
    {
        var (options, useMock) = await ResolveAsync(ct);
        if (useMock)
            return await new MockPaymentProvider(ProviderType, _publicBaseUrl).GetStatusAsync(providerPaymentId, ct);

        var client = CreateQiClient(options);
        var response = await client.GetAsync($"{options.BaseUrl.TrimEnd('/')}/payment/{providerPaymentId}/status", ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(raw);
        var statusText = doc.RootElement.TryGetProperty("status", out var st) ? st.GetString() : "PENDING";
        var status = statusText?.ToUpperInvariant() switch
        {
            "PAID" or "SUCCESS" or "DEPOSITED" or "APPROVED" or "COMPLETED" => PaymentStatus.Paid,
            "DECLINED" or "REJECTED" or "FAILED" => PaymentStatus.Declined,
            "CANCELLED" or "CANCELED" => PaymentStatus.Cancelled,
            "EXPIRED" => PaymentStatus.Expired,
            _ => PaymentStatus.Pending
        };
        return new ProviderStatusResult { Status = status, RawResponse = raw };
    }

    public async Task<bool> CancelAsync(string providerPaymentId, CancellationToken ct = default)
    {
        var (options, useMock) = await ResolveAsync(ct);
        if (useMock) return true;
        var client = CreateQiClient(options);
        var body = new { requestId = Guid.NewGuid().ToString() };
        var response = await client.PostAsync($"{options.BaseUrl.TrimEnd('/')}/payment/{providerPaymentId}/cancel",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"), ct);
        return response.IsSuccessStatusCode;
    }

    private HttpClient CreateQiClient(ProviderEnvCredentials options)
    {
        var client = HttpClientFactory.CreateClient("qi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}")));
        client.DefaultRequestHeaders.Remove("X-Terminal-Id");
        client.DefaultRequestHeaders.Add("X-Terminal-Id", options.TerminalId);
        return client;
    }

    public async Task<ProviderWebhookResult?> HandleWebhookAsync(string payload, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        if (!await _settings.MatchesWebhookSecretAsync(ProviderType, headers, ct))
            return null;

        var test = await _settings.GetCredentialsAsync(ProviderType, ProviderEnvironment.Test, ct);
        var prod = await _settings.GetCredentialsAsync(ProviderType, ProviderEnvironment.Production, ct);
        var hasSecret = !string.IsNullOrWhiteSpace(test.WebhookSecret) || !string.IsNullOrWhiteSpace(prod.WebhookSecret);
        // Without a configured webhook secret, require Basic auth matching Qi credentials (either env).
        if (!hasSecret
            && !WebhookSecrets.MatchesBasicAuth(test.Username, test.Password, headers)
            && !WebhookSecrets.MatchesBasicAuth(prod.Username, prod.Password, headers))
            return null;

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var providerId = root.TryGetProperty("paymentId", out var pid) ? pid.GetString()
            : root.TryGetProperty("id", out var id) ? id.GetString() : null;
        var statusText = root.TryGetProperty("status", out var st) ? st.GetString() : "PENDING";
        var status = statusText?.ToUpperInvariant() switch
        {
            "PAID" or "SUCCESS" or "DEPOSITED" or "APPROVED" => PaymentStatus.Paid,
            "DECLINED" or "REJECTED" => PaymentStatus.Declined,
            "CANCELLED" or "CANCELED" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending
        };
        return new ProviderWebhookResult
        {
            ProviderPaymentId = providerId,
            Status = status,
            RawPayload = payload
        };
    }
}

/// <summary>
/// Al Qaseh Payment Gateway — hosted payment page for non-PCI merchants.
/// Docs: https://docs.alqaseh.com/payment-api
/// </summary>
public class AlqasehPaymentProvider : HttpPaymentProviderBase, IPaymentProvider
{
    private readonly IProviderSettingsService _settings;
    private readonly string _publicBaseUrl;

    private static readonly JsonSerializerOptions SnakeJson = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public AlqasehPaymentProvider(
        IHttpClientFactory factory,
        IProviderSettingsService settings,
        IOptions<AppOptions> appOptions,
        ILogger<AlqasehPaymentProvider> logger)
        : base(factory, logger)
    {
        _settings = settings;
        _publicBaseUrl = appOptions.Value.PublicBaseUrl;
    }

    public PaymentProviderType ProviderType => PaymentProviderType.Alqaseh;

    private async Task<(ProviderEnvCredentials Creds, bool UseMock)> ResolveAsync(CancellationToken ct)
    {
        var creds = await _settings.GetActiveCredentialsAsync(PaymentProviderType.Alqaseh, ct);
        var useMock = await _settings.UseMockAsync(ct) &&
                      (string.IsNullOrWhiteSpace(creds.ClientId) || string.IsNullOrWhiteSpace(creds.ClientSecret));
        return (creds, useMock);
    }

    public async Task<ProviderPaymentResult> CreatePaymentAsync(CreateProviderPaymentRequest request, CancellationToken ct = default)
    {
        var (creds, useMock) = await ResolveAsync(ct);
        if (useMock)
            return await new MockPaymentProvider(PaymentProviderType.Alqaseh, _publicBaseUrl).CreatePaymentAsync(request, ct);

        try
        {
            var orderId = request.PaymentId.ToString("N");
            var body = new Dictionary<string, object?>
            {
                ["amount"] = request.Amount,
                ["country"] = "IQ",
                ["currency"] = string.IsNullOrWhiteSpace(request.Currency) ? "IQD" : request.Currency,
                ["order_id"] = orderId,
                ["redirect_url"] = FirstUrl(request.SuccessUrl, request.StatusCallbackUrl),
                ["webhook_url"] = request.StatusCallbackUrl ?? "",
                ["transaction_type"] = "Retail",
                ["description"] = Truncate(request.Description, 200),
                ["token_expiry_in_hour"] = 1,
                ["custom_data"] = new Dictionary<string, string>
                {
                    ["merchant_order_id"] = request.MerchantOrderId ?? "",
                    ["fynexpay_payment_id"] = orderId
                }
            };

            var raw = await SendAsync(HttpMethod.Post, creds, "egw/payments/create", body, ct);
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (!TryGetString(root, "payment_id", out var paymentId) || string.IsNullOrWhiteSpace(paymentId))
                return new ProviderPaymentResult { Success = false, ErrorMessage = "Alqaseh: missing payment_id", RawResponse = raw };

            TryGetString(root, "token", out var token);
            var checkout = BuildPayUrl(creds, token);
            return new ProviderPaymentResult
            {
                Success = true,
                ProviderPaymentId = paymentId,
                CheckoutUrl = checkout,
                ReadableCode = token,
                ValidUntilUtc = DateTime.UtcNow.AddHours(1),
                RawResponse = raw
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Alqaseh create payment failed");
            return new ProviderPaymentResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ProviderStatusResult> GetStatusAsync(string providerPaymentId, CancellationToken ct = default)
    {
        var (creds, useMock) = await ResolveAsync(ct);
        if (useMock)
            return await new MockPaymentProvider(PaymentProviderType.Alqaseh, _publicBaseUrl).GetStatusAsync(providerPaymentId, ct);

        try
        {
            var raw = await SendAsync(HttpMethod.Get, creds, $"egw/payments/{Uri.EscapeDataString(providerPaymentId)}", null, ct);
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var statusText = TryGetString(root, "status", out var st) ? st
                : TryGetString(root, "payment_status", out var ps) ? ps
                : "prepared";
            return new ProviderStatusResult
            {
                Status = MapStatus(statusText),
                RawResponse = raw
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Alqaseh get status failed for {Id}", providerPaymentId);
            return new ProviderStatusResult { Status = PaymentStatus.Pending, FailureReason = ex.Message };
        }
    }

    public async Task<bool> CancelAsync(string providerPaymentId, CancellationToken ct = default)
    {
        var (creds, useMock) = await ResolveAsync(ct);
        if (useMock) return true;

        try
        {
            await SendAsync(HttpMethod.Post, creds, "egw/payments/revoke",
                new Dictionary<string, object?> { ["payment_id"] = providerPaymentId }, ct);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Alqaseh revoke failed for {Id}", providerPaymentId);
            return false;
        }
    }

    public async Task<ProviderWebhookResult?> HandleWebhookAsync(string payload, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        if (!await _settings.MatchesWebhookSecretAsync(PaymentProviderType.Alqaseh, headers, ct))
            return null;

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        TryGetString(root, "payment_id", out var providerId);
        TryGetString(root, "order_id", out var orderId);
        var statusText = TryGetString(root, "payment_status", out var ps) ? ps
            : TryGetString(root, "status", out var st) ? st
            : "Pending";

        Guid? paymentId = null;
        if (!string.IsNullOrWhiteSpace(orderId) && Guid.TryParseExact(orderId, "N", out var parsed))
            paymentId = parsed;
        else if (!string.IsNullOrWhiteSpace(orderId) && Guid.TryParse(orderId, out var parsed2))
            paymentId = parsed2;

        return new ProviderWebhookResult
        {
            PaymentId = paymentId,
            ProviderPaymentId = providerId,
            Status = MapStatus(statusText),
            RawPayload = payload
        };
    }

    private async Task<string> SendAsync(
        HttpMethod method,
        ProviderEnvCredentials creds,
        string relativePath,
        object? body,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(creds.BaseUrl))
            throw new InvalidOperationException("Alqaseh BaseUrl غير مضبوط");
        if (string.IsNullOrWhiteSpace(creds.ClientId) || string.IsNullOrWhiteSpace(creds.ClientSecret))
            throw new InvalidOperationException("Alqaseh ClientId/ClientSecret غير مضبوطين");

        var client = HttpClientFactory.CreateClient("alqaseh");
        var url = $"{creds.BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
        using var req = new HttpRequestMessage(method, url);
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{creds.ClientId}:{creds.ClientSecret}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        if (body != null)
            req.Content = new StringContent(JsonSerializer.Serialize(body, SnakeJson), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(req, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Alqaseh {(int)response.StatusCode}: {Truncate(raw, 400)}");
        return raw;
    }

    private static string BuildPayUrl(ProviderEnvCredentials creds, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "";
        var payBase = string.IsNullOrWhiteSpace(creds.AuthUrl)
            ? InferPayBase(creds.BaseUrl)
            : creds.AuthUrl.TrimEnd('/');
        return $"{payBase}/pay/{token}";
    }

    private static string InferPayBase(string apiBase)
    {
        if (apiBase.Contains("api-test", StringComparison.OrdinalIgnoreCase))
            return "https://pay-test.alqaseh.com";
        return "https://pay.alqaseh.com";
    }

    private static PaymentStatus MapStatus(string? statusText)
    {
        var s = (statusText ?? "").Trim().ToLowerInvariant();
        return s switch
        {
            "succeeded" or "success" or "paid" => PaymentStatus.Paid,
            "failed" or "fail" => PaymentStatus.Failed,
            "declined" or "decline" or "rejected" => PaymentStatus.Declined,
            "expired" or "expire" => PaymentStatus.Expired,
            "revoked" or "cancelled" or "canceled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending
        };
    }

    private static bool TryGetString(JsonElement root, string name, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(name, out var el))
            return false;
        value = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string FirstUrl(params string?[] urls)
        => urls.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u)) ?? "";

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= max ? value : value[..max];
    }
}

internal static class WebhookSecrets
{
    public static bool MatchesOptionalSecret(string? configuredSecret, IDictionary<string, string> headers)
    {
        if (string.IsNullOrWhiteSpace(configuredSecret))
            return true;

        if (!headers.TryGetValue("X-Webhook-Secret", out var provided)
            && !headers.TryGetValue("X-Fynexpay-Provider-Secret", out provided))
            return false;

        var a = Encoding.UTF8.GetBytes(configuredSecret.Trim());
        var b = Encoding.UTF8.GetBytes(provided.Trim());
        return a.Length == b.Length && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, b);
    }

    public static bool MatchesBasicAuth(string? username, string? password, IDictionary<string, string> headers)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;
        if (!headers.TryGetValue("Authorization", out var auth) || string.IsNullOrWhiteSpace(auth))
            return false;
        if (!auth.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(auth["Basic ".Length..].Trim()));
            var expected = $"{username}:{password}";
            var a = Encoding.UTF8.GetBytes(expected);
            var b = Encoding.UTF8.GetBytes(decoded);
            return a.Length == b.Length && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, b);
        }
        catch
        {
            return false;
        }
    }
}
