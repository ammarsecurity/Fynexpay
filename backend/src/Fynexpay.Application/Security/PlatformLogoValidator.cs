namespace Fynexpay.Application.Security;

/// <summary>
/// Validates platform logos: PNG only, exactly 500×500, with an alpha channel (cut-out / transparent).
/// </summary>
public static class PlatformLogoValidator
{
    public const int RequiredSize = 500;
    public const int MaxBytes = 1_500_000;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static void Validate(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 33)
            throw new ArgumentException("ملف الشعار غير صالح");

        if (bytes.Length > MaxBytes)
            throw new ArgumentException("حجم الشعار كبير جداً (الحد 1.5MB)");

        for (var i = 0; i < PngSignature.Length; i++)
        {
            if (bytes[i] != PngSignature[i])
                throw new ArgumentException("الشعار يجب أن يكون بصيغة PNG فقط");
        }

        // IHDR: length@8, type@12 ("IHDR"), width@16, height@20, bitDepth@24, colorType@25
        if (bytes[12] != (byte)'I' || bytes[13] != (byte)'H' || bytes[14] != (byte)'D' || bytes[15] != (byte)'R')
            throw new ArgumentException("ملف PNG تالف");

        var width = ReadInt32Be(bytes.Slice(16, 4));
        var height = ReadInt32Be(bytes.Slice(20, 4));
        if (width != RequiredSize || height != RequiredSize)
            throw new ArgumentException($"الشعار يجب أن يكون {RequiredSize}×{RequiredSize} بكسل");

        var colorType = bytes[25];
        // 4 = greyscale+alpha, 6 = truecolor+alpha (transparent / cut-out)
        if (colorType is not (4 or 6))
            throw new ArgumentException("الشعار يجب أن يكون PNG شفاف (قناة ألفا) — خلفية مفرّغة");
    }

    private static int ReadInt32Be(ReadOnlySpan<byte> span) =>
        (span[0] << 24) | (span[1] << 16) | (span[2] << 8) | span[3];
}
