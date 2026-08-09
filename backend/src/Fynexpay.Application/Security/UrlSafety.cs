using System.Net;
using System.Net.Sockets;

namespace Fynexpay.Application.Security;

public static class UrlSafety
{
    public static void ValidateMerchantUrls(string? successUrl, string? failureUrl, string? callbackUrl, string platformDomain)
    {
        if (!string.IsNullOrWhiteSpace(successUrl))
            EnsureAllowedMerchantUrl(successUrl, platformDomain, allowPrivateHost: false);
        if (!string.IsNullOrWhiteSpace(failureUrl))
            EnsureAllowedMerchantUrl(failureUrl, platformDomain, allowPrivateHost: false);
        if (!string.IsNullOrWhiteSpace(callbackUrl))
            EnsureAllowedMerchantUrl(callbackUrl, platformDomain, allowPrivateHost: false);
    }

    public static void EnsureAllowedMerchantUrl(string url, string platformDomain, bool allowPrivateHost)
    {
        if (!TryParseHttpUrl(url, out var uri))
            throw new ArgumentException("رابط غير صالح — يجب أن يكون http/https");

        if (!IsLocalhost(uri.Host) && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("الروابط الخارجية يجب أن تستخدم HTTPS");

        var host = NormalizeHost(uri);
        if (!HostMatchesPlatform(host, platformDomain))
            throw new ArgumentException($"الرابط يجب أن يكون على دومين المنصة المعتمد ({platformDomain})");

        // Localhost platforms may use loopback URLs; block private/metadata hosts otherwise.
        var platformIsLocal = platformDomain.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
                              || platformDomain.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase);
        if (!allowPrivateHost && !platformIsLocal && IsBlockedCallbackHost(uri.Host))
            throw new ArgumentException("لا يُسمح بعناوين داخلية أو خاصة في روابط الإشعار/الرجوع");
    }

    public static bool IsSafeRedirectUrl(string? url, string? platformDomain)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(platformDomain))
            return false;
        try
        {
            EnsureAllowedMerchantUrl(url, platformDomain, allowPrivateHost: false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsSafeCallbackTarget(string url, string? platformDomain = null)
    {
        if (!TryParseHttpUrl(url, out var uri))
            return false;

        var platformIsLocal = !string.IsNullOrWhiteSpace(platformDomain)
            && (platformDomain.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
                || platformDomain.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase));

        if (!platformIsLocal)
        {
            if (IsBlockedCallbackHost(uri.Host))
                return false;
            if (IPAddress.TryParse(uri.Host, out var ip) && IsPrivateOrLoopback(ip))
                return false;
        }

        return true;
    }

    public static bool TryParseHttpUrl(string url, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(url))
            return false;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsed))
            return false;
        if (parsed.Scheme is not ("http" or "https"))
            return false;
        if (string.IsNullOrWhiteSpace(parsed.Host))
            return false;
        uri = parsed;
        return true;
    }

    public static bool HostMatchesPlatform(string host, string platformDomain)
    {
        host = host.Trim().TrimEnd('.').ToLowerInvariant();
        platformDomain = platformDomain.Trim().TrimEnd('.').ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
            host = host[4..];
        return host == platformDomain || host.EndsWith("." + platformDomain, StringComparison.Ordinal);
    }

    private static string NormalizeHost(Uri uri)
    {
        var host = uri.Host.Trim().TrimEnd('.').ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
            host = host[4..];
        if (IsLocalhost(host) && !uri.IsDefaultPort)
            return $"{host}:{uri.Port}";
        return host;
    }

    private static bool IsLocalhost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("::1", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);

    private static bool IsBlockedCallbackHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return true;

        var h = host.Trim().ToLowerInvariant();
        if (h is "localhost" or "127.0.0.1" or "::1" or "0.0.0.0" or "metadata" or "metadata.google.internal")
            return true;
        if (h.EndsWith(".internal", StringComparison.Ordinal) || h.EndsWith(".local", StringComparison.Ordinal))
            return true;
        if (h.StartsWith("169.254.", StringComparison.Ordinal))
            return true;
        if (IPAddress.TryParse(h, out var ip) && IsPrivateOrLoopback(ip))
            return true;
        return false;
    }

    private static bool IsPrivateOrLoopback(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            if (b[0] == 10) return true;
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
            if (b[0] == 192 && b[1] == 168) return true;
            if (b[0] == 169 && b[1] == 254) return true;
            if (b[0] == 127) return true;
        }
        return false;
    }
}
