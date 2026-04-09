using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.RevokeTenantPlan;

public sealed record RevokeTenantPlanCommand(Guid TenantPlanId) : ICommand;
