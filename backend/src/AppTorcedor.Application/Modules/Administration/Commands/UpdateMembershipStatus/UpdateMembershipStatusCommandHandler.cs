using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateMembershipStatus;

public sealed class UpdateMembershipStatusCommandHandler(IMembershipWritePort membership)
    : IRequestHandler<UpdateMembershipStatusCommand, bool>
{
    public Task<bool> Handle(UpdateMembershipStatusCommand request, CancellationToken cancellationToken) =>
        membership.UpdateStatusAsync(request.MembershipId, request.Status, cancellationToken);
}
