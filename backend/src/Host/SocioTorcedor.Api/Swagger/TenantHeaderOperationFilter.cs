using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SocioTorcedor.Api.Swagger;

/// <summary>
/// Adiciona o header <c>X-Tenant-Id</c> às operações que exigem resolução de tenant (exclui <c>/api/backoffice/*</c>).
/// </summary>
public sealed class TenantHeaderOperationFilter : IOperationFilter
{
    private const string HeaderName = "X-Tenant-Id";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (IsBackofficeOperation(context))
            return;

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

    private static bool IsBackofficeOperation(OperationFilterContext context)
    {
        var path = context.ApiDescription?.RelativePath;
        return !string.IsNullOrEmpty(path) &&
               path.StartsWith("api/backoffice/", StringComparison.OrdinalIgnoreCase);
    }
}
