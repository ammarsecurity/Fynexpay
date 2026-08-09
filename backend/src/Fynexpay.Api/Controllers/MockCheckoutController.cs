using System.Globalization;
using System.Net;
using System.Text;
using Fynexpay.Api.Security;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Services;
using Fynexpay.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fynexpay.Api.Controllers;

[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public class MockCheckoutController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly PaymentService _payments;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;

    public MockCheckoutController(
        IAppDbContext db,
        PaymentService payments,
        IHostEnvironment env,
        IConfiguration config)
    {
        _db = db;
        _payments = payments;
        _env = env;
        _config = config;
    }

    private bool Allowed => MockPaymentAccess.IsAllowed(_env, _config);

    [HttpGet("/mock-checkout/{paymentId:guid}")]
    public async Task<IActionResult> Page(Guid paymentId, CancellationToken ct)
    {
        if (!Allowed)
            return NotFound();

        var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment == null)
            return Content(Html("دفعة غير موجودة", "<p>لم يتم العثور على هذه الدفعة.</p>", null), "text/html; charset=utf-8");

        var amount = payment.Amount.ToString("N0", CultureInfo.GetCultureInfo("ar-IQ"));
        var body = $"""
            <div class="card">
              <div class="brand">Fynex<span>pay</span> Mock</div>
              <p class="muted">صفحة دفع تجريبية — ليست بوابة حقيقية</p>
              <div class="row"><span>المبلغ</span><strong>{WebUtility.HtmlEncode(amount)} د.ع</strong></div>
              <div class="row"><span>الطلب</span><strong>{WebUtility.HtmlEncode(payment.MerchantOrderId)}</strong></div>
              <div class="row"><span>المزود</span><strong>{payment.Provider}</strong></div>
              <div class="row"><span>الحالة</span><strong class="status">{payment.Status}</strong></div>
            """;

        if (payment.Status == PaymentStatus.Pending)
        {
            body += $"""
              <form method="post" action="/mock-checkout/{paymentId}/pay">
                <button class="pay" type="submit">إتمام الدفع بنجاح</button>
              </form>
              <form method="post" action="/mock-checkout/{paymentId}/fail">
                <button class="fail" type="submit">فشل الدفع</button>
              </form>
            """;
        }
        else if (payment.Status == PaymentStatus.Paid)
        {
            body += "<p class=\"ok\">تم الدفع بنجاح. يمكنك إغلاق هذه الصفحة والعودة للوحة التاجر.</p>";
            if (!string.IsNullOrWhiteSpace(payment.SuccessUrl))
                body += $"<p><a href=\"{WebUtility.HtmlEncode(payment.SuccessUrl)}\">الانتقال لصفحة النجاح</a></p>";
        }
        else
        {
            body += $"<p class=\"bad\">انتهت العملية بحالة: {payment.Status}</p>";
            if (!string.IsNullOrWhiteSpace(payment.FailureUrl))
                body += $"<p><a href=\"{WebUtility.HtmlEncode(payment.FailureUrl)}\">الانتقال لصفحة الفشل</a></p>";
        }

        body += "</div>";
        return Content(Html("دفع تجريبي", body, payment.Status.ToString()), "text/html; charset=utf-8");
    }

    [HttpPost("/mock-checkout/{paymentId:guid}/pay")]
    public async Task<IActionResult> Pay(Guid paymentId, CancellationToken ct)
    {
        if (!Allowed) return NotFound();

        var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment == null) return NotFound();

        if (payment.Status == PaymentStatus.Pending)
        {
            await _payments.ApplyProviderStatusAsync(
                paymentId,
                PaymentStatus.Paid,
                "MockCheckout",
                """{"status":"Paid","source":"mock-checkout-page"}""",
                ct: ct);
        }

        return Redirect($"/mock-checkout/{paymentId}");
    }

    [HttpPost("/mock-checkout/{paymentId:guid}/fail")]
    public async Task<IActionResult> Fail(Guid paymentId, CancellationToken ct)
    {
        if (!Allowed) return NotFound();

        var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment == null) return NotFound();

        if (payment.Status == PaymentStatus.Pending)
        {
            await _payments.ApplyProviderStatusAsync(
                paymentId,
                PaymentStatus.Failed,
                "MockCheckout",
                """{"status":"Failed","source":"mock-checkout-page"}""",
                "ألغى الزبون الدفع في صفحة الـ Mock",
                ct);
        }

        return Redirect($"/mock-checkout/{paymentId}");
    }

    private static string Html(string title, string body, string? status)
    {
        var statusColor = status == "Paid" ? "#0f7a45" : status == "Pending" ? "#c45c26" : "#b42318";
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"ar\" dir=\"rtl\"><head><meta charset=\"utf-8\" />");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.Append("<title>").Append(WebUtility.HtmlEncode(title)).Append("</title>");
        sb.Append("<link href=\"https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@700;800&family=Tajawal:wght@400;700;800&display=swap\" rel=\"stylesheet\" />");
        sb.Append("<style>");
        sb.Append("body{margin:0;min-height:100vh;display:grid;place-items:center;font-family:\"Tajawal\",\"Plus Jakarta Sans\",sans-serif;background:radial-gradient(circle at 80% 10%,rgba(68,87,255,.12),transparent 35%),#f7f8fc;color:#0b0a33}");
        sb.Append(".card{width:min(420px,92vw);background:#fff;color:#0b0a33;border-radius:24px;padding:28px;box-shadow:0 18px 50px rgba(11,10,51,.08);border:1px solid #e6e8f2}");
        sb.Append(".brand{font-family:\"Plus Jakarta Sans\",sans-serif;font-size:1.6rem;font-weight:800}.brand span{color:#4457ff}.muted{color:#7a7d9c;margin:6px 0 18px}");
        sb.Append(".row{display:flex;justify-content:space-between;gap:12px;padding:10px 0;border-bottom:1px solid #e6e8f2}");
        sb.Append("form{margin-top:12px}button{width:100%;border:0;border-radius:999px;padding:14px;font:inherit;font-weight:700;cursor:pointer}");
        sb.Append(".pay{background:#4457ff;color:#fff;box-shadow:0 12px 28px rgba(68,87,255,.28)}.fail{background:transparent;color:#f04438;border:1px solid #efc4c0!important}");
        sb.Append(".ok{color:#12b76a;font-weight:700}.bad{color:#f04438;font-weight:700}a{color:#4457ff}");
        sb.Append(".status{color:").Append(statusColor).Append('}');
        sb.Append("</style></head><body>").Append(body).Append("</body></html>");
        return sb.ToString();
    }
}
