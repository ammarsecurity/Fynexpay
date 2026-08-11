using Fynexpay.Domain.Enums;

namespace Fynexpay.Domain.Entities;

public class MerchantPlatform : BaseEntity
{
    public Guid MerchantId { get; set; }
    public Merchant Merchant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    /// <summary>Normalized host only, e.g. shop.example.com</summary>
    public string Domain { get; set; } = string.Empty;
    /// <summary>Public URL to a 500×500 transparent PNG logo.</summary>
    public string? LogoUrl { get; set; }
    public PlatformStatus Status { get; set; } = PlatformStatus.Pending;
    public string? AdminNotes { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByUserId { get; set; }

    /// <summary>Encrypted one-time live API key (AES-GCM); cleared after claim. Never store plaintext.</summary>
    public string? OneTimeApiKey { get; set; }
    /// <summary>Encrypted one-time test/sandbox API key (AES-GCM); cleared after claim.</summary>
    public string? OneTimeTestApiKey { get; set; }

    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
