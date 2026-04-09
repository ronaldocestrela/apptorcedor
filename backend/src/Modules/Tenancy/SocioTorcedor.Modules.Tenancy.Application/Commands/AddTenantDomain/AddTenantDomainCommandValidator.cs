using FluentValidation;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.AddTenantDomain;

public sealed class AddTenantDomainCommandValidator : AbstractValidator<AddTenantDomainCommand>
{
    public AddTenantDomainCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Origin).NotEmpty().MaximumLength(2048);
    }
}
