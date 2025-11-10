using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Persistence;

public static class AppDbContextSeed
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        // 1. Roles
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

        // 2. SuperAdmin user
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

        // 3. Admin user (secondary)
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

        // 4. Customer demo user
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

        // 5. Categories
        if (!context.Categories.Any())
        {
            context.Categories.AddRange(
                new Category { NameEn = "Electronics", NameAr = "إلكترونيات" },
                new Category { NameEn = "Fashion", NameAr = "أزياء" },
                new Category { NameEn = "Home & Kitchen", NameAr = "منزل ومطبخ" }
            );
            await context.SaveChangesAsync();
        }

        // 6. Products (ensure at least one category exists)
        if (!context.Products.Any())
        {
            var firstCat = context.Categories.First();
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
        }

        // 7. Featured product sample
        if (!context.FeaturedProducts.Any() && context.Products.Any())
        {
            var prod = context.Products.First();
            context.FeaturedProducts.Add(new FeaturedProduct
            {
                ProductId = prod.Id,
                DisplayOrder = 1
            });
        }

        await context.SaveChangesAsync();
    }
}
