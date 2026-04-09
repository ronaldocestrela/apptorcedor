using FluentValidation;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.UpdateSaaSPlan;

public sealed class UpdateSaaSPlanCommandValidator : AbstractValidator<UpdateSaaSPlanCommand>
{
    public UpdateSaaSPlanCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.MonthlyPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.YearlyPrice).GreaterThanOrEqualTo(0).When(x => x.YearlyPrice.HasValue);
        RuleFor(x => x.MaxMembers).GreaterThanOrEqualTo(0);
    }
}
