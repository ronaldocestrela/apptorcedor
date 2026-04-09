using Microsoft.OpenApi;
using SocioTorcedor.Api.Extensions;
using SocioTorcedor.Api.Swagger;
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
    // OpenApi3_0 is serialized as "openapi":"3.0.4" by Microsoft.OpenApi 2.x; stale Swagger UI can reject unknown 3.0.x (hard-refresh: Ctrl+Shift+R).
    // OpenApi3_1 is opt-in in Swashbuckle 10 and matches swagger-ui-dist 5.32+ bundled with the package.
    app.UseSwagger(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);
    app.UseSwaggerUI(options =>
    {
        // Path from site root avoids wrong resolution behind some proxies; relative "v1/swagger.json" is also valid.
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Socio Torcedor API v1");
        options.UseSocioTorcedorSwaggerDarkMode();
    });
}

app.UseSocioTorcedorMiddleware();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .AllowAnonymous()
    .WithTags("Health");

app.MapControllers();

app.Run();

public partial class Program;
