using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateMembershipStatus;

public sealed class UpdateMembershipStatusCommandHandler(IMembershipAdministrationPort membership)
    : IRequestHandler<UpdateMembershipStatusCommand, MembershipStatusUpdateResult>
{
    public Task<MembershipStatusUpdateResult> Handle(UpdateMembershipStatusCommand request, CancellationToken cancellationToken) =>
        membership.UpdateStatusAsync(
            request.MembershipId,
            request.Status,
            request.Reason,
            request.ActorUserId,
            cancellationToken);
}
