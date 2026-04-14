using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAppConfigurations;

public sealed class ListAppConfigurationsQueryHandler(IAppConfigurationPort configuration)
    : IRequestHandler<ListAppConfigurationsQuery, IReadOnlyList<AppConfigurationEntryDto>>
{
    public Task<IReadOnlyList<AppConfigurationEntryDto>> Handle(ListAppConfigurationsQuery request, CancellationToken cancellationToken) =>
        configuration.ListAsync(cancellationToken);
}
