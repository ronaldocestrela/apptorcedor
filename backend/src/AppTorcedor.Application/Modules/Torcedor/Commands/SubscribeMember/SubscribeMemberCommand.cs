using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.SubscribeMember;

public sealed record SubscribeMemberCommand(Guid UserId, Guid PlanId) : IRequest<SubscribeMemberResult>;
