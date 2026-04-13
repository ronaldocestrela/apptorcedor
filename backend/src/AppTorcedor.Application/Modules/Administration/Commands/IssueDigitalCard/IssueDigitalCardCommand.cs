using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.IssueDigitalCard;

public sealed record IssueDigitalCardCommand(Guid MembershipId, Guid ActorUserId) : IRequest<DigitalCardMutationResult>;
