using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.RegenerateDigitalCard;

public sealed record RegenerateDigitalCardCommand(Guid DigitalCardId, string? Reason, Guid ActorUserId)
    : IRequest<DigitalCardMutationResult>;
