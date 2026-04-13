using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.RefundPayment;

public sealed record RefundPaymentCommand(Guid PaymentId, string? Reason, Guid ActorUserId) : IRequest<PaymentMutationResult>;
