using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Backoffice.Domain.Entities;

namespace SocioTorcedor.Modules.Backoffice.Infrastructure.Persistence.Configurations;

public sealed class SaaSPlanFeatureConfiguration : IEntityTypeConfiguration<SaaSPlanFeature>
{
    public void Configure(EntityTypeBuilder<SaaSPlanFeature> builder)
    {
        builder.ToTable("SaaSPlanFeatures");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Key).HasMaxLength(128).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(512);
        builder.Property(f => f.Value).HasMaxLength(4000);
    }
}
