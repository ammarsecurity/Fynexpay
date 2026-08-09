using System.Security.Cryptography;
using System.Text;
using Fynexpay.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Fynexpay.Infrastructure.Security;

public sealed class SecretProtector : ISecretProtector
{
    private readonly byte[] _key;

    public SecretProtector(IConfiguration configuration)
    {
        var material = configuration["App:EncryptionKey"]
            ?? configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("App:EncryptionKey or Jwt:Key must be configured for secret protection.");
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(material));
    }

    public string Protect(string plainText)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainText);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(plainText);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plain, cipher, tag);
        var packed = new byte[nonce.Length + tag.Length + cipher.Length];
        Buffer.BlockCopy(nonce, 0, packed, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, packed, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipher, 0, packed, nonce.Length + tag.Length, cipher.Length);
        return "enc:" + Convert.ToBase64String(packed);
    }

    public string Unprotect(string protectedText)
    {
        ArgumentException.ThrowIfNullOrEmpty(protectedText);
        if (!protectedText.StartsWith("enc:", StringComparison.Ordinal))
        {
            // Legacy plaintext rows — return as-is then caller should clear/re-issue.
            return protectedText;
        }

        var packed = Convert.FromBase64String(protectedText["enc:".Length..]);
        if (packed.Length < 12 + 16 + 1)
            throw new CryptographicException("Invalid protected payload");

        var nonce = packed.AsSpan(0, 12);
        var tag = packed.AsSpan(12, 16);
        var cipher = packed.AsSpan(28);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
