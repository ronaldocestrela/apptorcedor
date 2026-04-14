using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Services.Payments;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class StripePaymentProviderTests
{
    [Fact]
    public async Task CreateCardAsync_throws_when_api_key_missing()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(
            new PaymentsOptions
            {
                Stripe = new PaymentsStripeOptions
                {
                    SuccessUrl = "https://example.com/ok",
                    CancelUrl = "https://example.com/cancel",
                },
            });
        var sut = new StripePaymentProvider(opts);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateCardAsync(Guid.NewGuid(), 10m, "BRL", CancellationToken.None));
    }

    [Fact]
    public void ProviderKey_is_Stripe()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new PaymentsOptions());
        var sut = new StripePaymentProvider(opts);
        Assert.Equal("Stripe", sut.ProviderKey);
    }
}
