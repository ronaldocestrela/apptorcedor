using FluentValidation;

namespace SocioTorcedor.Modules.Membership.Application.Queries.ListMemberPlans;

public sealed class ListMemberPlansQueryValidator : AbstractValidator<ListMemberPlansQuery>
{
    public ListMemberPlansQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
