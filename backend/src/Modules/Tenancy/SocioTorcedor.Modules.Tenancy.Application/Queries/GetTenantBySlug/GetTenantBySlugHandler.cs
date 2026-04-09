using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Modules.Tenancy.Application.Queries.GetTenantBySlug;

public sealed class GetTenantBySlugHandler(ITenantRepository repository)
    : IQueryHandler<GetTenantBySlugQuery, TenantContext>
{
    public async Task<Result<TenantContext>> Handle(
        GetTenantBySlugQuery query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Slug))
            return Result<TenantContext>.Fail(Error.Validation(nameof(query.Slug), "Tenant slug is required."));

        var dto = await repository.GetBySlugAsync(query.Slug.Trim(), cancellationToken);
        if (dto is null)
            return Result<TenantContext>.Fail(Error.NotFound("Tenant.NotFound", "Tenant not found for slug."));

        var context = new TenantContext(
            dto.TenantId,
            dto.Name,
            dto.Slug,
            dto.ConnectionString,
            dto.AllowedOrigins);

        return Result<TenantContext>.Ok(context);
    }
}
