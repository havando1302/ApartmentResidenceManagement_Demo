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

        // Generated nullable keys let MySQL enforce uniqueness only for active rows.
        builder.Property<int?>("ActiveResidentId")
            .HasComputedColumnSql("CASE WHEN `IsActive` = 1 THEN `ResidentId` ELSE NULL END", stored: true);

        builder.HasIndex("ActiveResidentId")
            .IsUnique();

        builder.Property<int?>("ActiveOwnerApartmentId")
            .HasComputedColumnSql("CASE WHEN `IsActive` = 1 AND `RelationshipType` = 0 THEN `ApartmentId` ELSE NULL END", stored: true);

        builder.HasIndex("ActiveOwnerApartmentId")
            .IsUnique();

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ResidenceHistories_ActiveEndDate",
                "(`IsActive` = 1 AND `EndDate` IS NULL) OR (`IsActive` = 0 AND `EndDate` IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_ResidenceHistories_DateRange",
                "`EndDate` IS NULL OR `EndDate` >= `StartDate`");
        });

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
