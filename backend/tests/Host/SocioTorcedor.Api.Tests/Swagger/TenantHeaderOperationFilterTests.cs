using System.Runtime.Serialization;
using FluentAssertions;
using Microsoft.OpenApi.Models;
using SocioTorcedor.Api.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SocioTorcedor.Api.Tests.Swagger;

public sealed class TenantHeaderOperationFilterTests
{
    // OperationFilterContext is not read by TenantHeaderOperationFilter.Apply; ctor args are heavy to fake in unit tests.
#pragma warning disable SYSLIB0050 // FormatterServices — test-only uninitialized instance
    private static OperationFilterContext CreateUnusedOperationFilterContext() =>
        (OperationFilterContext)FormatterServices.GetUninitializedObject(typeof(OperationFilterContext));
#pragma warning restore SYSLIB0050

    [Fact]
    public void Apply_AddsXTenantIdParameter()
    {
        var filter = new TenantHeaderOperationFilter();
        var operation = new OpenApiOperation();
        var context = CreateUnusedOperationFilterContext();

        filter.Apply(operation, context);

        operation.Parameters.Should().NotBeNull();
        operation.Parameters.Should().ContainSingle();
        var param = operation.Parameters!.Single();
        param.Name.Should().Be("X-Tenant-Id");
        param.In.Should().Be(ParameterLocation.Header);
        param.Required.Should().BeTrue();
        param.Schema.Should().NotBeNull();
        param.Schema!.Type.Should().Be("string");
        param.Description.Should().Contain("tenant");
    }

    [Fact]
    public void Apply_DoesNotDuplicateParameter()
    {
        var filter = new TenantHeaderOperationFilter();
        var operation = new OpenApiOperation();
        var context = CreateUnusedOperationFilterContext();

        filter.Apply(operation, context);
        filter.Apply(operation, context);

        operation.Parameters.Should().NotBeNull();
        operation.Parameters!.Count(p =>
            string.Equals(p.Name, "X-Tenant-Id", StringComparison.OrdinalIgnoreCase) &&
            p.In == ParameterLocation.Header).Should().Be(1);
    }
}
