using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Payments;

public sealed class MembershipScheduledCancellationEffectiveSweep(AppDbContext db) : IMembershipScheduledCancellationEffectiveSweep
{
    public Task<int> ApplyAsync(CancellationToken cancellationToken = default) =>
        ApplyAsync(DateTimeOffset.UtcNow, cancellationToken);

    public async Task<int> ApplyAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        var list = await db.Memberships
            .Where(m => m.Status == MembershipStatus.Ativo && m.EndDate != null && m.EndDate <= nowUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var m in list)
            ApplyEffectiveCancellation(m, nowUtc);

        if (list.Count > 0)
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return list.Count;
    }

    private void ApplyEffectiveCancellation(MembershipRecord m, DateTimeOffset nowUtc)
    {
        var from = m.Status;
        m.Status = MembershipStatus.Cancelado;
        m.NextDueDate = null;

        db.MembershipHistories.Add(
            new MembershipHistoryRecord
            {
                Id = Guid.NewGuid(),
                MembershipId = m.Id,
                UserId = m.UserId,
                EventType = MembershipHistoryEventTypes.StatusChanged,
                FromStatus = from,
                ToStatus = MembershipStatus.Cancelado,
                FromPlanId = m.PlanId,
                ToPlanId = m.PlanId,
                Reason = "Encerramento efetivo após cancelamento agendado pelo torcedor (D.7).",
                ActorUserId = null,
                CreatedAt = nowUtc,
            });
    }
}
