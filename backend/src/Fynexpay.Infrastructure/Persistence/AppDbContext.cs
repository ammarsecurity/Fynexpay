using Fynexpay.Application.Abstractions;
using Fynexpay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fynexpay.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<MerchantPlatform> MerchantPlatforms => Set<MerchantPlatform>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletLedgerEntry> WalletLedgerEntries => Set<WalletLedgerEntry>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
    public DbSet<PayoutRequest> PayoutRequests => Set<PayoutRequest>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Database.BeginTransactionAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.FullNameAr).HasMaxLength(200);
            e.HasOne(x => x.Merchant).WithMany(m => m.Users).HasForeignKey(x => x.MerchantId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Merchant>(e =>
        {
            e.Property(x => x.BusinessName).HasMaxLength(200);
            e.Property(x => x.CommissionPercent).HasPrecision(5, 2);
            e.Property(x => x.FibCommissionPercent).HasPrecision(5, 2);
            e.Property(x => x.ZainCashCommissionPercent).HasPrecision(5, 2);
            e.Property(x => x.QiCommissionPercent).HasPrecision(5, 2);
            e.Property(x => x.SuperQiCommissionPercent).HasPrecision(5, 2);
            e.Property(x => x.AlqasehCommissionPercent).HasPrecision(5, 2);
            e.Property(x => x.WebhookSecret).HasMaxLength(128);
            e.Property(x => x.KycIdFrontUrl).HasMaxLength(500);
            e.Property(x => x.KycIdBackUrl).HasMaxLength(500);
            e.Property(x => x.KycPassportUrl).HasMaxLength(500);
            e.Property(x => x.KycAdminNotes).HasMaxLength(1000);
            e.HasOne(x => x.Wallet).WithOne(w => w.Merchant).HasForeignKey<Wallet>(w => w.MerchantId);
        });

        modelBuilder.Entity<MerchantPlatform>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Domain).HasMaxLength(255);
            e.Property(x => x.LogoUrl).HasMaxLength(500);
            e.Property(x => x.AdminNotes).HasMaxLength(1000);
            e.Property(x => x.OneTimeApiKey).HasColumnType("longtext");
            e.Property(x => x.OneTimeTestApiKey).HasColumnType("longtext");
            e.HasIndex(x => new { x.MerchantId, x.Domain }).IsUnique();
            e.HasOne(x => x.Merchant).WithMany(m => m.Platforms).HasForeignKey(x => x.MerchantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApiKey>(e =>
        {
            e.HasIndex(x => x.KeyPrefix);
            e.Property(x => x.KeyPrefix).HasMaxLength(16);
            e.Property(x => x.KeyHash).HasMaxLength(128);
            e.HasOne(x => x.MerchantPlatform).WithMany(p => p.ApiKeys)
                .HasForeignKey(x => x.MerchantPlatformId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Wallet>(e =>
        {
            e.Property(x => x.AvailableBalance).HasPrecision(18, 2);
            e.Property(x => x.PendingBalance).HasPrecision(18, 2);
            e.Property(x => x.LifetimeGross).HasPrecision(18, 2);
            e.Property(x => x.LifetimeFees).HasPrecision(18, 2);
            e.Property(x => x.LifetimePayouts).HasPrecision(18, 2);
            e.HasIndex(x => x.MerchantId).IsUnique();
        });

        modelBuilder.Entity<WalletLedgerEntry>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.PlatformFee).HasPrecision(18, 2);
            e.Property(x => x.NetAmount).HasPrecision(18, 2);
            e.Property(x => x.MerchantOrderId).HasMaxLength(100);
            e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            // MySQL treats NULL ≠ NULL, so multiple payments without an idempotency key remain allowed.
            e.HasIndex(x => new { x.MerchantId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => x.ProviderPaymentId);
            e.HasIndex(x => x.MerchantPlatformId);
            e.Property(x => x.QrCode).HasColumnType("longtext");
            e.Property(x => x.ProviderRawResponse).HasColumnType("longtext");
            e.Property(x => x.CustomerPhone).HasMaxLength(20);
            e.Property(x => x.CustomerEmail).HasMaxLength(256);
            e.HasOne(x => x.MerchantPlatform).WithMany(p => p.Payments)
                .HasForeignKey(x => x.MerchantPlatformId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PaymentEvent>(e =>
        {
            e.Property(x => x.Payload).HasColumnType("longtext");
        });

        modelBuilder.Entity<PayoutRequest>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.DestinationDetails).HasMaxLength(500);
        });

        modelBuilder.Entity<PlatformSetting>(e =>
        {
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Key).HasMaxLength(100);
        });

        modelBuilder.Entity<OtpChallenge>(e =>
        {
            e.Property(x => x.PhoneE164).HasMaxLength(20);
            e.Property(x => x.TargetEmail).HasMaxLength(256);
            e.Property(x => x.CodeHash).HasMaxLength(128);
            e.Property(x => x.PayloadJson).HasColumnType("longtext");
            e.HasIndex(x => new { x.Purpose, x.PhoneE164, x.CreatedAtUtc });
            e.HasIndex(x => x.PaymentId);
        });

        modelBuilder.Entity<AppNotification>(e =>
        {
            e.ToTable("Notifications");
            e.Property(x => x.Type).HasMaxLength(64);
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Body).HasMaxLength(1000);
            e.Property(x => x.LinkUrl).HasMaxLength(500);
            e.Property(x => x.PayloadJson).HasColumnType("longtext");
            e.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAtUtc });
            e.HasIndex(x => x.MerchantId);
        });
    }
}
