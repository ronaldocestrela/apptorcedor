using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AppPermission> Permissions => Set<AppPermission>();
    public DbSet<AppRolePermission> RolePermissions => Set<AppRolePermission>();
    public DbSet<MembershipPlanRecord> MembershipPlans => Set<MembershipPlanRecord>();
    public DbSet<MembershipRecord> Memberships => Set<MembershipRecord>();
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();
    public DbSet<AppConfigurationEntry> AppConfigurationEntries => Set<AppConfigurationEntry>();
    public DbSet<StaffInviteRecord> StaffInvites => Set<StaffInviteRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppPermission>(entity =>
        {
            entity.ToTable("AppPermissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<AppRolePermission>(entity =>
        {
            entity.ToTable("AppRolePermissions");
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
            entity.HasIndex(x => x.RoleId);
            entity.HasIndex(x => x.PermissionId);
        });

        builder.Entity<MembershipPlanRecord>(entity =>
        {
            entity.ToTable("MembershipPlans");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.BillingCycle).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.DiscountPercentage).HasPrecision(5, 2);
        });

        builder.Entity<MembershipRecord>(entity =>
        {
            entity.ToTable("Memberships");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasIndex(x => x.UserId).IsUnique();
            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne<MembershipPlanRecord>()
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PaymentRecord>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.MembershipId);
            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne<MembershipRecord>()
                .WithMany()
                .HasForeignKey(x => x.MembershipId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLogEntry>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(256).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.EntityType);
        });

        builder.Entity<AppConfigurationEntry>(entity =>
        {
            entity.ToTable("AppConfigurationEntries");
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasMaxLength(128);
            entity.Property(x => x.Value).IsRequired();
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => x.UserId);
        });

        builder.Entity<StaffInviteRecord>(entity =>
        {
            entity.ToTable("StaffInvites");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => x.NormalizedEmail);
        });
    }
}
