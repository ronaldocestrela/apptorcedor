using System.Net;
using AppTorcedor.Api.Controllers;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class StripeWebhookApiTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Stripe_webhook_returns_500_when_signing_secret_not_configured()
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe");
        req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
        Assert.True(res.Headers.TryGetValues(StripeWebhooksController.WebhookResultHeaderName, out var values));
        Assert.Equal("ConfigurationError", Assert.Single(values));
    }
}
