using System.Text;
using System.Text.RegularExpressions;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Abstractions.Messaging;
using Fynexpay.Application.Abstractions.Payments;
using Fynexpay.Application.Services;
using Fynexpay.Domain.Entities;
using Fynexpay.Domain.Enums;
using Fynexpay.Infrastructure.Auth;
using Fynexpay.Infrastructure.Messaging;
using Fynexpay.Infrastructure.Payments;
using Fynexpay.Infrastructure.Persistence;
using Fynexpay.Infrastructure.Security;
using Fynexpay.Infrastructure.Webhooks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Fynexpay.Infrastructure;

public static class DependencyInjection
{
    private static readonly Regex SafeIdent = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppOptions>(configuration.GetSection("App"));
        services.Configure<PaymentProvidersOptions>(configuration.GetSection("PaymentProviders"));

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default must be configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ISecretProtector, SecretProtector>();
        services.AddScoped<IMerchantWebhookSender, MerchantWebhookSender>();
        services.AddScoped<IProviderSettingsService, ProviderSettingsService>();
        services.AddScoped<IUltramsgSettingsService, UltramsgSettingsService>();
        services.AddScoped<INotificationSettingsService, NotificationSettingsService>();
        services.AddScoped<IUltramsgClient, UltramsgClient>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IPaymentProviderResolver, PaymentProviderResolver>();
        services.AddScoped<IPaymentProvider, FibPaymentProvider>();
        services.AddScoped<IPaymentProvider, ZainCashPaymentProvider>();
        services.AddScoped<IPaymentProvider, QiPaymentProvider>();
        services.AddScoped<IPaymentProvider, SuperQiPaymentProvider>();
        services.AddScoped<IPaymentProvider, AlqasehPaymentProvider>();

