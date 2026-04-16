using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Branding;
using AppTorcedor.Application.Modules.Branding.Queries.GetPublicBranding;

namespace AppTorcedor.Application.Tests;

public sealed class GetPublicBrandingQueryHandlerTests
{
    [Fact]
    public async Task Returns_null_when_missing()
    {
        var fake = new FakeAppConfigurationPort();
        var handler = new GetPublicBrandingQueryHandler(fake);
        var dto = await handler.Handle(new GetPublicBrandingQuery(), CancellationToken.None);
        Assert.Null(dto.TeamShieldUrl);
    }

    [Fact]
    public async Task Returns_trimmed_url_when_configured()
    {
        var fake = new FakeAppConfigurationPort
        {
            Entry = new AppConfigurationEntryDto(BrandConfigurationKeys.TeamShieldUrl, "  /uploads/x.png  ", 1, DateTimeOffset.UtcNow, null),
        };
        var handler = new GetPublicBrandingQueryHandler(fake);
        var dto = await handler.Handle(new GetPublicBrandingQuery(), CancellationToken.None);
        Assert.Equal("/uploads/x.png", dto.TeamShieldUrl);
    }

    private sealed class FakeAppConfigurationPort : IAppConfigurationPort
    {
        public AppConfigurationEntryDto? Entry { get; init; }

        public Task<IReadOnlyList<AppConfigurationEntryDto>> ListAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<AppConfigurationEntryDto?> GetAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(key == BrandConfigurationKeys.TeamShieldUrl ? Entry : null);

        public Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
