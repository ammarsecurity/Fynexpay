namespace Fynexpay.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string email, string role, Guid? merchantId, string fullName);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IApiKeyService
{
    (string PlainKey, string Prefix, string Hash) Generate(bool isTest = false);
    string Hash(string plainKey);
    bool Verify(string plainKey, string hash);
}

public interface IMerchantWebhookSender
{
    Task SendPaymentUpdateAsync(Guid paymentId, CancellationToken ct = default);
}
