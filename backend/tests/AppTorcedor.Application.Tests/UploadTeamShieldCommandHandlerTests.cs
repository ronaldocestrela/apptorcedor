using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Branding;
using AppTorcedor.Application.Modules.Branding.Commands.UploadTeamShield;

namespace AppTorcedor.Application.Tests;

public sealed class UploadTeamShieldCommandHandlerTests
{
    [Fact]
    public async Task Handle_persists_url_and_deletes_previous_when_different()
    {
        var storage = new FakeTeamShieldStorage { NextUrl = "/uploads/team-shield/a.jpg" };
        var config = new FakeAppConfigurationPort
        {
            Entries = new Dictionary<string, AppConfigurationEntryDto>
            {
                [BrandConfigurationKeys.TeamShieldUrl] = new(
                    BrandConfigurationKeys.TeamShieldUrl,
                    "/uploads/team-shield/old.jpg",
                    1,
                    DateTimeOffset.UtcNow,
                    null),
            },
        };
        var handler = new UploadTeamShieldCommandHandler(storage, config);
        await using var stream = new MemoryStream([1, 2, 3]);
        var result = await handler.Handle(new UploadTeamShieldCommand(stream, "s.png", "image/png"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("/uploads/team-shield/a.jpg", result!.TeamShieldUrl);
        Assert.Single(config.UpsertCalls);
        Assert.Equal(BrandConfigurationKeys.TeamShieldUrl, config.UpsertCalls[0].Key);
        Assert.Equal("/uploads/team-shield/a.jpg", config.UpsertCalls[0].Value);
        Assert.Single(storage.DeleteCalls);
        Assert.Equal("/uploads/team-shield/old.jpg", storage.DeleteCalls[0]);
    }

    [Fact]
    public async Task Handle_returns_null_when_storage_rejects()
    {
        var storage = new FakeTeamShieldStorage { NextUrl = null };
        var config = new FakeAppConfigurationPort();
        var handler = new UploadTeamShieldCommandHandler(storage, config);
        await using var stream = new MemoryStream([1]);
        var result = await handler.Handle(new UploadTeamShieldCommand(stream, "s.jpg", "image/jpeg"), CancellationToken.None);

        Assert.Null(result);
        Assert.Empty(config.UpsertCalls);
    }

    private sealed class FakeTeamShieldStorage : ITeamShieldStorage
    {
        public string? NextUrl { get; init; }
        public List<string> DeleteCalls { get; } = [];

        public Task<string?> SaveTeamShieldAsync(
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
            => Task.FromResult(NextUrl);

        public Task<bool> DeleteTeamShieldAsync(string shieldUrl, CancellationToken cancellationToken = default)
        {
            DeleteCalls.Add(shieldUrl);
            return Task.FromResult(true);
        }
    }

    private sealed class FakeAppConfigurationPort : IAppConfigurationPort
    {
        public Dictionary<string, AppConfigurationEntryDto> Entries { get; init; } = new();
        public List<(string Key, string Value)> UpsertCalls { get; } = [];

        public Task<IReadOnlyList<AppConfigurationEntryDto>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AppConfigurationEntryDto>>(Entries.Values.ToList());

        public Task<AppConfigurationEntryDto?> GetAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(Entries.TryGetValue(key, out var v) ? v : null);

        public Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            UpsertCalls.Add((key, value));
            Entries[key] = new AppConfigurationEntryDto(key, value, 1, DateTimeOffset.UtcNow, null);
            return Task.CompletedTask;
        }
    }
}
