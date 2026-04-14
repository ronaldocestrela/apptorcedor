using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminMemberships;

public sealed class ListAdminMembershipsQueryHandler(IMembershipAdministrationPort membership)
    : IRequestHandler<ListAdminMembershipsQuery, AdminMembershipListPageDto>
{
    public Task<AdminMembershipListPageDto> Handle(ListAdminMembershipsQuery request, CancellationToken cancellationToken) =>
        membership.ListMembershipsAsync(request.Status, request.UserId, request.Page, request.PageSize, cancellationToken);
}
