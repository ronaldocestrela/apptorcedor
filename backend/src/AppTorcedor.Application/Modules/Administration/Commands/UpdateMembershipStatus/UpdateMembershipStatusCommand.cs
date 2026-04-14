using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateMembershipStatus;

public sealed record UpdateMembershipStatusCommand(
    Guid MembershipId,
    MembershipStatus Status,
    string Reason,
    Guid ActorUserId) : IRequest<MembershipStatusUpdateResult>;
