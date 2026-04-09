using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Modules.Tenancy.Application.Queries.GetTenantById;

public sealed class GetTenantByIdHandler(ITenantRepository repository)
    : IQueryHandler<GetTenantByIdQuery, TenantDetailDto>
{
    public async Task<Result<TenantDetailDto>> Handle(GetTenantByIdQuery query, CancellationToken cancellationToken)
    {
        var dto = await repository.GetByIdAsync(query.TenantId, cancellationToken);
        if (dto is null)
            return Result<TenantDetailDto>.Fail(Error.NotFound("Tenant.NotFound", "Tenant not found."));

        return Result<TenantDetailDto>.Ok(dto);
    }
}
