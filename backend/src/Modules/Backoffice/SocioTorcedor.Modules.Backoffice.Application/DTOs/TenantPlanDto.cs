using SocioTorcedor.Modules.Backoffice.Domain.Enums;

namespace SocioTorcedor.Modules.Backoffice.Application.DTOs;

public sealed record TenantPlanDto(
    Guid Id,
    Guid TenantId,
    Guid SaaSPlanId,
    string PlanName,
    DateTime StartDate,
    DateTime? EndDate,
    TenantPlanStatus Status,
    BillingCycle BillingCycle);
