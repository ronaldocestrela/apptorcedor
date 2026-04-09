using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Queries.GetMemberById;

public sealed class GetMemberByIdHandler(
    IMemberProfileRepository repository,
    ICurrentTenantContext tenantContext) : IQueryHandler<GetMemberByIdQuery, MemberProfileDto>
{
    public async Task<Result<MemberProfileDto>> Handle(
        GetMemberByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result<MemberProfileDto>.Fail(
                Error.Failure("Tenant.Required", "Tenant context is not resolved."));

        var profile = await repository.GetTrackedByIdAsync(query.MemberId, cancellationToken);
        if (profile is null)
            return Result<MemberProfileDto>.Fail(
                Error.NotFound("Membership.ProfileNotFound", "Member profile was not found."));

        return Result<MemberProfileDto>.Ok(profile.ToDto());
    }
}
