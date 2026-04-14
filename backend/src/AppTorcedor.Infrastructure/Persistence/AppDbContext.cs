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
    public DbSet<ProcessedStripeWebhookEventRecord> ProcessedStripeWebhookEvents => Set<ProcessedStripeWebhookEventRecord>();
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();
    public DbSet<AppConfigurationEntry> AppConfigurationEntries => Set<AppConfigurationEntry>();
    public DbSet<StaffInviteRecord> StaffInvites => Set<StaffInviteRecord>();
    public DbSet<LegalDocumentRecord> LegalDocuments => Set<LegalDocumentRecord>();
    public DbSet<LegalDocumentVersionRecord> LegalDocumentVersions => Set<LegalDocumentVersionRecord>();
    public DbSet<UserConsentRecord> UserConsents => Set<UserConsentRecord>();
    public DbSet<PrivacyRequestRecord> PrivacyRequests => Set<PrivacyRequestRecord>();
    public DbSet<UserProfileRecord> UserProfiles => Set<UserProfileRecord>();
    public DbSet<DigitalCardRecord> DigitalCards => Set<DigitalCardRecord>();
    public DbSet<GameRecord> Games => Set<GameRecord>();
    public DbSet<TicketRecord> Tickets => Set<TicketRecord>();
    public DbSet<NewsArticleRecord> NewsArticles => Set<NewsArticleRecord>();
    public DbSet<InAppNotificationRecord> InAppNotifications => Set<InAppNotificationRecord>();
    public DbSet<LoyaltyCampaignRecord> LoyaltyCampaigns => Set<LoyaltyCampaignRecord>();
    public DbSet<LoyaltyPointRuleRecord> LoyaltyPointRules => Set<LoyaltyPointRuleRecord>();
    public DbSet<LoyaltyPointLedgerEntryRecord> LoyaltyPointLedgerEntries => Set<LoyaltyPointLedgerEntryRecord>();
    public DbSet<BenefitPartnerRecord> BenefitPartners => Set<BenefitPartnerRecord>();
    public DbSet<BenefitOfferRecord> BenefitOffers => Set<BenefitOfferRecord>();
    public DbSet<BenefitOfferPlanEligibilityRecord> BenefitOfferPlanEligibilities => Set<BenefitOfferPlanEligibilityRecord>();
    public DbSet<BenefitOfferMembershipStatusEligibilityRecord> BenefitOfferMembershipStatusEligibilities =>
        Set<BenefitOfferMembershipStatusEligibilityRecord>();
    public DbSet<BenefitRedemptionRecord> BenefitRedemptions => Set<BenefitRedemptionRecord>();
    public DbSet<SupportTicketRecord> SupportTickets => Set<SupportTicketRecord>();
    public DbSet<SupportTicketMessageRecord> SupportTicketMessages => Set<SupportTicketMessageRecord>();
    public DbSet<SupportTicketMessageAttachmentRecord> SupportTicketMessageAttachments => Set<SupportTicketMessageAttachmentRecord>();
    public DbSet<SupportTicketHistoryRecord> SupportTicketHistories => Set<SupportTicketHistoryRecord>();

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

        builder.Entity<ProcessedStripeWebhookEventRecord>(entity =>
        {
            entity.ToTable("ProcessedStripeWebhookEvents");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).HasMaxLength(255);
            entity.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.ProcessedAtUtc);
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

        builder.Entity<DigitalCardRecord>(entity =>
        {
            entity.ToTable("DigitalCards");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.Token).HasMaxLength(64).IsRequired();
            entity.Property(x => x.InvalidationReason).HasMaxLength(2000);
            entity.HasIndex(x => x.Token).IsUnique();
            entity.HasIndex(x => new { x.MembershipId, x.Version }).IsUnique();
            entity.HasIndex(x => x.UserId);
            entity
                .HasIndex(x => x.MembershipId)
                .IsUnique()
                .HasFilter("[Status] = 1");
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

        builder.Entity<GameRecord>(entity =>
        {
            entity.ToTable("Games");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Opponent).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Competition).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.GameDate);
            entity.HasIndex(x => x.IsActive);
        });

        builder.Entity<TicketRecord>(entity =>
        {
            entity.ToTable("Tickets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.ExternalTicketId).HasMaxLength(128);
            entity.Property(x => x.QrCode).HasMaxLength(2048);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.GameId);
            entity.HasIndex(x => x.ExternalTicketId);
            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne<GameRecord>()
                .WithMany()
                .HasForeignKey(x => x.GameId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<NewsArticleRecord>(entity =>
        {
            entity.ToTable("NewsArticles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(2000);
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.UpdatedAt);
        });

        builder.Entity<InAppNotificationRecord>(entity =>
        {
            entity.ToTable("InAppNotifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PreviewText).HasMaxLength(500);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasIndex(x => new { x.UserId, x.Status });
            entity.HasIndex(x => new { x.Status, x.ScheduledAt });
            entity.HasIndex(x => x.NewsArticleId);
            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne<NewsArticleRecord>()
                .WithMany()
                .HasForeignKey(x => x.NewsArticleId)
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

        builder.Entity<LoyaltyCampaignRecord>(entity =>
        {
            entity.ToTable("LoyaltyCampaigns");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasIndex(x => x.Status);
        });

        builder.Entity<LoyaltyPointRuleRecord>(entity =>
        {
            entity.ToTable("LoyaltyPointRules");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Trigger).HasConversion<int>();
            entity.HasIndex(x => x.CampaignId);
            entity
                .HasOne<LoyaltyCampaignRecord>()
                .WithMany()
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LoyaltyPointLedgerEntryRecord>(entity =>
        {
            entity.ToTable("LoyaltyPointLedgerEntries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SourceType).HasConversion<int>();
            entity.Property(x => x.SourceKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(2000);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => new { x.SourceType, x.SourceKey }).IsUnique();
            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne<LoyaltyCampaignRecord>()
                .WithMany()
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<BenefitPartnerRecord>(entity =>
        {
            entity.ToTable("BenefitPartners");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.HasIndex(x => x.IsActive);
        });

        builder.Entity<BenefitOfferRecord>(entity =>
        {
            entity.ToTable("BenefitOffers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.HasIndex(x => x.PartnerId);
            entity.HasIndex(x => new { x.IsActive, x.StartAt, x.EndAt });
            entity
                .HasOne<BenefitPartnerRecord>()
                .WithMany()
                .HasForeignKey(x => x.PartnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BenefitOfferPlanEligibilityRecord>(entity =>
        {
            entity.ToTable("BenefitOfferPlanEligibilities");
            entity.HasKey(x => new { x.OfferId, x.PlanId });
            entity.HasIndex(x => x.PlanId);
            entity
                .HasOne<BenefitOfferRecord>()
                .WithMany()
                .HasForeignKey(x => x.OfferId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne<MembershipPlanRecord>()
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BenefitOfferMembershipStatusEligibilityRecord>(entity =>
        {
            entity.ToTable("BenefitOfferMembershipStatusEligibilities");
            entity.HasKey(x => new { x.OfferId, x.Status });
            entity.Property(x => x.Status).HasConversion<int>();
            entity
                .HasOne<BenefitOfferRecord>()
                .WithMany()
                .HasForeignKey(x => x.OfferId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BenefitRedemptionRecord>(entity =>
        {
            entity.ToTable("BenefitRedemptions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => x.OfferId);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.CreatedAt);
            entity
                .HasOne<BenefitOfferRecord>()
                .WithMany()
                .HasForeignKey(x => x.OfferId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SupportTicketRecord>(entity =>
        {
            entity.ToTable("SupportTickets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Queue).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Priority).HasConversion<int>();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasIndex(x => x.RequesterUserId);
            entity.HasIndex(x => x.AssignedAgentUserId);
            entity.HasIndex(x => new { x.Status, x.Queue });
            entity.HasIndex(x => x.SlaDeadlineUtc);
            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.RequesterUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.AssignedAgentUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SupportTicketMessageRecord>(entity =>
        {
            entity.ToTable("SupportTicketMessages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Body).HasMaxLength(8000).IsRequired();
            entity.HasIndex(x => x.TicketId);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity
                .HasOne<SupportTicketRecord>()
                .WithMany()
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SupportTicketMessageAttachmentRecord>(entity =>
        {
            entity.ToTable("SupportTicketMessageAttachments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => x.MessageId);
            entity
                .HasOne<SupportTicketMessageRecord>()
                .WithMany()
                .HasForeignKey(x => x.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SupportTicketHistoryRecord>(entity =>
        {
            entity.ToTable("SupportTicketHistories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.FromValue).HasMaxLength(256);
            entity.Property(x => x.ToValue).HasMaxLength(256);
            entity.Property(x => x.Reason).HasMaxLength(2000);
            entity.HasIndex(x => x.TicketId);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity
                .HasOne<SupportTicketRecord>()
                .WithMany()
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
