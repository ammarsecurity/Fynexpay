using System.Globalization;
using System.Net;
using System.Text;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Abstractions.Messaging;
using Fynexpay.Application.Abstractions.Payments;
using Fynexpay.Application.Security;
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
    private readonly IProviderSettingsService _providerSettings;
    private readonly OtpService _otp;
    private readonly IUltramsgSettingsService _ultramsgSettings;

    public HostedCheckoutController(
        IAppDbContext db,
        PaymentService payments,
        IProviderSettingsService providerSettings,
        OtpService otp,
        IUltramsgSettingsService ultramsgSettings)
    {
        _db = db;
        _payments = payments;
        _providerSettings = providerSettings;
        _otp = otp;
        _ultramsgSettings = ultramsgSettings;
    }

    [HttpGet("/checkout/{paymentId:guid}")]
    public async Task<IActionResult> Page(Guid paymentId, CancellationToken ct)
    {
        var lang = ResolveLang();

        if (await _payments.TryPurgeExpiredIncompleteCheckoutAsync(paymentId, ct))
            return Content(Render(T(lang, "انتهت الجلسة", "Session expired"), EmptyState(
                T(lang, "انتهت صلاحية رابط الدفع (ساعة واحدة) وتم إغلاق الجلسة ومسحها لأن العملية لم تُكتمل.",
                  "This payment link expired after one hour and the session was closed because checkout was not completed."),
                lang), null, lang), "text/html; charset=utf-8");

        var payment = await _db.Payments.AsNoTracking()
            .Include(p => p.Merchant)
            .Include(p => p.MerchantPlatform)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

        if (payment == null)
            return Content(Render(T(lang, "دفعة غير موجودة", "Payment not found"), EmptyState(T(lang, "لم يتم العثور على رابط الدفع.", "Payment link was not found."), lang), null, lang), "text/html; charset=utf-8");

        if (payment.Status == PaymentStatus.Paid)
            return Content(Render(T(lang, "تم الدفع", "Paid"), Shell(payment, ReturnState(payment, success: true, autoRedirect: false, lang, await _providerSettings.GetAsync(ct)), false, lang), "Paid", lang), "text/html; charset=utf-8");

        if (payment.Status is PaymentStatus.Failed or PaymentStatus.Declined or PaymentStatus.Cancelled or PaymentStatus.Expired)
            return Content(Render(T(lang, "فشل الدفع", "Payment failed"), Shell(payment, ReturnState(payment, success: false, autoRedirect: false, lang, await _providerSettings.GetAsync(ct)), false, lang), payment.Status.ToString(), lang), "text/html; charset=utf-8");

        if (payment.Provider != PaymentProviderType.Auto && !string.IsNullOrWhiteSpace(payment.ProviderCheckoutUrl))
        {
            var cont = $"""
                <div class="state">
                  <div class="icon soft">→</div>
                  <h2>{T(lang, "متابعة الدفع", "Continue payment")}</h2>
                  <p class="muted">{T(lang, "تم تجهيز بوابة الدفع. أكمل العملية للانتهاء.", "Your payment gateway is ready. Continue to finish.")}</p>
                  <a class="btn" href="{H(payment.ProviderCheckoutUrl)}">{T(lang, "متابعة الدفع", "Continue payment")}</a>
                </div>
                """;
            return Content(Render(T(lang, "متابعة الدفع", "Continue payment"), Shell(payment, cont, false, lang), "Pending", lang), "text/html; charset=utf-8");
        }

        var wa = await _ultramsgSettings.GetAsync(ct);
        var needsVerify = wa.Enabled && wa.RequireCheckoutOtp && (wa.UsesWhatsApp() || wa.UsesEmail())
                          && payment.CustomerPhoneVerifiedAtUtc == null;
        if (needsVerify)
        {
            var phoneError = Request.Query["error"].FirstOrDefault();
            var phoneUi = VerifyUi(payment, lang, phoneError, wa);
            return Content(Render(T(lang, "تأكيد الهوية", "Verify identity"), Shell(payment, phoneUi, false, lang), "Verify", lang), "text/html; charset=utf-8");
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
            var empty = EmptyState(T(lang, "لا توجد طرق دفع متاحة حالياً. يرجى التواصل مع المتجر.", "No payment methods are available. Please contact the store."), lang);
            return Content(Render(T(lang, "الدفع", "Checkout"), Shell(payment, empty, false, lang), "Pending", lang), "text/html; charset=utf-8");
        }

        var settings = await _providerSettings.GetAsync(ct);
        var cards = new StringBuilder();
        cards.Append("<div class=\"providers\">");
        foreach (var p in providers)
        {
            var logo = LogoUrl(settings, p);
            var pname = DisplayName(settings, p);
            var logoHtml = string.IsNullOrWhiteSpace(logo)
                ? $"<span class=\"p-fallback\">{H((pname.Length > 0 ? pname : "P")[..1])}</span>"
                : $"<img class=\"p-logo\" src=\"{H(logo)}\" alt=\"\" />";
            var choose = T(lang, "اختيار", "Select");
            cards.Append($"""
                <form method="post" action="/checkout/{paymentId}/pay?lang={lang}">
                  <input type="hidden" name="provider" value="{p}" />
                  <button type="submit" class="provider">
                    <span class="p-logo-wrap">{logoHtml}</span>
                    <span class="p-text">
                      <span class="p-desc">{H(ProviderHint(lang))}</span>
                    </span>
                    <span class="p-go"><span>{choose}</span><i aria-hidden="true">←</i></span>
                  </button>
                </form>
                """);
        }
        cards.Append("</div>");

        var err = Request.Query["error"].FirstOrDefault();
        var errHtml = string.IsNullOrWhiteSpace(err) ? "" : $"<div class=\"error\">{H(err)}</div>";

        var body = $"""
            <div class="choose">
              <div class="choose-head">
                <h2>{T(lang, "اختر طريقة الدفع", "Choose payment method")}</h2>
                <p class="muted">
                  <svg class="shield" width="15" height="15" viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M12 2l8 3v6c0 5-3.4 9.4-8 11-4.6-1.6-8-6-8-11V5l8-3z" stroke="#031838" stroke-width="1.8"/><path d="M9.5 12.2l1.8 1.8 3.8-3.8" stroke="#031838" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>
                  {T(lang, "ادفع بأمان عبر أحد المزودين المعتمدين", "Pay securely through a trusted provider")}
                </p>
              </div>
              {errHtml}
              {cards}
            </div>
            """;

        return Content(Render(T(lang, "إتمام الدفع", "Checkout"), Shell(payment, body, true, lang), "Pending", lang), "text/html; charset=utf-8");
    }

    [HttpGet("/checkout/{paymentId:guid}/return")]
    public async Task<IActionResult> ProviderReturn(Guid paymentId, [FromQuery] string? result, CancellationToken ct)
    {
        var lang = ResolveLang();

        if (await _payments.TryPurgeExpiredIncompleteCheckoutAsync(paymentId, ct))
            return Content(Render(T(lang, "انتهت الجلسة", "Session expired"), EmptyState(
                T(lang, "انتهت صلاحية رابط الدفع وتم إغلاق الجلسة.", "The payment link expired and the session was closed."),
                lang), null, lang), "text/html; charset=utf-8");

        var payment = await _db.Payments
            .Include(p => p.Merchant)
            .Include(p => p.MerchantPlatform)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

        if (payment == null)
            return Content(Render(T(lang, "دفعة غير موجودة", "Payment not found"), EmptyState(T(lang, "لم يتم العثور على رابط الدفع.", "Payment link was not found."), lang), null, lang), "text/html; charset=utf-8");

        if (payment.Status == PaymentStatus.Pending &&
            payment.Provider != PaymentProviderType.Auto &&
            !string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
        {
            try
            {
                await _payments.SyncFromProviderAsync(payment.Id, ct);
                payment = await _db.Payments.Include(p => p.Merchant).Include(p => p.MerchantPlatform)
                    .FirstAsync(p => p.Id == paymentId, ct);
            }
            catch
            {
                // keep current payment row
            }
        }

        // Never trust ?result=success — only server-side Paid status counts.
        var success = payment.Status == PaymentStatus.Paid;
        var html = ReturnState(payment, success: success, autoRedirect: true, lang, await _providerSettings.GetAsync(ct));
        var title = success ? T(lang, "تم الدفع", "Paid") : T(lang, "نتيجة الدفع", "Payment result");
        return Content(Render(title, Shell(payment, html, false, lang), success ? "Paid" : payment.Status.ToString(), lang), "text/html; charset=utf-8");
    }

    [HttpPost("/checkout/{paymentId:guid}/otp/send")]
    public async Task<IActionResult> SendOtp(Guid paymentId, [FromForm] string? phone, [FromForm] string? email, CancellationToken ct)
    {
        var lang = ResolveLang();
        try
        {
            if (await _payments.TryPurgeExpiredIncompleteCheckoutAsync(paymentId, ct))
                return Redirect($"/checkout/{paymentId}?lang={lang}");

            var result = await _otp.SendCheckoutOtpAsync(paymentId, phone, email, ct);
            var q = $"lang={lang}&step=code&cid={result.ChallengeId}&mask={Uri.EscapeDataString(result.MaskedDestination)}&via={Uri.EscapeDataString(result.Via)}";
            if (!string.IsNullOrWhiteSpace(result.DevCode))
                q += $"&dev={Uri.EscapeDataString(result.DevCode)}";
            return Redirect($"/checkout/{paymentId}?{q}");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Redirect($"/checkout/{paymentId}?lang={lang}&error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    [HttpPost("/checkout/{paymentId:guid}/otp/verify")]
    public async Task<IActionResult> VerifyOtp(Guid paymentId, [FromForm] Guid challengeId, [FromForm] string code, CancellationToken ct)
    {
        var lang = ResolveLang();
        try
        {
            if (await _payments.TryPurgeExpiredIncompleteCheckoutAsync(paymentId, ct))
                return Redirect($"/checkout/{paymentId}?lang={lang}");

            await _otp.VerifyCheckoutOtpAsync(paymentId, challengeId, code, ct);
            return Redirect($"/checkout/{paymentId}?lang={lang}");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Redirect($"/checkout/{paymentId}?lang={lang}&step=code&cid={challengeId}&error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    [HttpPost("/checkout/{paymentId:guid}/pay")]
    public async Task<IActionResult> Pay(Guid paymentId, [FromForm] string provider, CancellationToken ct)
    {
        var lang = ResolveLang();
        try
        {
            if (await _payments.TryPurgeExpiredIncompleteCheckoutAsync(paymentId, ct))
                return Redirect($"/checkout/{paymentId}?lang={lang}");

            var wa = await _ultramsgSettings.GetAsync(ct);
            if (wa.Enabled && wa.RequireCheckoutOtp && (wa.UsesWhatsApp() || wa.UsesEmail())
                && !await _otp.IsCheckoutVerifiedAsync(paymentId, ct))
                return Redirect($"/checkout/{paymentId}?lang={lang}&error={Uri.EscapeDataString(T(lang, "يجب إكمال التحقق أولاً", "Verification is required first"))}");

            var result = await _payments.InitiateAsync(paymentId, provider, ct);
            if (!string.IsNullOrWhiteSpace(result.ProviderCheckoutUrl))
                return Redirect(result.ProviderCheckoutUrl);

            return Redirect($"/checkout/{paymentId}?lang={lang}");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Redirect($"/checkout/{paymentId}?lang={lang}&error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    private string VerifyUi(Domain.Entities.Payment payment, string lang, string? error, UltramsgSettings settings)
    {
        var step = Request.Query["step"].FirstOrDefault();
        var cid = Request.Query["cid"].FirstOrDefault();
        var mask = Request.Query["mask"].FirstOrDefault();
        var via = Request.Query["via"].FirstOrDefault();
        var dev = Request.Query["dev"].FirstOrDefault();
        var errHtml = string.IsNullOrWhiteSpace(error) ? "" : $"<div class=\"error\">{H(error)}</div>";
        var prefPhone = payment.CustomerPhone ?? "";
        var prefEmail = payment.CustomerEmail ?? "";
        var useWa = settings.UsesWhatsApp();
        var useEmail = settings.UsesEmail();

        if (string.Equals(step, "code", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(cid, out _))
        {
            var viaLabel = string.IsNullOrWhiteSpace(via) ? "" : $" ({H(via)})";
            var devHint = string.IsNullOrWhiteSpace(dev)
                ? ""
                : $"<p class=\"dev-hint\">DEV code: <strong class=\"ltr\">{H(dev)}</strong></p>";
            var codeIcon = useWa && !useEmail
                ? """<i class="bi bi-whatsapp" aria-hidden="true"></i>"""
                : useEmail && !useWa
                    ? """<i class="bi bi-envelope-fill" aria-hidden="true"></i>"""
                    : """<i class="bi bi-shield-lock-fill" aria-hidden="true"></i>""";
            var codeIconTone = useWa && !useEmail ? "wa" : useEmail && !useWa ? "mail" : "secure";
            return $"""
                <div class="verify-box">
                  <div class="verify-icon {codeIconTone}" aria-hidden="true">{codeIcon}</div>
                  <h2>{T(lang, "أدخل رمز التحقق", "Enter verification code")}</h2>
                  <p class="muted">{T(lang, "أرسلنا رمزاً إلى", "We sent a code to")} <strong class="ltr">{H(mask)}</strong>{viaLabel}</p>
                  {errHtml}
                  {devHint}
                  <form method="post" action="/checkout/{payment.Id}/otp/verify?lang={lang}" class="verify-form">
                    <input type="hidden" name="challengeId" value="{H(cid)}" />
                    <label class="field-label">{T(lang, "رمز التحقق", "Verification code")}</label>
                    <input class="otp-input ltr" name="code" inputmode="numeric" autocomplete="one-time-code" maxlength="6" placeholder="••••••" required />
                    <button class="btn btn-send" type="submit">
                      <i class="bi bi-check2-circle" aria-hidden="true"></i>
                      <span>{T(lang, "تأكيد والمتابعة", "Confirm & continue")}</span>
                    </button>
                  </form>
                  <form method="post" action="/checkout/{payment.Id}/otp/send?lang={lang}" class="resend-form">
                    <input type="hidden" name="phone" value="{H(prefPhone)}" />
                    <input type="hidden" name="email" value="{H(prefEmail)}" />
                    <button class="link-btn" type="submit">
                      <i class="bi bi-arrow-clockwise" aria-hidden="true"></i>
                      {T(lang, "إعادة إرسال الرمز", "Resend code")}
                    </button>
                  </form>
                </div>
                """;
        }

        var phoneField = !useWa ? "" : $"""
            <label class="field-label"><i class="bi bi-whatsapp" aria-hidden="true"></i> {T(lang, "رقم الواتساب", "WhatsApp number")}</label>
            <div class="input-wrap">
              <i class="bi bi-phone input-ico" aria-hidden="true"></i>
              <input class="phone-input has-ico ltr" name="phone" type="tel" value="{H(prefPhone)}" placeholder="07xxxxxxxxx" {(useEmail ? "" : "required")} />
            </div>
            <p class="hint">{T(lang, "يمكن إدخال الرقم المحلي أو بالصيغة الدولية +964…", "Use a local number or international format +964…")}</p>
            """;
        var emailField = !useEmail ? "" : $"""
            <label class="field-label"><i class="bi bi-envelope" aria-hidden="true"></i> {T(lang, "البريد الإلكتروني", "Email")}</label>
            <div class="input-wrap">
              <i class="bi bi-at input-ico" aria-hidden="true"></i>
              <input class="phone-input has-ico ltr" name="email" type="email" value="{H(prefEmail)}" placeholder="name@email.com" {(useWa ? "" : "required")} />
            </div>
            """;

        var (title, hint, btnLabel, btnIcon, iconClass, iconTone) = (useWa, useEmail) switch
        {
            (true, true) => (
                T(lang, "تأكيد قبل الدفع", "Verify before payment"),
                T(lang, "سنرسل الرمز عبر واتساب والبريد معاً لحماية العملية.", "We will send the code via WhatsApp and email to protect this payment."),
                T(lang, "إرسال رمز التحقق", "Send verification code"),
                """<i class="bi bi-send-fill" aria-hidden="true"></i>""",
                """<i class="bi bi-shield-lock-fill" aria-hidden="true"></i>""",
                "secure"),
            (false, true) => (
                T(lang, "تأكيد البريد الإلكتروني", "Verify email"),
                T(lang, "سنرسل رمزاً لمرة واحدة إلى بريدك قبل اختيار طريقة الدفع.", "We will send a one-time code to your email before choosing a payment method."),
                T(lang, "إرسال رمز البريد", "Send email code"),
                """<i class="bi bi-envelope-fill" aria-hidden="true"></i>""",
                """<i class="bi bi-envelope-fill" aria-hidden="true"></i>""",
                "mail"),
            _ => (
                T(lang, "تأكيد رقم الواتساب", "Verify WhatsApp number"),
                T(lang, "لحماية عملية الدفع، نرسل رمزاً لمرة واحدة عبر واتساب قبل اختيار طريقة الدفع.", "To protect this payment, we send a one-time WhatsApp code before you choose a method."),
                T(lang, "إرسال رمز واتساب", "Send WhatsApp code"),
                """<i class="bi bi-whatsapp" aria-hidden="true"></i>""",
                """<i class="bi bi-whatsapp" aria-hidden="true"></i>""",
                "wa")
        };

        return $"""
            <div class="verify-box">
              <div class="verify-icon {iconTone}" aria-hidden="true">{iconClass}</div>
              <h2>{title}</h2>
              <p class="muted">{hint}</p>
              {errHtml}
              <form method="post" action="/checkout/{payment.Id}/otp/send?lang={lang}" class="verify-form">
                {phoneField}
                {emailField}
                <button class="btn btn-send" type="submit">
                  {btnIcon}
                  <span>{btnLabel}</span>
                </button>
              </form>
            </div>
            """;
    }

    private string ResolveLang()
    {
        var q = Request.Query["lang"].FirstOrDefault();
        return string.Equals(q, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ar";
    }

    private static string T(string lang, string ar, string en) => lang == "en" ? en : ar;

    private string Shell(Domain.Entities.Payment payment, string inner, bool showProviders, string lang)
    {
        var culture = lang == "en" ? CultureInfo.GetCultureInfo("en-IQ") : CultureInfo.GetCultureInfo("ar-IQ");
        var amount = payment.Amount.ToString("N0", culture);
        var amountWords = lang == "en"
            ? $"{payment.Amount:N0} Iraqi Dinars"
            : AmountToArabicWords(payment.Amount);
        var txId = payment.Id.ToString("N")[..8].ToUpperInvariant();
        var orderId = string.IsNullOrWhiteSpace(payment.MerchantOrderId) ? "—" : payment.MerchantOrderId;
        var otherLang = lang == "ar" ? "en" : "ar";
        var langLabel = lang == "ar" ? "العربية" : "English";
        var path = Request.Path.Value ?? $"/checkout/{payment.Id}";
        var switchUrl = $"{path}?lang={otherLang}";
        var deadlineUtc = AsUtc(payment.ExpiredAtUtc ?? payment.CreatedAtUtc.AddHours(1));
        var expiresMs = new DateTimeOffset(deadlineUtc).ToUnixTimeMilliseconds();
        var remainingMs = Math.Max(0, (long)(deadlineUtc - DateTime.UtcNow).TotalMilliseconds);
        var showTimer = payment.Status == PaymentStatus.Pending;
        var timerHtml = !showTimer ? "" : $"""
                    <div class="ttl" id="checkoutTtl" data-expires-ms="{expiresMs}" data-payment="{payment.Id:N}">
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" aria-hidden="true"><circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.7"/><path d="M12 7v5l3 2" stroke="currentColor" stroke-width="1.7" stroke-linecap="round"/></svg>
                      <span>{T(lang, "ينتهي خلال", "Expires in")}</span>
                      <strong class="ttl-count ltr" id="checkoutTtlCount">{FormatRemaining(remainingMs)}</strong>
                    </div>
                    """;
        var timerScript = !showTimer ? "" : """
            <script>
            (function(){
              var el = document.getElementById('checkoutTtl');
              var out = document.getElementById('checkoutTtlCount');
              if (!el || !out) return;
              var expires = Number(el.getAttribute('data-expires-ms'));
              var key = 'fp-checkout-exp-' + (el.getAttribute('data-payment') || '');
              if (!expires || isNaN(expires)) return;
              var done = false;
              function tick(){
                if (done) return;
                var ms = expires - Date.now();
                if (ms <= 0) {
                  done = true;
                  out.textContent = '00:00:00';
                  el.classList.add('expired');
                  // إعادة تحميل مرة واحدة فقط لإغلاق/مسح الجلسة — بدون حلقة رفرش
                  if (!sessionStorage.getItem(key)) {
                    sessionStorage.setItem(key, '1');
                    setTimeout(function(){ location.reload(); }, 400);
                  }
                  return;
                }
                var s = Math.floor(ms / 1000);
                var h = Math.floor(s / 3600); s %= 3600;
                var m = Math.floor(s / 60); s %= 60;
                out.textContent = String(h).padStart(2,'0') + ':' + String(m).padStart(2,'0') + ':' + String(s).padStart(2,'0');
                setTimeout(tick, 1000);
              }
              tick();
            })();
            </script>
            """;

        return $"""
            <div class="page">
              <div class="page-tools">
                <a class="lang" href="{H(switchUrl)}" title="Language">
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" aria-hidden="true"><circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.7"/><path d="M3 12h18M12 3c2.5 2.8 3.8 5.8 3.8 9S14.5 18.2 12 21c-2.5-2.8-3.8-5.8-3.8-9S9.5 5.8 12 3z" stroke="currentColor" stroke-width="1.7"/></svg>
                  <span>{langLabel}</span>
                  <span class="lang-caret" aria-hidden="true">▾</span>
                </a>
              </div>

              <div class="brand-lockup">
                {(payment.MerchantPlatform != null && !string.IsNullOrWhiteSpace(payment.MerchantPlatform.LogoUrl)
                    ? $"""<img class="platform-logo" src="{H(payment.MerchantPlatform.LogoUrl)}" alt="{H(payment.MerchantPlatform.Name)}" width="56" height="56" />"""
                    : "")}
                <img class="brand-logo" src="/full-logo.png" alt="Fynexpay" width="180" height="41" />
              </div>

              <article class="card">
                <section class="hero">
                  <div class="hero-top">
                    <span class="secure">
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M12 2l8 3v6c0 5-3.4 9.4-8 11-4.6-1.6-8-6-8-11V5l8-3z" stroke="currentColor" stroke-width="1.8"/><path d="M9.5 12.2l1.8 1.8 3.8-3.8" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>
                      {T(lang, "دفع آمن", "Secure payment")}
                    </span>
                    {timerHtml}
                  </div>
                  <div class="amount-block">
                    <p class="amount-label">{T(lang, "المبلغ المطلوب", "Amount due")}</p>
                    <h1 class="amount"><span class="num ltr">{H(amount)}</span> <small>{T(lang, "د.ع", "IQD")}</small></h1>
                    <p class="amount-words">{H(amountWords)}</p>
                  </div>
                  <div class="meta">
                    <div class="meta-box">
                      <span>{T(lang, "رقم العملية", "Transaction ID")}</span>
                      <div class="meta-val">
                        <strong class="ltr" id="txId">{H(txId)}</strong>
                        <button type="button" class="copy" data-copy="{H(txId)}" aria-label="copy">
                          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" aria-hidden="true"><rect x="9" y="9" width="11" height="11" rx="2" stroke="currentColor" stroke-width="1.8"/><path d="M5 15V5a2 2 0 0 1 2-2h10" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>
                        </button>
                      </div>
                    </div>
                    <div class="meta-box">
                      <span>{T(lang, "رقم الطلب", "Order ID")}</span>
                      <div class="meta-val">
                        <strong class="ltr" id="orderId">{H(orderId)}</strong>
                        <button type="button" class="copy" data-copy="{H(orderId)}" aria-label="copy">
                          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" aria-hidden="true"><rect x="9" y="9" width="11" height="11" rx="2" stroke="currentColor" stroke-width="1.8"/><path d="M5 15V5a2 2 0 0 1 2-2h10" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>
                        </button>
                      </div>
                    </div>
                  </div>
                </section>
                <section class="body">
                  {inner}
                </section>
              </article>

              <footer class="foot">
                <div class="foot-brand">
                  <img class="foot-logo" src="/icon-logo.png" alt="" width="18" height="13" />
                  {T(lang, "مدعوم بواسطة", "Powered by")} <strong>Fynexpay</strong>
                </div>
                <span class="foot-sep" aria-hidden="true"></span>
                <div class="foot-secure">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M7 11V8a5 5 0 0 1 10 0v3" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/><rect x="5" y="11" width="14" height="10" rx="2.5" stroke="currentColor" stroke-width="1.8"/></svg>
                  {T(lang, "جميع البيانات مشفرة وآمنة · الرابط صالح لساعة واحدة", "All data is encrypted and secure · Link valid for 1 hour")}
                </div>
              </footer>
            </div>
            <script src="/checkout-copy.js"></script>
            {timerScript}
            """;
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static string FormatRemaining(long remainingMs)
    {
        var s = Math.Max(0, remainingMs / 1000);
        var h = s / 3600; s %= 3600;
        var m = s / 60; s %= 60;
        return $"{h:00}:{m:00}:{s:00}";
    }

    private static string EmptyState(string text, string lang) =>
        $"<div class=\"state\"><div class=\"icon soft\">!</div><p>{H(text)}</p></div>";

    private static string ReturnState(Domain.Entities.Payment payment, bool success, bool autoRedirect, string lang, ProviderRuntimeSettings? settings = null)
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
            var note = T(lang, "جاري إعادتك إلى المتجر خلال", "Returning you to the store in");
            var now = T(lang, "العودة للمتجر الآن", "Return to store now");
            var sec = T(lang, "ثانية", "seconds");
            redirectBlock =
                "<p class=\"muted redirect-note\">" + note + " <strong id=\"cd\">" + delaySec +
                "</strong> " + sec + "…</p>" +
                "<a class=\"" + btnClass + "\" href=\"" + H(merchantUrl) + "\">" + now + "</a>" +
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
                  <h1>{T(lang, "تم الدفع بنجاح", "Payment successful")}</h1>
                  <p>{T(lang, "تمت العملية عبر Fynexpay", "Processed via Fynexpay")}</p>
                  {redirectBlock}
                </div>
                """;
        }

        return $"""
            <div class="state bad">
              <div class="icon">!</div>
              <h1>{T(lang, "لم تكتمل العملية", "Payment incomplete")}</h1>
              <p>{H(payment.FailureReason ?? T(lang, "تعذّر إتمام الدفع", "Could not complete the payment"))}</p>
              {redirectBlock}
            </div>
            """;
    }

    private static string? ResolveMerchantReturnUrl(Domain.Entities.Payment payment, bool success)
    {
        var domain = payment.MerchantPlatform?.Domain;
        string? candidate;
        if (success)
            candidate = string.IsNullOrWhiteSpace(payment.SuccessUrl) ? null : payment.SuccessUrl;
        else if (!string.IsNullOrWhiteSpace(payment.FailureUrl))
            candidate = payment.FailureUrl;
        else if (!string.IsNullOrWhiteSpace(payment.SuccessUrl))
        {
            var sep = payment.SuccessUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            candidate = $"{payment.SuccessUrl}{sep}fynexpay_status=failed";
        }
        else
            candidate = null;

        if (candidate == null)
            return null;

        if (string.IsNullOrWhiteSpace(domain) || !UrlSafety.IsSafeRedirectUrl(candidate, domain))
            return null;

        return candidate;
    }

    private static string DisplayName(ProviderRuntimeSettings? settings, PaymentProviderType p)
    {
        var fallback = p switch
        {
            PaymentProviderType.Fib => "FIB",
            PaymentProviderType.ZainCash => "Zain Cash",
            PaymentProviderType.Qi => "QI Card",
            PaymentProviderType.SuperQi => "SuperQi",
            PaymentProviderType.Alqaseh => "Alqaseh",
            _ => p.ToString()
        };
        if (settings == null) return fallback;
        var bundle = p switch
        {
            PaymentProviderType.Fib => settings.Fib,
            PaymentProviderType.ZainCash => settings.ZainCash,
            PaymentProviderType.Qi => settings.Qi,
            PaymentProviderType.SuperQi => settings.SuperQi,
            PaymentProviderType.Alqaseh => settings.Alqaseh,
            _ => null
        };
        return bundle?.ResolveDisplayName(fallback) ?? fallback;
    }

    private static string ProviderHint(string lang) => lang == "en"
        ? "Secure payment method"
        : "طريقة دفع آمنة";

    private static string? LogoUrl(ProviderRuntimeSettings s, PaymentProviderType p)
    {
        var custom = p switch
        {
            PaymentProviderType.Fib => s.Fib.LogoUrl,
            PaymentProviderType.ZainCash => s.ZainCash.LogoUrl,
            PaymentProviderType.Qi => s.Qi.LogoUrl,
            PaymentProviderType.SuperQi => s.SuperQi.LogoUrl,
            PaymentProviderType.Alqaseh => s.Alqaseh.LogoUrl,
            _ => null
        };
        if (!string.IsNullOrWhiteSpace(custom)) return custom;
        return p switch
        {
            PaymentProviderType.Fib => "/providers/fib.svg",
            PaymentProviderType.ZainCash => "/providers/zaincash.svg",
            PaymentProviderType.Qi => "/providers/qi.svg",
            PaymentProviderType.SuperQi => "/providers/superqi.svg",
            PaymentProviderType.Alqaseh => "/providers/alqaseh.svg",
            _ => null
        };
    }

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? "");

    private static string AmountToArabicWords(decimal amount)
    {
        var n = (long)Math.Floor(Math.Abs(amount));
        if (n == 0) return "صفر دينار عراقي";
        return $"{ToArabicWords(n)} دينار عراقي";
    }

    private static string ToArabicWords(long number)
    {
        if (number == 0) return "صفر";
        string[] ones = ["", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة", "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر"];
        string[] tens = ["", "", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون"];
        string[] hundreds = ["", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة"];

        string BelowThousand(long n)
        {
            if (n == 0) return "";
            var parts = new List<string>();
            var h = n / 100;
            var r = n % 100;
            if (h > 0) parts.Add(hundreds[h]);
            if (r > 0)
            {
                if (r < 20) parts.Add(ones[r]);
                else
                {
                    var t = r / 10;
                    var o = r % 10;
                    if (o > 0) parts.Add($"{ones[o]} و{tens[t]}");
                    else parts.Add(tens[t]);
                }
            }
            return string.Join(" و", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        var result = new List<string>();
        var millions = number / 1_000_000;
        var thousands = (number % 1_000_000) / 1000;
        var rest = number % 1000;

        if (millions > 0)
        {
            if (millions == 1) result.Add("مليون");
            else if (millions == 2) result.Add("مليونان");
            else if (millions is >= 3 and <= 10) result.Add($"{BelowThousand(millions)} ملايين");
            else result.Add($"{BelowThousand(millions)} مليون");
        }

        if (thousands > 0)
        {
            if (thousands == 1) result.Add("ألف");
            else if (thousands == 2) result.Add("ألفان");
            else if (thousands is >= 3 and <= 10) result.Add($"{BelowThousand(thousands)} آلاف");
            else result.Add($"{BelowThousand(thousands)} ألف");
        }

        if (rest > 0) result.Add(BelowThousand(rest));
        return string.Join(" و", result);
    }

    private static string Render(string title, string body, string? status, string lang)
    {
        var dir = lang == "en" ? "ltr" : "rtl";
        var htmlLang = lang == "en" ? "en" : "ar";
        return $$"""
            <!DOCTYPE html>
            <html lang="{{htmlLang}}" dir="{{dir}}">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>{{WebUtility.HtmlEncode(title)}} · Fynexpay</title>
              <link rel="icon" type="image/png" href="/icon-logo.png" />
              <link rel="preconnect" href="https://fonts.googleapis.com" />
              <link href="https://fonts.googleapis.com/css2?family=Cairo:wght@400;600;700;800&family=Plus+Jakarta+Sans:wght@600;700;800&display=swap" rel="stylesheet" />
              <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" />
              <style>
                :root{
                  --ink:#031838; --muted:#5b6b86; --line:#e2e8f2;
                  --brand:#031838; --brand-soft:rgba(3, 24, 56,.12);
                  --navy:#031838; --ok:#12b76a; --bad:#f04438;
                  --shadow:0 24px 60px rgba(3,24,56,.12);
                }
                *{box-sizing:border-box}
                body{
                  margin:0;min-height:100vh;
                  font-family: {{(lang == "en" ? "\"Plus Jakarta Sans\",\"Cairo\",sans-serif" : "\"Cairo\",\"Plus Jakarta Sans\",sans-serif")}};
                  color:var(--ink);
                  background:
                    radial-gradient(900px 420px at 85% -5%, rgba(3, 24, 56,.10), transparent 55%),
                    radial-gradient(700px 380px at 5% 100%, rgba(3,24,56,.06), transparent 50%),
                    #f4f6fb;
                  position:relative;
                  overflow-x:hidden;
                }
                body:before, body:after{
                  content:"";position:fixed;pointer-events:none;z-index:0;
                  width:520px;height:520px;border-radius:50%;
                  border:1px solid rgba(3, 24, 56,.08);
                }
                body:before{top:-180px;inset-inline-end:-160px;box-shadow:0 0 0 60px rgba(3, 24, 56,.03), inset 0 0 0 80px rgba(3,24,56,.02)}
                body:after{bottom:-220px;inset-inline-start:-180px;box-shadow:0 0 0 40px rgba(3,24,56,.02)}
                .page{position:relative;z-index:1;width:min(520px,94vw);margin:28px auto 36px}
                .page-tools{display:flex;justify-content:flex-end;margin-bottom:10px}
                .lang{
                  display:inline-flex;align-items:center;gap:6px;
                  color:var(--muted);text-decoration:none;font-weight:700;font-size:.9rem;
                  background:#fff;border:1px solid var(--line);border-radius:999px;padding:7px 12px;
                  box-shadow:0 4px 14px rgba(3,24,56,.04);
                }
                .lang:hover{color:var(--brand);border-color:rgba(3, 24, 56,.3)}
                .lang-caret{font-size:.7rem;opacity:.7}
                .brand-lockup{
                  display:flex;align-items:center;justify-content:center;gap:14px;margin:8px 0 18px;flex-wrap:wrap;
                }
                .platform-logo{
                  display:block;width:56px;height:56px;object-fit:contain;
                  background:transparent;border:0;
                }
                .brand-logo{
                  display:block;height:40px;width:auto;max-width:min(220px,70vw);
                  object-fit:contain;
                }
                .foot-logo{
                  display:block;height:16px;width:auto;object-fit:contain;
                }
                .card{
                  background:#fff;border-radius:28px;overflow:hidden;
                  box-shadow:var(--shadow);border:1px solid rgba(226,232,242,.9);
                }
                .hero{
                  background:
                    radial-gradient(420px 180px at 100% 0%, rgba(255,255,255,.18), transparent 55%),
                    linear-gradient(145deg,#031838 0%, #031838 45%, #021225 100%);
                  color:#fff;padding:22px 22px 18px;position:relative;overflow:hidden;
                }
                .hero:before{
                  content:"";position:absolute;inset:0;opacity:.22;pointer-events:none;
                  background:
                    radial-gradient(circle at 20% 120%, transparent 0 38%, rgba(255,255,255,.35) 39% 40%, transparent 41%),
                    radial-gradient(circle at 80% -20%, transparent 0 40%, rgba(255,255,255,.25) 41% 42%, transparent 43%),
                    repeating-linear-gradient(-18deg, transparent 0 18px, rgba(255,255,255,.07) 18px 19px);
                }
                .hero > *{position:relative}
                .hero-top{display:flex;justify-content:space-between;align-items:center;margin-bottom:18px}
                .secure{
                  display:inline-flex;align-items:center;gap:6px;
                  background:rgba(255,255,255,.16);border:1px solid rgba(255,255,255,.22);
                  backdrop-filter:blur(6px);border-radius:999px;padding:6px 12px;font-size:.82rem;font-weight:700;
                }
                .ttl{
                  display:inline-flex;align-items:center;gap:6px;
                  background:rgba(255,255,255,.16);border:1px solid rgba(255,255,255,.22);
                  backdrop-filter:blur(6px);border-radius:999px;padding:6px 12px;font-size:.8rem;font-weight:700;
                }
                .ttl-count{font-family:"Plus Jakarta Sans",sans-serif;font-variant-numeric:tabular-nums;letter-spacing:.02em}
                .ttl.expired{background:rgba(239,68,68,.25);border-color:rgba(239,68,68,.45)}
                .amount-block{text-align:center;margin-bottom:18px}
                .amount-label{margin:0 0 6px;opacity:.9;font-weight:600;font-size:.95rem}
                .amount{margin:0;font-size:2.55rem;font-weight:800;letter-spacing:-.03em;line-height:1.1}
                .amount .num{font-variant-numeric:tabular-nums}
                .amount small{font-size:1.05rem;font-weight:700;opacity:.95}
                .amount-words{margin:8px 0 0;opacity:.88;font-size:.92rem;font-weight:600}
                .meta{display:grid;grid-template-columns:1fr 1fr;gap:10px}
                .meta-box{
                  background:rgba(255,255,255,.14);border:1px solid rgba(255,255,255,.2);
                  border-radius:16px;padding:10px 12px;min-width:0;
                }
                .meta-box span{display:block;font-size:.75rem;opacity:.85;margin-bottom:4px;font-weight:600}
                .meta-val{display:flex;align-items:center;justify-content:space-between;gap:8px}
                .meta-val strong{
                  font-size:.88rem;font-weight:800;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;
                }
                .copy{
                  border:0;background:rgba(255,255,255,.14);color:#fff;border-radius:10px;
                  width:30px;height:30px;display:grid;place-items:center;cursor:pointer;flex-shrink:0;
                }
                .copy:hover,.copy.ok{background:rgba(255,255,255,.28)}
                .ltr{direction:ltr;unicode-bidi:plaintext}
                .body{padding:22px}
                .choose-head{margin-bottom:14px}
                .choose h2{margin:0 0 6px;font-size:1.15rem;color:var(--navy);font-weight:800}
                .choose .muted{
                  margin:0;color:var(--muted);display:flex;align-items:center;gap:6px;font-size:.9rem;font-weight:600;
                }
                .shield{font-size:.95rem}
                .providers{display:grid;gap:10px}
                .provider{
                  width:100%;text-align:inherit;border:1px solid var(--line);background:#fff;
                  border-radius:16px;padding:12px 14px;cursor:pointer;font:inherit;
                  display:flex;align-items:center;gap:12px;transition:.18s ease;
                }
                .provider:hover{
                  border-color:rgba(3, 24, 56,.35);
                  box-shadow:0 10px 24px rgba(3, 24, 56,.10);
                  transform:translateY(-1px);
                }
                .p-logo-wrap{
                  width:48px;height:48px;border-radius:14px;background:#f8fafc;border:1px solid var(--line);
                  display:grid;place-items:center;flex-shrink:0;overflow:hidden;
                }
                .p-logo{width:100%;height:100%;object-fit:contain;padding:6px}
                .p-fallback{font-weight:800;color:var(--brand);font-size:1.1rem}
                .p-text{display:grid;gap:2px;flex:1;min-width:0;text-align:start}
                .p-name{font-weight:800;font-size:1rem;color:var(--navy)}
                .p-desc{color:var(--muted);font-size:.86rem;font-weight:600}
                .p-go{
                  display:inline-flex;align-items:center;gap:6px;
                  background:var(--brand-soft);color:var(--brand);
                  border-radius:12px;padding:9px 12px;font-weight:800;font-size:.88rem;white-space:nowrap;
                }
                .p-go i{font-style:normal;font-family:"Plus Jakarta Sans",sans-serif}
                html[dir="ltr"] .p-go i{display:inline-block;transform:scaleX(-1)}
                .btn{
                  display:inline-flex;align-items:center;justify-content:center;gap:8px;
                  margin-top:14px;border:0;cursor:pointer;
                  background:var(--brand);color:#fff;text-decoration:none;
                  border-radius:12px;padding:12px 18px;min-height:48px;
                  font:inherit;font-weight:700;font-size:.95rem;letter-spacing:0;
                  box-shadow:0 8px 20px rgba(3, 24, 56,.28);
                  transition:.15s ease;
                }
                .btn:hover{background:#021225}
                .btn-send{
                  width:100%;margin-top:8px;
                  border-radius:12px;min-height:48px;
                  font-family:inherit;font-weight:700;font-size:15px;
                  box-shadow:0 8px 18px rgba(3, 24, 56,.22);
                }
                .btn-send i{font-size:1.15rem;line-height:1}
                .btn.ghost{background:transparent;color:var(--bad);border:1px solid #efc4c0;box-shadow:none}
                .state{text-align:center;padding:8px 0}
                .state h1,.state h2{margin:0 0 8px;font-size:1.25rem}
                .state p{margin:0;color:var(--muted);font-weight:600}
                .state .icon{
                  width:56px;height:56px;border-radius:50%;display:grid;place-items:center;margin:0 auto 12px;
                  font-size:1.4rem;font-weight:800;
                }
                .state .icon.soft{background:var(--brand-soft);color:var(--brand)}
                .state.ok .icon{background:rgba(18,183,106,.12);color:var(--ok)}
                .state.bad .icon{background:rgba(240,68,56,.1);color:var(--bad)}
                .error{background:#fff1f0;color:var(--bad);border:1px solid #f0c9c5;border-radius:12px;padding:10px 12px;margin-bottom:12px;font-weight:700}
                .redirect-note{margin-top:12px!important}
                .verify-box{text-align:center;padding:4px 0 8px}
                .verify-box h2{margin:0 0 8px;font-size:1.2rem;font-weight:800;color:var(--navy)}
                .verify-box .muted{margin:0 auto 16px;max-width:38ch;line-height:1.7;font-weight:600;font-size:.92rem}
                .verify-icon{
                  width:64px;height:64px;border-radius:18px;margin:0 auto 14px;
                  display:grid;place-items:center;font-size:1.75rem;line-height:1;
                }
                .verify-icon.wa{background:rgba(37,211,102,.12);color:#25D366;border:1px solid rgba(37,211,102,.22)}
                .verify-icon.mail{background:rgba(3, 24, 56,.1);color:var(--brand);border:1px solid rgba(3, 24, 56,.18)}
                .verify-icon.secure{background:var(--brand-soft);color:var(--brand);border:1px solid rgba(3, 24, 56,.18)}
                .verify-form{display:grid;gap:8px;text-align:start;max-width:360px;margin:0 auto}
                .field-label{
                  display:inline-flex;align-items:center;gap:6px;
                  font-weight:700;font-size:.9rem;color:var(--navy);margin-top:4px;
                }
                .field-label i{color:#25D366;font-size:1rem}
                .field-label .bi-envelope{color:var(--brand)}
                .input-wrap{position:relative}
                .input-ico{
                  position:absolute;top:50%;inset-inline-start:14px;transform:translateY(-50%);
                  color:#94a3b8;font-size:1.05rem;pointer-events:none;
                }
                .phone-input,.otp-input{
                  width:100%;border:1px solid var(--line);border-radius:12px;padding:12px 14px;
                  font:inherit;font-weight:600;font-size:15px;background:#fff;color:var(--ink);
                  min-height:48px;
                }
                .phone-input.has-ico{padding-inline-start:42px}
                .otp-input{text-align:center;letter-spacing:.35em;font-size:1.35rem;font-weight:800}
                .phone-input:focus,.otp-input:focus{
                  outline:0;border-color:rgba(3, 24, 56,.55);
                  box-shadow:0 0 0 4px rgba(3, 24, 56,.14);
                }
                .hint{margin:0 0 6px;color:var(--muted);font-size:13px;font-weight:600}
                .resend-form{margin-top:14px}
                .link-btn{
                  border:0;background:transparent;color:var(--brand);font:inherit;font-weight:700;
                  cursor:pointer;display:inline-flex;align-items:center;gap:6px;
                }
                .link-btn:hover{text-decoration:underline}
                .dev-hint{background:#fef9c3;border:1px solid #fde68a;border-radius:12px;padding:8px 10px;font-size:.85rem;margin-bottom:10px}
                .foot{
                  margin-top:18px;display:flex;align-items:center;justify-content:center;flex-wrap:wrap;gap:10px 14px;
                  color:var(--muted);font-size:.85rem;font-weight:700;
                }
                .foot-brand,.foot-secure{display:inline-flex;align-items:center;gap:6px}
                .foot-brand strong{color:var(--navy);font-family:"Plus Jakarta Sans",sans-serif}
                .foot-sep{width:1px;height:14px;background:#c9d2e3}
                form{margin:0}
                @media (max-width:520px){
                  .amount{font-size:2.1rem}
                  .meta{grid-template-columns:1fr}
                  .foot-sep{display:none}
                }
              </style>
            </head>
            <body data-status="{{WebUtility.HtmlEncode(status ?? "")}}">{{body}}</body>
            </html>
            """;
    }
}
