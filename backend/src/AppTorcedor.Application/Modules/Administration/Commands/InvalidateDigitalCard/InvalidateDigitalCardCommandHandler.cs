using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.InvalidateDigitalCard;

public sealed class InvalidateDigitalCardCommandHandler(IDigitalCardAdministrationPort port)
    : IRequestHandler<InvalidateDigitalCardCommand, DigitalCardMutationResult>
{
    public Task<DigitalCardMutationResult> Handle(InvalidateDigitalCardCommand request, CancellationToken cancellationToken) =>
        port.InvalidateDigitalCardAsync(request.DigitalCardId, request.Reason, request.ActorUserId, cancellationToken);
}
