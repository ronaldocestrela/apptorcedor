using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.CreateSubscriptionCheckout;

public sealed record CreateTorcedorSubscriptionCheckoutCommand(
    Guid UserId,
    Guid PlanId,
    TorcedorSubscriptionPaymentMethod PaymentMethod) : IRequest<CreateTorcedorSubscriptionCheckoutResult>;
