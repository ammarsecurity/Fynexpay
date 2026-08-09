using Fynexpay.Application.Abstractions;
using Fynexpay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.FullName).HasMaxLength(200);
            e.HasOne(x => x.Merchant).WithMany(m => m.Users).HasForeignKey(x => x.MerchantId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Merchant>(e =>
        {
            e.Property(x => x.BusinessName).HasMaxLength(200);
            e.Property(x => x.CommissionPercent).HasPrecision(5, 2);
            e.Property(x => x.WebhookSecret).HasMaxLength(128);
            e.HasOne(x => x.Wallet).WithOne(w => w.Merchant).HasForeignKey<Wallet>(w => w.MerchantId);
        });

        modelBuilder.Entity<MerchantPlatform>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Domain).HasMaxLength(255);
            e.Property(x => x.AdminNotes).HasMaxLength(1000);
            e.Property(x => x.OneTimeApiKey).HasColumnType("longtext");
            e.HasIndex(x => new { x.MerchantId, x.Domain }).IsUnique();
            e.HasOne(x => x.Merchant).WithMany(m => m.Platforms).HasForeignKey(x => x.MerchantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApiKey>(e =>
        {
            e.HasIndex(x => x.KeyPrefix);
            e.Property(x => x.KeyPrefix).HasMaxLength(16);
            e.Property(x => x.KeyHash).HasMaxLength(128);
            e.HasOne(x => x.MerchantPlatform).WithOne(p => p.ApiKey)
                .HasForeignKey<ApiKey>(x => x.MerchantPlatformId)
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
            e.HasIndex(x => new { x.MerchantId, x.IdempotencyKey });
            e.HasIndex(x => x.ProviderPaymentId);
            e.HasIndex(x => x.MerchantPlatformId);
            e.Property(x => x.QrCode).HasColumnType("longtext");
            e.Property(x => x.ProviderRawResponse).HasColumnType("longtext");
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
    }
}
