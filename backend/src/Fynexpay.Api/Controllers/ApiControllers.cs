using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Abstractions.Payments;
using Fynexpay.Application.DTOs;
using Fynexpay.Application.Services;
using Fynexpay.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fynexpay.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Dashboard Auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterMerchantRequest request, CancellationToken ct)
    {
        try { return Ok(await _auth.RegisterMerchantAsync(request, ct)); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try { return Ok(await _auth.LoginAsync(request, ct)); }
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
    private readonly IAppDbContext _db;

    public MerchantDashboardController(
        MerchantAdminService merchants,
        MerchantPlatformService platforms,
        WalletService wallets,
        PayoutService payouts,
        PaymentService payments,
        IAppDbContext db)
    {
        _merchants = merchants;
        _platforms = platforms;
        _wallets = wallets;
        _payouts = payouts;
        _payments = payments;
        _db = db;
    }

    private Guid MerchantId => Guid.Parse(User.FindFirstValue("merchant_id")!);

    [HttpGet("me")]
    public async Task<ActionResult<MerchantDto>> Me(CancellationToken ct) => Ok(await _merchants.GetMerchantAsync(MerchantId, ct));

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
        var payment = await _payments.GetDetailAsync(id, MerchantId, ct);
        return payment == null ? NotFound() : Ok(payment);
    }

    [HttpPost("test-payments")]
    public async Task<ActionResult<PaymentDto>> CreateTestPayment([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        try
        {
            if (request.MerchantPlatformId is null)
                return BadRequest(new { message = "اختر منصة معتمدة لإنشاء دفعة تجريبية" });
            var idem = $"dashboard-test-{Guid.NewGuid():N}";
            return Ok(await _payments.CreateAsync(MerchantId, request, idem, ct, request.MerchantPlatformId));
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
    public async Task<ActionResult<PaymentDto>> MockCompleteTestPayment(Guid id, CancellationToken ct)
    {
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
        try { return Ok(new { apiKey = await _platforms.ClaimKeyAsync(MerchantId, id, ct) }); }
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
        try { return Ok(await _payouts.CreateAsync(MerchantId, request, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
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

    public AdminController(
        MerchantAdminService merchants,
        MerchantPlatformService platforms,
        PayoutService payouts,
        PaymentService payments,
        IAppDbContext db,
        IProviderSettingsService providerSettings,
        LandingContentService landing)
    {
        _merchants = merchants;
        _platforms = platforms;
        _payouts = payouts;
        _payments = payments;
        _db = db;
        _providerSettings = providerSettings;
        _landing = landing;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<PlatformStatsDto>> Stats(CancellationToken ct) => Ok(await _merchants.GetStatsAsync(ct));

    [HttpGet("platforms")]
    public async Task<ActionResult<IEnumerable<MerchantPlatformDto>>> Platforms(
        [FromQuery] string? status,
        [FromQuery] string? q,
        CancellationToken ct)
        => Ok(await _platforms.ListAdminAsync(status, q, ct));

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
                m.Status.ToString(), m.CommissionPercent, m.WebsiteUrl, m.CreatedAtUtc,
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
        => Ok(await _providerSettings.LoadOfficialSandboxDemoAsync(ct));

    [HttpGet("landing")]
    public async Task<ActionResult<LandingContentDto>> GetLanding(CancellationToken ct)
        => Ok(await _landing.GetAsync(ct));

    [HttpPut("landing")]
    public async Task<ActionResult<LandingContentDto>> SaveLanding([FromBody] LandingContentDto request, CancellationToken ct)
        => Ok(await _landing.SaveAsync(request, ct));

    [HttpPost("landing/reset")]
    public async Task<ActionResult<LandingContentDto>> ResetLanding(CancellationToken ct)
        => Ok(await _landing.ResetAsync(ct));

    [HttpPost("providers/{key}/logo")]
    [RequestSizeLimit(2_000_000)]
    public async Task<ActionResult<ProviderRuntimeSettings>> UploadProviderLogo(string key, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp" or ".svg"))
            return BadRequest(new { message = "Allowed: png, jpg, jpeg, webp, svg" });

        var settings = await _providerSettings.GetAsync(ct);
        var bundle = ResolveBundle(settings, key);
        if (bundle == null)
            return BadRequest(new { message = "Unknown provider key" });

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "providers");
        Directory.CreateDirectory(uploadsDir);
        var safeKey = key.Trim().ToLowerInvariant();
        var fileName = $"{safeKey}{ext}";
        var fullPath = Path.Combine(uploadsDir, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream, ct);

        bundle.LogoUrl = $"/uploads/providers/{fileName}?v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        return Ok(await _providerSettings.SaveAsync(settings, ct));
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
[Tags("Merchant Public API")]
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
    public async Task<ActionResult<PaymentDto>> CreatePayment([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        var idem = Request.Headers["X-Idempotency-Key"].FirstOrDefault();
        var platformId = HttpContext.Items.TryGetValue("MerchantPlatformId", out var pid) && pid is Guid g ? g : (Guid?)null;
        try { return Ok(await _payments.CreateAsync(MerchantId, request, idem, ct, platformId)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("payments/{id:guid}")]
    public async Task<ActionResult<PaymentDto>> GetPayment(Guid id, CancellationToken ct)
    {
        var payment = await _payments.GetAsync(MerchantId, id, ct);
        return payment == null ? NotFound() : Ok(payment);
    }

    [HttpPost("payments/{id:guid}/cancel")]
    public async Task<ActionResult<PaymentDto>> CancelPayment(Guid id, CancellationToken ct)
    {
        try { return Ok(await _payments.CancelAsync(MerchantId, id, ct)); }
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
public class WebhooksController : ControllerBase
{
    private readonly IPaymentProviderResolver _resolver;
    private readonly PaymentService _payments;

    public WebhooksController(IPaymentProviderResolver resolver, PaymentService payments)
    {
        _resolver = resolver;
        _payments = payments;
    }

    [HttpPost("{provider}")]
    public async Task<IActionResult> Handle(string provider, CancellationToken ct)
    {
        if (!Enum.TryParse<PaymentProviderType>(provider, true, out var type) || type == PaymentProviderType.Auto)
            return BadRequest();

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(ct);
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        var providerImpl = _resolver.Resolve(type);
        var result = await providerImpl.HandleWebhookAsync(payload, headers, ct);
        if (result == null)
            return Ok(new { received = true });

        if (result.PaymentId.HasValue)
            await _payments.ApplyProviderStatusAsync(result.PaymentId.Value, result.Status, type.ToString(), result.RawPayload, ct: ct);
        else if (!string.IsNullOrWhiteSpace(result.ProviderPaymentId))
            await _payments.ApplyByProviderPaymentIdAsync(result.ProviderPaymentId, type, result.Status, result.RawPayload, ct: ct);

        return Ok(new { received = true });
    }

    [HttpPost("mock/complete/{paymentId:guid}")]
    public async Task<IActionResult> MockComplete(Guid paymentId, CancellationToken ct)
    {
        await _payments.ApplyProviderStatusAsync(paymentId, PaymentStatus.Paid, "Mock", JsonSerializer.Serialize(new { paymentId, status = "Paid" }), ct: ct);
        return Ok(new { completed = true, paymentId });
    }
}
