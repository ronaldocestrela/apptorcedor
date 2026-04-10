using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Identity.Infrastructure.Entities;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserLegalConsentConfiguration : IEntityTypeConfiguration<UserLegalConsent>
{
    public void Configure(EntityTypeBuilder<UserLegalConsent> builder)
    {
        builder.ToTable("UserLegalConsents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Kind).HasConversion<int>().IsRequired();
        builder.Property(x => x.LegalDocumentVersionId).IsRequired();
        builder.Property(x => x.AcceptedAtUtc).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasMaxLength(512);

        builder.HasIndex(x => new { x.UserId, x.Kind, x.AcceptedAtUtc });

        builder.HasOne(x => x.LegalDocumentVersion)
            .WithMany()
            .HasForeignKey(x => x.LegalDocumentVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
