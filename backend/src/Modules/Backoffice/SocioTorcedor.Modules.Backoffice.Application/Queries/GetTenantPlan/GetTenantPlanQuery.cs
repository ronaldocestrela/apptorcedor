using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Backoffice.Application.DTOs;

namespace SocioTorcedor.Modules.Backoffice.Application.Queries.GetTenantPlan;

public sealed record GetTenantPlanQuery(Guid TenantId) : IQuery<TenantPlanDto>;
