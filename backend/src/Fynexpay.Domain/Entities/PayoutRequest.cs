using Fynexpay.Domain.Enums;

namespace Fynexpay.Domain.Entities;

public class PayoutRequest : BaseEntity
{
    public Guid MerchantId { get; set; }
    public Merchant Merchant { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "IQD";
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;
    public string DestinationType { get; set; } = "BankTransfer";
    public string DestinationDetails { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
