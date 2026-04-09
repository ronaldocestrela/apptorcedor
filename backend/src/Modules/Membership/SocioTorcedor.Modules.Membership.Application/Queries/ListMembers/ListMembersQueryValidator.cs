using FluentValidation;

namespace SocioTorcedor.Modules.Membership.Application.Queries.ListMembers;

public sealed class ListMembersQueryValidator : AbstractValidator<ListMembersQuery>
{
    public ListMembersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