        services.AddHttpClient("merchant-webhooks").ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient("fib");
        services.AddHttpClient("fib-auth");
        services.AddHttpClient("zaincash");
        services.AddHttpClient("qi");
        services.AddHttpClient("alqaseh");
        services.AddHttpClient("ultramsg").ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30));

        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be configured with at least 32 characters (use env var Jwt__Key in production).");

        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        if (!string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase)
            && jwtKey.Contains("DevOnly", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Jwt:Key must not use the Development placeholder in non-Development environments.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "Fynexpay",
                    ValidAudience = configuration["Jwt:Audience"] ?? "Fynexpay",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static async Task SeedAsync(IServiceProvider services, IHostEnvironment env)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await db.Database.EnsureCreatedAsync();
        await EnsureSchemaAsync(db);

        // Never seed a well-known admin outside Development.
        if (!env.IsDevelopment())
            return;

        var adminPassword = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminPassword))
            return;

        if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var email = (configuration["Seed:AdminEmail"] ?? "admin@fynexpay.iq").Trim().ToLowerInvariant();
            db.Users.Add(new User
            {
                Email = email,
                FullName = "مدير المنصة",
                PasswordHash = hasher.Hash(adminPassword),
                Role = UserRole.Admin,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureSchemaAsync(AppDbContext db)
    {
        await EnsureColumnAsync(db, "Merchants", "FibEnabled", "TINYINT(1) NOT NULL DEFAULT 1");
        await EnsureColumnAsync(db, "Merchants", "ZainCashEnabled", "TINYINT(1) NOT NULL DEFAULT 1");
        await EnsureColumnAsync(db, "Merchants", "QiEnabled", "TINYINT(1) NOT NULL DEFAULT 1");
        await EnsureColumnAsync(db, "Merchants", "SuperQiEnabled", "TINYINT(1) NOT NULL DEFAULT 1");
        await EnsureColumnAsync(db, "Merchants", "AlqasehEnabled", "TINYINT(1) NOT NULL DEFAULT 1");
        await EnsureColumnAsync(db, "Merchants", "FibCommissionPercent", "decimal(5,2) NOT NULL DEFAULT 2.50");
        await EnsureColumnAsync(db, "Merchants", "ZainCashCommissionPercent", "decimal(5,2) NOT NULL DEFAULT 2.50");
        await EnsureColumnAsync(db, "Merchants", "QiCommissionPercent", "decimal(5,2) NOT NULL DEFAULT 2.50");
        await EnsureColumnAsync(db, "Merchants", "SuperQiCommissionPercent", "decimal(5,2) NOT NULL DEFAULT 2.50");
        await EnsureColumnAsync(db, "Merchants", "AlqasehCommissionPercent", "decimal(5,2) NOT NULL DEFAULT 2.50");
        await EnsureColumnAsync(db, "Payments", "ProviderCheckoutUrl", "longtext NULL");
        await EnsureColumnAsync(db, "Payments", "RefundLedgerApplied", "TINYINT(1) NOT NULL DEFAULT 0");
        await EnsureMerchantPlatformsTableAsync(db);
        await EnsureColumnAsync(db, "ApiKeys", "MerchantPlatformId", "char(36) NULL");
        await EnsureColumnAsync(db, "Payments", "MerchantPlatformId", "char(36) NULL");
        await EnsureColumnAsync(db, "MerchantPlatforms", "LogoUrl", "varchar(500) NULL");
        await EnsureColumnAsync(db, "Payments", "CustomerPhone", "varchar(20) NULL");
        await EnsureColumnAsync(db, "Payments", "CustomerPhoneVerifiedAtUtc", "datetime(6) NULL");
        await EnsureColumnAsync(db, "Payments", "CustomerEmail", "varchar(256) NULL");
        await EnsureColumnAsync(db, "Payments", "IsTest", "TINYINT(1) NOT NULL DEFAULT 0");
        await EnsureColumnAsync(db, "ApiKeys", "IsTest", "TINYINT(1) NOT NULL DEFAULT 0");
        await EnsureColumnAsync(db, "MerchantPlatforms", "OneTimeTestApiKey", "longtext NULL");
        await EnsureOtpChallengesTableAsync(db);
        await EnsureColumnAsync(db, "OtpChallenges", "TargetEmail", "varchar(256) NULL");
        await EnsureNotificationsTableAsync(db);
    }

    private static async Task EnsureNotificationsTableAsync(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Notifications'
            """;
        var exists = Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;
        if (exists) return;

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE `Notifications` (
              `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
              `UserId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
              `MerchantId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL,
              `Type` varchar(64) NOT NULL,
              `Title` varchar(200) NOT NULL,
              `Body` varchar(1000) NOT NULL,
              `LinkUrl` varchar(500) NULL,
              `PayloadJson` longtext NULL,
              `IsRead` tinyint(1) NOT NULL,
              `ReadAtUtc` datetime(6) NULL,
              `EmailSent` tinyint(1) NOT NULL,
              `WhatsAppSent` tinyint(1) NOT NULL,
              `CreatedAtUtc` datetime(6) NOT NULL,
              `UpdatedAtUtc` datetime(6) NULL,
              PRIMARY KEY (`Id`),
              KEY `IX_Notifications_UserId_IsRead_CreatedAtUtc` (`UserId`, `IsRead`, `CreatedAtUtc`),
              KEY `IX_Notifications_MerchantId` (`MerchantId`)
            ) CHARACTER SET utf8mb4;
            """);
    }

    private static async Task EnsureOtpChallengesTableAsync(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'OtpChallenges'
            """;
        var exists = Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;
        if (exists) return;

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE `OtpChallenges` (
              `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
              `Purpose` int NOT NULL,
              `PhoneE164` varchar(20) NOT NULL,
              `CodeHash` varchar(128) NOT NULL,
              `ExpiresAtUtc` datetime(6) NOT NULL,
              `Attempts` int NOT NULL,
              `MaxAttempts` int NOT NULL,
              `Consumed` tinyint(1) NOT NULL,
              `PaymentId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL,
              `TargetEmail` varchar(256) NULL,
              `PayloadJson` longtext NULL,
              `LastSentAtUtc` datetime(6) NULL,
              `CreatedAtUtc` datetime(6) NOT NULL,
              `UpdatedAtUtc` datetime(6) NULL,
              PRIMARY KEY (`Id`),
              KEY `IX_OtpChallenges_PaymentId` (`PaymentId`),
              KEY `IX_OtpChallenges_Purpose_PhoneE164_CreatedAtUtc` (`Purpose`, `PhoneE164`, `CreatedAtUtc`)
            ) CHARACTER SET utf8mb4;
            """);
    }

    private static async Task EnsureMerchantPlatformsTableAsync(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'MerchantPlatforms'
            """;
        var exists = Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;
        if (exists) return;

        // No DB-level FK: Merchants.Id charset/collation from EnsureCreated can differ across installs.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE `MerchantPlatforms` (
              `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
              `MerchantId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
              `Name` varchar(200) NOT NULL,
              `Domain` varchar(255) NOT NULL,
              `Status` int NOT NULL,
              `AdminNotes` varchar(1000) NULL,
              `ReviewedAtUtc` datetime(6) NULL,
              `ReviewedByUserId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL,
              `OneTimeApiKey` longtext NULL,
              `CreatedAtUtc` datetime(6) NOT NULL,
              `UpdatedAtUtc` datetime(6) NULL,
              PRIMARY KEY (`Id`),
              UNIQUE KEY `IX_MerchantPlatforms_MerchantId_Domain` (`MerchantId`, `Domain`),
              KEY `IX_MerchantPlatforms_MerchantId` (`MerchantId`)
            ) CHARACTER SET utf8mb4;
            """);
    }

    private static async Task EnsureColumnAsync(AppDbContext db, string table, string column, string definition)
    {
        if (!SafeIdent.IsMatch(table) || !SafeIdent.IsMatch(column))
            throw new InvalidOperationException("Invalid schema identifier");

        // Allow only a constrained DDL definition alphabet (no user input reaches here).
        if (!Regex.IsMatch(definition, @"^[A-Za-z0-9_\(\)\s,\.]+$"))
            throw new InvalidOperationException("Invalid column definition");

        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND COLUMN_NAME = @column
            """;
        var pTable = cmd.CreateParameter();
        pTable.ParameterName = "@table";
        pTable.Value = table;
        cmd.Parameters.Add(pTable);
        var pCol = cmd.CreateParameter();
        pCol.ParameterName = "@column";
        pCol.Value = column;
        cmd.Parameters.Add(pCol);

        var exists = Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;
        if (!exists)
            await db.Database.ExecuteSqlRawAsync($"ALTER TABLE `{table}` ADD COLUMN `{column}` {definition}");
    }
}
