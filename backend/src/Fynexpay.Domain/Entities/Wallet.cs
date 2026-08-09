namespace Fynexpay.Domain.Entities;

public class Wallet : BaseEntity
{
    public Guid MerchantId { get; set; }
    public Merchant Merchant { get; set; } = null!;
    public decimal AvailableBalance { get; set; }
    public decimal PendingBalance { get; set; }
    public decimal LifetimeGross { get; set; }
    public decimal LifetimeFees { get; set; }
    public decimal LifetimePayouts { get; set; }
    public string Currency { get; set; } = "IQD";

    public ICollection<WalletLedgerEntry> Entries { get; set; } = new List<WalletLedgerEntry>();
}
