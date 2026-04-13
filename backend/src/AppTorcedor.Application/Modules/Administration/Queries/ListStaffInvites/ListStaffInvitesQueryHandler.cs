using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListStaffInvites;

public sealed class ListStaffInvitesQueryHandler(IStaffAdministrationPort staff)
    : IRequestHandler<ListStaffInvitesQuery, IReadOnlyList<StaffInviteRowDto>>
{
    public Task<IReadOnlyList<StaffInviteRowDto>> Handle(ListStaffInvitesQuery request, CancellationToken cancellationToken) =>
        staff.ListPendingInvitesAsync(cancellationToken);
}
