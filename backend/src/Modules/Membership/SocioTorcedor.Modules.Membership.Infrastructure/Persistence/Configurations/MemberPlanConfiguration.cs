using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Membership.Domain.Entities;

namespace SocioTorcedor.Modules.Membership.Infrastructure.Persistence.Configurations;

public sealed class MemberPlanConfiguration : IEntityTypeConfiguration<MemberPlan>
{
    public void Configure(EntityTypeBuilder<MemberPlan> builder)
    {
        builder.ToTable("MemberPlans");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nome).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Nome).IsUnique();

        builder.Property(x => x.Descricao).HasMaxLength(2000);
        builder.Property(x => x.Preco).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.OwnsMany(
            x => x.Vantagens,
            navigation =>
            {
                navigation.ToJson("Vantagens");
                navigation.Property(v => v.Descricao).HasMaxLength(300).IsRequired();
            });
    }
}
