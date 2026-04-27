using AppTorcedor.Api.Tests.Support;
using AppTorcedor.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppTorcedor.Api.Tests;

/// <summary>Factory que substitui <see cref="IEmailSender"/> por instância capturável para testes de recuperação de senha.</summary>
public sealed class PasswordResetWebApplicationFactory : AppWebApplicationFactory
{
    public CapturingEmailSender EmailCapture { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<CapturingEmailSender>(_ => EmailCapture);
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<CapturingEmailSender>());
        });
    }
}
