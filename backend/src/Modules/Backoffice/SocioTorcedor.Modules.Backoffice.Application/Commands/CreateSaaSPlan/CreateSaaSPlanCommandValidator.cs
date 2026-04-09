using FluentValidation;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.CreateSaaSPlan;

public sealed class CreateSaaSPlanCommandValidator : AbstractValidator<CreateSaaSPlanCommand>
{
    public CreateSaaSPlanCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.MonthlyPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.YearlyPrice).GreaterThanOrEqualTo(0).When(x => x.YearlyPrice.HasValue);
        RuleFor(x => x.MaxMembers).GreaterThanOrEqualTo(0);
    }
}
