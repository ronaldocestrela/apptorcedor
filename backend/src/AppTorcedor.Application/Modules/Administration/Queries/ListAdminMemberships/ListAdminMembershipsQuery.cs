using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminMemberships;

public sealed record ListAdminMembershipsQuery(
    MembershipStatus? Status,
    Guid? UserId,
    int Page = 1,
    int PageSize = 20) : IRequest<AdminMembershipListPageDto>;
