using Microsoft.AspNetCore.Hosting;

namespace AppTorcedor.Api.Tests;

/// <summary>
/// API de teste com CORS estático + cache dinâmico em 0s para leitura imediata do banco.
/// </summary>
public sealed class CorsHybridWebApplicationFactory : AppWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Cors:AllowedOrigins:0", "https://static-cors.example");
        builder.UseSetting("Cors:DynamicOriginsCacheSeconds", "0");
    }
}
