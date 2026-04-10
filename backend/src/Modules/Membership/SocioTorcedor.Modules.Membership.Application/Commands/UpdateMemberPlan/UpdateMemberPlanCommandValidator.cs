using FluentValidation;

namespace SocioTorcedor.Modules.Membership.Application.Commands.UpdateMemberPlan;

public sealed class UpdateMemberPlanCommandValidator : AbstractValidator<UpdateMemberPlanCommand>
{
    public UpdateMemberPlanCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(2000);
        RuleFor(x => x.Preco).GreaterThanOrEqualTo(0);
        When(x => x.Vantagens is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.Vantagens!).NotEmpty().MaximumLength(300);
        });
    }
}
