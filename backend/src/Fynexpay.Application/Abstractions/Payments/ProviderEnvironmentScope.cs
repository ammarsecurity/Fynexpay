namespace Fynexpay.Application.Abstractions.Payments;

/// <summary>
/// Per-async-flow override so provider HTTP clients use Test or Production
/// credentials for a specific payment (not the admin global toggle).
/// </summary>
public static class ProviderEnvironmentScope
{
    private static readonly AsyncLocal<ProviderEnvironment?> Override = new();

    public static ProviderEnvironment? Current => Override.Value;

    public static IDisposable Use(ProviderEnvironment environment)
    {
        var previous = Override.Value;
        Override.Value = environment;
        return new Resetter(previous);
    }

    public static IDisposable Use(bool isTest) =>
        Use(isTest ? ProviderEnvironment.Test : ProviderEnvironment.Production);

    private sealed class Resetter(ProviderEnvironment? previous) : IDisposable
    {
        public void Dispose() => Override.Value = previous;
    }
}
