using Fynexpay.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fynexpay.Api.Background;

/// <summary>
/// Periodically deletes Pending/Expired checkout payments that exceeded the 1-hour TTL.
/// </summary>
public sealed class ExpiredCheckoutPurgeService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ExpiredCheckoutPurgeService> _logger;

    public ExpiredCheckoutPurgeService(IServiceScopeFactory scopes, ILogger<ExpiredCheckoutPurgeService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the app a moment to finish startup / schema ensure.
        try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var payments = scope.ServiceProvider.GetRequiredService<PaymentService>();
                var removed = await payments.PurgeExpiredIncompleteCheckoutsAsync(stoppingToken);
                if (removed > 0)
                    _logger.LogInformation("Purged {Count} expired incomplete checkout payment(s)", removed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Expired checkout purge failed");
            }

            try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
