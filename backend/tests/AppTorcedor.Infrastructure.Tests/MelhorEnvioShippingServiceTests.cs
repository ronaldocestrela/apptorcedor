using System.Net;
using System.Text;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Services.Shipping;
using Microsoft.Extensions.Logging.Abstractions;
namespace AppTorcedor.Infrastructure.Tests;

public sealed class MelhorEnvioShippingServiceTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }

        public Func<HttpRequestMessage, string> ResponseFactory { get; init; } = _ => "[]";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var json = ResponseFactory(request);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }

    [Fact]
    public async Task Calculate_returns_empty_when_to_cep_invalid()
    {
        var handler = new FakeHandler();
        var client = new HttpClient(handler);
        var opts = Microsoft.Extensions.Options.Options.Create(
            new AppTorcedor.Infrastructure.Options.MelhorEnvioOptions { Token = "x", FromPostalCode = "44085520", UserAgent = "T" });
        var sut = new MelhorEnvioShippingService(client, opts, NullLogger<MelhorEnvioShippingService>.Instance);
        Assert.Empty(await sut.CalculateAsync("123"));
        Assert.Empty(await sut.CalculateAsync(""));
    }

    [Fact]
    public async Task Calculate_returns_empty_when_token_missing()
    {
        var handler = new FakeHandler();
        var client = new HttpClient(handler);
        var opts = Microsoft.Extensions.Options.Options.Create(new AppTorcedor.Infrastructure.Options.MelhorEnvioOptions { Token = "", FromPostalCode = "44085520" });
        var sut = new MelhorEnvioShippingService(client, opts, NullLogger<MelhorEnvioShippingService>.Instance);
        var r = await sut.CalculateAsync("44088698");
        Assert.Empty(r);
    }

    [Fact]
    public async Task Calculate_maps_rows_and_filters_errors()
    {
        const string json =
            """
            [
              {"id":1,"name":"X","error":"no route"},
              {"id":2,"name":"SEDEX","price":"12.50","company":{"name":"Correios","picture":"http://x"},"delivery_range":{"max":3}}
            ]
            """;
        var handler = new FakeHandler { ResponseFactory = _ => json };
        var client = new HttpClient(handler);
        var opts = Microsoft.Extensions.Options.Options.Create(
            new AppTorcedor.Infrastructure.Options.MelhorEnvioOptions
            {
                Token = "t",
                FromPostalCode = "44085520",
                UserAgent = "App",
                PackageHeight = 4,
                PackageWidth = 12,
                PackageLength = 17,
                PackageWeight = 0.3m,
            });
        var sut = new MelhorEnvioShippingService(client, opts, NullLogger<MelhorEnvioShippingService>.Instance);

        IReadOnlyList<ShippingOptionDto> r = await sut.CalculateAsync("44088698");

        Assert.Single(r);
        Assert.Equal(2, r[0].ServiceId);
        Assert.Equal("SEDEX", r[0].ServiceName);
        Assert.Equal("Correios", r[0].CarrierName);
        Assert.Equal("http://x", r[0].PictureUrl);
        Assert.Equal(12.50m, r[0].Price);
        Assert.Equal(3, r[0].DeliveryDays);

        Assert.Contains("postal_code", handler.LastRequestBody ?? "", StringComparison.Ordinal);
        Assert.Contains("44085520", handler.LastRequestBody ?? "", StringComparison.Ordinal);
        Assert.Contains("44088698", handler.LastRequestBody ?? "", StringComparison.Ordinal);
    }
}
