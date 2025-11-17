using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.EntitiesConfigurations;

public class ProductImageConfigurations : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasOne(pi => pi.Product)
               .WithMany(p => p.Images)
               .HasForeignKey(pi => pi.ProductId)
               .OnDelete(DeleteBehavior.Cascade);

        // Ensure only one IsMain per product using filtered unique index (SQL Server)
        builder.HasIndex(pi => new { pi.ProductId, pi.IsMain })
               .HasFilter("[IsMain] = 1")
               .IsUnique();

        builder.Property(pi => pi.Path)
               .IsRequired()
               .HasMaxLength(512);
    }
}