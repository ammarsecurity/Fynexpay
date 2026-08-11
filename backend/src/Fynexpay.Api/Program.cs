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

    c.SwaggerDoc("internal", new OpenApiInfo
    {
        Title = "Fynexpay Internal API",
        Version = "v1",
        Description = "واجهات لوحة التحكم والأدمن — للاستخدام الداخلي فقط."
    });

    // Merchant doc = only controllers marked GroupName = "merchant"
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        var group = apiDesc.GroupName ?? string.Empty;
        if (string.Equals(docName, "merchant", StringComparison.OrdinalIgnoreCase))
            return string.Equals(group, "merchant", StringComparison.OrdinalIgnoreCase);

        if (string.Equals(docName, "internal", StringComparison.OrdinalIgnoreCase))
            return !string.Equals(group, "merchant", StringComparison.OrdinalIgnoreCase);

        return false;
    });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Merchant platform API key — header X-Api-Key",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Dashboard JWT — Authorization: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Default security shown in UI; merchants use ApiKey on /v1.
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Merchant API first — what Docs / integration page should open
        c.SwaggerEndpoint("/swagger/merchant/swagger.json", "Merchant API");
        c.SwaggerEndpoint("/swagger/internal/swagger.json", "Internal (Dashboard/Admin)");
        c.DocumentTitle = "Fynexpay Merchant API";
        c.DefaultModelsExpandDepth(-1);
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        c.EnableFilter();
    });
}

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

app.MapGet("/", () => app.Environment.IsDevelopment()
    ? Results.Redirect("/swagger/index.html?urls.primaryName=Merchant%20API")
    : Results.Ok(new { service = "Fynexpay", status = "ok" }));

app.Run();

public partial class Program { }
