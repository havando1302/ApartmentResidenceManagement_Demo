using ApartmentResidenceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApartmentResidenceManagement.Infrastructure.Configurations;

public class ApartmentConfiguration : IEntityTypeConfiguration<Apartment>
{
    public void Configure(EntityTypeBuilder<Apartment> builder)
    {
        builder.ToTable("Apartments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ApartmentNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(a => a.ApartmentNumber)
            .IsUnique();

        builder.Property(a => a.Floor)
            .IsRequired();

        builder.Property(a => a.Area)
            .IsRequired();

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<int>();
    }
}
