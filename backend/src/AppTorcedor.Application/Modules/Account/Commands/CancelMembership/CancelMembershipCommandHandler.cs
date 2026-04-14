using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Commands.CancelMembership;

public sealed class CancelMembershipCommandHandler(ITorcedorMembershipCancellationPort port)
    : IRequestHandler<CancelMembershipCommand, CancelMembershipResult>
{
    public Task<CancelMembershipResult> Handle(CancelMembershipCommand request, CancellationToken cancellationToken) =>
        port.CancelMembershipAsync(request.UserId, cancellationToken);
}
