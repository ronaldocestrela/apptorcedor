using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ConciliatePayment;

public sealed class ConciliatePaymentCommandHandler(IPaymentsAdministrationPort port)
    : IRequestHandler<ConciliatePaymentCommand, PaymentMutationResult>
{
    public Task<PaymentMutationResult> Handle(ConciliatePaymentCommand request, CancellationToken cancellationToken) =>
        port.ConciliatePaymentAsync(request.PaymentId, request.PaidAt, request.ActorUserId, cancellationToken);
}
