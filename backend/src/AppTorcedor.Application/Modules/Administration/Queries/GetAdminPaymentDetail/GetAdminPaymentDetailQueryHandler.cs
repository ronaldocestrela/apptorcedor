using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminPaymentDetail;

public sealed class GetAdminPaymentDetailQueryHandler(IPaymentsAdministrationPort port)
    : IRequestHandler<GetAdminPaymentDetailQuery, AdminPaymentDetailDto?>
{
    public Task<AdminPaymentDetailDto?> Handle(GetAdminPaymentDetailQuery request, CancellationToken cancellationToken) =>
        port.GetPaymentByIdAsync(request.PaymentId, cancellationToken);
}
