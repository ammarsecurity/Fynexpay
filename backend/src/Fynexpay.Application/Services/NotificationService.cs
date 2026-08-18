using System.Text.Json;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Abstractions.Messaging;
using Fynexpay.Application.DTOs;
using Fynexpay.Domain.Entities;
using Fynexpay.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fynexpay.Application.Services;

public class NotificationService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IAppDbContext _db;
    private readonly INotificationSettingsService _settings;
    private readonly IUltramsgSettingsService _ultramsgSettings;
    private readonly IUltramsgClient _ultramsg;
    private readonly IEmailSender _email;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IAppDbContext db,
        INotificationSettingsService settings,
        IUltramsgSettingsService ultramsgSettings,
        IUltramsgClient ultramsg,
        IEmailSender email,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _settings = settings;
        _ultramsgSettings = ultramsgSettings;
        _ultramsg = ultramsg;
        _email = email;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationDto>> ListForUserAsync(Guid userId, int take = 40, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);
        var list = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<int> UnreadCountAsync(Guid userId, CancellationToken ct = default)
        => await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<NotificationSummaryDto> SummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await ListForUserAsync(userId, 40, ct);
        var unread = items.Count(i => !i.IsRead);
        return new NotificationSummaryDto(unread, items);
    }

    public async Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, ct);
        if (n == null || n.IsRead) return;
        n.IsRead = true;
        n.ReadAtUtc = DateTime.UtcNow;
        n.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        if (list.Count == 0) return;
        var now = DateTime.UtcNow;
        foreach (var n in list)
        {
            n.IsRead = true;
            n.ReadAtUtc = now;
            n.UpdatedAtUtc = now;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task NotifyAdminsSafeAsync(
        string type,
        string title,
        string body,
        string? linkUrl = null,
        Guid? merchantId = null,
        object? payload = null,
        CancellationToken ct = default)
    {
        try
        {
            var s = await _settings.GetAsync(ct);
            if (!IsEventEnabled(s, type)) return;

            var admins = await _db.Users
                .Where(u => u.Role == UserRole.Admin && u.IsActive)
                .ToListAsync(ct);
            if (admins.Count == 0) return;

            await DeliverToUsersAsync(admins, type, title, body, linkUrl, merchantId, payload, s, ct);
            await SendAdminAlertPhoneAsync(type, title, body, s, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed admin notification {Type}", type);
        }
    }

    public async Task NotifyMerchantUsersSafeAsync(
        Guid merchantId,
        string type,
        string title,
        string body,
        string? linkUrl = null,
        object? payload = null,
        CancellationToken ct = default)
    {
        try
        {
            var s = await _settings.GetAsync(ct);
            if (!IsEventEnabled(s, type)) return;

            var users = await _db.Users
                .Where(u => u.MerchantId == merchantId && u.IsActive)
                .ToListAsync(ct);
            if (users.Count == 0) return;

            await DeliverToUsersAsync(users, type, title, body, linkUrl, merchantId, payload, s, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed merchant notification {Type} for {MerchantId}", type, merchantId);
        }
    }

    private async Task DeliverToUsersAsync(
        IReadOnlyList<User> users,
        string type,
        string title,
        string body,
        string? linkUrl,
        Guid? merchantId,
        object? payload,
        NotificationSettings settings,
        CancellationToken ct)
    {
        var payloadJson = payload == null ? null : JsonSerializer.Serialize(payload, JsonOpts);
        var created = new List<AppNotification>();

        if (settings.InAppEnabled)
        {
            foreach (var user in users)
            {
                var n = new AppNotification
                {
                    UserId = user.Id,
                    MerchantId = merchantId,
                    Type = type,
                    Title = title,
                    Body = body,
                    LinkUrl = linkUrl,
                    PayloadJson = payloadJson
                };
                _db.Notifications.Add(n);
                created.Add(n);
            }
            await _db.SaveChangesAsync(ct);
        }

        if (!settings.EmailEnabled && !settings.WhatsAppEnabled)
            return;

        var ultra = await _ultramsgSettings.GetAsync(ct);
        foreach (var user in users)
        {
            var row = created.FirstOrDefault(c => c.UserId == user.Id);
            var emailSent = false;
            var waSent = false;

            if (settings.EmailEnabled && !string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    var html =
                        "<div style=\"font-family:Tahoma,Arial,sans-serif;direction:rtl;text-align:right;line-height:1.7\">" +
                        $"<h2 style=\"margin:0 0 12px;color:#031838\">{System.Net.WebUtility.HtmlEncode(title)}</h2>" +
                        $"<p style=\"margin:0 0 12px;color:#334155\">{System.Net.WebUtility.HtmlEncode(body)}</p>" +
                        "<p style=\"margin:16px 0 0;color:#94a3b8;font-size:12px\">Fynexpay</p></div>";
                    await _email.SendAsync(user.Email, title, html, ct);
                    emailSent = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Notification email failed for user {UserId}", user.Id);
                }
            }

            if (settings.WhatsAppEnabled && ultra.WhatsAppEnabled &&
                !string.IsNullOrWhiteSpace(ultra.InstanceId) &&
                !string.IsNullOrWhiteSpace(ultra.Token) &&
                !string.IsNullOrWhiteSpace(user.Phone))
            {
                try
                {
                    var phone = NormalizePhone(user.Phone, ultra.DefaultCountryCode);
                    if (!string.IsNullOrWhiteSpace(phone))
                    {
                        await _ultramsg.SendTemplateAsync(
                            phone,
                            WhatsAppTemplateKeys.ForNotification(type),
                            new Dictionary<string, string?> { ["title"] = title, ["body"] = body },
                            ct);
                        waSent = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Notification WhatsApp failed for user {UserId}", user.Id);
                }
            }

            if (row != null && (emailSent || waSent))
            {
                row.EmailSent = emailSent;
                row.WhatsAppSent = waSent;
                row.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        if (created.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    private async Task SendAdminAlertPhoneAsync(
        string type,
        string title,
        string body,
        NotificationSettings settings,
        CancellationToken ct)
    {
        var ultra = await _ultramsgSettings.GetAsync(ct);
        if (string.IsNullOrWhiteSpace(ultra.AdminAlertPhone))
            return;
        if (!ultra.WhatsAppEnabled || string.IsNullOrWhiteSpace(ultra.InstanceId) || string.IsNullOrWhiteSpace(ultra.Token))
            return;

        // Payout alerts always go to the ops WhatsApp when configured.
        // Other admin events follow the WhatsApp notification toggle.
        if (type != NotificationTypes.PayoutRequested && !settings.WhatsAppEnabled)
            return;

        var phone = NormalizePhone(ultra.AdminAlertPhone, ultra.DefaultCountryCode);
        if (string.IsNullOrWhiteSpace(phone)) return;

        try
        {
            await _ultramsg.SendTemplateAsync(
                phone,
                WhatsAppTemplateKeys.ForNotification(type),
                new Dictionary<string, string?> { ["title"] = title, ["body"] = body },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin alert WhatsApp failed for {Type}", type);
        }
    }

    private static bool IsEventEnabled(NotificationSettings s, string type) => type switch
    {
        NotificationTypes.MerchantRegistered => s.NotifyAdminMerchantRegistered,
        NotificationTypes.MerchantActivated or NotificationTypes.MerchantSuspended or NotificationTypes.MerchantRejected
            => s.NotifyMerchantStatusChanged,
        NotificationTypes.PlatformSubmitted => s.NotifyAdminPlatformSubmitted,
        NotificationTypes.PlatformApproved or NotificationTypes.PlatformRejected or NotificationTypes.PlatformSuspended
            => s.NotifyMerchantPlatformReviewed,
        NotificationTypes.PayoutRequested => s.NotifyAdminPayoutRequested,
        NotificationTypes.PayoutApproved or NotificationTypes.PayoutCompleted or NotificationTypes.PayoutRejected
            => s.NotifyMerchantPayoutReviewed,
        NotificationTypes.PaymentPaid => s.NotifyMerchantPaymentPaid,
        _ => true
    };

    private static string? NormalizePhone(string? raw, string defaultCountryCode = "964")
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith('0') && digits.Length >= 10)
            digits = defaultCountryCode + digits[1..];
        if (digits.Length < 10) return null;
        return digits;
    }

    private static NotificationDto Map(AppNotification n) => new(
        n.Id, n.Type, n.Title, n.Body, n.LinkUrl, n.IsRead, n.CreatedAtUtc, n.MerchantId);
}
