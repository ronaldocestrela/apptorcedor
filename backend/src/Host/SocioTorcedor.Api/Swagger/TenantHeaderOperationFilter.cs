using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SocioTorcedor.Api.Swagger;

/// <summary>
/// Adiciona o header <c>X-Tenant-Id</c> a todas as operações do OpenAPI para o Swagger UI enviar o slug do tenant.
/// </summary>
public sealed class TenantHeaderOperationFilter : IOperationFilter
{
    private const string HeaderName = "X-Tenant-Id";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);

        operation.Parameters ??= [];

        if (operation.Parameters.Any(p =>
                string.Equals(p.Name, HeaderName, StringComparison.OrdinalIgnoreCase) &&
                p.In == ParameterLocation.Header))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HeaderName,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Slug do tenant (ex: flamengo)",
            Schema = new OpenApiSchema { Type = "string" }
        });
    }
}
