using ApartmentResidenceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApartmentResidenceManagement.Infrastructure.Configurations;

public class ResidenceHistoryConfiguration : IEntityTypeConfiguration<ResidenceHistory>
{
    public void Configure(EntityTypeBuilder<ResidenceHistory> builder)
    {
        builder.ToTable("ResidenceHistories");

        builder.HasKey(rh => rh.Id);

        builder.Property(rh => rh.StartDate)
            .IsRequired();

        builder.Property(rh => rh.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(rh => rh.RelationshipType)
            .IsRequired()
            .HasConversion<int>();

        builder.HasOne(rh => rh.Apartment)
            .WithMany(a => a.ResidenceHistories)
            .HasForeignKey(rh => rh.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rh => rh.Resident)
            .WithMany(r => r.ResidenceHistories)
            .HasForeignKey(rh => rh.ResidentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
