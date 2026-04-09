using FluentValidation;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.AssignPlanToTenant;

public sealed class AssignPlanToTenantCommandValidator : AbstractValidator<AssignPlanToTenantCommand>
{
    public AssignPlanToTenantCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.SaaSPlanId).NotEmpty();
    }
}
