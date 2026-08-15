namespace Fynexpay.Application.DTOs;

public class LandingContentDto
{
    public LandingLocaleDto Ar { get; set; } = LandingDefaults.Arabic();
    public LandingLocaleDto En { get; set; } = LandingDefaults.English();
}

public class LandingLocaleDto
{
    public string NavFeatures { get; set; } = "";
    public string NavProviders { get; set; } = "";
    public string NavDevelopers { get; set; } = "";
    public string NavContact { get; set; } = "";
    public string Login { get; set; } = "";
    public string StartNow { get; set; } = "";
    public string Badge { get; set; } = "";
    public string HeroTitle { get; set; } = "";
    public string HeroBefore { get; set; } = "";
    public string HeroAccent { get; set; } = "";
    public string HeroAfter { get; set; } = "";
    public string HeroSubtitle { get; set; } = "";
    public string CtaMerchant { get; set; } = "";
    public string CtaDocs { get; set; } = "";
    public string FeaturesEyebrow { get; set; } = "";
    public string FeaturesTitle { get; set; } = "";
    public string FeaturesSubtitle { get; set; } = "";
    public List<LandingFeatureDto> Features { get; set; } = [];
    public string ProvidersEyebrow { get; set; } = "";
    public string ProvidersTitle { get; set; } = "";
    public string ProvidersSubtitle { get; set; } = "";
    public List<string> ProviderPills { get; set; } = [];
    public string ApiEyebrow { get; set; } = "";
    public string ApiTitle { get; set; } = "";
    public string ApiSubtitle { get; set; } = "";
    public string CtaTitle { get; set; } = "";
    public string CtaSubtitle { get; set; } = "";
    public string CtaRegister { get; set; } = "";
    public string CtaContact { get; set; } = "";
    public string Footer { get; set; } = "";
    public string FooterDisclaimer { get; set; } = "";
    public string FooterLegalNote { get; set; } = "";
    public string MockDashboard { get; set; } = "";
    public string MockToday { get; set; } = "";
    public string MockSuccess { get; set; } = "";
    public string MockAmount { get; set; } = "";
    public string MockChooseProvider { get; set; } = "";
    public string ContactEyebrow { get; set; } = "";
    public string ContactTitle { get; set; } = "";
    public string ContactSubtitle { get; set; } = "";
    public string ContactEmailLabel { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string ContactPhoneLabel { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string ContactAddressLabel { get; set; } = "";
    public string ContactAddress { get; set; } = "";
    public string ContactHoursLabel { get; set; } = "";
    public string ContactHours { get; set; } = "";
    public string ContactFormName { get; set; } = "";
    public string ContactFormEmail { get; set; } = "";
    public string ContactFormMessage { get; set; } = "";
    public string ContactFormSubmit { get; set; } = "";
    public string ContactFormNote { get; set; } = "";
    public string ContactFormSuccess { get; set; } = "";
    public LegalBundleDto Legal { get; set; } = new();
}

public class LandingFeatureDto
{
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
}

public static class LandingDefaults
{
    public static LandingContentDto Create() => new()
    {
        Ar = Arabic(),
        En = English()
    };

    public static LandingLocaleDto Arabic() => new()
    {
        NavFeatures = "المزايا",
        NavProviders = "طرق الدفع",
        NavDevelopers = "للمطوّرين",
        NavContact = "تواصل معنا",
        Login = "دخول",
        StartNow = "ابدأ الآن ←",
        Badge = "منصة متاجر إلكترونية مع دفع مدمج",
        HeroTitle = "FynexPay — أنشئ متجرك الإلكتروني",
        HeroBefore = "FynexPay —",
        HeroAccent = "أنشئ متجرك",
        HeroAfter = "الإلكتروني",
        HeroSubtitle = "منصة متاجر إلكترونية. الدفع مدمج داخل متاجر FynexPay فقط — ليست بوابة دفع مستقلة.",
        CtaMerchant = "ابدأ الآن",
        CtaDocs = "وثائق API",
        FeaturesEyebrow = "المزايا",
        FeaturesTitle = "كل ما تحتاجه لإدارة متجرك",
        FeaturesSubtitle = "من الكتالوج حتى تأكيد الطلب: متجرك وطلبك ودفع زبونك في منصة واحدة.",
        Features =
        [
            new() { Icon = "API", Title = "API موحّد", Body = "أنشئ دفعة بمفتاح واحد بدون التعامل مع كل مزود على حدة." },
            new() { Icon = "Pay", Title = "صفحة دفع مستضافة", Body = "الزبون يرى التفاصيل ويختار طريقة الدفع داخل Fynexpay." },
            new() { Icon = "٪", Title = "عمولة تلقائية", Body = "اقتطاع عمولة المنصة وإضافة الصافي لمحفظة التاجر فوراً." },
            new() { Icon = "Hook", Title = "Webhooks موقّعة", Body = "إشعارات HMAC عند تغيّر حالة الدفعة لتحديث طلباتك بأمان." },
            new() { Icon = "Card", Title = "بطاقات ومحافظ", Body = "ادعم عدة طرق دفع محلية عبر تكامل واحد جاهز للإنتاج." },
            new() { Icon = "IQ", Title = "مصمم للعراق", Body = "دينار عراقي، واجهة عربية RTL، وتجربة دفع محلية من اليوم الأول." }
        ],
        ProvidersEyebrow = "طرق الدفع",
        ProvidersTitle = "متكامل مع أدوات الدفع التي يعرفها زبونك",
        ProvidersSubtitle = "فعّل طرق الدفع من لوحة الأدمن، واترك للتاجر اختيار ما يظهر لزبائنه.",
        ProviderPills = [],
        ApiEyebrow = "للمطوّرين",
        ApiTitle = "أنشئ دفعة في ثوانٍ",
        ApiSubtitle = "أرسل المبلغ ونوع الخدمة فقط — الزبون يختار طريقة الدفع من الصفحة المستضافة.",
        CtaTitle = "جاهز تفتح متجرك؟",
        CtaSubtitle = "سجّل، أنشئ متجرك على FynexPay، وابدأ البيع مع دفع مدمج داخل المنصة.",
        CtaRegister = "إنشاء حساب ←",
        CtaContact = "تسجيل الدخول",
        Footer = "منصة متاجر إلكترونية — الدفع لمتاجر FynexPay",
        FooterDisclaimer = "موقع Fynexpay وأي محتوى مذكور فيه غير معتمد أو مرتبط مباشرة أو مرخص أو مدعوم من قبل أي مؤسسة مالية (ما لم يُذكر خلاف ذلك صراحة). جميع أسماء الشركات والشعارات والعلامات التجارية المذكورة هي ملك لأصحابها الأصليين.",
        FooterLegalNote = "Fynexpay مسجّلة في العراق تحت الكيان القانوني: يُحدَّث الاسم ورقم التسجيل من لوحة الإدارة. المقر: بغداد، العراق.",
        MockDashboard = "لوحة المدفوعات",
        MockToday = "اليوم",
        MockSuccess = "نجاح",
        MockAmount = "المبلغ",
        MockChooseProvider = "اختيار طريقة الدفع",
        ContactEyebrow = "تواصل",
        ContactTitle = "تواصل معنا",
        ContactSubtitle = "فريق FynexPay جاهز لمساعدتك في إنشاء المتجر أو إدارة الطلبات.",
        ContactEmailLabel = "البريد",
        ContactEmail = "hello@fynexpay.iq",
        ContactPhoneLabel = "الهاتف",
        ContactPhone = "07809726258",
        ContactAddressLabel = "العنوان",
        ContactAddress = "بغداد، العراق",
        ContactHoursLabel = "ساعات العمل",
        ContactHours = "الأحد – الخميس · 9:00 – 17:00",
        ContactFormName = "الاسم",
        ContactFormEmail = "بريدك الإلكتروني",
        ContactFormMessage = "رسالتك",
        ContactFormSubmit = "إرسال الرسالة",
        ContactFormNote = "سيتم فتح بريدك لإرسال الرسالة مباشرة إلى فريق الدعم.",
        ContactFormSuccess = "تم تجهيز الرسالة — أكمل الإرسال من تطبيق البريد.",
        Legal = LegalContentDefaults.Arabic()
    };

    public static LandingLocaleDto English() => new()
    {
        NavFeatures = "Features",
        NavProviders = "Payment methods",
        NavDevelopers = "Developers",
        NavContact = "Contact",
        Login = "Sign in",
        StartNow = "Get started →",
        Badge = "An e-commerce platform with built-in payments",
        HeroTitle = "FynexPay — Create your online store",
        HeroBefore = "FynexPay —",
        HeroAccent = "Create your store",
        HeroAfter = "online",
        HeroSubtitle = "An e-commerce store platform. Checkout is built into FynexPay stores only — not a standalone payment gateway.",
        CtaMerchant = "Get started",
        CtaDocs = "API docs",
        FeaturesEyebrow = "Features",
        FeaturesTitle = "Everything you need to run your store",
        FeaturesSubtitle = "From catalog to order confirmation: your store, orders, and customer checkout on one platform.",
        Features =
        [
            new() { Icon = "API", Title = "Unified API", Body = "Create a payment with one key — no need to integrate each provider yourself." },
            new() { Icon = "Pay", Title = "Hosted checkout", Body = "Customers see the details and choose a payment method inside Fynexpay." },
            new() { Icon = "٪", Title = "Automatic fees", Body = "Platform commission is deducted and net amount is credited instantly." },
            new() { Icon = "Hook", Title = "Signed webhooks", Body = "HMAC notifications when payment status changes so you can update orders safely." },
            new() { Icon = "Card", Title = "Cards & wallets", Body = "Support multiple local payment methods through one production-ready integration." },
            new() { Icon = "IQ", Title = "Built for Iraq", Body = "IQD, Arabic RTL, and a local checkout experience from day one." }
        ],
        ProvidersEyebrow = "Payment methods",
        ProvidersTitle = "Integrated with payment tools your customers already use",
        ProvidersSubtitle = "Enable methods from the admin panel, and let merchants choose what customers see.",
        ProviderPills = [],
        ApiEyebrow = "Developers",
        ApiTitle = "Create a payment in seconds",
        ApiSubtitle = "Send amount and service type only — the customer picks a method on hosted checkout.",
        CtaTitle = "Ready to open your store?",
        CtaSubtitle = "Register, create your FynexPay store, and start selling with checkout built into the platform.",
        CtaRegister = "Create account →",
        CtaContact = "Sign in",
        Footer = "E-commerce store platform — checkout for FynexPay stores",
        FooterDisclaimer = "The Fynexpay website and any content on it are not endorsed by, affiliated with, licensed by, or sponsored by any financial institution (unless expressly stated otherwise). All company names, logos, and trademarks mentioned remain the property of their respective owners.",
        FooterLegalNote = "Fynexpay is registered in Iraq under its legal entity: update the official name and registration number from the admin panel. Headquarters: Baghdad, Iraq.",
        MockDashboard = "Payments dashboard",
        MockToday = "Today",
        MockSuccess = "Success",
        MockAmount = "Amount",
        MockChooseProvider = "Choose payment method",
        ContactEyebrow = "Contact",
        ContactTitle = "Contact us",
        ContactSubtitle = "The FynexPay team is ready to help you create a store or manage orders.",
        ContactEmailLabel = "Email",
        ContactEmail = "hello@fynexpay.iq",
        ContactPhoneLabel = "Phone",
        ContactPhone = "07809726258",
        ContactAddressLabel = "Address",
        ContactAddress = "Baghdad, Iraq",
        ContactHoursLabel = "Working hours",
        ContactHours = "Sun – Thu · 9:00 – 17:00",
        ContactFormName = "Name",
        ContactFormEmail = "Your email",
        ContactFormMessage = "Your message",
        ContactFormSubmit = "Send message",
        ContactFormNote = "This opens your email client to send the message to our support team.",
        ContactFormSuccess = "Message drafted — finish sending from your email app.",
        Legal = LegalContentDefaults.English()
    };
}
