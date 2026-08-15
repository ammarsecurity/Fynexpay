namespace Fynexpay.Application.DTOs;

public static class LegalContentDefaults
{
    public static LegalBundleDto Arabic() => new()
    {
        Terms = new LegalPageDto
        {
            Nav = "الشروط والأحكام",
            Title = "شروط الخدمة",
            Updated = "آخر تحديث: ١٤ آب ٢٠٢٦",
            TocTitle = "في هذه الصفحة",
            Intro = "مرحباً بك في Fynexpay («فينكس باي»، «نحن»). تحكم شروط الخدمة هذه وصولك إلى خدمات تجميع الدفع والموقع ولوحة التحكم وواجهة البرمجة والخدمات المرتبطة بها. باستخدامك الخدمات فإنك توافق على هذه الشروط.",
            Sections =
            [
                S("1. قبول الشروط", "بإنشاء حساب أو استخدام المنصة فإنك تقر بقراءة هذه الشروط وسياسة الخصوصية والموافقة عليهما. إذا كنت تمثّل شركة فإنك تؤكد أن لديك صلاحية إلزام ذلك الكيان."),
                S("2. وصف الخدمات", "توفر Fynexpay منصة تجميع دفع للتجار في العراق، تشمل على سبيل المثال:",
                    "إنشاء روابط وصفحات دفع مستضافة",
                    "واجهة برمجة موحّدة للربط مع المتاجر والتطبيقات",
                    "محفظة تاجر وعمولة منصة تُحتسب تلقائياً",
                    "طلبات سحب إلى الحساب البنكي المسجّل",
                    "إشعارات Webhook موقّعة عند تغيّر حالة الدفعة"),
                S("3. تسجيل الحساب والأمان", "يجب تقديم بيانات صحيحة وكاملة، بما فيها الاسم بالعربية والإنجليزية وبيانات التواصل ومستندات التحقق عند الطلب. أنت مسؤول عن سرية بيانات الدخول وإبلاغنا فوراً بأي استخدام غير مصرّح."),
                S("4. الرسوم والتسويات", "تُحتسب عمولة المنصة وفق النسب المعروضة في لوحة التاجر وقد تتغيّر بإشعار مسبق. تُضاف صافي المبالغ الناجحة إلى محفظة التاجر، ويتم تحويل طلبات السحب بعد المراجعة إلى الحساب البنكي المحفوظ. التاجر مسؤول عن الاسترداد والنزاعات مع زبائنه."),
                S("5. التزامات التاجر", "يلتزم التاجر بالامتثال للقوانين العراقية النافذة، وبيع منتجات وخدمات مشروعة فقط، وتقديم وصف دقيق، ومعالجة شكاوى الزبائن بمهنية، وعدم استخدام المنصة في أنشطة محظورة."),
                S("6. الأنشطة المحظورة", "يُحظر استخدام الخدمات لأي غرض غير قانوني أو احتيالي أو لغسل الأموال أو انتهاك حقوق الغير أو محاولة اختراق الأنظمة. راجع صفحة المنتجات المحظورة للتفاصيل."),
                S("7. الملكية الفكرية", "شعارات Fynexpay وعلامتها ومحتوى المنصة ملك لنا أو لمرخّصينا. لا يجوز نسخها أو استخدامها بما يوحي بشراكة دون موافقة كتابية. يحتفظ التاجر بملكية محتواه ويمنحنا ترخيصاً لعرضه لتشغيل الخدمة."),
                S("8. تحديد المسؤولية", "إلى أقصى حد يسمح به القانون، لا نتحمل مسؤولية الأضرار غير المباشرة أو فوات الربح الناتجة عن انقطاع مزودي الدفع أو أخطاء التاجر أو نزاعات الزبائن. لا تتجاوز مسؤوليتنا الإجمالية الرسوم التي دفعتها للمنصة خلال الاثني عشر شهراً السابقة للمطالبة."),
                S("9. التعويض", "توافق على تعويض Fynexpay والعاملين فيها عن أي مطالبات أو خسائر ناشئة عن استخدامك للخدمات أو انتهاكك لهذه الشروط أو حقوق الغير."),
                S("10. الإنهاء", "يجوز تعليق أو إنهاء الحساب عند المخالفة أو بطلب منك. عند الإنهاء يتوقف الوصول فوراً وتُعالَج الأرصدة وطلبات السحب وفق إجراءات المراجعة المعتادة."),
                S("11. القانون الحاكم", "تخضع هذه الشروط لقوانين جمهورية العراق، وتكون محاكم بغداد صاحبة الاختصاص في أي نزاع."),
                S("12. التغييرات", "قد نحدّث هذه الشروط من وقت لآخر. يُنشر التحديث على الموقع مع تاريخ «آخر تحديث». استمرار الاستخدام بعد النشر يُعدّ قبولاً للنسخة الجديدة."),
                S("13. اتصل بنا", "للاستفسارات القانونية: legal@fynexpay.net — الموقع: https://fynexpay.net — بغداد، العراق.")
            ]
        },
        Privacy = new LegalPageDto
        {
            Nav = "سياسة الخصوصية",
            Title = "سياسة الخصوصية",
            Updated = "آخر تحديث: ١٤ آب ٢٠٢٦",
            TocTitle = "في هذه الصفحة",
            Intro = "تحترم Fynexpay خصوصيتك. توضّح هذه السياسة كيف نجمع بياناتك ونستخدمها ونحميها عند استخدام موقعنا ولوحة التحكم وخدمات الدفع.",
            Sections =
            [
                S("1. المعلومات التي نجمعها", "نجمع ما يلزم لتشغيل الحساب ومعالجة المدفوعات والامتثال:",
                    "الاسم والبريد ورقم الهاتف وبيانات النشاط",
                    "مستندات التحقق من الهوية (الهوية والجواز الإلكتروني)",
                    "بيانات الحساب البنكي لطلبات السحب",
                    "تفاصيل المدفوعات (المبلغ، الحالة، المزود، المعرّفات)",
                    "سجلات تقنية محدودة مثل عنوان IP والجهاز لأغراض الأمان"),
                S("2. كيف نستخدم معلوماتك", "نعالج البيانات لإنشاء الحساب وتأكيد الهوية عبر واتساب، ومعالجة المدفوعات والسحوبات، ومنع الاحتيال، وتقديم الدعم، والامتثال للمتطلبات القانونية، وإرسال إشعارات تشغيلية هامة."),
                S("3. المشاركة والإفصاح", "قد نشارك بيانات لازمة مع مزودي الدفع المحليين لتنفيذ العملية، ومع مزودي البنية التحتية والرسائل (مثل واتساب/SMTP) لتشغيل الخدمة، ومع الجهات المختصة عند وجوب القانون. لا نبيع بياناتك لأغراض تسويقية لأطراف ثالثة."),
                S("4. أمن البيانات", "نستخدم اتصالاً مشفّراً، وتقييد صلاحيات الوصول، وتوقيع Webhooks، ومراجعة طلبات السحب. بيانات البطاقة إن وُجدت تُعالَج لدى مزود الدفع ولا تُخزَّن كاملة على خوادمنا."),
                S("5. الاحتفاظ بالبيانات", "نحتفظ ببيانات الحساب طالما الحساب نشطاً، وبسجلات المعاملات للمدة التي تقتضيها الالتزامات المحاسبية والقانونية وحل النزاعات."),
                S("6. حقوقك", "يمكنك طلب الاطلاع على بياناتك أو تصحيحها أو حذف ما لا يتعارض مع الاحتفاظ القانوني، عبر التواصل مع فريق الدعم من لوحة الحساب أو البريد privacy@fynexpay.net."),
                S("7. ملفات تعريف الارتباط", "نستخدم ملفات ضرورية لتسجيل الدخول وتفضيل اللغة. يمكنك التحكم بها من المتصفح، وقد يتأثر عمل اللوحة عند تعطيل الملفات الأساسية."),
                S("8. خصوصية الأطفال", "الخدمات موجّهة للأعمال والأفراد البالغين 18 عاماً فأكثر. لا نجمع عن قصد بيانات الأطفال."),
                S("9. التغييرات", "قد نحدّث هذه السياسة مع نشر التاريخ الجديد على هذه الصفحة. ننصح بمراجعتها دورياً."),
                S("10. اتصل بنا", "لأسئلة الخصوصية: privacy@fynexpay.net — https://fynexpay.net — بغداد، العراق.")
            ]
        },
        Prohibited = new LegalPageDto
        {
            Nav = "المنتجات المحظورة",
            Title = "المنتجات والأنشطة المحظورة",
            Updated = "ساري اعتباراً من ١٤ آب ٢٠٢٦",
            TocTitle = "في هذه الصفحة",
            Intro = "نعمل على إبقاء بيئة الدفع آمنة ومتوافقة مع القوانين وشركاء الدفع. بعض الأنشطة والمنتجات غير مسموح بتحصيل ثمنها عبر Fynexpay.",
            Sections =
            [
                S("مقدمة", "بعض القيود تأتي من المتطلبات التنظيمية وشبكات البطاقات والمحافظ المحلية. باستخدامك المنصة فإنك توافق على عدم تحصيل مدفوعات لأي نشاط محظور هنا أو يخالف قانون العراق."),
                S("التنفيذ", "نحتفظ بحق إيقاف الحساب أو رفض الدفعات التي نرى أنها تخالف هذه السياسة. التكرار قد يؤدي إلى إنهاء الحساب وحجز الأرصدة للمراجعة."),
                S("منتجات مقيّدة بالعمر", "",
                    "السجائر الإلكترونية والفيب",
                    "المشروبات الكحولية والمخدرات",
                    "معدات صناعة أو تعاطي المواد المحظورة"),
                S("أنشطة مالية عالية المخاطر", "",
                    "العملات المشفرة وأي نشاط متعلق بها",
                    "التداول أو الفوركس غير المرخّص",
                    "التسويق الهرمي ومتعدد المستويات",
                    "بيع بطاقات مسبقة الدفع أو اشتراكات منصات رقمية بشكل غير مصرّح"),
                S("احتيال رقمي", "",
                    "بيع حسابات المنصات أو نقلها",
                    "خدمات زيادة المتابعين أو التفاعل الوهمي",
                    "الغش الأكاديمي وبيع الاختبارات"),
                S("محتوى وخدمات أخرى محظورة", "",
                    "الأسلحة والمواد الضارة",
                    "المحتوى العنيف أو المحرّض على الكراهية",
                    "انتهاك حقوق الملكية الفكرية",
                    "الهويات أو الوثائق المزيفة",
                    "الادعاءات العلاجية غير المثبتة وخدمات الدجل"),
                S("اتصل بنا", "للاستفسار عما إذا كان نشاطك مسموحاً: legal@fynexpay.net أو من صفحة التواصل.")
            ]
        },
        Brand = new LegalPageDto
        {
            Nav = "أصول العلامة",
            Title = "إرشادات علامة Fynexpay",
            Updated = "موارد لعرض العلامة بثبات واحتراف.",
            TocTitle = "في هذه الصفحة",
            Intro = "استخدم الأصول الرسمية فقط. لا تعدّل النسب أو الألوان بما يوحي بشراكة أو ترخيص لم نوافق عليه كتابياً.",
            Sections =
            [
                S("التسمية", "يُكتب الاسم Fynexpay كلمة واحدة بحرف F كبير. في العربية يمكن استخدام «فينكس باي». لا تختصر الاسم إلى صيغة تسويقية غير رسمية في المستندات القانونية."),
                S("الاستخدام", "اترك مساحة فارغة حول الشعار لا تقل عن ارتفاع حرف F في الشعار. لا تضع الشعار داخل اسم منتجك أو شركتك، ولا على خلفيات مزدحمة تمنع قراءته."),
                S("الألوان", "اللون الأساسي للعلامة هو الأزرق الداكن #031838. استخدمه على خلفيات فاتحة، والنسخة البيضاء من الشعار على الخلفيات الداكنة."),
                S("ما يُسمح به", "",
                    "استخدام الملفات الرسمية من هذه الصفحة أو حزمة محدّثة من الفريق",
                    "الحفاظ على التناسب والتباين الكافي",
                    "كتابة الاسم Fynexpay كما هو ما لم يُطلب الاسم القانوني الكامل"),
                S("ما لا يُسمح به", "",
                    "تمديد الشعار أو تدويره أو إضافة ظلال وتأثيرات",
                    "تغيير ألوان الشعار",
                    "الإيحاء بشراكة أو اعتماد دون موافقة خطية"),
                S("طلب موافقة", "للصحافة أو الفعاليات أو الاستخدام المشترك راسل press@fynexpay.net.")
            ]
        },
        Company = new CompanyPageDto
        {
            Nav = "الشركة",
            Title = "منصة متاجر إلكترونية صُممت للعراق.",
            Updated = "كيان تشغيلي لمنصة متاجر FynexPay.",
            Intro = "هذه الصفحة تعرّف التاجر من يقف وراء متجر FynexPay. الدفع مدمج داخل متاجر المنصة وليس بوابة مستقلة. حدّث البيانات القانونية من لوحة الإدارة عند اكتمال التسجيل الرسمي.",
            RegistrationTitle = "التسجيل",
            IraqTitle = "العراق",
            IraqLegalNameLabel = "الكيان القانوني",
            IraqLegalName = "Fynexpay — يُحدَّث الاسم التجاري الرسمي من لوحة الإدارة",
            IraqRegistryLabel = "السجل التجاري",
            IraqRegistry = "يُضاف رقم التسجيل عند اكتماله",
            IraqHqLabel = "المقر",
            IraqHq = "بغداد، العراق",
            CertsTitle = "الامتثال والتشغيل",
            CertsBody = "نعمل كمنصة تجميع مع مزودي دفع محليين مرخّصين. حدّث شهاداتك الفعلية هنا عند الحصول عليها.",
            Certs =
            [
                S("تجميع دفع محلي", "تمر المدفوعات عبر مزودين محليين حسب ما تفعّله الإدارة لكل تاجر، مع عمولة منصة واضحة في اللوحة."),
                S("تحقق من الهوية", "نطلب مستندات الهوية والجواز الإلكتروني ومراجعة إدارية قبل تفعيل بعض الصلاحيات."),
                S("حماية التشغيل", "تشفير النقل، مفاتيح API، وتوقيع Webhooks، ومراجعة طلبات السحب قبل التحويل.")
            ],
            ContactTitle = "تواصل الشركة",
            ContactEmail = "hello@fynexpay.net",
            ContactPhone = "07809726258",
            ContactWebsite = "https://fynexpay.net",
            Disclaimer = "موقع Fynexpay وأي محتوى فيه غير معتمد أو مرتبط مباشرة بأي مؤسسة مالية ما لم يُذكر خلاف ذلك صراحة. جميع أسماء المزودين والشعارات المذكورة ملك لأصحابها."
        }
    };

