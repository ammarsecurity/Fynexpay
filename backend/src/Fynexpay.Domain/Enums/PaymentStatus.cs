namespace Fynexpay.Domain.Enums;

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Declined = 3,
    Expired = 4,
    Cancelled = 5,
    Refunded = 6
}
