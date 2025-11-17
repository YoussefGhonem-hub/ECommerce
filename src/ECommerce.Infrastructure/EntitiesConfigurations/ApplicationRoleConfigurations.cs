using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.EntitiesConfigurations;

public class ApplicationRoleConfigurations : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        // Table (keep Identity default)
        builder.ToTable("AspNetRoles");

        // Key
        builder.HasKey(r => r.Id);

        // Identity defaults for Role
        builder.Property(r => r.Name)
            .HasMaxLength(256);

        builder.Property(r => r.NormalizedName)
            .HasMaxLength(256);

        builder.HasIndex(r => r.NormalizedName)
            .HasDatabaseName("RoleNameIndex")
            .IsUnique()
            .HasFilter("[NormalizedName] IS NOT NULL");

        builder.Property(r => r.ConcurrencyStamp)
            .IsConcurrencyToken();

        // Custom field
        builder.Property(r => r.DisplayName)
            .HasMaxLength(128);
    }
}

