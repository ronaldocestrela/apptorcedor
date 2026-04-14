using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.RegenerateDigitalCard;

public sealed class RegenerateDigitalCardCommandHandler(IDigitalCardAdministrationPort port)
    : IRequestHandler<RegenerateDigitalCardCommand, DigitalCardMutationResult>
{
    public Task<DigitalCardMutationResult> Handle(RegenerateDigitalCardCommand request, CancellationToken cancellationToken) =>
        port.RegenerateDigitalCardAsync(request.DigitalCardId, request.Reason, request.ActorUserId, cancellationToken);
}
