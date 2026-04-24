using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Services.Payments;
using Microsoft.EntityFrameworkCore;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class AsaasPaymentProviderTests
{
    [Fact]
    public void ProviderKey_is_Asaas()
    {
        var http = new HttpClient { BaseAddress = new Uri("https://api-sandbox.asaas.com/") };
        var opts = MsOptions.Create(new PaymentsOptions());
        using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var sut = new AsaasPaymentProvider(http, opts, db);
        Assert.Equal("Asaas", sut.ProviderKey);
    }

    [Fact]
    public async Task CreateCardAsync_throws_when_api_key_missing()
    {
        var http = new HttpClient { BaseAddress = new Uri("https://api-sandbox.asaas.com/") };
        var opts = MsOptions.Create(
            new PaymentsOptions
            {
                Asaas = new PaymentsAsaasOptions { SuccessUrl = "https://example.com/ok" },
            });
        using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var sut = new AsaasPaymentProvider(http, opts, db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateCardAsync(Guid.NewGuid(), 10m, "BRL", maxInstallments: null, CancellationToken.None));
    }

    [Fact]
    public async Task CreateCardAsync_throws_when_success_url_missing()
    {
        var http = new HttpClient { BaseAddress = new Uri("https://api-sandbox.asaas.com/") };
        var opts = MsOptions.Create(
            new PaymentsOptions
            {
                Asaas = new PaymentsAsaasOptions { ApiKey = "test_token" },
            });
        using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var sut = new AsaasPaymentProvider(http, opts, db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateCardAsync(Guid.NewGuid(), 10m, "BRL", maxInstallments: null, CancellationToken.None));
    }
}
