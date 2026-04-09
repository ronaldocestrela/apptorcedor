using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.ToggleSaaSPlan;

public sealed record ToggleSaaSPlanCommand(Guid Id) : ICommand;
