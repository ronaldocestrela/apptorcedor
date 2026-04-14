using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateNewsInAppNotifications;

public sealed class CreateNewsInAppNotificationsCommandHandler(INewsAdministrationPort news)
    : IRequestHandler<CreateNewsInAppNotificationsCommand, NewsNotificationCreateResult>
{
    public Task<NewsNotificationCreateResult> Handle(
        CreateNewsInAppNotificationsCommand request,
        CancellationToken cancellationToken) =>
        news.CreateInAppNotificationsForNewsAsync(
            request.NewsId,
            request.ScheduledAt,
            request.TargetUserIds,
            cancellationToken);
}
