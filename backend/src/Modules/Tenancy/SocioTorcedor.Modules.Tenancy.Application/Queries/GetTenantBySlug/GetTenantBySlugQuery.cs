using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Modules.Tenancy.Application.Queries.GetTenantBySlug;

public sealed record GetTenantBySlugQuery(string Slug) : IQuery<TenantContext>;
