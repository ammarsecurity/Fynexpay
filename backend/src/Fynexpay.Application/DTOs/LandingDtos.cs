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
        NavProviders = "المزودون",
        NavDevelopers = "للمطوّرين",
        NavContact = "تواصل معنا",
        Login = "دخول",
        StartNow = "ابدأ الآن ←",
        Badge = "جديد · صفحة دفع مستضافة + SuperQi جاهزة",
        HeroTitle = "محطة دفع واحدة لتطبيقك وموقعك في العراق",
        HeroSubtitle = "اربط FIB و ZainCash و QI و SuperQi خلف API موحّد، امنح كل تاجر محفظة، وخذ عمولتك تلقائياً من كل عملية.",
        CtaMerchant = "ابدأ كتاجر ←",
        CtaDocs = "وثائق API",
        FeaturesEyebrow = "المزايا",
        FeaturesTitle = "كل ما تحتاجه لقبول المدفوعات",
        FeaturesSubtitle = "من إنشاء الدفعة حتى إضافة الصافي لمحفظة التاجر — تدفق واضح وسريع للمطوّر والتاجر والزبون.",
        Features =
        [
            new() { Icon = "API", Title = "API موحّد", Body = "أنشئ دفعة بمفتاح واحد بدون التعامل مع كل مزود على حدة." },
            new() { Icon = "Pay", Title = "صفحة دفع مستضافة", Body = "الزبون يرى التفاصيل ويختار طريقة الدفع داخل Fynexpay." },
            new() { Icon = "٪", Title = "عمولة تلقائية", Body = "اقتطاع عمولة المنصة وإضافة الصافي لمحفظة التاجر فوراً." },
            new() { Icon = "Hook", Title = "Webhooks موقّعة", Body = "إشعارات HMAC عند تغيّر حالة الدفعة لتحديث طلباتك بأمان." },
            new() { Icon = "SQ", Title = "SuperQi + QI", Body = "دعم بطاقات QI ومحفظة SuperQi عبر بوابة Gate الرسمية." },
            new() { Icon = "IQ", Title = "مصمم للعراق", Body = "دينار عراقي، واجهة عربية RTL، ومزودون محليون من اليوم الأول." }
        ],
        ProvidersEyebrow = "المزودون",
        ProvidersTitle = "متكامل مع أدوات الدفع التي يعرفها زبونك",
        ProvidersSubtitle = "فعّل المزودين من لوحة الأدمن، واترك للتاجر اختيار ما يظهر لزبائنه.",
        ProviderPills = ["FIB Web Payments", "ZainCash", "QI Gate", "SuperQi"],
        ApiEyebrow = "للمطوّرين",
        ApiTitle = "أنشئ دفعة في ثوانٍ",
        ApiSubtitle = "أرسل المبلغ ونوع الخدمة فقط — الزبون يختار المزود من صفحة الدفع المستضافة.",
        CtaTitle = "جاهز تبدأ؟ افتح حساب تاجر اليوم",
        CtaSubtitle = "سجّل، فعّل حسابك، وأنشئ أول دفعة تجريبية خلال دقائق.",
        CtaRegister = "إنشاء حساب ←",
        CtaContact = "تسجيل الدخول",
        Footer = "بوابة دفع عراقية",
        MockDashboard = "لوحة المدفوعات",
        MockToday = "اليوم",
        MockSuccess = "نجاح",
        MockAmount = "المبلغ",
        MockChooseProvider = "اختيار المزود",
        ContactEyebrow = "تواصل",
        ContactTitle = "تواصل معنا",
        ContactSubtitle = "فريق Fynexpay جاهز لمساعدتك في الربط، التفعيل، أو أي استفسار عن المدفوعات.",
        ContactEmailLabel = "البريد",
        ContactEmail = "hello@fynexpay.iq",
        ContactPhoneLabel = "الهاتف",
        ContactPhone = "+964 770 000 0000",
        ContactAddressLabel = "العنوان",
        ContactAddress = "بغداد، العراق",
        ContactHoursLabel = "ساعات العمل",
        ContactHours = "الأحد – الخميس · 9:00 – 17:00",
        ContactFormName = "الاسم",
        ContactFormEmail = "بريدك الإلكتروني",
        ContactFormMessage = "رسالتك",
        ContactFormSubmit = "إرسال الرسالة",
        ContactFormNote = "سيتم فتح بريدك لإرسال الرسالة مباشرة إلى فريق الدعم.",
        ContactFormSuccess = "تم تجهيز الرسالة — أكمل الإرسال من تطبيق البريد."
    };

    public static LandingLocaleDto English() => new()
    {
        NavFeatures = "Features",
        NavProviders = "Providers",
        NavDevelopers = "Developers",
        NavContact = "Contact",
        Login = "Sign in",
        StartNow = "Get started →",
        Badge = "New · Hosted checkout + SuperQi ready",
        HeroTitle = "One payment hub for your app and website in Iraq",
        HeroSubtitle = "Connect FIB, ZainCash, QI, and SuperQi behind one API, give every merchant a wallet, and collect your fee automatically.",
        CtaMerchant = "Start as merchant →",
        CtaDocs = "API docs",
        FeaturesEyebrow = "Features",
        FeaturesTitle = "Everything you need to accept payments",
        FeaturesSubtitle = "From creating a payment to crediting the merchant wallet — a clear flow for developers, merchants, and customers.",
        Features =
        [
            new() { Icon = "API", Title = "Unified API", Body = "Create a payment with one key — no need to integrate each provider yourself." },
            new() { Icon = "Pay", Title = "Hosted checkout", Body = "Customers see the details and choose a payment method inside Fynexpay." },
            new() { Icon = "٪", Title = "Automatic fees", Body = "Platform commission is deducted and net amount is credited instantly." },
            new() { Icon = "Hook", Title = "Signed webhooks", Body = "HMAC notifications when payment status changes so you can update orders safely." },
            new() { Icon = "SQ", Title = "SuperQi + QI", Body = "QI cards and SuperQi wallet via the official Gate APIs." },
            new() { Icon = "IQ", Title = "Built for Iraq", Body = "IQD, Arabic RTL, and local providers from day one." }
        ],
        ProvidersEyebrow = "Providers",
        ProvidersTitle = "Integrated with payment tools your customers already use",
        ProvidersSubtitle = "Enable providers from the admin panel, and let merchants choose what customers see.",
        ProviderPills = ["FIB Web Payments", "ZainCash", "QI Gate", "SuperQi"],
        ApiEyebrow = "Developers",
        ApiTitle = "Create a payment in seconds",
        ApiSubtitle = "Send amount and service type only — the customer picks the provider on hosted checkout.",
        CtaTitle = "Ready to start? Open a merchant account today",
        CtaSubtitle = "Register, get activated, and create your first test payment in minutes.",
        CtaRegister = "Create account →",
        CtaContact = "Sign in",
        Footer = "Iraqi payment gateway",
        MockDashboard = "Payments dashboard",
        MockToday = "Today",
        MockSuccess = "Success",
        MockAmount = "Amount",
        MockChooseProvider = "Choose provider",
        ContactEyebrow = "Contact",
        ContactTitle = "Contact us",
        ContactSubtitle = "The Fynexpay team is ready to help with integration, activation, or any payment questions.",
        ContactEmailLabel = "Email",
        ContactEmail = "hello@fynexpay.iq",
        ContactPhoneLabel = "Phone",
        ContactPhone = "+964 770 000 0000",
        ContactAddressLabel = "Address",
        ContactAddress = "Baghdad, Iraq",
        ContactHoursLabel = "Working hours",
        ContactHours = "Sun – Thu · 9:00 – 17:00",
        ContactFormName = "Name",
        ContactFormEmail = "Your email",
        ContactFormMessage = "Your message",
        ContactFormSubmit = "Send message",
        ContactFormNote = "This opens your email client to send the message to our support team.",
        ContactFormSuccess = "Message drafted — finish sending from your email app."
    };
}
