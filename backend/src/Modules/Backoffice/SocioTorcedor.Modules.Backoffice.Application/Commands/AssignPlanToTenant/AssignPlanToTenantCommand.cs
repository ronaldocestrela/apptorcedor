using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Backoffice.Domain.Enums;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.AssignPlanToTenant;

public sealed record AssignPlanToTenantCommand(
    Guid TenantId,
    Guid SaaSPlanId,
    DateTime StartDate,
    DateTime? EndDate,
    BillingCycle BillingCycle) : ICommand<Guid>;
