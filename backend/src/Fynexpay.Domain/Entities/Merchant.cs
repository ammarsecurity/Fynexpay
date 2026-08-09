using Fynexpay.Domain.Enums;

namespace Fynexpay.Domain.Entities;

public class Merchant : BaseEntity
{
    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessNameAr { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public MerchantStatus Status { get; set; } = MerchantStatus.Pending;
    public decimal CommissionPercent { get; set; } = 2.5m;
    public string WebhookSecret { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string? Notes { get; set; }

    /// <summary>مزودو الدفع الذين يظهرون لزبون التاجر في صفحة الدفع المستضافة.</summary>
    public bool FibEnabled { get; set; } = true;
    public bool ZainCashEnabled { get; set; } = true;
    public bool QiEnabled { get; set; } = true;
    public bool SuperQiEnabled { get; set; } = true;

    public Wallet? Wallet { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<PayoutRequest> Payouts { get; set; } = new List<PayoutRequest>();
}
