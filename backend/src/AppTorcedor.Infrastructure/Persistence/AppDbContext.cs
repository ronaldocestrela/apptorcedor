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
    public DbSet<MembershipPlanBenefitRecord> MembershipPlanBenefits => Set<MembershipPlanBenefitRecord>();
    public DbSet<MembershipRecord> Memberships => Set<MembershipRecord>();
    public DbSet<MembershipHistoryRecord> MembershipHistories => Set<MembershipHistoryRecord>();
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();
    public DbSet<AppConfigurationEntry> AppConfigurationEntries => Set<AppConfigurationEntry>();
    public DbSet<StaffInviteRecord> StaffInvites => Set<StaffInviteRecord>();
    public DbSet<LegalDocumentRecord> LegalDocuments => Set<LegalDocumentRecord>();
    public DbSet<LegalDocumentVersionRecord> LegalDocumentVersions => Set<LegalDocumentVersionRecord>();
    public DbSet<UserConsentRecord> UserConsents => Set<UserConsentRecord>();
    public DbSet<PrivacyRequestRecord> PrivacyRequests => Set<PrivacyRequestRecord>();
    public DbSet<UserProfileRecord> UserProfiles => Set<UserProfileRecord>();

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
            entity.Property(x => x.Summary).HasMaxLength(2000);
            entity.Property(x => x.RulesNotes).HasMaxLength(4000);
            entity.HasIndex(x => x.IsPublished);
        });

        builder.Entity<MembershipPlanBenefitRecord>(entity =>
        {
            entity.ToTable("MembershipPlanBenefits");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.HasIndex(x => x.PlanId);
            entity
                .HasOne<MembershipPlanRecord>()
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
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

        builder.Entity<MembershipHistoryRecord>(entity =>
        {
            entity.ToTable("MembershipHistories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.FromStatus).HasConversion<int>();
            entity.Property(x => x.ToStatus).HasConversion<int>();
            entity.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => x.MembershipId);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.CreatedAt);
            entity
                .HasOne<MembershipRecord>()
                .WithMany()
                .HasForeignKey(x => x.MembershipId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentRecord>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PaymentMethod).HasMaxLength(32);
            entity.Property(x => x.ExternalReference).HasMaxLength(256);
            entity.Property(x => x.ProviderName).HasMaxLength(64);
            entity.Property(x => x.StatusReason).HasMaxLength(512);
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

        builder.Entity<LegalDocumentRecord>(entity =>
        {
            entity.ToTable("LegalDocuments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<int>();
            entity.Property(x => x.Title).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.Type).IsUnique();
        });

        builder.Entity<LegalDocumentVersionRecord>(entity =>
        {
            entity.ToTable("LegalDocumentVersions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.Content).IsRequired();
            entity.HasIndex(x => new { x.LegalDocumentId, x.VersionNumber }).IsUnique();
            entity
                .HasOne<LegalDocumentRecord>()
                .WithMany()
                .HasForeignKey(x => x.LegalDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserConsentRecord>(entity =>
        {
            entity.ToTable("UserConsents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ClientIp).HasMaxLength(45);
            entity.HasIndex(x => new { x.UserId, x.LegalDocumentVersionId }).IsUnique();
            entity.HasIndex(x => x.UserId);
            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne<LegalDocumentVersionRecord>()
                .WithMany()
                .HasForeignKey(x => x.LegalDocumentVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserProfileRecord>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.Document).HasMaxLength(32);
            entity.Property(x => x.PhotoUrl).HasMaxLength(2048);
            entity.Property(x => x.Address).HasMaxLength(2048);
            entity.Property(x => x.AdministrativeNote).HasMaxLength(2000);
            entity
                .HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<UserProfileRecord>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PrivacyRequestRecord>(entity =>
        {
            entity.ToTable("PrivacyRequests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Kind).HasConversion<int>();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.ErrorMessage).HasMaxLength(2048);
            entity.HasIndex(x => x.SubjectUserId);
            entity.HasIndex(x => x.CreatedAt);
            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.SubjectUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
