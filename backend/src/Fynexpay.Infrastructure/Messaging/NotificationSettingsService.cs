using System.Text.Json;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Abstractions.Messaging;
using Fynexpay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fynexpay.Infrastructure.Messaging;

public class NotificationSettingsService : INotificationSettingsService
{
    public const string SettingsKey = "notification_runtime";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IAppDbContext _db;
    private NotificationSettings? _cache;

    public NotificationSettingsService(IAppDbContext db) => _db = db;

    public async Task<NotificationSettings> GetAsync(CancellationToken ct = default)
    {
        if (_cache != null) return Clone(_cache);

        var row = await _db.PlatformSettings.FirstOrDefaultAsync(s => s.Key == SettingsKey, ct);
        NotificationSettings settings;
        if (row == null || string.IsNullOrWhiteSpace(row.Value))
        {
            settings = new NotificationSettings();
            await PersistAsync(settings, ct);
        }
        else
        {
            settings = JsonSerializer.Deserialize<NotificationSettings>(row.Value, JsonOpts) ?? new NotificationSettings();
        }

        _cache = Clone(settings);
        return Clone(settings);
    }

    public async Task<NotificationSettings> SaveAsync(NotificationSettings settings, CancellationToken ct = default)
    {
        await PersistAsync(settings, ct);
        _cache = Clone(settings);
        return Clone(settings);
    }

    private async Task PersistAsync(NotificationSettings settings, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(settings, JsonOpts);
        var row = await _db.PlatformSettings.FirstOrDefaultAsync(s => s.Key == SettingsKey, ct);
        if (row == null)
        {
            _db.PlatformSettings.Add(new PlatformSetting
            {
                Key = SettingsKey,
                Value = json
            });
        }
        else
        {
            row.Value = json;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    private static NotificationSettings Clone(NotificationSettings s) => new()
    {
        InAppEnabled = s.InAppEnabled,
        EmailEnabled = s.EmailEnabled,
        WhatsAppEnabled = s.WhatsAppEnabled,
        NotifyAdminMerchantRegistered = s.NotifyAdminMerchantRegistered,
        NotifyMerchantStatusChanged = s.NotifyMerchantStatusChanged,
        NotifyAdminPlatformSubmitted = s.NotifyAdminPlatformSubmitted,
        NotifyMerchantPlatformReviewed = s.NotifyMerchantPlatformReviewed,
        NotifyAdminPayoutRequested = s.NotifyAdminPayoutRequested,
        NotifyMerchantPayoutReviewed = s.NotifyMerchantPayoutReviewed,
        NotifyMerchantPaymentPaid = s.NotifyMerchantPaymentPaid
    };
}
