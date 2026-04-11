using Microsoft.EntityFrameworkCore;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Domain.Enums;
using SocioTorcedor.Modules.Membership.Infrastructure.Persistence;

namespace SocioTorcedor.Modules.Membership.Infrastructure.Services;

public sealed class MemberProfileStatusService : IMemberProfileStatusService
{
    public async Task TrySetActiveAsync(string tenantConnectionString, Guid memberProfileId, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<TenantMembershipDbContext>()
            .UseSqlServer(
                tenantConnectionString,
                o => o.MigrationsHistoryTable("__EFMembershipMigrationsHistory"))
            .Options;
        await using var db = new TenantMembershipDbContext(options);
        var profile = await db.MemberProfiles.FirstOrDefaultAsync(p => p.Id == memberProfileId, cancellationToken);
        if (profile is null)
            return;
        if (profile.Status == MemberStatus.Delinquent)
            profile.ChangeStatus(MemberStatus.Active);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task TrySetDelinquentAsync(string tenantConnectionString, Guid memberProfileId, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<TenantMembershipDbContext>()
            .UseSqlServer(
                tenantConnectionString,
                o => o.MigrationsHistoryTable("__EFMembershipMigrationsHistory"))
            .Options;
        await using var db = new TenantMembershipDbContext(options);
        var profile = await db.MemberProfiles.FirstOrDefaultAsync(p => p.Id == memberProfileId, cancellationToken);
        if (profile is null)
            return;
        if (profile.Status == MemberStatus.Active)
            profile.ChangeStatus(MemberStatus.Delinquent);
        await db.SaveChangesAsync(cancellationToken);
    }
}
