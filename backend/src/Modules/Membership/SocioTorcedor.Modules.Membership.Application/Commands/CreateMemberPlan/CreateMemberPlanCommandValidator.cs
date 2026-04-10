using FluentValidation;

namespace SocioTorcedor.Modules.Membership.Application.Commands.CreateMemberPlan;

public sealed class CreateMemberPlanCommandValidator : AbstractValidator<CreateMemberPlanCommand>
{
    public CreateMemberPlanCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(2000);
        RuleFor(x => x.Preco).GreaterThanOrEqualTo(0);
        When(x => x.Vantagens is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.Vantagens!).NotEmpty().MaximumLength(300);
        });
    }
}