    public static LegalBundleDto English() => new()
    {
        Terms = new LegalPageDto
        {
            Nav = "Terms",
            Title = "Terms of service",
            Updated = "Last updated: 14 Aug 2026",
            TocTitle = "On this page",
            Intro = "Welcome to Fynexpay (“we”, “us”). These Terms govern your access to our payment aggregation platform, website, dashboard, API, and related services. By using the services you agree to these Terms.",
            Sections =
            [
                S("1. Acceptance", "By creating an account or using the platform you confirm that you have read these Terms and our Privacy Policy. If you represent a company, you confirm you have authority to bind that entity."),
                S("2. Services", "Fynexpay provides payment aggregation for merchants in Iraq, including:",
                    "Hosted checkout pages and payment links",
                    "A unified API for websites and apps",
                    "Merchant wallets with automatic platform commission",
                    "Payout requests to a saved bank account",
                    "Signed webhooks when payment status changes"),
                S("3. Account & security", "You must provide accurate details, including Arabic and English names, contact data, and KYC documents when requested. You are responsible for login credentials and must notify us of unauthorized access."),
                S("4. Fees & settlement", "Platform commission follows the rates shown in the merchant dashboard and may change with notice. Successful payments credit the merchant wallet net of fees. Payouts are reviewed then sent to the saved bank account. You remain responsible for customer refunds and disputes."),
                S("5. Merchant obligations", "You must comply with applicable Iraqi law, sell only lawful goods and services, describe them accurately, handle complaints professionally, and avoid prohibited activities."),
                S("6. Prohibited use", "You may not use the services for illegal, fraudulent, or money-laundering purposes, to infringe others’ rights, or to attack our systems. See the Prohibited products page."),
                S("7. Intellectual property", "Fynexpay marks and platform content are owned by us or our licensors. Do not copy them or imply a partnership without written consent. You keep ownership of your content and grant us a license to operate the service."),
                S("8. Limitation of liability", "To the fullest extent permitted by law we are not liable for indirect damages or lost profits arising from provider outages, merchant errors, or customer disputes. Our aggregate liability will not exceed fees you paid us in the 12 months before the claim."),
                S("9. Indemnity", "You agree to indemnify Fynexpay and its staff against claims arising from your use of the services or your breach of these Terms or third-party rights."),
                S("10. Termination", "We may suspend or terminate an account for breach, and you may close yours. Access ends immediately; balances and payouts are handled under our review process."),
                S("11. Governing law", "These Terms are governed by the laws of the Republic of Iraq. Courts of Baghdad have exclusive jurisdiction."),
                S("12. Changes", "We may update these Terms and post the new “Last updated” date. Continued use after publication constitutes acceptance."),
                S("13. Contact", "Legal questions: legal@fynexpay.net — https://fynexpay.net — Baghdad, Iraq.")
            ]
        },
        Privacy = new LegalPageDto
        {
            Nav = "Privacy",
            Title = "Privacy policy",
            Updated = "Last updated: 14 Aug 2026",
            TocTitle = "On this page",
            Intro = "Fynexpay respects your privacy. This policy explains how we collect, use, and protect information when you use our website, dashboard, and payment services.",
            Sections =
            [
                S("1. Information we collect", "We collect what we need to run accounts and payments:",
                    "Name, email, phone, and business details",
                    "KYC documents (national ID and e-passport)",
                    "Bank account details for payouts",
                    "Payment records (amount, status, provider, identifiers)",
                    "Limited technical logs such as IP and device for security"),
                S("2. How we use it", "We use data to create accounts, send WhatsApp OTPs, process payments and payouts, prevent fraud, provide support, meet legal duties, and send operational notices."),
                S("3. Sharing", "We may share necessary data with local payment providers to complete a transaction, infrastructure and messaging vendors (WhatsApp/SMTP), and authorities when required by law. We do not sell your data for third-party marketing."),
                S("4. Security", "We use encrypted transport, access controls, signed webhooks, and payout review. Full card data, if any, is handled by the payment provider and is not stored in full on our servers."),
                S("5. Retention", "We keep account data while the account is active, and transaction records for as long as accounting, legal, and dispute needs require."),
                S("6. Your rights", "You may request access, correction, or deletion of data that is not subject to legal retention, via support or privacy@fynexpay.net."),
                S("7. Cookies", "We use essential cookies for sign-in and language. Disabling them may break the dashboard."),
                S("8. Children", "Services are for businesses and adults 18+. We do not knowingly collect children’s data."),
                S("9. Changes", "We may update this policy and change the date on this page. Please review it periodically."),
                S("10. Contact", "Privacy questions: privacy@fynexpay.net — https://fynexpay.net — Baghdad, Iraq.")
            ]
        },
        Prohibited = new LegalPageDto
        {
            Nav = "Prohibited",
            Title = "Prohibited products & activities",
            Updated = "Effective 14 Aug 2026",
            TocTitle = "On this page",
            Intro = "We keep checkout safe and compatible with local laws and payment partners. Some activities cannot be collected through Fynexpay.",
            Sections =
            [
                S("Overview", "Some restrictions come from regulation, card networks, and local wallets. By using the platform you agree not to collect payments for prohibited or unlawful activity in Iraq."),
                S("Enforcement", "We may pause accounts or decline payments that appear to violate this policy. Repeat violations may lead to termination and held balances pending review."),
                S("Age-restricted", "",
                    "E-cigarettes and vaping products",
                    "Alcohol and illegal drugs",
                    "Equipment for producing or using banned substances"),
                S("High-risk finance", "",
                    "Cryptocurrency and related activity",
                    "Unlicensed trading or forex",
                    "Pyramid / multi-level marketing",
                    "Unauthorized gift cards or platform subscriptions"),
                S("Digital fraud", "",
                    "Selling or transferring online accounts",
                    "Fake followers or engagement",
                    "Academic cheating and exam sales"),
                S("Other prohibited", "",
                    "Weapons and harmful materials",
                    "Violent or hate content",
                    "IP infringement",
                    "Fake IDs or documents",
                    "Unproven medical claims and occult services"),
                S("Contact", "To ask whether your activity is allowed: legal@fynexpay.net or the contact page.")
            ]
        },
        Brand = new LegalPageDto
        {
            Nav = "Brand",
            Title = "Fynexpay brand guidelines",
            Updated = "Resources for using the mark consistently.",
            TocTitle = "On this page",
            Intro = "Use official assets only. Do not alter proportions or colors in a way that implies a partnership we have not approved in writing.",
            Sections =
            [
                S("Name", "Write Fynexpay as one word with a capital F. In Arabic you may use «فينكس باي». Do not invent unofficial legal abbreviations."),
                S("Clear space", "Keep empty space around the logo at least the height of the F. Do not place the mark inside your product name or on busy photos."),
                S("Color", "Primary brand color is #031838. Use the dark logo on light backgrounds and the white logo on dark backgrounds."),
                S("Do", "",
                    "Use official files from this page or an updated pack from our team",
                    "Keep aspect ratio and contrast",
                    "Write Fynexpay unless the full legal name is required"),
                S("Don’t", "",
                    "Stretch, rotate, or add effects to the logo",
                    "Recolor the mark",
                    "Imply partnership or endorsement without written approval"),
                S("Approvals", "For press, events, or co-marketing email press@fynexpay.net.")
            ]
        },
        Company = new CompanyPageDto
        {
            Nav = "Company",
            Title = "An e-commerce store platform, built for Iraq.",
            Updated = "Operating entity for the FynexPay store platform.",
            Intro = "This page tells merchants who stands behind FynexPay stores. Checkout is built into platform stores, not offered as a standalone gateway. Update official registry details from the admin panel when registration is complete.",
            RegistrationTitle = "Registration",
            IraqTitle = "Iraq",
            IraqLegalNameLabel = "Legal entity",
            IraqLegalName = "Fynexpay — set the official legal name in the admin panel",
            IraqRegistryLabel = "Commercial registry",
            IraqRegistry = "Add the registration number when available",
            IraqHqLabel = "Headquarters",
            IraqHq = "Baghdad, Iraq",
            CertsTitle = "Operations & controls",
            CertsBody = "We aggregate licensed local payment methods. Replace these notes with real certifications when you obtain them.",
            Certs =
            [
                S("Local aggregation", "Payments go through local providers enabled by the platform admin, with clear commission in the dashboard."),
                S("Identity checks", "We collect ID and e-passport documents and review them before some account privileges."),
                S("Operational safeguards", "Encrypted transport, API keys, signed webhooks, and payout review before bank transfer.")
            ],
            ContactTitle = "Company contact",
            ContactEmail = "hello@fynexpay.net",
            ContactPhone = "07809726258",
            ContactWebsite = "https://fynexpay.net",
            Disclaimer = "The Fynexpay website and its content are not endorsed by or affiliated with any financial institution unless explicitly stated. Provider names and logos remain the property of their owners."
        }
    };

    private static LegalSectionDto S(string heading, string body, params string[] items) => new()
    {
        Heading = heading,
        Body = body,
        Items = [.. items]
    };
}
