using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminMembershipDetail;

public sealed class GetAdminMembershipDetailQueryHandler(IMembershipAdministrationPort membership)
    : IRequestHandler<GetAdminMembershipDetailQuery, AdminMembershipDetailDto?>
{
    public Task<AdminMembershipDetailDto?> Handle(GetAdminMembershipDetailQuery request, CancellationToken cancellationToken) =>
        membership.GetMembershipByIdAsync(request.MembershipId, cancellationToken);
}
