using System.Reflection;
using System.Runtime.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using NSubstitute;
using SocioTorcedor.Api.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SocioTorcedor.Api.Tests.Swagger;

public sealed class TenantHeaderOperationFilterTests
{
    private static readonly MethodInfo ApplyMethod =
        typeof(TenantHeaderOperationFilter).GetMethod(
            nameof(TenantHeaderOperationFilter.Apply),
            BindingFlags.Public | BindingFlags.Instance)!;

#pragma warning disable SYSLIB0050 // FormatterServices — test-only uninitialized instance
    private static OperationFilterContext CreateUnusedOperationFilterContext() =>
        (OperationFilterContext)FormatterServices.GetUninitializedObject(typeof(OperationFilterContext));
#pragma warning restore SYSLIB0050

    private static OperationFilterContext CreateContext(string relativePath)
    {
        var apiDescription = new ApiDescription { RelativePath = relativePath };
        var schemaGenerator = Substitute.For<ISchemaGenerator>();
        return new OperationFilterContext(apiDescription, schemaGenerator, new SchemaRepository(), new OpenApiDocument(), ApplyMethod);
    }

    [Fact]
    public void Apply_AddsXTenantIdParameter()
    {
        var filter = new TenantHeaderOperationFilter();
        var operation = new OpenApiOperation();
        var context = CreateContext("api/Auth/login");

        filter.Apply(operation, context);

        operation.Parameters.Should().NotBeNull();
        operation.Parameters.Should().ContainSingle();
        var param = operation.Parameters!.Single();
        param.Name.Should().Be("X-Tenant-Id");
        param.In.Should().Be(ParameterLocation.Header);
        param.Required.Should().BeTrue();
        param.Schema.Should().NotBeNull();
        param.Schema!.Type.Should().Be(JsonSchemaType.String);
        param.Description.Should().Contain("tenant");
    }

    [Fact]
    public void Apply_DoesNotDuplicateParameter()
    {
        var filter = new TenantHeaderOperationFilter();
        var operation = new OpenApiOperation();
        var context = CreateContext("api/Auth/register");

        filter.Apply(operation, context);
        filter.Apply(operation, context);

        operation.Parameters.Should().NotBeNull();
        operation.Parameters!.Count(p =>
            string.Equals(p.Name, "X-Tenant-Id", StringComparison.OrdinalIgnoreCase) &&
            p.In == ParameterLocation.Header).Should().Be(1);
    }

    [Fact]
    public void Apply_SkipsXTenantId_for_backoffice_routes()
    {
        var filter = new TenantHeaderOperationFilter();
        var operation = new OpenApiOperation();
        var context = CreateContext("api/backoffice/tenants");

        filter.Apply(operation, context);

        (operation.Parameters ?? []).Should().NotContain(p =>
            string.Equals(p.Name, "X-Tenant-Id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Apply_WithUninitializedContext_still_adds_parameter_when_api_description_missing()
    {
        var filter = new TenantHeaderOperationFilter();
        var operation = new OpenApiOperation();
        var context = CreateUnusedOperationFilterContext();

        filter.Apply(operation, context);

        operation.Parameters.Should().NotBeNull();
        operation.Parameters!.Should().ContainSingle(p => p.Name == "X-Tenant-Id");
    }
}
