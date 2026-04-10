using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Identity.Infrastructure.Entities;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class LegalDocumentVersionConfiguration : IEntityTypeConfiguration<LegalDocumentVersion>
{
    public void Configure(EntityTypeBuilder<LegalDocumentVersion> builder)
    {
        builder.ToTable("LegalDocumentVersions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<int>().IsRequired();
        builder.Property(x => x.VersionNumber).IsRequired();
        builder.Property(x => x.Content).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.PublishedAtUtc).IsRequired();
        builder.Property(x => x.IsCurrent).IsRequired();

        builder.HasIndex(x => new { x.Kind, x.IsCurrent });
    }
}
