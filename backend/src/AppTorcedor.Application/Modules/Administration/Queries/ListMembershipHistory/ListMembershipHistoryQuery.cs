using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListMembershipHistory;

public sealed record ListMembershipHistoryQuery(Guid MembershipId, int Take = 50) : IRequest<IReadOnlyList<MembershipHistoryEventDto>>;
