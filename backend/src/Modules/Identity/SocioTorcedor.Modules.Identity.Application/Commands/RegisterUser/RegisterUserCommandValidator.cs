using FluentValidation;

namespace SocioTorcedor.Modules.Identity.Application.Commands.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.AcceptedTermsDocumentId).NotEmpty();
        RuleFor(x => x.AcceptedPrivacyDocumentId).NotEmpty();
    }
}
