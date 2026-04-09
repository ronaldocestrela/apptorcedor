using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Membership.Domain.Entities;

namespace SocioTorcedor.Modules.Membership.Infrastructure.Persistence.Configurations;

public sealed class MemberProfileConfiguration : IEntityTypeConfiguration<MemberProfile>
{
    public void Configure(EntityTypeBuilder<MemberProfile> builder)
    {
        builder.ToTable("MemberProfiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.HasIndex(x => x.UserId).IsUnique();

        builder.Property(x => x.CpfDigits).HasMaxLength(11).IsRequired();
        builder.HasIndex(x => x.CpfDigits).IsUnique();

        builder.Property(x => x.DateOfBirth).IsRequired();
        builder.Property(x => x.Gender).HasConversion<int>().IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        builder.OwnsOne(
            x => x.Address,
            navigation =>
            {
                navigation.Property(a => a.Street).HasColumnName("Address_Street").HasMaxLength(200).IsRequired();
                navigation.Property(a => a.Number).HasColumnName("Address_Number").HasMaxLength(20).IsRequired();
                navigation.Property(a => a.Complement).HasColumnName("Address_Complement").HasMaxLength(120);
                navigation.Property(a => a.Neighborhood).HasColumnName("Address_Neighborhood").HasMaxLength(120).IsRequired();
                navigation.Property(a => a.City).HasColumnName("Address_City").HasMaxLength(120).IsRequired();
                navigation.Property(a => a.State).HasColumnName("Address_State").HasMaxLength(2).IsRequired();
                navigation.Property(a => a.ZipCode).HasColumnName("Address_ZipCode").HasMaxLength(8).IsRequired();
            });
    }
}
