namespace Fynexpay.Domain.Entities;

public class AppNotification : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? MerchantId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public string? PayloadJson { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public bool EmailSent { get; set; }
    public bool WhatsAppSent { get; set; }
}
