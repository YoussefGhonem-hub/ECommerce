using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Persistence;

public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Roles
        if (!await roleManager.Roles.AnyAsync())
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            await roleManager.CreateAsync(new IdentityRole("Customer"));
        }

        // Admin user
        var adminEmail = "admin@shop.com";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Administrator"
            };
            await userManager.CreateAsync(admin, "Admin@123");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Categories seed
        if (!await context.Categories.AnyAsync())
        {
            var cat1 = new Category { NameEn = "Electronics", NameAr = "إلكترونيات" };
            var cat2 = new Category { NameEn = "Fashion", NameAr = "موضة" };
            context.Categories.AddRange(cat1, cat2);
            await context.SaveChangesAsync();
        }

        // Products seed
        if (!await context.Products.AnyAsync())
        {
            var firstCat = await context.Categories.FirstAsync();
            context.Products.Add(new Product
            {
                NameEn = "iPhone 15",
                NameAr = "ايفون 15",
                SKU = "IP15",
                CategoryId = firstCat.Id,
                Price = 1200,
                StockQuantity = 50,
                Brand = "Apple"
            });
            context.Products.Add(new Product
            {
                NameEn = "Samsung TV",
                NameAr = "تلفزيون سامسونج",
                SKU = "SAMTV",
                CategoryId = firstCat.Id,
                Price = 800,
                StockQuantity = 20,
                Brand = "Samsung"
            });
            await context.SaveChangesAsync();
        }
    }
}
