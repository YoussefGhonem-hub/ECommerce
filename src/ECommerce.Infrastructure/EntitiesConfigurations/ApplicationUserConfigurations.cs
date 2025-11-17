using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.EntitiesConfigurations;

public class ApplicationUserConfigurations : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Owned type for social profiles (columns live on AspNetUsers)
        builder.OwnsOne(u => u.SocialProfiles, b =>
        {
            b.Property(p => p.FacebookUrl).HasMaxLength(512);
            b.Property(p => p.InstagramUrl).HasMaxLength(512);
            b.Property(p => p.YouTubeUrl).HasMaxLength(512);
            b.Property(p => p.TikTokUrl).HasMaxLength(512);
            b.Property(p => p.WebsiteUrl).HasMaxLength(512);
            b.Property(p => p.TelegramUrl).HasMaxLength(512);
            b.Property(p => p.WhatsAppUrl).HasMaxLength(512);
        });
    }
}