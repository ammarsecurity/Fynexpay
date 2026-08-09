using Fynexpay.Domain.Enums;

namespace Fynexpay.Application.Abstractions.Payments;

public sealed class CreateProviderPaymentRequest
{
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "IQD";
    public string Description { get; init; } = string.Empty;
    public string? StatusCallbackUrl { get; init; }
    public string? SuccessUrl { get; init; }
    public string? FailureUrl { get; init; }
    public string MerchantOrderId { get; init; } = string.Empty;
}

public sealed class ProviderPaymentResult
{
    public bool Success { get; init; }
    public string? ProviderPaymentId { get; init; }
    public string? CheckoutUrl { get; init; }
    public string? QrCode { get; init; }
    public string? ReadableCode { get; init; }
    public DateTime? ValidUntilUtc { get; init; }
    public string? RawResponse { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class ProviderStatusResult
{
    public PaymentStatus Status { get; init; }
    public DateTime? PaidAtUtc { get; init; }
    public string? FailureReason { get; init; }
    public string? RawResponse { get; init; }
}

public sealed class ProviderWebhookResult
{
    public Guid? PaymentId { get; init; }
    public string? ProviderPaymentId { get; init; }
    public PaymentStatus Status { get; init; }
    public string RawPayload { get; init; } = string.Empty;
}

public interface IPaymentProvider
{
    PaymentProviderType ProviderType { get; }
    Task<ProviderPaymentResult> CreatePaymentAsync(CreateProviderPaymentRequest request, CancellationToken ct = default);
    Task<ProviderStatusResult> GetStatusAsync(string providerPaymentId, CancellationToken ct = default);
    Task<bool> CancelAsync(string providerPaymentId, CancellationToken ct = default);
    Task<ProviderWebhookResult?> HandleWebhookAsync(string payload, IDictionary<string, string> headers, CancellationToken ct = default);
}

public interface IPaymentProviderResolver
{
    IPaymentProvider Resolve(PaymentProviderType type);
    PaymentProviderType ResolveAuto();
    IReadOnlyList<PaymentProviderType> GetEnabledProviders();
    Task<IReadOnlyList<PaymentProviderType>> GetEnabledProvidersAsync(CancellationToken ct = default);
}
