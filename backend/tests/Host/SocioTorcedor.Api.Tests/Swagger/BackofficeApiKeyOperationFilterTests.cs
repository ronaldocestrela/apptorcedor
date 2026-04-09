using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using NSubstitute;
using SocioTorcedor.Api.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SocioTorcedor.Api.Tests.Swagger;

public sealed class BackofficeApiKeyOperationFilterTests
{
    private static readonly MethodInfo ApplyMethod =
        typeof(BackofficeApiKeyOperationFilter).GetMethod(
            nameof(BackofficeApiKeyOperationFilter.Apply),
            BindingFlags.Public | BindingFlags.Instance)!;

    private static OperationFilterContext CreateContext(string relativePath)
    {
        var apiDescription = new ApiDescription { RelativePath = relativePath };
        var schemaGenerator = Substitute.For<ISchemaGenerator>();
        return new OperationFilterContext(apiDescription, schemaGenerator, new SchemaRepository(), ApplyMethod);
    }

    [Fact]
    public void Apply_Sets_security_for_backoffice_path()
    {
        var filter = new BackofficeApiKeyOperationFilter();
        var operation = new OpenApiOperation();
        var context = CreateContext("api/backoffice/plans");

        filter.Apply(operation, context);

        operation.Security.Should().NotBeNull();
        operation.Security!.Should().ContainSingle();
        var requirement = operation.Security.Single();
        requirement.Should().ContainSingle();
        var scheme = requirement.Keys.Single();
        scheme.Reference.Should().NotBeNull();
        scheme.Reference!.Type.Should().Be(ReferenceType.SecurityScheme);
        scheme.Reference.Id.Should().Be(BackofficeApiKeyOperationFilter.SecuritySchemeId);
    }

    [Fact]
    public void Apply_Does_not_set_security_for_tenant_routes()
    {
        var filter = new BackofficeApiKeyOperationFilter();
        var operation = new OpenApiOperation();
        var context = CreateContext("api/Auth/login");

        filter.Apply(operation, context);

        (operation.Security?.Count ?? 0).Should().Be(0);
    }
}
