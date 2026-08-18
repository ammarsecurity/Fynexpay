namespace Fynexpay.Application.Security;

public class OtpRequiredException : InvalidOperationException
{
    public OtpRequiredException()
        : base("يجب تأكيد رمز التحقق أولاً عبر /api/auth/login/send-otp ثم verify")
    {
    }
}
