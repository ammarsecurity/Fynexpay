using Fynexpay.Domain.Enums;

namespace Fynexpay.Domain.Entities;

public class MerchantPlatform : BaseEntity
{
    public Guid MerchantId { get; set; }
    public Merchant Merchant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    /// <summary>Normalized host only, e.g. shop.example.com</summary>
    public string Domain { get; set; } = string.Empty;
    public PlatformStatus Status { get; set; } = PlatformStatus.Pending;
    public string? AdminNotes { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByUserId { get; set; }

    /// <summary>Plain API key shown once to the merchant after approve/regenerate; cleared after claim.</summary>
    public string? OneTimeApiKey { get; set; }

    public ApiKey? ApiKey { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
