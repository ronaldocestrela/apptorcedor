using AppTorcedor.Application.Abstractions;
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

        return new AdminDashboardDto(active, delinquent, openTickets);
    }
}
