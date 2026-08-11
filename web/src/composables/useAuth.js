const copy = {
  ar: {
    loginTitle: 'تسجيل الدخول',
    loginSub: 'ادخل إلى لوحة التاجر أو الإدارة',
    registerTitle: 'إنشاء حساب تاجر',
    registerSub: 'سجّل متجرك وابدأ قبول المدفوعات خلال دقائق',
    email: 'البريد الإلكتروني',
    password: 'كلمة المرور',
    signIn: 'دخول',
    signingIn: 'جاري الدخول…',
    noAccount: 'ليس لديك حساب؟',
    registerLink: 'سجّل كتاجر',
    hasAccount: 'لديك حساب؟',
    loginLink: 'تسجيل الدخول',
    secureNote: 'اتصال آمن ومشفّر',
    fullName: 'الاسم الكامل',
    businessName: 'اسم النشاط',
    businessNameAr: 'اسم النشاط بالعربية',
    phone: 'رقم الجوال',
    website: 'الموقع',
    createAccount: 'إنشاء الحساب',
    sendCode: 'إرسال رمز التحقق',
    otpTitle: 'تأكيد الرمز',
    otpSub: 'أدخل الرمز المرسل إلى {phone}',
    otpCode: 'رمز التحقق',
    verify: 'تأكيد وإنشاء الحساب',
    resend: 'إعادة إرسال الرمز',
    back: 'العودة للنموذج',
    loginFail: 'تعذّر تسجيل الدخول',
    registerFail: 'تعذّر التسجيل',
    otpFail: 'رمز غير صحيح أو منتهي',
    loading: 'جاري التحميل…',
    whatsappRequired: 'يتطلب التسجيل تأكيد رقم الواتساب',
    emailRequired: 'يتطلب التسجيل تأكيد البريد',
    bothRequired: 'يتطلب التسجيل تأكيد الواتساب أو البريد',
    sideLoginTitle: 'مرحباً بعودتك',
    sideLoginBody: 'إدارة المدفوعات والمحفظة والتقارير من لوحة واحدة.',
    sideRegisterTitle: 'ابدأ مع Fynexpay',
    sideRegisterBody: 'اربط متجرك بـ API موحّد وامنح زبائنك تجربة دفع سلسة.',
    pointSecure: 'حماية وتشفير للبيانات',
    pointWallet: 'محفظة وصافي فوري',
    pointFast: 'تفعيل سريع للتاجر',
    backHome: 'العودة للرئيسية'
  },
  en: {
    loginTitle: 'Sign in',
    loginSub: 'Access the merchant or admin dashboard',
    registerTitle: 'Create merchant account',
    registerSub: 'Register your store and start accepting payments in minutes',
    email: 'Email',
    password: 'Password',
    signIn: 'Sign in',
    signingIn: 'Signing in…',
    noAccount: "Don't have an account?",
    registerLink: 'Register as merchant',
    hasAccount: 'Already have an account?',
    loginLink: 'Sign in',
    secureNote: 'Secure encrypted connection',
    fullName: 'Full name',
    businessName: 'Business name',
    businessNameAr: 'Business name (Arabic)',
    phone: 'Mobile number',
    website: 'Website',
    createAccount: 'Create account',
    sendCode: 'Send verification code',
    otpTitle: 'Verify code',
    otpSub: 'Enter the code sent to {phone}',
    otpCode: 'Verification code',
    verify: 'Verify & create account',
    resend: 'Resend code',
    back: 'Back to form',
    loginFail: 'Could not sign in',
    registerFail: 'Could not register',
    otpFail: 'Invalid or expired code',
    loading: 'Loading…',
    whatsappRequired: 'Registration requires WhatsApp verification',
    emailRequired: 'Registration requires email verification',
    bothRequired: 'Registration requires WhatsApp or email verification',
    sideLoginTitle: 'Welcome back',
    sideLoginBody: 'Manage payments, wallet, and reports from one dashboard.',
    sideRegisterTitle: 'Start with Fynexpay',
    sideRegisterBody: 'Connect your store with one API and give customers a smooth checkout.',
    pointSecure: 'Data protection & encryption',
    pointWallet: 'Wallet with instant net credit',
    pointFast: 'Fast merchant activation',
    backHome: 'Back to home'
  }
}

export function useAuthCopy(localeRef) {
  return {
    t: (key, params = {}) => {
      const pack = copy[localeRef.value === 'en' ? 'en' : 'ar'] || copy.ar
      let text = pack[key] || key
      for (const [k, v] of Object.entries(params)) {
        text = text.replace(`{${k}}`, v)
      }
      return text
    }
  }
}

export function handoffToDashboard(dashboardUrl, data) {
  const user = {
    userId: data.userId,
    email: data.email,
    fullName: data.fullName,
    role: data.role,
    merchantId: data.merchantId,
    merchantStatus: data.merchantStatus
  }
  const payload = btoa(unescape(encodeURIComponent(JSON.stringify({
    token: data.token,
    user
  }))))
  const base = String(dashboardUrl || 'http://localhost:5173').replace(/\/$/, '')
  window.location.href = `${base}/auth/handoff#${payload}`
}
