using FluentValidation;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.CreateTenant;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Slug).NotEmpty().MinimumLength(2).MaximumLength(63);
    }
}
