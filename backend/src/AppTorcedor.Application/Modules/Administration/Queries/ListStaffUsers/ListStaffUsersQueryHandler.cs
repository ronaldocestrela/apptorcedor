using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListStaffUsers;

public sealed class ListStaffUsersQueryHandler(IStaffAdministrationPort staff)
    : IRequestHandler<ListStaffUsersQuery, IReadOnlyList<StaffUserRowDto>>
{
    public Task<IReadOnlyList<StaffUserRowDto>> Handle(ListStaffUsersQuery request, CancellationToken cancellationToken) =>
        staff.ListStaffUsersAsync(cancellationToken);
}
