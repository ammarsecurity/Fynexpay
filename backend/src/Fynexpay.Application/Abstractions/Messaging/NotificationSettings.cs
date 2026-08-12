namespace Fynexpay.Application.Abstractions.Messaging;

public class NotificationSettings
{
    public bool InAppEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
    public bool WhatsAppEnabled { get; set; }

    public bool NotifyAdminMerchantRegistered { get; set; } = true;
    public bool NotifyMerchantStatusChanged { get; set; } = true;
    public bool NotifyAdminPlatformSubmitted { get; set; } = true;
    public bool NotifyMerchantPlatformReviewed { get; set; } = true;
    public bool NotifyAdminPayoutRequested { get; set; } = true;
    public bool NotifyMerchantPayoutReviewed { get; set; } = true;
    public bool NotifyMerchantPaymentPaid { get; set; } = true;
}

public interface INotificationSettingsService
{
    Task<NotificationSettings> GetAsync(CancellationToken ct = default);
    Task<NotificationSettings> SaveAsync(NotificationSettings settings, CancellationToken ct = default);
}

public static class NotificationTypes
{
    public const string MerchantRegistered = "MerchantRegistered";
    public const string MerchantActivated = "MerchantActivated";
    public const string MerchantSuspended = "MerchantSuspended";
    public const string MerchantRejected = "MerchantRejected";
    public const string PlatformSubmitted = "PlatformSubmitted";
    public const string PlatformApproved = "PlatformApproved";
    public const string PlatformRejected = "PlatformRejected";
    public const string PlatformSuspended = "PlatformSuspended";
    public const string PayoutRequested = "PayoutRequested";
    public const string PayoutApproved = "PayoutApproved";
    public const string PayoutCompleted = "PayoutCompleted";
    public const string PayoutRejected = "PayoutRejected";
    public const string PaymentPaid = "PaymentPaid";
    public const string KycSubmitted = "KycSubmitted";
    public const string KycApproved = "KycApproved";
    public const string KycRejected = "KycRejected";
}
