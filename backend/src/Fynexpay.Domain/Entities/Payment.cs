using Fynexpay.Domain.Enums;

namespace Fynexpay.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid MerchantId { get; set; }
    public Merchant Merchant { get; set; } = null!;
    public Guid? MerchantPlatformId { get; set; }
    public MerchantPlatform? MerchantPlatform { get; set; }
    public string MerchantOrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "IQD";
    public string? Description { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentProviderType Provider { get; set; }
    public string? ProviderPaymentId { get; set; }
    /// <summary>رابط صفحة Fynexpay المستضافة لاختيار المزود.</summary>
    public string? CheckoutUrl { get; set; }
    /// <summary>رابط بوابة المزود بعد اختيار الزبون.</summary>
    public string? ProviderCheckoutUrl { get; set; }
    public string? QrCode { get; set; }
    public string? ReadableCode { get; set; }
    public string? SuccessUrl { get; set; }
    public string? FailureUrl { get; set; }
    public string? CallbackUrl { get; set; }
    public string? IdempotencyKey { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal NetAmount { get; set; }
    public bool LedgerApplied { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? ExpiredAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public string? ProviderRawResponse { get; set; }

    public ICollection<PaymentEvent> Events { get; set; } = new List<PaymentEvent>();
}
