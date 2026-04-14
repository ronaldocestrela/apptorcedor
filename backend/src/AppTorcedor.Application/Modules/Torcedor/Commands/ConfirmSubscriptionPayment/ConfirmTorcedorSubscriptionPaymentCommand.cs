using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.ConfirmSubscriptionPayment;

public sealed record ConfirmTorcedorSubscriptionPaymentCommand(Guid PaymentId, string? WebhookSecret)
    : IRequest<ConfirmTorcedorSubscriptionPaymentResult>;
