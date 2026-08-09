namespace Fynexpay.Domain.Entities;

public class ApiKey : BaseEntity
{
    public Guid MerchantId { get; set; }
    public Merchant Merchant { get; set; } = null!;
    public Guid? MerchantPlatformId { get; set; }
    public MerchantPlatform? MerchantPlatform { get; set; }
    public string Name { get; set; } = "Default";
    public string KeyPrefix { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastUsedAtUtc { get; set; }
}
