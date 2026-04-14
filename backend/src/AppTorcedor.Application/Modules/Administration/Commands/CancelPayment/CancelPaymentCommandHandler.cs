using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CancelPayment;

public sealed class CancelPaymentCommandHandler(IPaymentsAdministrationPort port)
    : IRequestHandler<CancelPaymentCommand, PaymentMutationResult>
{
    public Task<PaymentMutationResult> Handle(CancelPaymentCommand request, CancellationToken cancellationToken) =>
        port.CancelPaymentAsync(request.PaymentId, request.Reason, request.ActorUserId, cancellationToken);
}
