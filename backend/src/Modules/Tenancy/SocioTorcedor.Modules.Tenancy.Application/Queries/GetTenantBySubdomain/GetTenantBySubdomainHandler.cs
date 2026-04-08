using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Modules.Tenancy.Application.Queries.GetTenantBySubdomain;

public sealed class GetTenantBySubdomainHandler(ITenantRepository repository)
    : IQueryHandler<GetTenantBySubdomainQuery, TenantContext>
{
    public async Task<Result<TenantContext>> Handle(
        GetTenantBySubdomainQuery query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Subdomain))
            return Result<TenantContext>.Fail(Error.Validation(nameof(query.Subdomain), "Subdomain is required."));

        var dto = await repository.GetBySubdomainAsync(query.Subdomain.Trim(), cancellationToken);
        if (dto is null)
            return Result<TenantContext>.Fail(Error.NotFound("Tenant.NotFound", "Tenant not found for subdomain."));

        var context = new TenantContext(
            dto.TenantId,
            dto.Name,
            dto.Subdomain,
            dto.ConnectionString,
            dto.AllowedOrigins);

        return Result<TenantContext>.Ok(context);
    }
}
