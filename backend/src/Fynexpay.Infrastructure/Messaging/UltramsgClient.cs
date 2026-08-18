using System.Net.Http.Headers;
using System.Text.Json;
using Fynexpay.Application.Abstractions.Messaging;
using Fynexpay.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fynexpay.Infrastructure.Messaging;

public class UltramsgClient : IUltramsgClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUltramsgSettingsService _settings;
    private readonly IOptions<AppOptions> _app;
    private readonly ILogger<UltramsgClient> _logger;

    public UltramsgClient(
        IHttpClientFactory httpClientFactory,
        IUltramsgSettingsService settings,
        IOptions<AppOptions> app,
        ILogger<UltramsgClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _app = app;
        _logger = logger;
    }

    public async Task<UltramsgStatusResult> GetStatusAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        if (string.IsNullOrWhiteSpace(s.InstanceId) || string.IsNullOrWhiteSpace(s.Token))
            return new UltramsgStatusResult(false, s.Enabled, "unconfigured", null, false, null, "Instance ID أو Token غير مضبوط");

        try
        {
            var client = _httpClientFactory.CreateClient("ultramsg");
            var url = $"https://api.ultramsg.com/{Uri.EscapeDataString(s.InstanceId)}/instance/status?token={Uri.EscapeDataString(s.Token)}";
            using var res = await client.GetAsync(url, ct);
            var raw = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                return new UltramsgStatusResult(true, s.Enabled, "error", null, false, raw, $"HTTP {(int)res.StatusCode}");

            var (accountStatus, subStatus) = ExtractStatus(raw);
            var ready = IsReadyStatus(accountStatus, subStatus);
            return new UltramsgStatusResult(true, s.Enabled, accountStatus, subStatus, ready, raw, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ultramsg status check failed");
            return new UltramsgStatusResult(true, s.Enabled, "error", null, false, null, ex.Message);
        }
    }

    public async Task<byte[]?> GetQrImageAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        if (string.IsNullOrWhiteSpace(s.InstanceId) || string.IsNullOrWhiteSpace(s.Token))
            throw new InvalidOperationException("إعدادات Ultramsg غير مكتملة");

        var client = _httpClientFactory.CreateClient("ultramsg");
        var url = $"https://api.ultramsg.com/{Uri.EscapeDataString(s.InstanceId)}/instance/qr?token={Uri.EscapeDataString(s.Token)}";
        using var res = await client.GetAsync(url, ct);
        var bytes = await res.Content.ReadAsByteArrayAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            var text = System.Text.Encoding.UTF8.GetString(bytes);
            throw new InvalidOperationException($"تعذّر جلب QR: {text}");
        }

        var contentType = res.Content.Headers.ContentType?.MediaType ?? "";
        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            using var doc = JsonDocument.Parse(bytes);
            if (doc.RootElement.TryGetProperty("qrCode", out var qr) ||
                doc.RootElement.TryGetProperty("base64", out qr))
            {
                var b64 = qr.GetString() ?? "";
                var comma = b64.IndexOf(',');
                if (comma >= 0) b64 = b64[(comma + 1)..];
                return Convert.FromBase64String(b64);
            }
            throw new InvalidOperationException("استجابة QR غير متوقعة من Ultramsg");
        }

        return bytes;
    }

    public async Task SendChatAsync(string phoneE164, string body, CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        if (string.IsNullOrWhiteSpace(s.InstanceId) || string.IsNullOrWhiteSpace(s.Token))
            throw new InvalidOperationException("إعدادات Ultramsg غير مكتملة");

        var client = _httpClientFactory.CreateClient("ultramsg");
        var url = $"https://api.ultramsg.com/{Uri.EscapeDataString(s.InstanceId)}/messages/chat";

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = s.Token,
            ["to"] = phoneE164,
            ["body"] = body
        });
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        using var res = await client.PostAsync(url, content, ct);
        var raw = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ultramsg error HTTP {(int)res.StatusCode}: {raw}");

        if (raw.Contains("\"error\"", StringComparison.OrdinalIgnoreCase) &&
            !raw.Contains("\"sent\":\"true\"", StringComparison.OrdinalIgnoreCase) &&
            !raw.Contains("\"sent\": \"true\"", StringComparison.OrdinalIgnoreCase) &&
            !raw.Contains("\"sent\":true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Ultramsg rejected message: {raw}");
        }
    }

    public async Task SendImageAsync(string phoneE164, string imageUrl, string? caption, CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        if (string.IsNullOrWhiteSpace(s.InstanceId) || string.IsNullOrWhiteSpace(s.Token))
            throw new InvalidOperationException("إعدادات Ultramsg غير مكتملة");

        var client = _httpClientFactory.CreateClient("ultramsg");
        var url = $"https://api.ultramsg.com/{Uri.EscapeDataString(s.InstanceId)}/messages/image";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = s.Token,
            ["to"] = phoneE164,
            ["image"] = imageUrl,
            ["caption"] = caption ?? ""
        });
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        using var res = await client.PostAsync(url, content, ct);
        var raw = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ultramsg image error HTTP {(int)res.StatusCode}: {raw}");

        if (raw.Contains("\"error\"", StringComparison.OrdinalIgnoreCase) &&
            !raw.Contains("\"sent\":\"true\"", StringComparison.OrdinalIgnoreCase) &&
            !raw.Contains("\"sent\": \"true\"", StringComparison.OrdinalIgnoreCase) &&
            !raw.Contains("\"sent\":true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Ultramsg rejected image: {raw}");
        }
    }

    public async Task SendTemplateAsync(
        string phoneE164,
        string templateKey,
        IReadOnlyDictionary<string, string?> vars,
        CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        var tpl = WhatsAppTemplates.Resolve(s, templateKey);
        var body = WhatsAppTemplates.Render(tpl.Body, vars);
        var image = Absolutize(FirstNonEmpty(tpl.ImageUrl, s.DefaultImageUrl));
        if (!string.IsNullOrWhiteSpace(image))
        {
            try
            {
                await SendImageAsync(phoneE164, image, body, ct);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ultramsg image send failed, falling back to text for {Key}", templateKey);
            }
        }

        await SendChatAsync(phoneE164, body, ct);
    }

    private string? Absolutize(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return null;
        var value = imageUrl.Trim();
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return value;

        var root = (_app.Value.PublicBaseUrl ?? "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(root)) return value;
        if (!value.StartsWith('/')) value = "/" + value;
        return root + value;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    /// <summary>
    /// Ultramsg may return:
    /// { "status": { "accountStatus": { "status": "authenticated", "substatus": "connected" } } }
    /// or flatter shapes.
    /// </summary>
    private static (string Status, string? SubStatus) ExtractStatus(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (TryReadAccount(root, out var status, out var sub))
                return (status, sub);

            if (root.TryGetProperty("status", out var statusNode))
            {
                if (TryReadAccount(statusNode, out status, out sub))
                    return (status, sub);

                if (statusNode.ValueKind == JsonValueKind.Object &&
                    statusNode.TryGetProperty("accountStatus", out var nested))
                {
                    if (TryReadAccount(nested, out status, out sub))
                        return (status, sub);
                    if (nested.ValueKind == JsonValueKind.String)
                        return (nested.GetString() ?? "unknown", null);
                }

                if (statusNode.ValueKind == JsonValueKind.String)
                    return (statusNode.GetString() ?? "unknown", null);
            }

            if (root.TryGetProperty("accountStatus", out var account))
            {
                if (TryReadAccount(account, out status, out sub))
                    return (status, sub);
                if (account.ValueKind == JsonValueKind.String)
                    return (account.GetString() ?? "unknown", null);
            }
        }
        catch
        {
            // ignore parse errors
        }

        return ("unknown", null);
    }

    private static bool TryReadAccount(JsonElement node, out string status, out string? subStatus)
    {
        status = "unknown";
        subStatus = null;

        if (node.ValueKind != JsonValueKind.Object)
            return false;

        // Direct { status, substatus }
        if (node.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.String)
        {
            status = st.GetString() ?? "unknown";
            if (node.TryGetProperty("substatus", out var sub) && sub.ValueKind == JsonValueKind.String)
                subStatus = sub.GetString();
            else if (node.TryGetProperty("subStatus", out sub) && sub.ValueKind == JsonValueKind.String)
                subStatus = sub.GetString();
            return true;
        }

        // Nested accountStatus object
        if (node.TryGetProperty("accountStatus", out var account))
        {
            if (account.ValueKind == JsonValueKind.Object)
                return TryReadAccount(account, out status, out subStatus);
            if (account.ValueKind == JsonValueKind.String)
            {
                status = account.GetString() ?? "unknown";
                return true;
            }
        }

        return false;
    }

    private static bool IsReadyStatus(string accountStatus, string? subStatus)
    {
        var s = (accountStatus ?? "").Trim().ToLowerInvariant();
        var sub = (subStatus ?? "").Trim().ToLowerInvariant();
        if (s is "authenticated" or "connected" or "ready")
            return true;
        if (s == "authenticated" && sub is "connected" or "online" or "")
            return true;
        return false;
    }
}
