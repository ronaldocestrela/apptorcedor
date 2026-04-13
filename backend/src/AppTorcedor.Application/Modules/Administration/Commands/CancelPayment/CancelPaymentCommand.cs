using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CancelPayment;

public sealed record CancelPaymentCommand(Guid PaymentId, string? Reason, Guid ActorUserId) : IRequest<PaymentMutationResult>;
