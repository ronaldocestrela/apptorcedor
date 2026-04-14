using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppTorcedor.Api.Tests;

/// <summary>Enables LGPD seed required for public registration / C.1 tests without affecting Part B LGPD flows.</summary>
public class AppWebApplicationFactoryWithLegalSeed : AppWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Testing:SeedMinimalLegalDocuments", "true");
    }
}
