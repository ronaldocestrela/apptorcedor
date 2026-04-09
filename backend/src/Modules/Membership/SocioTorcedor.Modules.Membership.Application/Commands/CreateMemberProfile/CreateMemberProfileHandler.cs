using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.DTOs;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Application.Commands.CreateMemberProfile;

public sealed class CreateMemberProfileHandler(
    IMemberProfileRepository repository,
    ICurrentUserAccessor currentUser,
    ICurrentTenantContext tenantContext) : ICommandHandler<CreateMemberProfileCommand, MemberProfileDto>
{
    public async Task<Result<MemberProfileDto>> Handle(
        CreateMemberProfileCommand command,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result<MemberProfileDto>.Fail(
                Error.Failure("Tenant.Required", "Tenant context is not resolved."));

        var userId = currentUser.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Result<MemberProfileDto>.Fail(
                Error.Failure("Membership.UserRequired", "Authenticated user is required."));

        if (await repository.ExistsByUserIdAsync(userId, cancellationToken))
            return Result<MemberProfileDto>.Fail(
                Error.Conflict("Membership.ProfileExists", "Member profile already exists for this user."));

        Cpf cpf;
        Address address;
        try
        {
            cpf = Cpf.Create(command.Cpf);
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

        var cpfTaken = await repository.ExistsByCpfDigitsAsync(cpf.Digits, cancellationToken);

        MemberProfile profile;
        try
        {
            profile = MemberProfile.Create(
                userId,
                cpf,
                command.DateOfBirth,
                command.Gender,
                command.Phone,
                address,
                () => cpfTaken);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<MemberProfileDto>.Fail(Error.Conflict("Membership.CpfConflict", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result<MemberProfileDto>.Fail(Error.Validation("Membership.InvalidInput", ex.Message));
        }

        await repository.AddAsync(profile, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result<MemberProfileDto>.Ok(profile.ToDto());
    }
}
