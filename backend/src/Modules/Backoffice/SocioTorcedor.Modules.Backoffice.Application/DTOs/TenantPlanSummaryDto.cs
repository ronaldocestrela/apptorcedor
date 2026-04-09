using SocioTorcedor.Modules.Backoffice.Domain.Enums;

namespace SocioTorcedor.Modules.Backoffice.Application.DTOs;

public sealed record TenantPlanSummaryDto(
    Guid TenantId,
    string TenantName,
    DateTime StartDate,
    TenantPlanStatus Status);
