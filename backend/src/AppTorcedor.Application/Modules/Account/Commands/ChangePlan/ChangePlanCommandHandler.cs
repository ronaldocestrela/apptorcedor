using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Commands.ChangePlan;

public sealed class ChangePlanCommandHandler(ITorcedorPlanChangePort port)
    : IRequestHandler<ChangePlanCommand, ChangePlanResult>
{
    public Task<ChangePlanResult> Handle(ChangePlanCommand request, CancellationToken cancellationToken) =>
        port.ChangePlanAsync(request.UserId, request.NewPlanId, request.PaymentMethod, cancellationToken);
}
