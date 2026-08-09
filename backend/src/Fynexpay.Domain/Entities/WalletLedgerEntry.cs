using Fynexpay.Domain.Enums;

namespace Fynexpay.Domain.Entities;

public class WalletLedgerEntry : BaseEntity
{
    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;
    public LedgerEntryType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Currency { get; set; } = "IQD";
    public string Description { get; set; } = string.Empty;
    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public Guid? PayoutRequestId { get; set; }
    public PayoutRequest? PayoutRequest { get; set; }
}
