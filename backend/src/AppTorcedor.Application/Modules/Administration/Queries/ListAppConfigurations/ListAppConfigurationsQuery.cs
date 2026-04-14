using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAppConfigurations;

public sealed record ListAppConfigurationsQuery : IRequest<IReadOnlyList<AppConfigurationEntryDto>>;
