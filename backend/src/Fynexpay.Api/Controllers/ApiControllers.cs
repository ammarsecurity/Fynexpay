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
    private readonly WalletService _wallets;
    private readonly PayoutService _payouts;
    private readonly PaymentService _payments;
    private readonly IAppDbContext _db;

    public MerchantDashboardController(
        MerchantAdminService merchants,
        WalletService wallets,
        PayoutService payouts,
        PaymentService payments,
        IAppDbContext db)
    {
        _merchants = merchants;
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
    public async Task<ActionResult<IEnumerable<PaymentDto>>> Payments([FromQuery] string? status, CancellationToken ct)
    {
        var q = _db.Payments.Where(p => p.MerchantId == MerchantId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var st))
            q = q.Where(p => p.Status == st);
        var list = await q.OrderByDescending(p => p.CreatedAtUtc).Take(200).ToListAsync(ct);
        return Ok(list.Select(p => PaymentService.Map(p)));
    }

    [HttpPost("test-payments")]
    public async Task<ActionResult<PaymentDto>> CreateTestPayment([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        try
        {
            var idem = $"dashboard-test-{Guid.NewGuid():N}";
            return Ok(await _payments.CreateAsync(MerchantId, request, idem, ct));
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

    [HttpGet("api-keys")]
    public async Task<ActionResult<IEnumerable<ApiKeyDto>>> ApiKeys(CancellationToken ct)
        => Ok(await _merchants.ListApiKeysAsync(MerchantId, ct));

    [HttpPost("api-keys")]
    public async Task<ActionResult<CreateApiKeyResponse>> CreateApiKey([FromBody] CreateApiKeyBody body, CancellationToken ct)
        => Ok(await _merchants.CreateApiKeyAsync(MerchantId, body.Name ?? "Key", ct));

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
    public async Task<ActionResult<IEnumerable<PayoutDto>>> Payouts(CancellationToken ct)
        => Ok(await _payouts.ListForMerchantAsync(MerchantId, ct));

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
    private readonly PayoutService _payouts;
    private readonly IAppDbContext _db;
    private readonly IProviderSettingsService _providerSettings;

    public AdminController(
        MerchantAdminService merchants,
        PayoutService payouts,
        IAppDbContext db,
        IProviderSettingsService providerSettings)
    {
        _merchants = merchants;
        _payouts = payouts;
        _db = db;
        _providerSettings = providerSettings;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<PlatformStatsDto>> Stats(CancellationToken ct) => Ok(await _merchants.GetStatsAsync(ct));

    [HttpGet("merchants")]
    public async Task<ActionResult<IEnumerable<MerchantDto>>> Merchants(CancellationToken ct)
        => Ok(await _merchants.ListAsync(ct));

    [HttpPatch("merchants/{id:guid}")]
    public async Task<ActionResult<MerchantDto>> UpdateMerchant(Guid id, [FromBody] UpdateMerchantAdminRequest request, CancellationToken ct)
    {
        try { return Ok(await _merchants.UpdateAsync(id, request, ct)); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("payments")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> Payments(CancellationToken ct)
    {
        var list = await _db.Payments.OrderByDescending(p => p.CreatedAtUtc).Take(500).ToListAsync(ct);
        return Ok(list.Select(p => PaymentService.Map(p)));
    }

    [HttpGet("payouts")]
    public async Task<ActionResult<IEnumerable<PayoutDto>>> Payouts(CancellationToken ct)
    {
        var list = await _db.PayoutRequests.OrderByDescending(p => p.CreatedAtUtc).Take(200).ToListAsync(ct);
        return Ok(list.Select(p => new PayoutDto(p.Id, p.Amount, p.Currency, p.Status.ToString(), p.DestinationType, p.DestinationDetails, p.AdminNote, p.CreatedAtUtc, p.ReviewedAtUtc, p.CompletedAtUtc)));
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
        try { return Ok(await _payments.CreateAsync(MerchantId, request, idem, ct)); }
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
