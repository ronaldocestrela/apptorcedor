using Microsoft.OpenApi;
using Scalar.AspNetCore;
using SocioTorcedor.Api.Extensions;
using SocioTorcedor.Api.Options;
using SocioTorcedor.Api.Tenancy;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BackofficeOptions>(builder.Configuration.GetSection(BackofficeOptions.SectionName));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenantContext, HttpContextTenantContext>();
builder.Services.AddSocioTorcedorApi(builder.Configuration);

var app = builder.Build();

await app.ApplyPendingEfCoreMigrationsAsync();

var exposeOpenApi = app.Environment.IsDevelopment()
    || string.Equals(
        Environment.GetEnvironmentVariable("EXPOSE_OPENAPI_JSON"),
        "true",
        StringComparison.OrdinalIgnoreCase);

if (exposeOpenApi)
{
    // OpenAPI 3.1 (opt-in in Swashbuckle 10); Scalar consumes this spec from /swagger/v1/swagger.json.
    app.UseSwagger(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);
}

app.UseSocioTorcedorMiddleware();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .AllowAnonymous()
    .WithTags("Health");

app.MapControllers();

if (exposeOpenApi)
{
    app.MapScalarApiReference(options =>
            options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json"))
        .AllowAnonymous();
}

app.Run();

public partial class Program;
