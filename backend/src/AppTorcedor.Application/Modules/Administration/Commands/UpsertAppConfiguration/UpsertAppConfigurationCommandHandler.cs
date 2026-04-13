using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpsertAppConfiguration;

public sealed class UpsertAppConfigurationCommandHandler(IAppConfigurationPort configuration)
    : IRequestHandler<UpsertAppConfigurationCommand, AppConfigurationEntryDto?>
{
    public async Task<AppConfigurationEntryDto?> Handle(UpsertAppConfigurationCommand request, CancellationToken cancellationToken)
    {
        await configuration.UpsertAsync(request.Key, request.Value, cancellationToken).ConfigureAwait(false);
        return await configuration.GetAsync(request.Key, cancellationToken).ConfigureAwait(false);
    }
}
