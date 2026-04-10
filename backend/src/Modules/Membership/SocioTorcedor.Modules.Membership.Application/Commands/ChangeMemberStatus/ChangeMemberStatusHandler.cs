using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Commands.ChangeMemberStatus;

public sealed class ChangeMemberStatusHandler(
    IMemberProfileRepository repository,
    ICurrentTenantContext tenantContext) : ICommandHandler<ChangeMemberStatusCommand, MemberProfileDto>
{
    public async Task<Result<MemberProfileDto>> Handle(
        ChangeMemberStatusCommand command,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result<MemberProfileDto>.Fail(
                Error.Failure("Tenant.Required", "Tenant context is not resolved."));

        var profile = await repository.GetTrackedByIdAsync(command.MemberId, cancellationToken);
        if (profile is null)
            return Result<MemberProfileDto>.Fail(
                Error.NotFound("Membership.ProfileNotFound", "Member profile was not found."));

        try
        {
            profile.ChangeStatus(command.Status);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<MemberProfileDto>.Fail(
                Error.Validation("Membership.InvalidStatusTransition", ex.Message));
        }

        await repository.SaveChangesAsync(cancellationToken);
        return Result<MemberProfileDto>.Ok(profile.ToDto());
    }
}
