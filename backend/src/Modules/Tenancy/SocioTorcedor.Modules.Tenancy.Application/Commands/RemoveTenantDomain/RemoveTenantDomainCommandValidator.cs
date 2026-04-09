using FluentValidation;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.RemoveTenantDomain;

public sealed class RemoveTenantDomainCommandValidator : AbstractValidator<RemoveTenantDomainCommand>
{
    public RemoveTenantDomainCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.DomainId).NotEmpty();
    }
}
