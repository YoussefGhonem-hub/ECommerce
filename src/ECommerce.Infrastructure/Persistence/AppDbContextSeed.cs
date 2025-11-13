using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

public static class AppDbContextSeed
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager);

        await SeedCategoriesAsync(context);
        await SeedProductsAsync(context);
        await SeedFeaturedProductsAsync(context);

        // Seed only Fashion products (+ attributes Color/Size, mappings, reviews, and AverageRating)
        await SeedFashionCatalogAsync(context, userManager);

        await SeedProductAttributesForTShirtsAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        string[] roles = { "SuperAdmin", "Admin", "Customer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = role,
                    NormalizedName = role.ToUpperInvariant()
                });
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        // SuperAdmin
        var superAdminEmail = "superadmin@shop.com";
        var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
        if (superAdmin is null)
        {
            superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true,
                FullName = "Super Administrator",
                IsActive = true
            };
            var createResult = await userManager.CreateAsync(superAdmin, "SuperAdmin@123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRolesAsync(superAdmin, new[] { "SuperAdmin", "Admin" });
            }
        }

        // Admin
        var adminEmail = "admin@shop.com";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "System Admin",
                IsActive = true
            };
            var createResult = await userManager.CreateAsync(admin, "Admin@123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        // Customer
        var customerEmail = "customer@shop.com";
        var customer = await userManager.FindByEmailAsync(customerEmail);
        if (customer is null)
        {
            customer = new ApplicationUser
            {
                UserName = customerEmail,
                Email = customerEmail,
                EmailConfirmed = true,
                FullName = "Demo Customer",
                IsActive = true
            };
            var createResult = await userManager.CreateAsync(customer, "Customer@123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(customer, "Customer");
            }
        }
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                new Category { NameEn = "Electronics", NameAr = "إلكترونيات" },
                new Category { NameEn = "Fashion", NameAr = "أزياء" },
                new Category { NameEn = "Home & Kitchen", NameAr = "منزل ومطبخ" }
            );

            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedFashionCatalogAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        // Ensure base attributes exist: Color, Size (+ allowed values)
        var colorAttr = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Name == "Color")
                        ?? (await context.ProductAttributes.AddAsync(new ProductAttribute { Name = "Color" })).Entity;

        var sizeAttr = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Name == "Size")
                       ?? (await context.ProductAttributes.AddAsync(new ProductAttribute { Name = "Size" })).Entity;

        await context.SaveChangesAsync();
    }

    private static async Task SeedFeaturedProductsAsync(ApplicationDbContext context)
    {
        if (!await context.FeaturedProducts.AnyAsync() && await context.Products.AnyAsync())
        {
            var prod = await context.Products.FirstAsync();
            context.FeaturedProducts.Add(new FeaturedProduct
            {
                ProductId = prod.Id,
                DisplayOrder = 1
            });

            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedProductAttributesForTShirtsAsync(ApplicationDbContext context)
    {
        // Ensure base attributes exist
        var colorAttr = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Name == "Color")
                        ?? (await context.ProductAttributes.AddAsync(new ProductAttribute { Name = "Color" })).Entity;

        var sizeAttr = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Name == "Size")
                       ?? (await context.ProductAttributes.AddAsync(new ProductAttribute { Name = "Size" })).Entity;

        var materialAttr = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Name == "Material")
                           ?? (await context.ProductAttributes.AddAsync(new ProductAttribute { Name = "Material" })).Entity;

        await context.SaveChangesAsync();

        // Ensure allowed values exist
        var colorValues = await EnsureAttributeValuesAsync(context, colorAttr, "White", "Black", "Red", "Blue");
        var sizeValues = await EnsureAttributeValuesAsync(context, sizeAttr, "S", "M", "L", "XL");
        var materialValues = await EnsureAttributeValuesAsync(context, materialAttr, "Cotton", "Polyester");

        // Find the T-Shirt product by SKU
        var tshirt = await context.Products.FirstOrDefaultAsync(p => p.SKU == "TS-1");
        if (tshirt is null) return;

        var existingMappings = await context.ProductAttributeMappings
            .Where(m => m.ProductId == tshirt.Id)
            .ToListAsync();

        // Helper to avoid duplicates
        void EnsureMapping(Guid productId, Guid attributeId, Guid? valueId)
        {
            var found = existingMappings.FirstOrDefault(m =>
                m.ProductId == productId &&
                m.ProductAttributeId == attributeId &&
                m.ProductAttributeValueId == valueId);

            if (found is null)
            {
                context.ProductAttributeMappings.Add(new ProductAttributeMapping
                {
                    ProductId = productId,
                    ProductAttributeId = attributeId,
                    ProductAttributeValueId = valueId
                });
            }
        }

        var white = colorValues.First(v => v.Value == "White");
        var sizeM = sizeValues.First(v => v.Value == "M");
        var cotton = materialValues.First(v => v.Value == "Cotton");

        EnsureMapping(tshirt.Id, colorAttr.Id, white.Id);
        EnsureMapping(tshirt.Id, sizeAttr.Id, sizeM.Id);
        EnsureMapping(tshirt.Id, materialAttr.Id, cotton.Id);

        await context.SaveChangesAsync();
    }

    private static async Task<List<ProductAttributeValue>> EnsureAttributeValuesAsync(
        ApplicationDbContext context,
        ProductAttribute attribute,
        params string[] values)
    {
        var existing = await context.ProductAttributeValues
            .Where(v => v.ProductAttributeId == attribute.Id)
            .ToListAsync();

        var ensured = new List<ProductAttributeValue>();
        foreach (var val in values)
        {
            var found = existing.FirstOrDefault(v => v.Value == val);
            if (found is null)
            {
                var entity = (await context.ProductAttributeValues.AddAsync(new ProductAttributeValue
                {
                    ProductAttributeId = attribute.Id,
                    Value = val
                })).Entity;

                ensured.Add(entity);
            }
            else
            {
                ensured.Add(found);
            }
        }

        await context.SaveChangesAsync();
        return ensured;
    }

    private static async Task SeedProductsAsync(ApplicationDbContext context)
    {
        // Seed only Fashion products: 15 items, with Color/Size attributes, reviews, and AverageRating
        var fashion = await context.Categories.FirstOrDefaultAsync(c => c.NameEn == "Fashion");
        if (fashion is null) return;

        // Ensure attributes exist
        var colorAttr = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Name == "Color")
                        ?? (await context.ProductAttributes.AddAsync(new ProductAttribute { Name = "Color" })).Entity;

        var sizeAttr = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Name == "Size")
                       ?? (await context.ProductAttributes.AddAsync(new ProductAttribute { Name = "Size" })).Entity;

        await context.SaveChangesAsync();

        // Allowed values
        var colorValues = await EnsureAttributeValuesAsync(context, colorAttr, "White", "Black", "Red", "Blue", "Green");
        var sizeValues = await EnsureAttributeValuesAsync(context, sizeAttr, "XS", "S", "M", "L", "XL");

        // If we already have 15 fashion products, stop
        var existingFashion = await context.Products
            .Where(p => p.CategoryId == fashion.Id)
            .ToListAsync();

        var toCreate = 20 - existingFashion.Count;
        if (toCreate <= 0) return;

        // Seed products
        var rnd = new Random(4242);
        string[] brands = { "Zara", "H&M", "Nike", "Adidas", "Uniqlo", "Levi's", "Gap" };

        var newProducts = new List<Product>();
        for (int i = 0; i < toCreate; i++)
        {
            var idx = existingFashion.Count + i + 1;
            var brand = brands[idx % brands.Length];
            var price = Math.Round((decimal)rnd.Next(15, 180) + rnd.Next(0, 99) / 100m, 2);

            var p = new Product
            {
                NameEn = $"Fashion Item {idx}",
                NameAr = $"منتج أزياء {idx}",
                SKU = $"FASH-{idx:D4}",
                CategoryId = fashion.Id,
                Price = price,
                StockQuantity = rnd.Next(20, 200),
                Brand = brand,
                Color = colorValues[idx % colorValues.Count].Value, // snapshot main color
                AllowBackorder = rnd.Next(0, 3) == 0 // ~33%
            };

            newProducts.Add(p);
        }

        await context.Products.AddRangeAsync(newProducts);
        await context.SaveChangesAsync();

        // Map attributes: all sizes + 3 rotating colors per product
        var mappings = new List<ProductAttributeMapping>();
        foreach (var p in newProducts)
        {
            // All sizes
            foreach (var s in sizeValues)
            {
                mappings.Add(new ProductAttributeMapping
                {
                    ProductId = p.Id,
                    ProductAttributeId = sizeAttr.Id,
                    ProductAttributeValueId = s.Id
                });
            }

            // 3 colors (distinct, rotating)
            var colorIdxs = Enumerable.Range(0, 3)
                .Select(k => (int)(((p.Id.GetHashCode() + k) & 0x7FFFFFFF) % colorValues.Count))
                .Distinct();

            foreach (var ci in colorIdxs)
            {
                var cv = colorValues[ci];
                mappings.Add(new ProductAttributeMapping
                {
                    ProductId = p.Id,
                    ProductAttributeId = colorAttr.Id,
                    ProductAttributeValueId = cv.Id
                });
            }
        }

        await context.ProductAttributeMappings.AddRangeAsync(mappings);
        await context.SaveChangesAsync();

        // Seed reviews (approved) from first 3 users and set AverageRating
        var reviewers = await context.Users.Take(3).ToListAsync();
        if (reviewers.Count > 0)
        {
            var reviews = new List<ProductReview>();
            foreach (var p in newProducts)
            {
                var ratings = new[] { 5, 4, 3 };
                for (int i = 0; i < reviewers.Count && i < ratings.Length; i++)
                {
                    var user = reviewers[i];
                    reviews.Add(new ProductReview
                    {
                        ProductId = p.Id,
                        UserId = user.Id.ToString(), // keep string FK copy
                        User = user,                  // set real FK
                        Rating = ratings[i],
                        Comment = $"Seed review {i + 1} for {p.NameEn}",
                        IsApproved = true
                    });
                }
            }

            if (reviews.Count > 0)
            {
                await context.ProductReviews.AddRangeAsync(reviews);
                await context.SaveChangesAsync();
            }
        }

        // Compute AverageRating for each newly created product
        foreach (var p in newProducts)
        {
            var avg = await context.ProductReviews
                .Where(r => r.ProductId == p.Id && r.IsApproved)
                .AverageAsync(r => (double?)r.Rating) ?? 0.0;

            p.AverageRating = Math.Round(avg, 2);
        }

        await context.SaveChangesAsync();
    }
}
