using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Tests.TestSupport;

/// <summary>Sem entradas — útil para testes que usam apenas fallback de template.</summary>
public sealed class EmptyAppConfigurationPort : IAppConfigurationPort
{
    public Task<IReadOnlyList<AppConfigurationEntryDto>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AppConfigurationEntryDto>>([]);

    public Task<AppConfigurationEntryDto?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult<AppConfigurationEntryDto?>(null);

    public Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
