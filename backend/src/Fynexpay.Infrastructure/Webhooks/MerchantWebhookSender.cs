using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fynexpay.Infrastructure.Webhooks;

public class MerchantWebhookSender : IMerchantWebhookSender
{
    private readonly IAppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MerchantWebhookSender> _logger;

    public MerchantWebhookSender(IAppDbContext db, IHttpClientFactory httpClientFactory, ILogger<MerchantWebhookSender> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendPaymentUpdateAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _db.Payments
            .Include(p => p.Merchant)
            .Include(p => p.MerchantPlatform)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment == null || string.IsNullOrWhiteSpace(payment.CallbackUrl))
            return;

        var platformDomain = payment.MerchantPlatform?.Domain;
        if (!UrlSafety.IsSafeCallbackTarget(payment.CallbackUrl, platformDomain))
        {
            _logger.LogWarning("Blocked unsafe merchant callback URL for payment {PaymentId}", paymentId);
            return;
        }

        if (platformDomain != null
            && !UrlSafety.IsSafeRedirectUrl(payment.CallbackUrl, platformDomain))
        {
            _logger.LogWarning("Blocked callback URL outside platform domain for payment {PaymentId}", paymentId);
            return;
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var payload = JsonSerializer.Serialize(new
        {
            id = payment.Id,
            orderId = payment.MerchantOrderId,
            amount = payment.Amount,
            currency = payment.Currency,
            status = payment.Status.ToString(),
            provider = payment.Provider.ToString(),
            description = payment.Description,
            checkoutUrl = string.IsNullOrWhiteSpace(payment.CheckoutUrl) ? null : payment.CheckoutUrl,
            createdAtUtc = payment.CreatedAtUtc,
            paidAtUtc = payment.PaidAtUtc,
            expiredAtUtc = payment.ExpiredAtUtc,
            failureReason = payment.FailureReason
        });

        var signature = ComputeHmac(payload, payment.Merchant.WebhookSecret);
        var client = _httpClientFactory.CreateClient("merchant-webhooks");
        using var request = new HttpRequestMessage(HttpMethod.Post, payment.CallbackUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Fynexpay-Signature", signature);
        request.Headers.Add("X-Fynexpay-Timestamp", timestamp);
        request.Headers.Add("X-Fynexpay-Event", "payment.updated");

        try
        {
            var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Merchant webhook returned {Status} for payment {PaymentId}", response.StatusCode, paymentId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Merchant webhook failed for payment {PaymentId}", paymentId);
        }
    }

    private static string ComputeHmac(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
