namespace Fynexpay.Domain.Enums;

public enum LedgerEntryType
{
    PaymentCredit = 1,
    PlatformFee = 2,
    PayoutDebit = 3,
    PayoutHold = 4,
    PayoutRelease = 5,
    Adjustment = 6
}
