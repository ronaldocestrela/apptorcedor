using AppTorcedor.Identity;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.SubscribeMember;

/// <summary>Evento interno após persistência de nova contratação (Parte D.3).</summary>
public sealed record MemberSubscribedEvent(
    Guid MembershipId,
    Guid UserId,
    Guid PlanId,
    MembershipStatus Status,
    DateTimeOffset OccurredAtUtc) : INotification;
