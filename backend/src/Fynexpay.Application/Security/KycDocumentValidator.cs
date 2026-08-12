namespace Fynexpay.Application.Security;

/// <summary>Validates KYC document images: JPG/PNG/WEBP up to 5MB.</summary>
public static class KycDocumentValidator
{
    public const int MaxBytes = 5_000_000;

    public static string ValidateAndGetExtension(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 12)
            throw new ArgumentException("ملف الصورة غير صالح");

        if (bytes.Length > MaxBytes)
            throw new ArgumentException("حجم الصورة كبير جداً (الحد 5MB)");

        if (IsPng(bytes)) return ".png";
        if (IsJpeg(bytes)) return ".jpg";
        if (IsWebp(bytes)) return ".webp";
        throw new ArgumentException("الصيغة المسموحة: JPG أو PNG أو WEBP فقط");
    }

    private static bool IsPng(ReadOnlySpan<byte> b) =>
        b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47;

    private static bool IsJpeg(ReadOnlySpan<byte> b) =>
        b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;

    private static bool IsWebp(ReadOnlySpan<byte> b) =>
        b.Length >= 12
        && b[0] == (byte)'R' && b[1] == (byte)'I' && b[2] == (byte)'F' && b[3] == (byte)'F'
        && b[8] == (byte)'W' && b[9] == (byte)'E' && b[10] == (byte)'B' && b[11] == (byte)'P';
}
