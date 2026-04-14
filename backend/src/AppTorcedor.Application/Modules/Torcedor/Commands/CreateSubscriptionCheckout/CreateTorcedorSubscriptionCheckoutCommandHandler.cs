using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.CreateSubscriptionCheckout;

public sealed class CreateTorcedorSubscriptionCheckoutCommandHandler(ITorcedorSubscriptionCheckoutPort port)
    : IRequestHandler<CreateTorcedorSubscriptionCheckoutCommand, CreateTorcedorSubscriptionCheckoutResult>
{
    public Task<CreateTorcedorSubscriptionCheckoutResult> Handle(
        CreateTorcedorSubscriptionCheckoutCommand request,
        CancellationToken cancellationToken) =>
        port.CreateCheckoutAsync(request.UserId, request.PlanId, request.PaymentMethod, cancellationToken);
}
