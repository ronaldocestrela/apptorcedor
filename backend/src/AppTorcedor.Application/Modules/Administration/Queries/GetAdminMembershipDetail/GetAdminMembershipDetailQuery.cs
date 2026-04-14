using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminMembershipDetail;

public sealed record GetAdminMembershipDetailQuery(Guid MembershipId) : IRequest<AdminMembershipDetailDto?>;
