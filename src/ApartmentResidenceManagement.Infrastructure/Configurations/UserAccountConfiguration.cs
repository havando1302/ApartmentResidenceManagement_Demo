using ApartmentResidenceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApartmentResidenceManagement.Infrastructure.Configurations;

public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts");

        builder.HasKey(ua => ua.Id);

        builder.Property(ua => ua.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(ua => ua.Username)
            .IsUnique();

        builder.Property(ua => ua.PasswordHash)
            .IsRequired();

        builder.Property(ua => ua.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ua => ua.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(ua => ua.Resident)
            .WithOne(r => r.UserAccount)
            .HasForeignKey<UserAccount>(ua => ua.ResidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
