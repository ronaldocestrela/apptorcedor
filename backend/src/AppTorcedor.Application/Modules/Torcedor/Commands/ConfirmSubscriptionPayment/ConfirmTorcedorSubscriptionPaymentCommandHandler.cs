using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.ConfirmSubscriptionPayment;

public sealed class ConfirmTorcedorSubscriptionPaymentCommandHandler(ITorcedorSubscriptionCheckoutPort port)
    : IRequestHandler<ConfirmTorcedorSubscriptionPaymentCommand, ConfirmTorcedorSubscriptionPaymentResult>
{
    public Task<ConfirmTorcedorSubscriptionPaymentResult> Handle(
        ConfirmTorcedorSubscriptionPaymentCommand request,
        CancellationToken cancellationToken) =>
        port.ConfirmPaymentAsync(request.PaymentId, request.WebhookSecret, cancellationToken);
}
