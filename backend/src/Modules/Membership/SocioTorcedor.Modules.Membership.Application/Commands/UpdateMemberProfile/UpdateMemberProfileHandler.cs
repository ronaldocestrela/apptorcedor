using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.DTOs;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Application.Commands.UpdateMemberProfile;

public sealed class UpdateMemberProfileHandler(
    IMemberProfileRepository repository,
    ICurrentUserAccessor currentUser,
    ICurrentTenantContext tenantContext) : ICommandHandler<UpdateMemberProfileCommand, MemberProfileDto>
{
    public async Task<Result<MemberProfileDto>> Handle(
        UpdateMemberProfileCommand command,
        CancellationToken cancellationToken)
    {
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

        Address address;
        try
        {
            address = Address.Create(
                command.Street,
                command.Number,
                command.Complement,
                command.Neighborhood,
                command.City,
                command.State,
                command.ZipCode);
        }
        catch (ArgumentException ex)
        {
            return Result<MemberProfileDto>.Fail(Error.Validation("Membership.InvalidInput", ex.Message));
        }

        try
        {
            profile.Update(command.DateOfBirth, command.Gender, command.Phone, address);
        }
        catch (ArgumentException ex)
        {
            return Result<MemberProfileDto>.Fail(Error.Validation("Membership.InvalidInput", ex.Message));
        }

        await repository.SaveChangesAsync(cancellationToken);

        return Result<MemberProfileDto>.Ok(profile.ToDto());
    }
}
