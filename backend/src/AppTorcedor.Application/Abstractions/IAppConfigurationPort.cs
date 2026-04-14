namespace AppTorcedor.Application.Abstractions;

public interface IAppConfigurationPort
{
    Task<IReadOnlyList<AppConfigurationEntryDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<AppConfigurationEntryDto?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default);
}

public sealed record AppConfigurationEntryDto(string Key, string Value, int Version, DateTimeOffset UpdatedAt, Guid? UpdatedByUserId);
