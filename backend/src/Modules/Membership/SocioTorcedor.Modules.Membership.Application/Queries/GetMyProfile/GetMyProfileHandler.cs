using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Queries.GetMyProfile;

public sealed class GetMyProfileHandler(
    IMemberProfileRepository repository,
    ICurrentUserAccessor currentUser,
    ICurrentTenantContext tenantContext) : IQueryHandler<GetMyProfileQuery, MemberProfileDto>
{
    public async Task<Result<MemberProfileDto>> Handle(GetMyProfileQuery query, CancellationToken cancellationToken)
    {
        _ = query;

        if (!tenantContext.IsResolved)
            return Result<MemberProfileDto>.Fail(
                Error.Failure("Tenant.Required", "Tenant context is not resolved."));

        var userId = currentUser.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Result<MemberProfileDto>.Fail(
                Error.Failure("Membership.UserRequired", "Authenticated user is required."));

        var profile = await repository.GetTrackedByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result<MemberProfileDto>.Fail(
                Error.NotFound("Membership.ProfileNotFound", "Member profile was not found."));

        return Result<MemberProfileDto>.Ok(profile.ToDto());
    }
}
