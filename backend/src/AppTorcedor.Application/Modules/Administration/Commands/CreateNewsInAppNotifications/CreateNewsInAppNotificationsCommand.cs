using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateNewsInAppNotifications;

public sealed record CreateNewsInAppNotificationsCommand(
    Guid NewsId,
    DateTimeOffset? ScheduledAt,
    IReadOnlyList<Guid>? TargetUserIds) : IRequest<NewsNotificationCreateResult>;
