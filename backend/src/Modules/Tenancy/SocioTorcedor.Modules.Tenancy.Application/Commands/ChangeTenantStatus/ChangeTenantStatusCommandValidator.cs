using FluentValidation;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.ChangeTenantStatus;

public sealed class ChangeTenantStatusCommandValidator : AbstractValidator<ChangeTenantStatusCommand>
{
    public ChangeTenantStatusCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
