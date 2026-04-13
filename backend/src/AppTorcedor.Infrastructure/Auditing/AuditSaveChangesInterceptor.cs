using System.Text.Json;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AppTorcedor.Infrastructure.Auditing;

public sealed class AuditSaveChangesInterceptor(ICurrentAuditContext auditContext) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var db = eventData.Context;
        if (db is not AppDbContext appDb)
            return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);

        var utcNow = DateTimeOffset.UtcNow;
        foreach (var entry in appDb.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached).ToList())
        {
            if (entry.Entity is AuditLogEntry)
                continue;
            if (!ShouldAudit(entry.Entity))
                continue;

            var entityTypeName = entry.Entity.GetType().Name;
            var entityId = GetEntityId(entry);
            var action = entry.State switch
            {
                EntityState.Added => $"{entityTypeName}.Create",
                EntityState.Deleted => $"{entityTypeName}.Delete",
                EntityState.Modified => $"{entityTypeName}.Update",
                _ => $"{entityTypeName}.Change",
            };

            string? oldValues = null;
            string? newValues = null;
            if (entry.State == EntityState.Added)
                newValues = Serialize(entry.CurrentValues);
            else if (entry.State == EntityState.Deleted)
                oldValues = Serialize(entry.OriginalValues);
            else if (entry.State == EntityState.Modified)
            {
                oldValues = Serialize(entry.OriginalValues);
                newValues = Serialize(entry.CurrentValues);
            }

            appDb.AuditLogs.Add(
                new AuditLogEntry
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = auditContext.UserId,
                    Action = action,
                    EntityType = entityTypeName,
                    EntityId = entityId,
                    OldValues = oldValues,
                    NewValues = newValues,
                    CorrelationId = auditContext.CorrelationId,
                    CreatedAt = utcNow,
                });
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private static bool ShouldAudit(object entity) =>
        entity is MembershipRecord
            or MembershipPlanRecord
            or PaymentRecord
            or AppConfigurationEntry
            or StaffInviteRecord
            or AppRolePermission;

    private static string GetEntityId(EntityEntry entry)
    {
        if (entry.Entity is AppConfigurationEntry cfg)
            return cfg.Key;

        var keyProps = entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).ToList();
        if (keyProps.Count == 0)
            return string.Empty;
        if (keyProps.Count == 1)
            return keyProps[0].CurrentValue?.ToString() ?? string.Empty;

        var dict = keyProps.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        return JsonSerializer.Serialize(dict);
    }

    private static string Serialize(PropertyValues values)
    {
        var dict = values.Properties.ToDictionary(p => p.Name, p => values[p]);
        return JsonSerializer.Serialize(dict);
    }
}
