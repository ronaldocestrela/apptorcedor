using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.News;

public sealed class InAppNotificationDispatchService(AppDbContext db) : IInAppNotificationDispatchService
{
    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var due = await db.InAppNotifications
            .Where(n => n.Status == InAppNotificationStatus.Pending && n.ScheduledAt <= now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var n in due)
        {
            n.Status = InAppNotificationStatus.Dispatched;
            n.DispatchedAt = now;
        }

        if (due.Count > 0)
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return due.Count;
    }
}
