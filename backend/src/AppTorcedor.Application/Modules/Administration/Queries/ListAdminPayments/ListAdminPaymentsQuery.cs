using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminPayments;

public sealed record ListAdminPaymentsQuery(
    string? Status,
    Guid? UserId,
    Guid? MembershipId,
    string? PaymentMethod,
    DateTimeOffset? DueFrom,
    DateTimeOffset? DueTo,
    int Page,
    int PageSize) : IRequest<AdminPaymentListPageDto>;
