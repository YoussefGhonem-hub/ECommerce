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

    private static async Task SeedProductsAsync(ApplicationDbContext context)
    {
        if (!await context.Products.AnyAsync())
        {
            var firstCat = await context.Categories.FirstAsync();

            context.Products.AddRange(
                new Product
                {
                    NameEn = "iPhone 15",
                    NameAr = "آيفون 15",
                    SKU = "IP15",
                    CategoryId = firstCat.Id,
                    Price = 999m,
                    StockQuantity = 50,
                    Brand = "Apple",
                    Color = "Black",
                    AllowBackorder = false
                },
                new Product
                {
                    NameEn = "T-Shirt",
                    NameAr = "قميص",
                    SKU = "TS-1",
                    CategoryId = firstCat.Id,
                    Price = 20m,
                    StockQuantity = 200,
                    Brand = "Zara",
                    Color = "White",
                    AllowBackorder = true
                }
            );

            await context.SaveChangesAsync();
        }
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
}
