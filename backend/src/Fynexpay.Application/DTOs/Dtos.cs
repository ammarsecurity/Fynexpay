using Fynexpay.Domain.Enums;

namespace Fynexpay.Application.DTOs;

public record RegisterMerchantRequest(
    string Email,
    string Password,
    string FullName,
    string BusinessName,
    string? BusinessNameAr,
    string? ContactPhone,
    string? WebsiteUrl);

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string Token,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    Guid? MerchantId,
    string? MerchantStatus);

public record CreatePaymentRequest(
    decimal Amount,
    string? Currency,
    string? OrderId,
    string? Description,
    string? ServiceType,
    string? Provider,
    string? SuccessUrl,
    string? FailureUrl,
    string? CallbackUrl);

public record InitiatePaymentRequest(string Provider);

public record PaymentDto(
    Guid Id,
    string OrderId,
    decimal Amount,
    string Currency,
    string Status,
    string Provider,
    string? Description,
    string? CheckoutUrl,
    string? ProviderCheckoutUrl,
    string? QrCode,
    string? ReadableCode,
    decimal PlatformFee,
    decimal NetAmount,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc,
    string? FailureReason,
    IReadOnlyList<string>? AvailableProviders);

public record MerchantPaymentMethodsDto(
    bool FibEnabled,
    bool ZainCashEnabled,
    bool QiEnabled,
    bool SuperQiEnabled,
    IReadOnlyList<string> PlatformEnabled,
    IReadOnlyList<string> EffectiveProviders);

public record UpdateMerchantPaymentMethodsRequest(
    bool? FibEnabled,
    bool? ZainCashEnabled,
    bool? QiEnabled,
    bool? SuperQiEnabled);

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

public record ApiKeyDto(Guid Id, string Name, string KeyPrefix, bool IsActive, DateTime CreatedAtUtc, DateTime? LastUsedAtUtc);
public record CreateApiKeyResponse(Guid Id, string Name, string KeyPrefix, string ApiKey, DateTime CreatedAtUtc);

public record MerchantDto(
    Guid Id,
    string BusinessName,
    string? BusinessNameAr,
    string ContactEmail,
    string? ContactPhone,
    string Status,
    decimal CommissionPercent,
    string? WebsiteUrl,
    DateTime CreatedAtUtc,
    decimal AvailableBalance);

public record UpdateMerchantAdminRequest(string? Status, decimal? CommissionPercent, string? Notes);
public record ReviewPayoutRequest(string Action, string? AdminNote);
public record PlatformStatsDto(
    int MerchantsCount,
    int ActiveMerchants,
    int PaymentsCount,
    decimal GrossVolume,
    decimal PlatformFees,
    int PendingPayouts);

public record ProviderConfigDto(
    string Provider,
    bool Enabled,
    int Priority,
    bool HasCredentials);

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
        if (Enum.TryParse<PaymentProviderType>(value, true, out var parsed))
            return parsed;
        throw new ArgumentException($"Unknown provider: {value}");
    }
}
