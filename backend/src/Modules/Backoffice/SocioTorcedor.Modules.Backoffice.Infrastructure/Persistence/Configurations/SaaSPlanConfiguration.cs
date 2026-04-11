using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Backoffice.Domain.Entities;

namespace SocioTorcedor.Modules.Backoffice.Infrastructure.Persistence.Configurations;

public sealed class SaaSPlanConfiguration : IEntityTypeConfiguration<SaaSPlan>
{
    public void Configure(EntityTypeBuilder<SaaSPlan> builder)
    {
        builder.ToTable("SaaSPlans");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(256).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.MonthlyPrice).HasPrecision(18, 2);
        builder.Property(p => p.YearlyPrice).HasPrecision(18, 2);
        builder.Property(p => p.StripePriceMonthlyId).HasMaxLength(128);
        builder.Property(p => p.StripePriceYearlyId).HasMaxLength(128);
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasMany(p => p.Features)
            .WithOne(f => f.SaaSPlan)
            .HasForeignKey(f => f.SaaSPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
