using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Queries.GetMemberPlanById;

public sealed class GetMemberPlanByIdHandler(
    IMemberPlanRepository repository,
    ICurrentTenantContext tenantContext) : IQueryHandler<GetMemberPlanByIdQuery, MemberPlanDto>
{
    public async Task<Result<MemberPlanDto>> Handle(
        GetMemberPlanByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result<MemberPlanDto>.Fail(
                Error.Failure("Tenant.Required", "Tenant context is not resolved."));

        var plan = await repository.GetByIdAsync(query.PlanId, cancellationToken);
        if (plan is null)
            return Result<MemberPlanDto>.Fail(
                Error.NotFound("Membership.PlanNotFound", "Member plan was not found."));

        return Result<MemberPlanDto>.Ok(plan.ToDto());
    }
}
