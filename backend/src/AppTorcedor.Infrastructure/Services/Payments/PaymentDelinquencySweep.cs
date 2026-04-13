using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Payments;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Payments;

public sealed class PaymentDelinquencySweep(AppDbContext db, IMembershipAdministrationPort membership) : IPaymentDelinquencySweep
{
    public async Task<PaymentDelinquencySweepResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var pendings = await db.Payments
            .Where(p => p.Status == PaymentChargeStatuses.Pending && p.DueDate < now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var overdueCount = 0;
        foreach (var p in pendings)
        {
            p.Status = PaymentChargeStatuses.Overdue;
            p.UpdatedAt = now;
            overdueCount++;
        }

        if (pendings.Count > 0)
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var activeWithOverdue = await db.Memberships.AsNoTracking()
            .Where(m => m.Status == MembershipStatus.Ativo)
            .Where(m => db.Payments.Any(p => p.MembershipId == m.Id && p.Status == PaymentChargeStatuses.Overdue))
            .Select(m => m.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var marked = 0;
        foreach (var membershipId in activeWithOverdue)
        {
            var r = await membership
                .ApplySystemMembershipTransitionAsync(
                    membershipId,
                    MembershipStatus.Inadimplente,
                    "Cobrança vencida em aberto — marcação automática (sweep).",
                    cancellationToken)
                .ConfigureAwait(false);
            if (r.Ok)
                marked++;
        }

        return new PaymentDelinquencySweepResult(overdueCount, marked);
    }
}
