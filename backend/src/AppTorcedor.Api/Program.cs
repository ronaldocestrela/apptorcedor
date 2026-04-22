using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Configuration;
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
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection(GoogleAuthOptions.SectionName));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();
builder.Services.AddSingleton<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
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
    options.AddPolicy(
        Policies.GamesOpponentLogosUpload,
        policy =>
            policy.RequireAssertion(ctx =>
                ctx.User.HasClaim(AppClaimTypes.Permission, ApplicationPermissions.JogosCriar)
                || ctx.User.HasClaim(AppClaimTypes.Permission, ApplicationPermissions.JogosEditar)));
});
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: ["ready"])
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

builder.Services.AddCors();
builder.Services.AddSingleton<IConfigureOptions<CorsOptions>, SpaCorsOptionsConfigure>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseCors("Spa");
var webRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(webRoot))
    Directory.CreateDirectory(webRoot);
app.UseStaticFiles();
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
