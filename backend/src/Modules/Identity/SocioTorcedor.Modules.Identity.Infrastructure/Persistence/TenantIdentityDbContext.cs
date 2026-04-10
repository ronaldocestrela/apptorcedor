using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SocioTorcedor.Modules.Identity.Domain.Entities;
using SocioTorcedor.Modules.Identity.Infrastructure.Entities;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Persistence;

public sealed class TenantIdentityDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public TenantIdentityDbContext(DbContextOptions<TenantIdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<LegalDocumentVersion> LegalDocumentVersions => Set<LegalDocumentVersion>();

    public DbSet<UserLegalConsent> UserLegalConsents => Set<UserLegalConsent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(TenantIdentityDbContext).Assembly);
    }
}
