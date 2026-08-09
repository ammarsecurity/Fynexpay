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
public class HostedCheckoutController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly PaymentService _payments;

    public HostedCheckoutController(IAppDbContext db, PaymentService payments)
    {
        _db = db;
        _payments = payments;
    }

    [HttpGet("/checkout/{paymentId:guid}")]
    public async Task<IActionResult> Page(Guid paymentId, CancellationToken ct)
    {
        var payment = await _db.Payments.AsNoTracking()
            .Include(p => p.Merchant)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

        if (payment == null)
            return Content(Render("دفعة غير موجودة", EmptyState("لم يتم العثور على رابط الدفع."), null), "text/html; charset=utf-8");

        if (payment.Status == PaymentStatus.Paid)
            return Content(Render("تم الدفع", Shell(payment, ReturnState(payment, success: true, autoRedirect: false), false), "Paid"), "text/html; charset=utf-8");

        if (payment.Status is PaymentStatus.Failed or PaymentStatus.Declined or PaymentStatus.Cancelled or PaymentStatus.Expired)
            return Content(Render("فشل الدفع", Shell(payment, ReturnState(payment, success: false, autoRedirect: false), false), payment.Status.ToString()), "text/html; charset=utf-8");

        // سبق اختيار مزود — متابعة الدفع
        if (payment.Provider != PaymentProviderType.Auto && !string.IsNullOrWhiteSpace(payment.ProviderCheckoutUrl))
        {
            var cont = $"""
                <div class="state">
                  <h2>متابعة الدفع عبر {H(DisplayName(payment.Provider))}</h2>
                  <p class="muted">تم تجهيز بوابة الدفع. أكمل العملية للانتهاء.</p>
                  <a class="btn" href="{H(payment.ProviderCheckoutUrl)}">متابعة الدفع</a>
                </div>
                """;
            return Content(Render("متابعة الدفع", Shell(payment, cont, false), "Pending"), "text/html; charset=utf-8");
        }

        IReadOnlyList<PaymentProviderType> providers;
        try
        {
            providers = await _payments.GetAvailableProvidersForPaymentAsync(paymentId, ct);
        }
        catch
        {
            providers = Array.Empty<PaymentProviderType>();
        }

        if (providers.Count == 0)
        {
            var empty = EmptyState("لا توجد طرق دفع متاحة حالياً. يرجى التواصل مع المتجر.");
            return Content(Render("الدفع", Shell(payment, empty, false), "Pending"), "text/html; charset=utf-8");
        }

        var cards = new StringBuilder();
        cards.Append("<div class=\"providers\">");
        foreach (var p in providers)
        {
            cards.Append($"""
                <form method="post" action="/checkout/{paymentId}/pay">
                  <input type="hidden" name="provider" value="{p}" />
                  <button type="submit" class="provider">
                    <span class="p-name">{H(DisplayName(p))}</span>
                    <span class="p-desc">{H(ProviderHint(p))}</span>
                    <span class="p-go">اختيار ←</span>
                  </button>
                </form>
                """);
        }
        cards.Append("</div>");

        var err = Request.Query["error"].FirstOrDefault();
        var errHtml = string.IsNullOrWhiteSpace(err) ? "" : $"<div class=\"error\">{H(err)}</div>";

        var body = $"""
            <div class="choose">
              <h2>اختر طريقة الدفع</h2>
              <p class="muted">ادفع بأمان عبر أحد المزودين المعتمدين</p>
              {errHtml}
              {cards}
            </div>
            """;

        return Content(Render("إتمام الدفع", Shell(payment, body, true), "Pending"), "text/html; charset=utf-8");
    }

    /// <summary>
    /// عودة المزود للمنصة أولاً، ثم التحويل لرابط التاجر (successUrl / failureUrl).
    /// </summary>
    [HttpGet("/checkout/{paymentId:guid}/return")]
    public async Task<IActionResult> ProviderReturn(Guid paymentId, [FromQuery] string? result, CancellationToken ct)
    {
        var payment = await _db.Payments
            .Include(p => p.Merchant)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

        if (payment == null)
            return Content(Render("دفعة غير موجودة", EmptyState("لم يتم العثور على رابط الدفع."), null), "text/html; charset=utf-8");

        var wantsSuccess = string.Equals(result, "success", StringComparison.OrdinalIgnoreCase);

        // حاول مزامنة الحالة من المزود إن كانت لا تزال معلّقة
        if (payment.Status == PaymentStatus.Pending &&
            payment.Provider != PaymentProviderType.Auto &&
            !string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
        {
            try
            {
                await _payments.SyncFromProviderAsync(payment.Id, ct);
                payment = await _db.Payments.Include(p => p.Merchant)
                    .FirstAsync(p => p.Id == paymentId, ct);
            }
            catch
            {
                // نعرض صفحة العودة حتى لو فشلت المزامنة
            }
        }

        var success = payment.Status == PaymentStatus.Paid ||
                      (wantsSuccess && payment.Status is PaymentStatus.Pending);
        var html = ReturnState(payment, success: success, autoRedirect: true);
        var title = success ? "تم الدفع" : "نتيجة الدفع";
        return Content(Render(title, Shell(payment, html, false), success ? "Paid" : "Failed"), "text/html; charset=utf-8");
    }

    [HttpPost("/checkout/{paymentId:guid}/pay")]
    public async Task<IActionResult> Pay(Guid paymentId, [FromForm] string provider, CancellationToken ct)
    {
        try
        {
            var result = await _payments.InitiateAsync(paymentId, provider, ct);
            if (!string.IsNullOrWhiteSpace(result.ProviderCheckoutUrl))
                return Redirect(result.ProviderCheckoutUrl);

            return Redirect($"/checkout/{paymentId}");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Redirect($"/checkout/{paymentId}?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    private static string Shell(Domain.Entities.Payment payment, string inner, bool showProviders)
    {
        var amount = payment.Amount.ToString("N0", CultureInfo.GetCultureInfo("ar-IQ"));
        var merchant = payment.Merchant?.BusinessNameAr;
        if (string.IsNullOrWhiteSpace(merchant))
            merchant = payment.Merchant?.BusinessName ?? "متجر";

        return $"""
            <div class="wrap">
              <header class="top">
                <div class="brand">Fynex<span>pay</span></div>
                <div class="secure">دفع آمن</div>
              </header>
              <section class="summary">
                <p class="merchant">{H(merchant)}</p>
                <h1 class="amount">{H(amount)} <small>د.ع</small></h1>
                <p class="service">{H(payment.Description ?? "خدمة")}</p>
                <div class="meta">
                  <div><span>رقم العملية</span><strong class="ltr">{payment.Id.ToString("N")[..8].ToUpperInvariant()}</strong></div>
                  <div><span>رقم الطلب</span><strong class="ltr">{H(payment.MerchantOrderId)}</strong></div>
                </div>
              </section>
              {inner}
              <footer class="foot">مدعوم بواسطة Fynexpay · بوابة دفع عراقية</footer>
            </div>
            """;
    }

    private static string EmptyState(string text) => $"<div class=\"state\"><p>{H(text)}</p></div>";

    private static string ReturnState(Domain.Entities.Payment payment, bool success, bool autoRedirect)
    {
        var merchantUrl = ResolveMerchantReturnUrl(payment, success);
        var hasMerchant = !string.IsNullOrWhiteSpace(merchantUrl);
        const int delaySec = 10;
        var delayMs = delaySec * 1000;

        var redirectBlock = "";
        if (autoRedirect && hasMerchant)
        {
            var urlJson = System.Text.Json.JsonSerializer.Serialize(merchantUrl);
            var btnClass = success ? "btn" : "btn ghost";
            redirectBlock =
                "<p class=\"muted redirect-note\">جاري إعادتك إلى المتجر خلال <strong id=\"cd\">" + delaySec +
                "</strong> ثانية…</p>" +
                "<a class=\"" + btnClass + "\" href=\"" + H(merchantUrl) + "\">العودة للمتجر الآن</a>" +
                "<script>(function(){var s=" + delaySec + ";var el=document.getElementById('cd');" +
                "var t=setInterval(function(){s-=1;if(el)el.textContent=String(s);" +
                "if(s<=0){clearInterval(t);location.href=" + urlJson + ";}},1000);" +
                "setTimeout(function(){location.href=" + urlJson + ";}," + delayMs + ");})();</script>";
        }

        if (success)
        {
            return $"""
                <div class="state ok">
                  <div class="icon">✓</div>
                  <h1>تم الدفع بنجاح</h1>
                  <p>تمت العملية عبر Fynexpay{(payment.Provider != PaymentProviderType.Auto ? $" · {H(DisplayName(payment.Provider))}" : "")}</p>
                  {redirectBlock}
                </div>
                """;
        }

        return $"""
            <div class="state bad">
              <div class="icon">!</div>
              <h1>لم تكتمل العملية</h1>
              <p>{H(payment.FailureReason ?? "تعذّر إتمام الدفع")}</p>
              {redirectBlock}
            </div>
            """;
    }

    private static string? ResolveMerchantReturnUrl(Domain.Entities.Payment payment, bool success)
    {
        if (success)
            return string.IsNullOrWhiteSpace(payment.SuccessUrl) ? null : payment.SuccessUrl;

        if (!string.IsNullOrWhiteSpace(payment.FailureUrl))
            return payment.FailureUrl;

        // إن لم يُرسل failureUrl نرجع لـ successUrl مع علامة فشل حتى لا يبقى الزبون عالقاً
        if (string.IsNullOrWhiteSpace(payment.SuccessUrl))
            return null;

        var sep = payment.SuccessUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{payment.SuccessUrl}{sep}fynexpay_status=failed";
    }

    private static string DisplayName(PaymentProviderType p) => p switch
    {
        PaymentProviderType.Fib => "FIB",
        PaymentProviderType.ZainCash => "ZainCash",
        PaymentProviderType.Qi => "QI Card",
        PaymentProviderType.SuperQi => "SuperQi",
        _ => p.ToString()
    };

    private static string ProviderHint(PaymentProviderType p) => p switch
    {
        PaymentProviderType.Fib => "ادفع عبر تطبيق First Iraqi Bank",
        PaymentProviderType.ZainCash => "محفظة زين كاش",
        PaymentProviderType.Qi => "بطاقات QI والبطاقات البنكية",
        PaymentProviderType.SuperQi => "ادفع عبر تطبيق SuperQi (ALIPAY)",
        _ => "مزود دفع"
    };

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? "");

    private static string Render(string title, string body, string? status)
    {
        return $$"""
            <!DOCTYPE html>
            <html lang="ar" dir="rtl">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>{{WebUtility.HtmlEncode(title)}} · Fynexpay</title>
              <link href="https://fonts.googleapis.com/css2?family=IBM+Plex+Sans+Arabic:wght@400;500;600;700&display=swap" rel="stylesheet" />
              <style>
                :root {
                  --ink:#10241f; --muted:#5d6f69; --line:#d9e4df;
                  --brand:#0c6b5c; --brand-2:#14967f; --sand:#f3f7f5;
                  --ok:#0f7a45; --bad:#b42318;
                }
                *{box-sizing:border-box}
                body{
                  margin:0;min-height:100vh;font-family:"IBM Plex Sans Arabic",Tahoma,sans-serif;
                  color:var(--ink);
                  background:
                    radial-gradient(1200px 500px at 100% -10%, rgba(20,150,127,.18), transparent 55%),
                    radial-gradient(900px 420px at -10% 110%, rgba(12,107,92,.14), transparent 50%),
                    linear-gradient(165deg,#e8f2ef 0%, #f7faf9 45%, #eef4f1 100%);
                }
                .wrap{width:min(520px,94vw);margin:32px auto 40px}
                .top{display:flex;justify-content:space-between;align-items:center;margin-bottom:14px}
                .brand{font-size:1.35rem;font-weight:700;letter-spacing:-.02em}
                .brand span{color:var(--brand)}
                .secure{font-size:.85rem;color:var(--brand);background:rgba(12,107,92,.1);padding:6px 12px;border-radius:999px;font-weight:600}
                .summary{
                  background:linear-gradient(145deg,#0c6b5c,#0a5549 60%,#083f36);
                  color:#f4fffb;border-radius:28px;padding:28px 24px 22px;
                  box-shadow:0 18px 40px rgba(8,63,54,.28);
                  position:relative;overflow:hidden;
                }
                .summary:before{
                  content:"";position:absolute;inset:auto -20% -40% auto;width:220px;height:220px;
                  background:radial-gradient(circle,rgba(255,255,255,.16),transparent 65%);
                }
                .merchant{margin:0;opacity:.85;font-weight:500}
                .amount{margin:10px 0 4px;font-size:2.4rem;font-weight:700;letter-spacing:-.03em}
                .amount small{font-size:1rem;font-weight:600;opacity:.85}
                .service{margin:0 0 18px;opacity:.92}
                .meta{display:grid;grid-template-columns:1fr 1fr;gap:10px;position:relative}
                .meta > div{background:rgba(255,255,255,.1);border-radius:14px;padding:10px 12px}
                .meta span{display:block;font-size:.78rem;opacity:.75;margin-bottom:4px}
                .ltr{direction:ltr;unicode-bidi:plaintext}
                .choose,.state{
                  margin-top:16px;background:#fff;border:1px solid var(--line);
                  border-radius:24px;padding:22px;box-shadow:0 10px 30px rgba(16,36,31,.06);
                }
                .choose h2,.state h1,.state h2{margin:0 0 6px}
                .muted{color:var(--muted);margin:0 0 16px}
                .providers{display:grid;gap:10px}
                .provider{
                  width:100%;text-align:right;border:1px solid var(--line);background:var(--sand);
                  border-radius:18px;padding:16px 16px 14px;cursor:pointer;font:inherit;
                  display:grid;gap:4px;transition:.18s ease;position:relative;
                }
                .provider:hover{border-color:rgba(12,107,92,.45);background:#fff;transform:translateY(-1px);box-shadow:0 8px 18px rgba(12,107,92,.08)}
                .p-name{font-weight:700;font-size:1.05rem}
                .p-desc{color:var(--muted);font-size:.92rem}
                .p-go{color:var(--brand);font-weight:700;margin-top:6px}
                .btn{
                  display:inline-flex;align-items:center;justify-content:center;margin-top:14px;
                  background:var(--brand);color:#fff;text-decoration:none;border-radius:14px;
                  padding:12px 18px;font-weight:700;
                }
                .btn.ghost{background:transparent;color:var(--bad);border:1px solid #efc4c0}
                .state{text-align:center}
                .state .icon{
                  width:56px;height:56px;border-radius:50%;display:grid;place-items:center;margin:0 auto 12px;
                  font-size:1.4rem;font-weight:700;
                }
                .state.ok .icon{background:rgba(15,122,69,.12);color:var(--ok)}
                .state.bad .icon{background:rgba(180,35,24,.1);color:var(--bad)}
                .error{background:#fff1f0;color:var(--bad);border:1px solid #f0c9c5;border-radius:12px;padding:10px 12px;margin-bottom:12px;font-weight:600}
                .redirect-note{margin-top:8px!important}
                .foot{margin-top:18px;text-align:center;color:var(--muted);font-size:.85rem}
                form{margin:0}
              </style>
            </head>
            <body>{{body}}</body>
            </html>
            """;
    }
}
