using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminPayments;

public sealed class ListAdminPaymentsQueryHandler(IPaymentsAdministrationPort port)
    : IRequestHandler<ListAdminPaymentsQuery, AdminPaymentListPageDto>
{
    public Task<AdminPaymentListPageDto> Handle(ListAdminPaymentsQuery request, CancellationToken cancellationToken) =>
        port.ListPaymentsAsync(
            request.Status,
            request.UserId,
            request.MembershipId,
            request.PaymentMethod,
            request.DueFrom,
            request.DueTo,
            request.Page,
            request.PageSize,
            cancellationToken);
}
