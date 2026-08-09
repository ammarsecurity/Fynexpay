using Fynexpay.Domain.Enums;

namespace Fynexpay.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? MerchantId { get; set; }
    public Merchant? Merchant { get; set; }
}
