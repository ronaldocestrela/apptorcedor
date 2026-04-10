using FluentValidation;

namespace SocioTorcedor.Modules.Membership.Application.Commands.ChangeMemberStatus;

public sealed class ChangeMemberStatusCommandValidator : AbstractValidator<ChangeMemberStatusCommand>
{
    public ChangeMemberStatusCommandValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
    }
}
