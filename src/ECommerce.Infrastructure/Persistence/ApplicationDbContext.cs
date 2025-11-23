using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Extensions;
using ECommerce.Shared.CurrentUser;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ECommerce.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Country> Countries => Set<Country>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CartItemAttribute> CartItemAttributes => Set<CartItemAttribute>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<ProductSetting> ProductSettings => Set<ProductSetting>();
    public DbSet<FavoriteProduct> FavoriteProducts => Set<FavoriteProduct>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<ShippingZone> ShippingZones => Set<ShippingZone>();
    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    public DbSet<ProductAttributeMapping> ProductAttributeMappings => Set<ProductAttributeMapping>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<FeaturedProduct> FeaturedProducts => Set<FeaturedProduct>();
    public DbSet<FaqCategory> FaqCategories => Set<FaqCategory>();
    public DbSet<Faq> Faqs => Set<Faq>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.GetOnlyNotDeletedEntities();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditing();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditing()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = CurrentUser.Id;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not BaseAuditableEntity auditable) continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    if (auditable.CreatedDate == default) auditable.CreatedDate = now;
                    if (auditable.CreatedBy == Guid.Empty && userId.HasValue) auditable.CreatedBy = userId.Value;
                    auditable.IsDeleted = false;
                    break;

                case EntityState.Modified:
                    auditable.ModifiedDate = now;
                    if (userId.HasValue) auditable.ModifiedBy = userId.Value;
                    entry.Property(nameof(BaseAuditableEntity.CreatedDate)).IsModified = false;
                    entry.Property(nameof(BaseAuditableEntity.CreatedBy)).IsModified = false;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    auditable.IsDeleted = true;
                    auditable.DeletedDate = now;
                    if (userId.HasValue) auditable.DeletedBy = userId.Value;
                    break;
            }
        }
    }


}
