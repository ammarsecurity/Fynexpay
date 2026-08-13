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
    /// <summary>عمولة المنصة عند الدفع عبر FIB (%).</summary>
    public decimal FibCommissionPercent { get; set; } = 2.5m;
    /// <summary>عمولة المنصة عند الدفع عبر ZainCash (%).</summary>
    public decimal ZainCashCommissionPercent { get; set; } = 2.5m;
    /// <summary>عمولة المنصة عند الدفع عبر QI (%).</summary>
    public decimal QiCommissionPercent { get; set; } = 2.5m;
    /// <summary>عمولة المنصة عند الدفع عبر SuperQi (%).</summary>
    public decimal SuperQiCommissionPercent { get; set; } = 2.5m;
    /// <summary>عمولة المنصة عند الدفع عبر Alqaseh (%).</summary>
    public decimal AlqasehCommissionPercent { get; set; } = 2.5m;
    public string WebhookSecret { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string? Notes { get; set; }

    /// <summary>مزودو الدفع الذين يظهرون لزبون التاجر في صفحة الدفع المستضافة.</summary>
    public bool FibEnabled { get; set; } = true;
    public bool ZainCashEnabled { get; set; } = true;
    public bool QiEnabled { get; set; } = true;
    public bool SuperQiEnabled { get; set; } = true;
    public bool AlqasehEnabled { get; set; } = true;

    /// <summary>هوية وطنية — الوجه الأمامي.</summary>
    public string? KycIdFrontUrl { get; set; }
    /// <summary>هوية وطنية — الوجه الخلفي.</summary>
    public string? KycIdBackUrl { get; set; }
    /// <summary>جواز السفر الإلكتروني.</summary>
    public string? KycPassportUrl { get; set; }
    public KycStatus KycStatus { get; set; } = KycStatus.None;
    public string? KycAdminNotes { get; set; }
    public DateTime? KycSubmittedAtUtc { get; set; }
    public DateTime? KycReviewedAtUtc { get; set; }

    /// <summary>اسم البنك للتحويل عند السحب.</summary>
    public string? BankName { get; set; }
    /// <summary>اسم صاحب الحساب كما في البنك.</summary>
    public string? BankAccountHolder { get; set; }
    /// <summary>رقم الحساب البنكي.</summary>
    public string? BankAccountNumber { get; set; }
    /// <summary>رقم الآيبان (اختياري).</summary>
    public string? BankIban { get; set; }

    public Wallet? Wallet { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    public ICollection<MerchantPlatform> Platforms { get; set; } = new List<MerchantPlatform>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<PayoutRequest> Payouts { get; set; } = new List<PayoutRequest>();
}
