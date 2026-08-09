using Fynexpay.Domain.Enums;

namespace Fynexpay.Domain.Entities;

public class PlatformSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class ProviderPrioritySetting
{
    public PaymentProviderType Provider { get; set; }
    public int Priority { get; set; }
    public bool Enabled { get; set; } = true;
}
