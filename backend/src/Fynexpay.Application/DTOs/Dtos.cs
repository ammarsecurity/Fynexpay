using Fynexpay.Domain.Enums;

namespace Fynexpay.Application.DTOs;

public record RegisterMerchantRequest(
    string Email,
    string Password,
    string FullName,
    string FullNameAr,
    string BusinessName,
    string? BusinessNameAr,
    string? ContactPhone,
    string? WebsiteUrl);

public record VerifyRegisterOtpRequest(Guid ChallengeId, string Code);

public record OtpSendResultDto(Guid ChallengeId, string MaskedPhone, int ExpiresInSeconds, string? DevCode, string? Via = null);

public record AuthPolicyDto(bool RequireWhatsAppOtp, bool WhatsAppEnabled, bool EmailEnabled = false, string Channel = "WhatsApp");

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string Token,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    Guid? MerchantId,
    string? MerchantStatus);

public record UserProfileDto(
    Guid UserId,
    string Email,
    string FullName,
    string? FullNameAr,
    string? Phone,
    string Role,
    Guid? MerchantId,
    string? MerchantStatus,
    string? BusinessName,
    string? BusinessNameAr,
    string? WebsiteUrl,
    string? KycStatus = null,
    string? KycIdFrontUrl = null,
    string? KycIdBackUrl = null,
    string? KycPassportUrl = null,
    string? KycAdminNotes = null,
    DateTime? KycSubmittedAtUtc = null,
    DateTime? KycReviewedAtUtc = null,
    bool KycCanUpload = true);

public record MerchantKycDto(
    string Status,
    string? IdFrontUrl,
    string? IdBackUrl,
    string? PassportUrl,
    string? AdminNotes,
    DateTime? SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    bool CanUpload,
    bool IsComplete);

public record ReviewMerchantKycRequest(string Action, string? Notes);

public record UpdateAdminProfileRequest(string FullName, string Email, string? Phone);

public record UpdateMerchantProfileRequest(
    string FullName,
    string FullNameAr,
    string Email,
    string? Phone,
    string BusinessName,
    string? BusinessNameAr,
    string? WebsiteUrl);

public record ConfirmProfileOtpRequest(Guid ChallengeId, string Code);

public record CreatePaymentRequest(
    decimal Amount,
    string? Currency,
    string? OrderId,
    string? Description,
    string? ServiceType,
    string? Provider,
    string? SuccessUrl,
    string? FailureUrl,
    string? CallbackUrl,
    Guid? MerchantPlatformId = null,
    string? CustomerPhone = null);

/// <summary>
/// Public /v1 create-payment body. Platform is inferred from X-Api-Key (not sent in JSON).
/// </summary>
public record CreatePublicPaymentRequest(
    decimal Amount,
    string? Currency,
    string? OrderId,
    string? ServiceType,
    string? SuccessUrl,
    string? FailureUrl,
    string? CallbackUrl,
    string? CustomerPhone = null);

public record InitiatePaymentRequest(string Provider);

public record PaymentEventDto(
    Guid Id,
    string Source,
    string EventType,
    string Payload,
    DateTime CreatedAtUtc);

public record PaymentDto(
    Guid Id,
    Guid MerchantId,
    Guid? MerchantPlatformId,
    string? MerchantName,
    string OrderId,
    decimal Amount,
    string Currency,
    string Status,
    string Provider,
    string? Description,
    string? CheckoutUrl,
    string? ProviderCheckoutUrl,
    string? PlatformReturnUrl,
    string? QrCode,
    string? ReadableCode,
    string? SuccessUrl,
    string? FailureUrl,
    string? CallbackUrl,
    string? ProviderPaymentId,
    string? IdempotencyKey,
    decimal PlatformFee,
    decimal NetAmount,
    bool LedgerApplied,
    bool IsTest,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? PaidAtUtc,
    DateTime? ExpiredAtUtc,
    string? FailureReason,
    string? ProviderRawResponse,
    IReadOnlyList<string>? AvailableProviders,
    IReadOnlyList<PaymentEventDto>? Events = null);

