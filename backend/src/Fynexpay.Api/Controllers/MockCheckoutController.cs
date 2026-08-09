using System.Globalization;
using System.Net;
using System.Text;
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

    public MockCheckoutController(IAppDbContext db, PaymentService payments)
    {
        _db = db;
        _payments = payments;
    }

    [HttpGet("/mock-checkout/{paymentId:guid}")]
    public async Task<IActionResult> Page(Guid paymentId, CancellationToken ct)
    {
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
        sb.Append("<link href=\"https://fonts.googleapis.com/css2?family=IBM+Plex+Sans+Arabic:wght@400;600;700&display=swap\" rel=\"stylesheet\" />");
        sb.Append("<style>");
        sb.Append("body{margin:0;min-height:100vh;display:grid;place-items:center;font-family:\"IBM Plex Sans Arabic\",Tahoma,sans-serif;background:linear-gradient(160deg,#071a17,#0d2f28 55%,#12352d);color:#f7f3ea}");
        sb.Append(".card{width:min(420px,92vw);background:rgba(255,253,248,.96);color:#1a2421;border-radius:24px;padding:28px;box-shadow:0 20px 50px rgba(0,0,0,.25)}");
        sb.Append(".brand{font-size:1.6rem;font-weight:700}.brand span{color:#0f6b5c}.muted{color:#5c6b66;margin:6px 0 18px}");
        sb.Append(".row{display:flex;justify-content:space-between;gap:12px;padding:10px 0;border-bottom:1px solid #e8e0d2}");
        sb.Append("form{margin-top:12px}button{width:100%;border:0;border-radius:14px;padding:14px;font:inherit;font-weight:700;cursor:pointer}");
        sb.Append(".pay{background:#0f6b5c;color:#fff}.fail{background:transparent;color:#b42318;border:1px solid #e8c4c0!important}");
        sb.Append(".ok{color:#0f7a45;font-weight:700}.bad{color:#b42318;font-weight:700}a{color:#0f6b5c}");
        sb.Append(".status{color:").Append(statusColor).Append('}');
        sb.Append("</style></head><body>").Append(body).Append("</body></html>");
        return sb.ToString();
    }
}
