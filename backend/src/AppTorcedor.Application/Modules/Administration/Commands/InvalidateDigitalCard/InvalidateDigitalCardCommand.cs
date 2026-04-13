using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.InvalidateDigitalCard;

public sealed record InvalidateDigitalCardCommand(Guid DigitalCardId, string Reason, Guid ActorUserId)
    : IRequest<DigitalCardMutationResult>;
