using Fynexpay.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fynexpay.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<WalletService>();
        services.AddScoped<PayoutService>();
        services.AddScoped<MerchantAdminService>();
        services.AddScoped<MerchantPlatformService>();
        services.AddScoped<LandingContentService>();
        services.AddScoped<OtpService>();
        services.AddScoped<NotificationService>();
        return services;
    }
}
