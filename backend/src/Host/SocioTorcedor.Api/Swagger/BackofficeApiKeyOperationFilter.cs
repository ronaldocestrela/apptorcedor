using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SocioTorcedor.Api.Swagger;

/// <summary>
/// Marca operações <c>/api/backoffice/*</c> como protegidas pelo esquema <c>BackofficeApiKey</c> (header <c>X-Api-Key</c>),
/// substituindo o requisito global JWT apenas nessas rotas.
/// </summary>
public sealed class BackofficeApiKeyOperationFilter : IOperationFilter
{
    public const string SecuritySchemeId = "BackofficeApiKey";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var path = context.ApiDescription?.RelativePath;
        if (string.IsNullOrEmpty(path) ||
            !path.StartsWith("api/backoffice/", StringComparison.OrdinalIgnoreCase))
            return;

        var schemeRef = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = SecuritySchemeId
            }
        };

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [schemeRef] = Array.Empty<string>()
            }
        ];
    }
}