/// <summary>
/// Lean payment payload for Merchant public API (/v1) and merchant webhooks.
/// </summary>
public record PublicPaymentDto(
    Guid Id,
    string OrderId,
    decimal Amount,
    string Currency,
    string Status,
    string Provider,
    string? Description,
    string? CheckoutUrl,
    string Mode,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc,
    DateTime? ExpiredAtUtc,
    string? FailureReason);

public record MerchantPaymentMethodsDto(
    bool FibEnabled,
    bool ZainCashEnabled,
    bool QiEnabled,
    bool SuperQiEnabled,
    bool AlqasehEnabled,
    IReadOnlyList<string> PlatformEnabled,
    IReadOnlyList<string> EffectiveProviders,
    IReadOnlyList<ProviderCatalogItemDto> Catalog);

public record UpdateMerchantPaymentMethodsRequest(
    bool? FibEnabled,
    bool? ZainCashEnabled,
    bool? QiEnabled,
    bool? SuperQiEnabled,
    bool? AlqasehEnabled);

public record WalletDto(
    decimal AvailableBalance,
    decimal PendingBalance,
    decimal LifetimeGross,
    decimal LifetimeFees,
    decimal LifetimePayouts,
    string Currency,
    IReadOnlyList<LedgerEntryDto> RecentEntries);

public record LedgerEntryDto(
    Guid Id,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string Description,
    DateTime CreatedAtUtc,
    Guid? PaymentId,
    Guid? PayoutRequestId);

public record CreatePayoutRequest(decimal Amount, string DestinationType, string DestinationDetails);

public record PayoutDto(
    Guid Id,
    decimal Amount,
    string Currency,
    string Status,
    string DestinationType,
    string DestinationDetails,
    string? AdminNote,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc,
    DateTime? CompletedAtUtc);

public record ApiKeyDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    bool IsActive,
    bool IsTest,
    DateTime CreatedAtUtc,
    DateTime? LastUsedAtUtc,
    Guid? MerchantPlatformId = null,
    string? PlatformName = null,
    string? PlatformDomain = null);

public record CreateApiKeyResponse(Guid Id, string Name, string KeyPrefix, string ApiKey, DateTime CreatedAtUtc);

public record CreateMerchantPlatformRequest(string Name, string Domain);
public record UpdateMerchantPlatformRequest(string? Name, string? Domain);
public record ReviewMerchantPlatformRequest(string Action, string? AdminNotes);

public record MerchantPlatformDto(
    Guid Id,
    Guid MerchantId,
    string? MerchantName,
    string Name,
    string Domain,
    string? LogoUrl,
    string Status,
    string? AdminNotes,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? ReviewedAtUtc,
    Guid? ApiKeyId,
    string? ApiKeyPrefix,
    Guid? TestApiKeyId,
    string? TestApiKeyPrefix,
    bool HasOneTimeApiKey,
    string? OneTimeApiKey = null,
    string? OneTimeTestApiKey = null);

public record MerchantPlatformDetailDto(
    Guid Id,
    Guid MerchantId,
    string? MerchantName,
    string? MerchantEmail,
    string? MerchantPhone,
    string? MerchantStatus,
    string Name,
    string Domain,
    string? LogoUrl,
    string Status,
    string? AdminNotes,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? ReviewedAtUtc,
    Guid? ReviewedByUserId,
    string? ReviewedByName,
    Guid? ApiKeyId,
    string? ApiKeyPrefix,
    bool ApiKeyIsActive,
    DateTime? ApiKeyCreatedAtUtc,
    Guid? TestApiKeyId,
    string? TestApiKeyPrefix,
    bool HasOneTimeApiKey,
    int PaymentsCount,
    decimal PaymentsVolume);

public record MerchantDto(
    Guid Id,
    string BusinessName,
    string? BusinessNameAr,
    string ContactEmail,
    string? ContactPhone,
    string Status,
    decimal CommissionPercent,
    decimal FibCommissionPercent,
    decimal ZainCashCommissionPercent,
    decimal QiCommissionPercent,
    decimal SuperQiCommissionPercent,
    decimal AlqasehCommissionPercent,
    string? WebsiteUrl,
    DateTime CreatedAtUtc,
    decimal AvailableBalance);

