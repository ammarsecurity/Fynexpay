using System.Threading.RateLimiting;
using Fynexpay.Application;
using Fynexpay.Api.Background;
using Fynexpay.Api.Cors;
using Fynexpay.Api.Middleware;
using Fynexpay.Api.Swagger;
using Fynexpay.Infrastructure;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddHostedService<ExpiredCheckoutPurgeService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<ICorsPolicyProvider, DynamicCorsPolicyProvider>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 20;
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("webhooks", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 120;
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("api-keys", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 60;
        opt.QueueLimit = 0;
    });
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("merchant", new OpenApiInfo
    {
        Title = "Fynexpay Merchant API",
        Version = "v1",
        Description =
            "نفس مرجع صفحة دليل الربط في لوحة التاجر.\n\n" +
            "المصادقة: `X-Api-Key` (مفتاح المنصة المعتمدة).\n" +
            "المنصة تُستنتج من المفتاح — لا تُرسل merchantPlatformId في الـ body.\n" +
            "بعد الإنشاء: وجّه الزبون إلى `checkoutUrl`."
    });

    c.SchemaFilter<MerchantApiExamplesFilter>();
    c.OperationFilter<MerchantApiExamplesFilter>();

    // Public docs: Merchant API only — never expose dashboard/admin endpoints.
    c.DocInclusionPredicate((docName, apiDesc) =>
        string.Equals(docName, "merchant", StringComparison.OrdinalIgnoreCase)
        && string.Equals(apiDesc.GroupName, "merchant", StringComparison.OrdinalIgnoreCase));

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Merchant platform API key — header X-Api-Key",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    // Actual origins come from DynamicCorsPolicyProvider
    options.AddDefaultPolicy(_ => { });
});

var app = builder.Build();

try
{
    await Fynexpay.Infrastructure.DependencyInjection.SeedAsync(app.Services, app.Environment);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Database seed skipped — ensure MySQL is running and connection string is correct.");
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/merchant/swagger.json", "Merchant API");
    c.DocumentTitle = "Fynexpay Merchant API";
    c.DefaultModelsExpandDepth(-1);
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    c.EnableFilter();
    c.RoutePrefix = "swagger";
});

var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(uploadsRoot, "uploads", "providers"));
Directory.CreateDirectory(Path.Combine(uploadsRoot, "uploads", "platforms"));

app.UseRateLimiter();
app.UseCors();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsRoot),
    RequestPath = ""
});
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { service = "Fynexpay", status = "ok" }));

app.Run();

public partial class Program { }
