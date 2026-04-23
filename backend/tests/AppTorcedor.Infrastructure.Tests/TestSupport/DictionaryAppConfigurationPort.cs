using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Tests.TestSupport;

public sealed class DictionaryAppConfigurationPort : IAppConfigurationPort
{
    private readonly IReadOnlyDictionary<string, string> _values;

    public DictionaryAppConfigurationPort(IReadOnlyDictionary<string, string> values) =>
        _values = values;

    public Task<IReadOnlyList<AppConfigurationEntryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = _values
            .Select(
                kv => new AppConfigurationEntryDto(kv.Key, kv.Value, 1, DateTimeOffset.UtcNow, null))
            .ToList();
        return Task.FromResult<IReadOnlyList<AppConfigurationEntryDto>>(list);
    }

    public Task<AppConfigurationEntryDto?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_values.TryGetValue(key, out var v))
            return Task.FromResult<AppConfigurationEntryDto?>(null);
        return Task.FromResult<AppConfigurationEntryDto?>(new AppConfigurationEntryDto(key, v, 1, DateTimeOffset.UtcNow, null));
    }

    public Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
