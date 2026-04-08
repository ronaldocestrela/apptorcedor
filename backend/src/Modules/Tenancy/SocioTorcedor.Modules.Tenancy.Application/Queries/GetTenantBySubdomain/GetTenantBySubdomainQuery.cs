using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Modules.Tenancy.Application.Queries.GetTenantBySubdomain;

public sealed record GetTenantBySubdomainQuery(string Subdomain) : IQuery<TenantContext>;
