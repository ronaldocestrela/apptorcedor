using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Commands.ChangePlan;

public sealed record ChangePlanCommand(
    Guid UserId,
    Guid NewPlanId,
    TorcedorSubscriptionPaymentMethod PaymentMethod) : IRequest<ChangePlanResult>;
