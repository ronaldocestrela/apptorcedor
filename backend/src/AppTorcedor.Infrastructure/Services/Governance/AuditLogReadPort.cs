using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Governance;

public sealed class AuditLogReadPort(AppDbContext db) : IAuditLogReadPort
{
    public async Task<IReadOnlyList<AuditLogRowDto>> ListRecentAsync(
        string? entityType,
        int take,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 500);
        var query = db.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .Select(
                a => new AuditLogRowDto(
                    a.Id,
                    a.ActorUserId,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.OldValues,
                    a.NewValues,
                    a.CorrelationId,
                    a.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AuditLogRowDto>> ListForSubjectUserAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 500);
        var id = userId.ToString();
        var userEntity = nameof(ApplicationUser);
        var profileEntity = nameof(UserProfileRecord);
        return await db.AuditLogs.AsNoTracking()
            .Where(
                a =>
                    (a.EntityType == userEntity && a.EntityId == id)
                    || (a.EntityType == profileEntity && a.EntityId == id))
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .Select(
                a => new AuditLogRowDto(
                    a.Id,
                    a.ActorUserId,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.OldValues,
                    a.NewValues,
                    a.CorrelationId,
                    a.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
