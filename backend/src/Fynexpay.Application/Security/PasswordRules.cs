using System.Text.RegularExpressions;

namespace Fynexpay.Application.Security;

public static partial class PasswordRules
{
    public const int MinLength = 8;

    public static void Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
            throw new ArgumentException($"كلمة المرور يجب أن تكون {MinLength} أحرف على الأقل");

        if (!HasLetter().IsMatch(password) || !HasDigit().IsMatch(password))
            throw new ArgumentException("كلمة المرور يجب أن تحتوي على حرف ورقم على الأقل");
    }

    public static void ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || email.Length < 5)
            throw new ArgumentException("البريد الإلكتروني غير صالح");
    }

    public static void ValidateRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{fieldName} مطلوب");
    }

    [GeneratedRegex(@"[A-Za-z\u0600-\u06FF]")]
    private static partial Regex HasLetter();

    [GeneratedRegex(@"\d")]
    private static partial Regex HasDigit();
}
