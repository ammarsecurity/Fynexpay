using Fynexpay.Application.DTOs;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fynexpay.Api.Swagger;

/// <summary>
/// Keeps Merchant Swagger examples aligned with the Docs integration guide.
/// </summary>
public sealed class MerchantApiExamplesFilter : ISchemaFilter, IOperationFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(CreatePublicPaymentRequest))
        {
            schema.Description = "إنشاء دفعة — المنصة تُستنتج من X-Api-Key. لا ترسل merchantPlatformId.";
            schema.Example = new OpenApiObject
            {
                ["amount"] = new OpenApiInteger(5000),
                ["currency"] = new OpenApiString("IQD"),
                ["orderId"] = new OpenApiString("ORD-1001"),
                ["serviceType"] = new OpenApiString("Monthly subscription"),
                ["callbackUrl"] = new OpenApiString("https://shop.example.com/hooks/fynexpay"),
                ["successUrl"] = new OpenApiString("https://shop.example.com/success"),
                ["failureUrl"] = new OpenApiString("https://shop.example.com/failed")
            };
            schema.Required = new HashSet<string> { "amount", "serviceType" };
        }
        else if (context.Type == typeof(CreatePayoutRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["amount"] = new OpenApiInteger(100000)
            };
        }
        else if (context.Type == typeof(PublicPaymentDto))
        {
            schema.Example = new OpenApiObject
            {
                ["id"] = new OpenApiString("5bac8b83-0000-0000-0000-000000000001"),
                ["orderId"] = new OpenApiString("ORD-1001"),
                ["amount"] = new OpenApiInteger(5000),
                ["currency"] = new OpenApiString("IQD"),
                ["status"] = new OpenApiString("Pending"),
                ["provider"] = new OpenApiString("PendingSelection"),
                ["description"] = new OpenApiString("Monthly subscription"),
                ["checkoutUrl"] = new OpenApiString("http://localhost:5080/checkout/5bac8b83-0000-0000-0000-000000000001"),
                ["createdAtUtc"] = new OpenApiString("2026-08-11T19:40:36.7691927Z"),
                ["paidAtUtc"] = new OpenApiNull(),
                ["expiredAtUtc"] = new OpenApiString("2026-08-11T20:40:36.7691936Z"),
                ["failureReason"] = new OpenApiNull()
            };
        }
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.Equals(context.ApiDescription.GroupName, "merchant", StringComparison.OrdinalIgnoreCase))
            return;

        var path = context.ApiDescription.RelativePath ?? "";
        ApplySecurity(operation, path);

        if (path.StartsWith("v1/payments", StringComparison.OrdinalIgnoreCase)
            && string.Equals(context.ApiDescription.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("cancel", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "إنشاء دفعة";
            operation.Description =
                "أنشئ دفعة مربوطة بمنصة مفتاح الـ API. أعد توجيه الزبون إلى checkoutUrl ليختار المزود.\n\n" +
                "Headers المطلوبة: `Authorization: Bearer fx_merch_...`, `X-Api-Key`, `Content-Type: application/json`.\n" +
                "موصى به: `X-Idempotency-Key`, و`Origin` عند الاستدعاء من المتصفح (يجب أن يطابق دومين المنصة).";

            operation.Parameters ??= new List<OpenApiParameter>();
            EnsureHeader(operation, "X-Idempotency-Key", "مفتاح منع التكرار — مثال: order-1001", example: "order-1001");
            EnsureHeader(operation, "Origin", "أصل المتصفح إن وُجد — مثال: https://shop.example.com", example: "https://shop.example.com");
        }
        else if (path.StartsWith("v1/payments/{id}", StringComparison.OrdinalIgnoreCase)
                 && string.Equals(context.ApiDescription.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "حالة الدفعة";
            operation.Description =
                "استعلام حالة دفعة بالمعرّف. يتطلب Bearer التاجر و X-Api-Key للمنصة.\n" +
                "عند الاستدعاء من المتصفح أرسل `Origin` ليطابق دومين المنصة.";
            operation.Parameters ??= new List<OpenApiParameter>();
            EnsureHeader(operation, "Origin", "أصل المتصفح إن وُجد — مثال: https://shop.example.com", example: "https://shop.example.com");
        }
        else if (path.Contains("cancel", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "إلغاء دفعة";
            operation.Description = "إلغاء دفعة ما زالت Pending. يتطلب Bearer التاجر و X-Api-Key للمنصة.";
        }
        else if (path.StartsWith("v1/wallet", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "المحفظة";
            operation.Description = "رصيد التاجر والصافي المتاح للسحب. يتطلب `Authorization: Bearer fx_merch_...` فقط.";
        }
        else if (path.StartsWith("v1/payouts", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "طلب سحب";
            operation.Description = "إنشاء طلب سحب من الرصيد المتاح. يتطلب `Authorization: Bearer fx_merch_...` فقط.";
        }
    }

    private static void ApplySecurity(OpenApiOperation operation, string path)
    {
        var bearer = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };
        var requirement = new OpenApiSecurityRequirement { [bearer] = Array.Empty<string>() };

        if (path.StartsWith("v1/payments", StringComparison.OrdinalIgnoreCase))
        {
            var apiKey = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            };
            requirement[apiKey] = Array.Empty<string>();
        }

        operation.Security = new List<OpenApiSecurityRequirement> { requirement };
    }

    private static void EnsureHeader(OpenApiOperation operation, string name, string description, string example)
    {
        if (operation.Parameters.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            return;

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = name,
            In = ParameterLocation.Header,
            Required = false,
            Description = description,
            Schema = new OpenApiSchema { Type = "string", Example = new OpenApiString(example) }
        });
    }
}
