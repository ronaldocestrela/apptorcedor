using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppTorcedor.Api.Tests;

public sealed class AppWebApplicationFactoryWithFakeGoogle : AppWebApplicationFactoryWithLegalSeed
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(AppTorcedor.Api.Services.IGoogleIdTokenValidator));
            services.AddSingleton<AppTorcedor.Api.Services.IGoogleIdTokenValidator, FakeGoogleIdTokenValidator>();
        });
    }
}
