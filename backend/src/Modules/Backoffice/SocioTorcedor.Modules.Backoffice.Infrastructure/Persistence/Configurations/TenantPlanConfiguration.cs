using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Backoffice.Domain.Entities;
using SocioTorcedor.Modules.Backoffice.Domain.Enums;

namespace SocioTorcedor.Modules.Backoffice.Infrastructure.Persistence.Configurations;

public sealed class TenantPlanConfiguration : IEntityTypeConfiguration<TenantPlan>
{
    public void Configure(EntityTypeBuilder<TenantPlan> builder)
    {
        builder.ToTable("TenantPlans");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.SaaSPlanId).IsRequired();
        builder.Property(t => t.StartDate).IsRequired();
        builder.Property(t => t.Status).HasConversion<int>().IsRequired();
        builder.Property(t => t.BillingCycle).HasConversion<int>().IsRequired();

        builder.HasOne<SaaSPlan>()
            .WithMany()
            .HasForeignKey(t => t.SaaSPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.TenantId, t.Status })
            .IsUnique()
            .HasFilter("[Status] = " + (int)TenantPlanStatus.Active);
    }
}
