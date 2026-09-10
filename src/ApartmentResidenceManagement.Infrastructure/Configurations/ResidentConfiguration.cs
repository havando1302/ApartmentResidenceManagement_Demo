using ApartmentResidenceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApartmentResidenceManagement.Infrastructure.Configurations;

public class ResidentConfiguration : IEntityTypeConfiguration<Resident>
{
    public void Configure(EntityTypeBuilder<Resident> builder)
    {
        builder.ToTable("Residents");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.DateOfBirth)
            .IsRequired();

        builder.Property(r => r.Gender)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.IdentityCard)
            .HasMaxLength(20);

        builder.HasIndex(r => r.IdentityCard)
            .IsUnique()
            .HasFilter("[IdentityCard] IS NOT NULL");

        builder.Property(r => r.PhoneNumber)
            .HasMaxLength(15);

        builder.Property(r => r.HomeTown)
            .HasMaxLength(200);
    }
}
