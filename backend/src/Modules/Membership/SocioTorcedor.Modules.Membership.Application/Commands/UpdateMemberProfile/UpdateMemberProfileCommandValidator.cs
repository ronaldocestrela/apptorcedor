using FluentValidation;

namespace SocioTorcedor.Modules.Membership.Application.Commands.UpdateMemberProfile;

public sealed class UpdateMemberProfileCommandValidator : AbstractValidator<UpdateMemberProfileCommand>
{
    public UpdateMemberProfileCommandValidator()
    {
        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.UtcNow.Date)
            .WithMessage("Date of birth must be in the past.");
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Number).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Complement).MaximumLength(120);
        RuleFor(x => x.Neighborhood).NotEmpty().MaximumLength(120);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.State).NotEmpty().Length(2);
        RuleFor(x => x.ZipCode).NotEmpty();
    }
}
