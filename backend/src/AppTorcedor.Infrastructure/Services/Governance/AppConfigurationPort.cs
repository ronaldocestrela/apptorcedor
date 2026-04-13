using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Auditing;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Governance;

public sealed class AppConfigurationPort(AppDbContext db, ICurrentAuditContext auditContext) : IAppConfigurationPort
{
    public async Task<IReadOnlyList<AppConfigurationEntryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.AppConfigurationEntries.AsNoTracking().OrderBy(x => x.Key).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows
            .Select(x => new AppConfigurationEntryDto(x.Key, x.Value, x.Version, x.UpdatedAt, x.UpdatedByUserId))
            .ToList();
    }

    public async Task<AppConfigurationEntryDto?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var row = await db.AppConfigurationEntries.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, cancellationToken).ConfigureAwait(false);
        return row is null ? null : new AppConfigurationEntryDto(row.Key, row.Value, row.Version, row.UpdatedAt, row.UpdatedByUserId);
    }

    public async Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var existing = await db.AppConfigurationEntries.FirstOrDefaultAsync(x => x.Key == key, cancellationToken).ConfigureAwait(false);
        var userId = auditContext.UserId;
        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            db.AppConfigurationEntries.Add(
                new AppConfigurationEntry
                {
                    Key = key,
                    Value = value,
                    Version = 1,
                    UpdatedAt = now,
                    UpdatedByUserId = userId,
                });
        }
        else
        {
            existing.Value = value;
            existing.Version += 1;
            existing.UpdatedAt = now;
            existing.UpdatedByUserId = userId;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
