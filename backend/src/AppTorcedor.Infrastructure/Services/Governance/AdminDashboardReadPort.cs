using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Payments;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Governance;

public sealed class AdminDashboardReadPort(AppDbContext db) : IAdminDashboardReadPort
{
    public async Task<AdminDashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var active = await db.Memberships.AsNoTracking()
            .CountAsync(m => m.Status == MembershipStatus.Ativo, cancellationToken)
            .ConfigureAwait(false);
        var delinquent = await db.Memberships.AsNoTracking()
            .CountAsync(m => m.Status == MembershipStatus.Inadimplente, cancellationToken)
            .ConfigureAwait(false);

        var openTickets = await db.SupportTickets.AsNoTracking()
            .CountAsync(
                t => t.Status == SupportTicketStatus.Open
                    || t.Status == SupportTicketStatus.InProgress
                    || t.Status == SupportTicketStatus.WaitingUser,
                cancellationToken)
            .ConfigureAwait(false);

        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
        var totalFaturadoLast30Days = await db.Payments.AsNoTracking()
            .Where(p =>
                p.Status == PaymentChargeStatuses.Paid
                && p.PaidAt != null
                && p.PaidAt >= thirtyDaysAgo)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken)
            .ConfigureAwait(false) ?? 0m;

        return new AdminDashboardDto(active, delinquent, openTickets, totalFaturadoLast30Days);
    }
}
