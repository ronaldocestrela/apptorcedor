using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Backoffice.Application.DTOs;

namespace SocioTorcedor.Modules.Backoffice.Application.Queries.GetSaaSPlanById;

public sealed record GetSaaSPlanByIdQuery(Guid Id) : IQuery<SaaSPlanDetailDto>;
