using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Middleware;
using AppTorcedor.Api.Options;
using AppTorcedor.Api.Services;
using AppTorcedor.Application;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure;
using AppTorcedor.Infrastructure.Identity;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole(o =>
{
    o.IncludeScopes = true;
    o.TimestampFormat = "O";
});

builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();
builder.Services.AddScoped<IAuthService, AuthService>();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is required.");
if (string.IsNullOrWhiteSpace(jwt.Key) || Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 UTF-8 bytes.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        Policies.AdminDashboard,
        policy =>
            policy.RequireAssertion(ctx =>
                ctx.User.HasClaim(AppClaimTypes.Permission, ApplicationPermissions.UsuariosVisualizar)
                || ctx.User.HasClaim(AppClaimTypes.Permission, ApplicationPermissions.ConfiguracoesVisualizar)));
});
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: ["ready"])
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o =>
{
    o.AddPolicy(
        "Spa",
        p =>
        {
            if (allowedOrigins.Length == 0)
                p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            else
                p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseCors("Spa");
app.UseAuthentication();
app.UseMiddleware<RequestContextMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

await using (var scope = app.Services.CreateAsyncScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();
    var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    // Banco relacional (ex.: SQL Server): aplica todas as migrations pendentes a cada subida.
    // In-memory (testes / dev sem SQL): não há migrator; EnsureCreated monta o schema.
    if (db.Database.IsRelational())
    {
        log.LogInformation("Applying pending EF Core migrations...");
        await db.Database.MigrateAsync().ConfigureAwait(false);
        log.LogInformation("Database migrations are up to date.");
    }
    else
    {
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    await IdentityDataSeeder.SeedAsync(sp).ConfigureAwait(false);
}

await app.RunAsync().ConfigureAwait(false);

public partial class Program;
