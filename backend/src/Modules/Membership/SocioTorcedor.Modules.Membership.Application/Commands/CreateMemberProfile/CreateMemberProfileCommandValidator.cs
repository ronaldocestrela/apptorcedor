using FluentValidation;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Application.Commands.CreateMemberProfile;

public sealed class CreateMemberProfileCommandValidator : AbstractValidator<CreateMemberProfileCommand>
{
    public CreateMemberProfileCommandValidator()
    {
        RuleFor(x => x.Cpf).NotEmpty().Must(BeValidCpf).WithMessage("CPF is invalid.");
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

    private static bool BeValidCpf(string cpf)
    {
        try
        {
            _ = Cpf.Create(cpf);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
