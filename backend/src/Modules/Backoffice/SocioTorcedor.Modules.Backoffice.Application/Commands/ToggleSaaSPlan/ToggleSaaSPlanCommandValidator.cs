using FluentValidation;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.ToggleSaaSPlan;

public sealed class ToggleSaaSPlanCommandValidator : AbstractValidator<ToggleSaaSPlanCommand>
{
    public ToggleSaaSPlanCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
