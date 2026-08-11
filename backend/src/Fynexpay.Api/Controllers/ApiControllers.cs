using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Fynexpay.Api.Security;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Abstractions.Messaging;
using Fynexpay.Application.Abstractions.Payments;
using Fynexpay.Application.DTOs;
using Fynexpay.Application.Security;
using Fynexpay.Application.Services;
using Fynexpay.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Fynexpay.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Dashboard Auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    [HttpGet("register/policy")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthPolicyDto>> RegisterPolicy(CancellationToken ct)
        => Ok(await _auth.GetRegisterPolicyAsync(ct));

    [HttpPost("register/send-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<OtpSendResultDto>> SendRegisterOtp([FromBody] RegisterMerchantRequest request, CancellationToken ct)
    {
        try { return Ok(await _auth.SendRegisterOtpAsync(request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("register/verify-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> VerifyRegisterOtp([FromBody] VerifyRegisterOtpRequest request, CancellationToken ct)
    {
        try { return Ok(await _auth.VerifyRegisterOtpAsync(request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterMerchantRequest request, CancellationToken ct)
    {
        try { return Ok(await _auth.RegisterMerchantAsync(request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try { return Ok(await _auth.LoginAsync(request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }
}

[ApiController]
[Route("api/merchant")]
[Authorize(Roles = "MerchantOwner,MerchantStaff")]
[Tags("Dashboard Merchant")]
public class MerchantDashboardController : ControllerBase
{
    private readonly MerchantAdminService _merchants;
    private readonly MerchantPlatformService _platforms;
    private readonly WalletService _wallets;
    private readonly PayoutService _payouts;
    private readonly PaymentService _payments;
    private readonly NotificationService _notifications;
    private readonly ProfileService _profiles;
    private readonly IAppDbContext _db;

    public MerchantDashboardController(
        MerchantAdminService merchants,
        MerchantPlatformService platforms,
        WalletService wallets,
        PayoutService payouts,
        PaymentService payments,
        NotificationService notifications,
        ProfileService profiles,
        IAppDbContext db)
    {
        _merchants = merchants;
        _platforms = platforms;
        _wallets = wallets;
        _payouts = payouts;
        _payments = payments;
        _notifications = notifications;
        _profiles = profiles;
        _db = db;
    }

    private Guid MerchantId => Guid.Parse(User.FindFirstValue("merchant_id")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task EnsureActiveMerchantAsync(CancellationToken ct)
    {
        var me = await _merchants.GetMerchantAsync(MerchantId, ct);
        if (!string.Equals(me.Status, MerchantStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("حساب التاجر غير مفعّل بعد");
    }

    [HttpGet("me")]
    public async Task<ActionResult<MerchantDto>> Me(CancellationToken ct) => Ok(await _merchants.GetMerchantAsync(MerchantId, ct));

    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> Profile(CancellationToken ct)
        => Ok(await _profiles.GetAsync(UserId, ct));

    [HttpPost("profile/request-otp")]
    public async Task<ActionResult<OtpSendResultDto>> RequestProfileOtp([FromBody] UpdateMerchantProfileRequest request, CancellationToken ct)
    {
        try { return Ok(await _profiles.RequestMerchantChangeAsync(UserId, request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    [HttpPost("profile/confirm")]
    public async Task<ActionResult<AuthResponse>> ConfirmProfile([FromBody] ConfirmProfileOtpRequest request, CancellationToken ct)
    {
        try { return Ok(await _profiles.ConfirmMerchantChangeAsync(UserId, request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    [HttpGet("wallet")]
    public async Task<ActionResult<WalletDto>> Wallet(CancellationToken ct) => Ok(await _wallets.GetAsync(MerchantId, ct));

    [HttpGet("payments")]
    public async Task<ActionResult<PagedResult<PaymentDto>>> Payments(
        [FromQuery] string? status,
        [FromQuery] string? provider,
        [FromQuery] string? q,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        await _payments.PurgeExpiredIncompleteCheckoutsAsync(ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var query = _db.Payments.Where(p => p.MerchantId == MerchantId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var st))
            query = query.Where(p => p.Status == st);
        if (!string.IsNullOrWhiteSpace(provider) && Enum.TryParse<PaymentProviderType>(provider, true, out var pv) && pv != PaymentProviderType.Auto)
            query = query.Where(p => p.Provider == pv);
        if (from.HasValue) query = query.Where(p => p.CreatedAtUtc >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(p => p.CreatedAtUtc <= to.Value.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                (p.MerchantOrderId != null && p.MerchantOrderId.Contains(term)) ||
                (p.Description != null && p.Description.Contains(term)) ||
                p.Id.ToString().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var list = await query.OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(new PagedResult<PaymentDto>(list.Select(p => _payments.Map(p)).ToList(), total, page, pageSize));
    }

    [HttpGet("payments/{id:guid}")]
    public async Task<ActionResult<PaymentDto>> PaymentDetail(Guid id, CancellationToken ct)
    {
        if (await _payments.TryPurgeExpiredIncompleteCheckoutAsync(id, ct))
            return NotFound();
        var payment = await _payments.GetDetailAsync(id, MerchantId, ct);
        return payment == null ? NotFound() : Ok(payment);
    }

    [HttpPost("test-payments")]
    public async Task<ActionResult<PaymentDto>> CreateTestPayment([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        try
        {
            await EnsureActiveMerchantAsync(ct);
            if (request.MerchantPlatformId is null)
                return BadRequest(new { message = "اختر منصة معتمدة لإنشاء دفعة تجريبية" });
            var idem = $"dashboard-test-{Guid.NewGuid():N}";
            return Ok(await _payments.CreateAsync(MerchantId, request, idem, ct, request.MerchantPlatformId, isTest: true));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("test-payments/{id:guid}")]
    public async Task<ActionResult<PaymentDto>> GetTestPayment(Guid id, CancellationToken ct)
    {
        var payment = await _payments.GetAsync(MerchantId, id, ct);
        return payment == null ? NotFound() : Ok(payment);
    }

    [HttpPost("test-payments/{id:guid}/mock-complete")]
    public async Task<ActionResult<PaymentDto>> MockCompleteTestPayment(
        Guid id,
        [FromServices] IHostEnvironment env,
        [FromServices] IConfiguration config,
        CancellationToken ct)
    {
        if (!MockPaymentAccess.IsAllowed(env, config))
            return NotFound();

        try { await EnsureActiveMerchantAsync(ct); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }

        var existing = await _payments.GetAsync(MerchantId, id, ct);
        if (existing == null) return NotFound();

        await _payments.ApplyProviderStatusAsync(
            id,
            PaymentStatus.Paid,
            "DashboardMock",
            System.Text.Json.JsonSerializer.Serialize(new { paymentId = id, status = "Paid", source = "merchant-test-ui" }),
            ct: ct);

        var updated = await _payments.GetAsync(MerchantId, id, ct);
        return Ok(updated);
    }

    [HttpGet("payment-methods")]
    public async Task<ActionResult<MerchantPaymentMethodsDto>> PaymentMethods(CancellationToken ct)
        => Ok(await _payments.GetPaymentMethodsAsync(MerchantId, ct));

    [HttpPut("payment-methods")]
    public async Task<ActionResult<MerchantPaymentMethodsDto>> UpdatePaymentMethods(
        [FromBody] UpdateMerchantPaymentMethodsRequest request, CancellationToken ct)
    {
        try { return Ok(await _payments.UpdatePaymentMethodsAsync(MerchantId, request, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("platforms")]
    public async Task<ActionResult<IEnumerable<MerchantPlatformDto>>> Platforms(CancellationToken ct)
        => Ok(await _platforms.ListForMerchantAsync(MerchantId, ct));

    [HttpPost("platforms")]
    public async Task<ActionResult<MerchantPlatformDto>> CreatePlatform([FromBody] CreateMerchantPlatformRequest request, CancellationToken ct)
    {
        try { return Ok(await _platforms.RequestAsync(MerchantId, request, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPatch("platforms/{id:guid}")]
    public async Task<ActionResult<MerchantPlatformDto>> UpdatePlatform(Guid id, [FromBody] UpdateMerchantPlatformRequest request, CancellationToken ct)
    {
        try { return Ok(await _platforms.UpdateAsync(MerchantId, id, request, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("platforms/{id:guid}/regenerate-key")]
    public async Task<ActionResult<MerchantPlatformDto>> RegeneratePlatformKey(Guid id, CancellationToken ct)
    {
        try { return Ok(await _platforms.RegenerateKeyAsync(MerchantId, id, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("platforms/{id:guid}/claim-key")]
    public async Task<ActionResult<object>> ClaimPlatformKey(Guid id, CancellationToken ct)
    {
        try
        {
            var (live, test) = await _platforms.ClaimKeysAsync(MerchantId, id, ct);
            return Ok(new { apiKey = live, testApiKey = test, liveApiKey = live });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("platforms/{id:guid}/logo")]
    [RequestSizeLimit(2_000_000)]
    public async Task<ActionResult<MerchantPlatformDto>> UploadPlatformLogo(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "لم يتم رفع ملف" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".png")
            return BadRequest(new { message = "الشعار يجب أن يكون PNG فقط — مقاس 500×500 بخلفية شفافة" });

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        try
        {
            PlatformLogoValidator.Validate(bytes);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "platforms");
        Directory.CreateDirectory(uploadsDir);

        // Remove previous file for this platform if present under our uploads folder.
        var existing = (await _platforms.ListForMerchantAsync(MerchantId, ct)).FirstOrDefault(p => p.Id == id);
        if (existing?.LogoUrl is { } oldUrl && oldUrl.StartsWith("/uploads/platforms/", StringComparison.OrdinalIgnoreCase))
        {
            var oldName = Path.GetFileName(oldUrl.Split('?', 2)[0]);
            var oldPath = Path.Combine(uploadsDir, oldName);
            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);
        }

        var fileName = $"{id:N}-{Guid.NewGuid():N}.png";
        var fullPath = Path.Combine(uploadsDir, fileName);
        await System.IO.File.WriteAllBytesAsync(fullPath, bytes, ct);
        var logoUrl = $"/uploads/platforms/{fileName}?v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        try { return Ok(await _platforms.SetLogoAsync(MerchantId, id, logoUrl, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("platforms/{id:guid}/logo")]
    public async Task<ActionResult<MerchantPlatformDto>> RemovePlatformLogo(Guid id, CancellationToken ct)
    {
        try
        {
            var list = await _platforms.ListForMerchantAsync(MerchantId, ct);
            var existing = list.FirstOrDefault(p => p.Id == id);
            if (existing?.LogoUrl is { } oldUrl && oldUrl.StartsWith("/uploads/platforms/", StringComparison.OrdinalIgnoreCase))
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "platforms");
                var oldName = Path.GetFileName(oldUrl.Split('?', 2)[0]);
                var oldPath = Path.Combine(uploadsDir, oldName);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            return Ok(await _platforms.ClearLogoAsync(MerchantId, id, ct));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("api-keys")]
    public async Task<ActionResult<IEnumerable<ApiKeyDto>>> ApiKeys(CancellationToken ct)
        => Ok(await _merchants.ListApiKeysAsync(MerchantId, ct));

    [HttpPost("api-keys")]
    public async Task<ActionResult> CreateApiKey([FromBody] CreateApiKeyBody body, CancellationToken ct)
    {
        try
        {
            await _merchants.CreateApiKeyAsync(MerchantId, body.Name ?? "Key", ct);
            return Ok();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("api-keys/{id:guid}")]
    public async Task<IActionResult> RevokeApiKey(Guid id, CancellationToken ct)
    {
        await _merchants.RevokeApiKeyAsync(MerchantId, id, ct);
        return NoContent();
    }

    [HttpGet("webhook-secret")]
    public async Task<ActionResult<object>> WebhookSecret(CancellationToken ct)
        => Ok(new { secret = await _merchants.GetWebhookSecretAsync(MerchantId, ct) });

    [HttpGet("payouts")]
    public async Task<ActionResult<PagedResult<PayoutDto>>> Payouts(
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var query = _db.PayoutRequests.Where(p => p.MerchantId == MerchantId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PayoutStatus>(status, true, out var st))
            query = query.Where(p => p.Status == st);
        if (from.HasValue) query = query.Where(p => p.CreatedAtUtc >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(p => p.CreatedAtUtc <= to.Value.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                p.DestinationDetails.Contains(term) ||
                p.DestinationType.Contains(term) ||
                p.Id.ToString().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var list = await query.OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var items = list.Select(p => new PayoutDto(
            p.Id, p.Amount, p.Currency, p.Status.ToString(), p.DestinationType, p.DestinationDetails,
            p.AdminNote, p.CreatedAtUtc, p.ReviewedAtUtc, p.CompletedAtUtc)).ToList();
        return Ok(new PagedResult<PayoutDto>(items, total, page, pageSize));
    }

    [HttpPost("payouts")]
    public async Task<ActionResult<PayoutDto>> CreatePayout([FromBody] CreatePayoutRequest request, CancellationToken ct)
    {
        try
        {
            await EnsureActiveMerchantAsync(ct);
            return Ok(await _payouts.CreateAsync(MerchantId, request, ct));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<NotificationSummaryDto>> Notifications(CancellationToken ct)
        => Ok(await _notifications.SummaryAsync(UserId, ct));

    [HttpPost("notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkNotificationRead(Guid id, CancellationToken ct)
    {
        await _notifications.MarkReadAsync(UserId, id, ct);
        return NoContent();
    }

    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead(CancellationToken ct)
    {
        await _notifications.MarkAllReadAsync(UserId, ct);
        return NoContent();
    }

    public record CreateApiKeyBody(string? Name);
}

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[Tags("Dashboard Admin")]
public class AdminController : ControllerBase
{
    private readonly MerchantAdminService _merchants;
    private readonly MerchantPlatformService _platforms;
    private readonly PayoutService _payouts;
    private readonly PaymentService _payments;
    private readonly IAppDbContext _db;
    private readonly IProviderSettingsService _providerSettings;
    private readonly LandingContentService _landing;
    private readonly IUltramsgSettingsService _ultramsgSettings;
    private readonly IUltramsgClient _ultramsg;
    private readonly IEmailSender _emailSender;
    private readonly NotificationService _notifications;
    private readonly INotificationSettingsService _notificationSettings;
    private readonly ProfileService _profiles;

    public AdminController(
        MerchantAdminService merchants,
        MerchantPlatformService platforms,
        PayoutService payouts,
        PaymentService payments,
        IAppDbContext db,
        IProviderSettingsService providerSettings,
        LandingContentService landing,
        IUltramsgSettingsService ultramsgSettings,
        IUltramsgClient ultramsg,
        IEmailSender emailSender,
        NotificationService notifications,
        INotificationSettingsService notificationSettings,
        ProfileService profiles)
    {
        _merchants = merchants;
        _platforms = platforms;
        _payouts = payouts;
        _payments = payments;
        _db = db;
        _providerSettings = providerSettings;
        _landing = landing;
        _ultramsgSettings = ultramsgSettings;
        _ultramsg = ultramsg;
        _emailSender = emailSender;
        _notifications = notifications;
        _notificationSettings = notificationSettings;
        _profiles = profiles;
    }

    private Guid AdminUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> Profile(CancellationToken ct)
        => Ok(await _profiles.GetAsync(AdminUserId, ct));

    [HttpPut("profile")]
    public async Task<ActionResult<AuthResponse>> UpdateProfile([FromBody] UpdateAdminProfileRequest request, CancellationToken ct)
    {
        try { return Ok(await _profiles.UpdateAdminAsync(AdminUserId, request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<PlatformStatsDto>> Stats(CancellationToken ct) => Ok(await _merchants.GetStatsAsync(ct));

    [HttpGet("platforms")]
    public async Task<ActionResult<IEnumerable<MerchantPlatformDto>>> Platforms(
        [FromQuery] string? status,
        [FromQuery] string? q,
        CancellationToken ct)
        => Ok(await _platforms.ListAdminAsync(status, q, ct));

    [HttpGet("platforms/{id:guid}")]
    public async Task<ActionResult<MerchantPlatformDetailDto>> PlatformDetail(Guid id, CancellationToken ct)
    {
        try { return Ok(await _platforms.GetAdminDetailAsync(id, ct)); }
        catch (InvalidOperationException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPatch("platforms/{id:guid}")]
    public async Task<ActionResult<MerchantPlatformDto>> ReviewPlatform(
        Guid id,
        [FromBody] ReviewMerchantPlatformRequest request,
        CancellationToken ct)
    {
        try
        {
            var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _platforms.ReviewAsync(id, adminId, request, ct));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("merchants")]
    public async Task<ActionResult<PagedResult<MerchantDto>>> Merchants(
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var query = _db.Merchants.Include(m => m.Wallet).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MerchantStatus>(status, true, out var st))
            query = query.Where(m => m.Status == st);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(m =>
                m.BusinessName.Contains(term) ||
                (m.BusinessNameAr != null && m.BusinessNameAr.Contains(term)) ||
                m.ContactEmail.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var list = await query.OrderByDescending(m => m.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new MerchantDto(
                m.Id, m.BusinessName, m.BusinessNameAr, m.ContactEmail, m.ContactPhone,
                m.Status.ToString(),
                m.CommissionPercent,
                m.FibCommissionPercent,
                m.ZainCashCommissionPercent,
                m.QiCommissionPercent,
                m.SuperQiCommissionPercent,
                m.AlqasehCommissionPercent,
                m.WebsiteUrl, m.CreatedAtUtc,
                m.Wallet != null ? m.Wallet.AvailableBalance : 0))
            .ToListAsync(ct);
        return Ok(new PagedResult<MerchantDto>(list, total, page, pageSize));
    }

    [HttpGet("merchants/{id:guid}")]
    public async Task<ActionResult<MerchantDetailDto>> MerchantDetail(Guid id, CancellationToken ct)
    {
        try { return Ok(await _merchants.GetDetailAsync(id, ct)); }
        catch (InvalidOperationException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPatch("merchants/{id:guid}")]
    public async Task<ActionResult<MerchantDto>> UpdateMerchant(Guid id, [FromBody] UpdateMerchantAdminRequest request, CancellationToken ct)
    {
        try { return Ok(await _merchants.UpdateAsync(id, request, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("merchants/{id:guid}")]
    public async Task<IActionResult> DeleteMerchant(Guid id, CancellationToken ct)
    {
        try
        {
            await _merchants.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("payments")]
    public async Task<ActionResult<PagedResult<PaymentDto>>> Payments(
        [FromQuery] string? status,
        [FromQuery] string? provider,
        [FromQuery] string? q,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        await _payments.PurgeExpiredIncompleteCheckoutsAsync(ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var query = _db.Payments.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var st))
            query = query.Where(p => p.Status == st);
        if (!string.IsNullOrWhiteSpace(provider) && Enum.TryParse<PaymentProviderType>(provider, true, out var pv) && pv != PaymentProviderType.Auto)
            query = query.Where(p => p.Provider == pv);
        if (from.HasValue) query = query.Where(p => p.CreatedAtUtc >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(p => p.CreatedAtUtc <= to.Value.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                (p.MerchantOrderId != null && p.MerchantOrderId.Contains(term)) ||
                (p.Description != null && p.Description.Contains(term)) ||
                p.Id.ToString().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var list = await query.Include(p => p.Merchant).OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(new PagedResult<PaymentDto>(list.Select(p => _payments.Map(p)).ToList(), total, page, pageSize));
    }

    [HttpGet("payments/{id:guid}")]
    public async Task<ActionResult<PaymentDto>> PaymentDetail(Guid id, CancellationToken ct)
    {
        if (await _payments.TryPurgeExpiredIncompleteCheckoutAsync(id, ct))
            return NotFound();
        var payment = await _payments.GetDetailAsync(id, null, ct);
        return payment == null ? NotFound() : Ok(payment);
    }

    [HttpGet("payouts")]
    public async Task<ActionResult<PagedResult<PayoutDto>>> Payouts(
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var query = _db.PayoutRequests.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PayoutStatus>(status, true, out var st))
            query = query.Where(p => p.Status == st);
        if (from.HasValue) query = query.Where(p => p.CreatedAtUtc >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(p => p.CreatedAtUtc <= to.Value.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                p.DestinationDetails.Contains(term) ||
                p.DestinationType.Contains(term) ||
                p.Id.ToString().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var list = await query.OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var items = list.Select(p => new PayoutDto(
            p.Id, p.Amount, p.Currency, p.Status.ToString(), p.DestinationType, p.DestinationDetails,
            p.AdminNote, p.CreatedAtUtc, p.ReviewedAtUtc, p.CompletedAtUtc)).ToList();
        return Ok(new PagedResult<PayoutDto>(items, total, page, pageSize));
    }

    [HttpPost("payouts/{id:guid}/review")]
    public async Task<ActionResult<PayoutDto>> ReviewPayout(Guid id, [FromBody] ReviewPayoutRequest request, CancellationToken ct)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try { return Ok(await _payouts.ReviewAsync(id, adminId, request, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("providers")]
    public async Task<ActionResult<ProviderRuntimeSettings>> Providers(CancellationToken ct)
        => Ok(await _providerSettings.GetAsync(ct));

    [HttpPut("providers")]
    public async Task<ActionResult<ProviderRuntimeSettings>> SaveProviders([FromBody] ProviderRuntimeSettings request, CancellationToken ct)
    {
        try { return Ok(await _providerSettings.SaveAsync(request, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("providers/environment")]
    public async Task<ActionResult<ProviderRuntimeSettings>> SetEnvironment([FromBody] SetProviderEnvironmentRequest request, CancellationToken ct)
    {
        try { return Ok(await _providerSettings.SetEnvironmentAsync(request.Environment, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("providers/load-demo")]
    public async Task<ActionResult<ProviderRuntimeSettings>> LoadDemo(CancellationToken ct)
    {
        try { return Ok(await _providerSettings.LoadOfficialSandboxDemoAsync(ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("landing")]
    public async Task<ActionResult<LandingContentDto>> GetLanding(CancellationToken ct)
        => Ok(await _landing.GetAsync(ct));

    [HttpPut("landing")]
    public async Task<ActionResult<LandingContentDto>> SaveLanding([FromBody] LandingContentDto request, CancellationToken ct)
        => Ok(await _landing.SaveAsync(request, ct));

    [HttpPost("landing/reset")]
    public async Task<ActionResult<LandingContentDto>> ResetLanding(CancellationToken ct)
        => Ok(await _landing.ResetAsync(ct));

    [HttpGet("ultramsg")]
    public async Task<ActionResult<UltramsgSettings>> GetUltramsg(CancellationToken ct)
    {
        var s = await _ultramsgSettings.GetAsync(ct);
        return Ok(_ultramsgSettings.MaskSecrets(s));
    }

    [HttpPut("ultramsg")]
    public async Task<ActionResult<UltramsgSettings>> SaveUltramsg([FromBody] UltramsgSettings request, CancellationToken ct)
    {
        try { return Ok(await _ultramsgSettings.SaveAsync(request, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("ultramsg/status")]
    public async Task<ActionResult<UltramsgStatusResult>> UltramsgStatus(CancellationToken ct)
        => Ok(await _ultramsg.GetStatusAsync(ct));

    [HttpGet("ultramsg/qr")]
    public async Task<IActionResult> UltramsgQr(CancellationToken ct)
    {
        try
        {
            var bytes = await _ultramsg.GetQrImageAsync(ct);
            if (bytes == null || bytes.Length == 0)
                return BadRequest(new { message = "لا توجد صورة QR" });
            return File(bytes, "image/png");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public record UltramsgTestMessageRequest(string Phone, string? Message);
    public record EmailTestRequest(string? To);

    [HttpPost("ultramsg/test")]
    public async Task<IActionResult> UltramsgTest([FromBody] UltramsgTestMessageRequest request, CancellationToken ct)
    {
        try
        {
            var settings = await _ultramsgSettings.GetAsync(ct);
            if (string.IsNullOrWhiteSpace(request.Phone))
                return BadRequest(new { message = "رقم الهاتف مطلوب" });
            var phone = request.Phone.Trim();
            if (!phone.StartsWith('+'))
            {
                var digits = new string(phone.Where(char.IsDigit).ToArray());
                if (digits.StartsWith('0'))
                    digits = settings.DefaultCountryCode + digits[1..];
                phone = "+" + digits;
            }
            var body = string.IsNullOrWhiteSpace(request.Message)
                ? "اختبار اتصال Fynexpay عبر Ultramsg ✓"
                : request.Message.Trim();
            await _ultramsg.SendChatAsync(phone, body, ct);
            return Ok(new { message = "تم إرسال رسالة الاختبار", to = phone });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("ultramsg/test-email")]
    public async Task<IActionResult> UltramsgTestEmail([FromBody] EmailTestRequest request, CancellationToken ct)
    {
        try
        {
            var settings = await _ultramsgSettings.GetAsync(ct);
            var to = string.IsNullOrWhiteSpace(request.To) ? settings.FromEmail : request.To.Trim();
            if (string.IsNullOrWhiteSpace(to) || !to.Contains('@'))
                return BadRequest(new { message = "بريد الاختبار غير صالح" });

            await _emailSender.SendAsync(
                to,
                "اختبار بريد Fynexpay",
                "<div style=\"font-family:Tahoma,Arial,sans-serif;direction:rtl\"><h3>نجح اتصال SMTP</h3><p>هذه رسالة اختبار من إعدادات التحقق في Fynexpay.</p></div>",
                ct);
            return Ok(new { message = "تم إرسال بريد الاختبار", to });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.Net.Mail.SmtpException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<NotificationSummaryDto>> Notifications(CancellationToken ct)
        => Ok(await _notifications.SummaryAsync(AdminUserId, ct));

    [HttpPost("notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkNotificationRead(Guid id, CancellationToken ct)
    {
        await _notifications.MarkReadAsync(AdminUserId, id, ct);
        return NoContent();
    }

    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead(CancellationToken ct)
    {
        await _notifications.MarkAllReadAsync(AdminUserId, ct);
        return NoContent();
    }

    [HttpGet("notification-settings")]
    public async Task<ActionResult<NotificationSettings>> GetNotificationSettings(CancellationToken ct)
        => Ok(await _notificationSettings.GetAsync(ct));

    [HttpPut("notification-settings")]
    public async Task<ActionResult<NotificationSettings>> SaveNotificationSettings([FromBody] NotificationSettings request, CancellationToken ct)
    {
        try { return Ok(await _notificationSettings.SaveAsync(request, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("providers/{key}/logo")]
    [RequestSizeLimit(2_000_000)]
    public async Task<ActionResult<ProviderRuntimeSettings>> UploadProviderLogo(string key, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp"))
            return BadRequest(new { message = "Allowed: png, jpg, jpeg, webp (SVG disabled for security)" });

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        if (!LooksLikeImage(bytes.AsSpan(0, Math.Min(bytes.Length, 12)), ext))
            return BadRequest(new { message = "File content does not match an allowed image type" });

        var settings = await _providerSettings.GetAsync(ct);
        var bundle = ResolveBundle(settings, key);
        if (bundle == null)
            return BadRequest(new { message = "Unknown provider key" });

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "providers");
        Directory.CreateDirectory(uploadsDir);
        var safeKey = key.Trim().ToLowerInvariant();
        var fileName = $"{safeKey}-{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsDir, fileName);
        await System.IO.File.WriteAllBytesAsync(fullPath, bytes, ct);

        bundle.LogoUrl = $"/uploads/providers/{fileName}?v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        return Ok(await _providerSettings.SaveAsync(settings, ct));
    }

    private static bool LooksLikeImage(ReadOnlySpan<byte> header, string ext)
    {
        if (header.Length < 3) return false;
        if (ext is ".png")
            return header.Length >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        if (ext is ".jpg" or ".jpeg")
            return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        if (ext is ".webp")
            return header.Length >= 12
                   && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
                   && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P';
        return false;
    }

    [HttpDelete("providers/{key}/logo")]
    public async Task<ActionResult<ProviderRuntimeSettings>> RemoveProviderLogo(string key, CancellationToken ct)
    {
        var settings = await _providerSettings.GetAsync(ct);
        var bundle = ResolveBundle(settings, key);
        if (bundle == null)
            return BadRequest(new { message = "Unknown provider key" });
        bundle.LogoUrl = null;
        return Ok(await _providerSettings.SaveAsync(settings, ct));
    }

    private static ProviderBundleSettings? ResolveBundle(ProviderRuntimeSettings settings, string key) =>
        key.Trim().ToLowerInvariant() switch
        {
            "fib" => settings.Fib,
            "zaincash" or "zain" => settings.ZainCash,
            "qi" => settings.Qi,
            "superqi" or "super-qi" => settings.SuperQi,
            "alqaseh" or "al-qaseh" or "qaseh" => settings.Alqaseh,
            _ => null
        };
}

[ApiController]
[Route("api/providers")]
[AllowAnonymous]
[Tags("Providers Catalog")]
public class ProvidersCatalogController : ControllerBase
{
    private readonly PaymentService _payments;

    public ProvidersCatalogController(PaymentService payments) => _payments = payments;

    [HttpGet("catalog")]
    public async Task<ActionResult<IReadOnlyList<ProviderCatalogItemDto>>> Catalog(CancellationToken ct)
        => Ok(await _payments.BuildProviderCatalogAsync(ct));
}

[ApiController]
[Route("api/landing")]
[AllowAnonymous]
[Tags("Landing")]
public class LandingController : ControllerBase
{
    private readonly LandingContentService _landing;

    public LandingController(LandingContentService landing) => _landing = landing;

    [HttpGet]
    public async Task<ActionResult<LandingContentDto>> Get(CancellationToken ct)
        => Ok(await _landing.GetAsync(ct));
}

public record SetProviderEnvironmentRequest(string Environment);

[ApiController]
[Route("v1")]
[Tags("Merchant API")]
[ApiExplorerSettings(GroupName = "merchant")]
[EnableRateLimiting("api-keys")]
public class MerchantPublicApiController : ControllerBase
{
    private readonly PaymentService _payments;
    private readonly WalletService _wallets;
    private readonly PayoutService _payouts;

    public MerchantPublicApiController(PaymentService payments, WalletService wallets, PayoutService payouts)
    {
        _payments = payments;
        _wallets = wallets;
        _payouts = payouts;
    }

    private Guid MerchantId => (Guid)HttpContext.Items["MerchantId"]!;

    [HttpPost("payments")]
    public async Task<ActionResult<PublicPaymentDto>> CreatePayment([FromBody] CreatePublicPaymentRequest request, CancellationToken ct)
    {
        var idem = Request.Headers["X-Idempotency-Key"].FirstOrDefault();
        var platformId = HttpContext.Items.TryGetValue("MerchantPlatformId", out var pid) && pid is Guid g ? g : (Guid?)null;
        var mapped = new CreatePaymentRequest(
            request.Amount,
            request.Currency,
            request.OrderId,
            request.ServiceType,
            request.ServiceType,
            null,
            request.SuccessUrl,
            request.FailureUrl,
            request.CallbackUrl,
            null,
            request.CustomerPhone);
        try
        {
            var isTest = HttpContext.Items.TryGetValue("ApiKeyIsTest", out var t) && t is true;
            return Ok(_payments.ToPublic(await _payments.CreateAsync(MerchantId, mapped, idem, ct, platformId, isTest)));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("payments/{id:guid}")]
    public async Task<ActionResult<PublicPaymentDto>> GetPayment(Guid id, CancellationToken ct)
    {
        if (await _payments.TryPurgeExpiredIncompleteCheckoutAsync(id, ct))
            return NotFound();
        var payment = await _payments.GetAsync(MerchantId, id, ct);
        return payment == null ? NotFound() : Ok(_payments.ToPublic(payment));
    }

    [HttpPost("payments/{id:guid}/cancel")]
    public async Task<ActionResult<PublicPaymentDto>> CancelPayment(Guid id, CancellationToken ct)
    {
        try { return Ok(_payments.ToPublic(await _payments.CancelAsync(MerchantId, id, ct))); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("wallet")]
    public async Task<ActionResult<WalletDto>> Wallet(CancellationToken ct) => Ok(await _wallets.GetAsync(MerchantId, ct));

    [HttpPost("payouts")]
    public async Task<ActionResult<PayoutDto>> CreatePayout([FromBody] CreatePayoutRequest request, CancellationToken ct)
    {
        try { return Ok(await _payouts.CreateAsync(MerchantId, request, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
[Tags("Provider Webhooks")]
[EnableRateLimiting("webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IPaymentProviderResolver _resolver;
    private readonly PaymentService _payments;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;

    public WebhooksController(
        IPaymentProviderResolver resolver,
        PaymentService payments,
        IHostEnvironment env,
        IConfiguration config)
    {
        _resolver = resolver;
        _payments = payments;
        _env = env;
        _config = config;
    }

    [HttpPost("{provider}")]
    public async Task<IActionResult> Handle(string provider, CancellationToken ct)
    {
        if (!Enum.TryParse<PaymentProviderType>(provider, true, out var type) || type == PaymentProviderType.Auto)
            return BadRequest();

        if (string.Equals(provider, "mock", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(ct);
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        var providerImpl = _resolver.Resolve(type);
        var result = await providerImpl.HandleWebhookAsync(payload, headers, ct);
        if (result == null)
            return Unauthorized(new { received = false, reason = "invalid_signature_or_payload" });

        // Defense in depth: confirm non-pending statuses with the provider API before mutating ledger.
        var statusToApply = result.Status;
        if (statusToApply != PaymentStatus.Pending && !string.IsNullOrWhiteSpace(result.ProviderPaymentId))
        {
            try
            {
                var live = await providerImpl.GetStatusAsync(result.ProviderPaymentId, ct);
                if (live.Status == PaymentStatus.Pending && statusToApply == PaymentStatus.Paid)
                    return Ok(new { received = true, applied = false, reason = "provider_status_unconfirmed" });
                if (live.Status != PaymentStatus.Pending)
                    statusToApply = live.Status;
            }
            catch
            {
                if (statusToApply == PaymentStatus.Paid)
                    return Ok(new { received = true, applied = false, reason = "provider_status_check_failed" });
            }
        }

        if (result.PaymentId.HasValue)
            await _payments.ApplyProviderStatusAsync(result.PaymentId.Value, statusToApply, type.ToString(), result.RawPayload, ct: ct);
        else if (!string.IsNullOrWhiteSpace(result.ProviderPaymentId))
            await _payments.ApplyByProviderPaymentIdAsync(result.ProviderPaymentId, type, statusToApply, result.RawPayload, ct: ct);

        return Ok(new { received = true, applied = true });
    }

    [HttpPost("mock/complete/{paymentId:guid}")]
    public async Task<IActionResult> MockComplete(Guid paymentId, CancellationToken ct)
    {
        if (!MockPaymentAccess.IsAllowed(_env, _config))
            return NotFound();

        var expected = _config["Security:MockWebhookSecret"];
        if (string.IsNullOrWhiteSpace(expected)
            || !Request.Headers.TryGetValue("X-Mock-Webhook-Secret", out var provided)
            || !string.Equals(expected, provided.ToString(), StringComparison.Ordinal))
            return Unauthorized();

        await _payments.ApplyProviderStatusAsync(paymentId, PaymentStatus.Paid, "Mock", JsonSerializer.Serialize(new { paymentId, status = "Paid" }), ct: ct);
        return Ok(new { completed = true, paymentId });
    }
}
