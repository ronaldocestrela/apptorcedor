using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.Contracts;
using SocioTorcedor.Modules.Backoffice.Application.DTOs;

namespace SocioTorcedor.Modules.Backoffice.Application.Queries.GetSaaSPlanById;

public sealed class GetSaaSPlanByIdHandler(ISaaSPlanRepository repository)
    : IQueryHandler<GetSaaSPlanByIdQuery, SaaSPlanDetailDto>
{
    public async Task<Result<SaaSPlanDetailDto>> Handle(
        GetSaaSPlanByIdQuery query,
        CancellationToken cancellationToken)
    {
        var dto = await repository.GetDetailByIdAsync(query.Id, cancellationToken);
        if (dto is null)
            return Result<SaaSPlanDetailDto>.Fail(Error.NotFound("SaaSPlan.NotFound", "SaaS plan not found."));

        return Result<SaaSPlanDetailDto>.Ok(dto);
    }
}
