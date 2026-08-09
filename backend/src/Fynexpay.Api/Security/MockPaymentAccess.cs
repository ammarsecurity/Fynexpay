namespace Fynexpay.Api.Security;

public static class MockPaymentAccess
{
    public static bool IsAllowed(IHostEnvironment env, IConfiguration config)
        => env.IsDevelopment()
           && string.Equals(config["Security:AllowMockPayments"], "true", StringComparison.OrdinalIgnoreCase);
}