public record MerchantOwnerDto(
    Guid Id,
    string Email,
    string FullName,
    string? FullNameAr,
    string? Phone,
    bool IsActive,
    DateTime CreatedAtUtc);

public record MerchantDetailDto(
    Guid Id,
    string BusinessName,
    string? BusinessNameAr,
    string ContactEmail,
    string? ContactPhone,
    string Status,
    decimal CommissionPercent,
    decimal FibCommissionPercent,
    decimal ZainCashCommissionPercent,
    decimal QiCommissionPercent,
    decimal SuperQiCommissionPercent,
    decimal AlqasehCommissionPercent,
    string? WebsiteUrl,
    string? Notes,
    string WebhookSecret,
    bool FibEnabled,
    bool ZainCashEnabled,
    bool QiEnabled,
    bool SuperQiEnabled,
    bool AlqasehEnabled,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    decimal AvailableBalance,
    decimal PendingBalance,
    decimal LifetimeGross,
    decimal LifetimeFees,
    int PaymentsCount,
    int ApiKeysCount,
    IReadOnlyList<MerchantOwnerDto> Owners,
    string? KycStatus = null,
    string? KycIdFrontUrl = null,
    string? KycIdBackUrl = null,
    string? KycPassportUrl = null,
    string? KycAdminNotes = null,
    DateTime? KycSubmittedAtUtc = null,
    DateTime? KycReviewedAtUtc = null);

public record UpdateMerchantAdminRequest(
    string? Status,
    decimal? CommissionPercent,
    decimal? FibCommissionPercent,
    decimal? ZainCashCommissionPercent,
    decimal? QiCommissionPercent,
    decimal? SuperQiCommissionPercent,
    decimal? AlqasehCommissionPercent,
    string? Notes,
    string? BusinessName,
    string? BusinessNameAr,
    string? ContactEmail,
    string? ContactPhone,
    string? WebsiteUrl,
    bool? FibEnabled,
    bool? ZainCashEnabled,
    bool? QiEnabled,
    bool? SuperQiEnabled,
    bool? AlqasehEnabled,
    string? OwnerFullName,
    string? OwnerFullNameAr,
    string? OwnerEmail,
    string? OwnerPhone,
    string? NewPassword);
public record ReviewPayoutRequest(string Action, string? AdminNote);

public record NamedCountDto(string Key, int Count, decimal Amount = 0);

public record DailyVolumePointDto(string Date, int Count, decimal Volume, decimal Fees);

public record PlatformStatsDto(
    int MerchantsCount,
    int ActiveMerchants,
    int PendingMerchants,
    int PaymentsCount,
    int PaidCount,
    int PendingPayments,
    int FailedPayments,
    decimal GrossVolume,
    decimal PlatformFees,
    decimal NetToMerchants,
    decimal AvgTicket,
    int PendingPayouts,
    IReadOnlyList<DailyVolumePointDto> Last14Days,
    IReadOnlyList<NamedCountDto> ByStatus,
    IReadOnlyList<NamedCountDto> ByProvider);

public record ProviderConfigDto(
    string Provider,
    bool Enabled,
    int Priority,
    bool HasCredentials);

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string? LinkUrl,
    bool IsRead,
    DateTime CreatedAtUtc,
    Guid? MerchantId);

public record NotificationSummaryDto(int UnreadCount, IReadOnlyList<NotificationDto> Items);

public record ProviderCatalogItemDto(
    string Key,
    string Name,
    string? LogoUrl,
    bool Enabled,
    int Priority);

public static class EnumMaps
{
    public static PaymentProviderType ParseProvider(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return PaymentProviderType.Auto;
        if (value.Equals("superqi", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("super-qi", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("alipay", StringComparison.OrdinalIgnoreCase))
            return PaymentProviderType.SuperQi;
        if (value.Equals("al-qaseh", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("al_qaseh", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("qaseh", StringComparison.OrdinalIgnoreCase))
            return PaymentProviderType.Alqaseh;
        if (Enum.TryParse<PaymentProviderType>(value, true, out var parsed))
            return parsed;
        throw new ArgumentException($"Unknown provider: {value}");
    }
}
