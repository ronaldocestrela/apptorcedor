using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Modules.Tenancy.Application.Queries.GetTenantById;

public sealed record GetTenantByIdQuery(Guid TenantId) : IQuery<TenantDetailDto>;
