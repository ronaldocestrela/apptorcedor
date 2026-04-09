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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
