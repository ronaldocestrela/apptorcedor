using FluentValidation;

namespace SocioTorcedor.Modules.Identity.Application.Commands.PublishLegalDocumentVersion;

public sealed class PublishLegalDocumentVersionCommandValidator : AbstractValidator<PublishLegalDocumentVersionCommand>
{
    public PublishLegalDocumentVersionCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(500_000);
    }
}
