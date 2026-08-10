using Fynexpay.Domain.Enums;

namespace Fynexpay.Domain.Entities;

public class OtpChallenge : BaseEntity
{
    public OtpPurpose Purpose { get; set; }
    public string PhoneE164 { get; set; } = string.Empty;
    public string? TargetEmail { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public bool Consumed { get; set; }
    public Guid? PaymentId { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime? LastSentAtUtc { get; set; }
}
