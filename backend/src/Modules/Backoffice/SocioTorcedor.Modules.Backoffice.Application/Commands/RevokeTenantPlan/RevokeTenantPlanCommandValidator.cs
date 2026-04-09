using FluentValidation;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.RevokeTenantPlan;

public sealed class RevokeTenantPlanCommandValidator : AbstractValidator<RevokeTenantPlanCommand>
{
    public RevokeTenantPlanCommandValidator()
    {
        RuleFor(x => x.TenantPlanId).NotEmpty();
    }
}
