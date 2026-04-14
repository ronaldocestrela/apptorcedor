using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ConciliatePayment;

public sealed record ConciliatePaymentCommand(Guid PaymentId, DateTimeOffset? PaidAt, Guid ActorUserId)
    : IRequest<PaymentMutationResult>;
