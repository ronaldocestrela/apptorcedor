using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpsertAppConfiguration;

public sealed record UpsertAppConfigurationCommand(string Key, string Value) : IRequest<AppConfigurationEntryDto?>;
