using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.RefundPayment;

public sealed class RefundPaymentCommandHandler(IPaymentsAdministrationPort port)
    : IRequestHandler<RefundPaymentCommand, PaymentMutationResult>
{
    public Task<PaymentMutationResult> Handle(RefundPaymentCommand request, CancellationToken cancellationToken) =>
        port.RefundPaymentAsync(request.PaymentId, request.Reason, request.ActorUserId, cancellationToken);
}
