using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminDigitalCards;

public sealed record ListAdminDigitalCardsQuery(
    Guid? UserId,
    Guid? MembershipId,
    string? Status,
    int Page,
    int PageSize) : IRequest<AdminDigitalCardListPageDto>;
