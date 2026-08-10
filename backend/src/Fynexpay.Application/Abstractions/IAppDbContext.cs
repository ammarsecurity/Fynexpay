using Fynexpay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fynexpay.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Merchant> Merchants { get; }
    DbSet<MerchantPlatform> MerchantPlatforms { get; }
    DbSet<ApiKey> ApiKeys { get; }
    DbSet<Wallet> Wallets { get; }
    DbSet<WalletLedgerEntry> WalletLedgerEntries { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentEvent> PaymentEvents { get; }
    DbSet<PayoutRequest> PayoutRequests { get; }
    DbSet<PlatformSetting> PlatformSettings { get; }
    DbSet<OtpChallenge> OtpChallenges { get; }
    DbSet<AppNotification> Notifications { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
