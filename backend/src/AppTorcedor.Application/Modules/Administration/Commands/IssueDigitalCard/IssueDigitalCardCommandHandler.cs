using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.IssueDigitalCard;

public sealed class IssueDigitalCardCommandHandler(IDigitalCardAdministrationPort port)
    : IRequestHandler<IssueDigitalCardCommand, DigitalCardMutationResult>
{
    public Task<DigitalCardMutationResult> Handle(IssueDigitalCardCommand request, CancellationToken cancellationToken) =>
        port.IssueDigitalCardAsync(request.MembershipId, request.ActorUserId, cancellationToken);
}
