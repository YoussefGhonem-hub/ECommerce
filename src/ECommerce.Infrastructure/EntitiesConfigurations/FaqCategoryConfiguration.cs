using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.EntitiesConfigurations;

public class FaqCategoryConfiguration : IEntityTypeConfiguration<FaqCategory>
{
    public void Configure(EntityTypeBuilder<FaqCategory> builder)
    {
        builder.Property(c => c.NameEn).IsRequired().HasMaxLength(128);
        builder.Property(c => c.NameAr).IsRequired().HasMaxLength(128);
        builder.Property(c => c.IsActive).HasDefaultValue(true);
        builder.Property(c => c.DisplayOrder).HasDefaultValue(0);

        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => c.DisplayOrder);
        builder.HasIndex(c => c.NameEn);
        builder.HasIndex(c => c.NameAr);
    }
}