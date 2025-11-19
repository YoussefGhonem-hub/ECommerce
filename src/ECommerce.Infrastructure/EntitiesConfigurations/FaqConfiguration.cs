using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.EntitiesConfigurations;

public class FaqConfiguration : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> builder)
    {
        builder.Property(f => f.QuestionEn).IsRequired();
        builder.Property(f => f.QuestionAr).IsRequired();
        builder.Property(f => f.AnswerEn).IsRequired();
        builder.Property(f => f.AnswerAr).IsRequired();
        builder.Property(f => f.IsActive).HasDefaultValue(true);
        builder.Property(f => f.DisplayOrder).HasDefaultValue(0);

        builder.HasIndex(f => f.FaqCategoryId);
        builder.HasIndex(f => f.IsActive);
        builder.HasIndex(f => f.DisplayOrder);

        builder.HasOne(f => f.Category)
            .WithMany(c => c.Faqs)
            .HasForeignKey(f => f.FaqCategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}