using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApartmentResidenceManagement.Infrastructure.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.LicensePlate)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(v => v.LicensePlate)
            .IsUnique();

        builder.Property(v => v.Brand)
            .HasMaxLength(50);

        builder.Property(v => v.VehicleType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(v => v.RegistrationStatus)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(VehicleRegistrationStatus.Pending);

        builder.HasOne(v => v.Owner)
            .WithMany(r => r.Vehicles)
            .HasForeignKey(v => v.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
