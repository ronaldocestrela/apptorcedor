using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListMembershipHistory;

public sealed class ListMembershipHistoryQueryHandler(IMembershipAdministrationPort membership)
    : IRequestHandler<ListMembershipHistoryQuery, IReadOnlyList<MembershipHistoryEventDto>>
{
    public Task<IReadOnlyList<MembershipHistoryEventDto>> Handle(ListMembershipHistoryQuery request, CancellationToken cancellationToken) =>
        membership.ListHistoryAsync(request.MembershipId, request.Take, cancellationToken);
}
