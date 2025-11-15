using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace ECommerce.Infrastructure.Persistence;

public static class ApplicationDbContextSeed
{
    private record CitySeed(string nameEn, string nameAr);
    private record CountrySeed(string nameEn, string nameAr, List<CitySeed> cities);
    private record RootSeed(List<CountrySeed> countries);

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

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

        // Categories seed (existing)
        if (!await context.Categories.AnyAsync())
        {
            var cat1 = new Category { NameEn = "Electronics", NameAr = "إلكترونيات" };
            var cat2 = new Category { NameEn = "Fashion", NameAr = "موضة" };
            context.Categories.AddRange(cat1, cat2);
            await context.SaveChangesAsync();
        }

        // Products seed (existing)
        if (!await context.Products.AnyAsync())
        {
            var firstCat = await context.Categories.FirstAsync();
            context.Products.AddRange(
                new Product
                {
                    NameEn = "iPhone 15",
                    NameAr = "ايفون 15",
                    SKU = "IP15",
                    CategoryId = firstCat.Id,
                    Price = 1200,
                    StockQuantity = 50,
                    Brand = "Apple"
                },
                new Product
                {
                    NameEn = "Samsung TV",
                    NameAr = "تلفزيون سامسونج",
                    SKU = "SAMTV",
                    CategoryId = firstCat.Id,
                    Price = 800,
                    StockQuantity = 20,
                    Brand = "Samsung"
                }
            );
            await context.SaveChangesAsync();
        }

        // Countries/Cities/ShippingZones from JSON
        var fileName = "eg.locations.json";
        var candidate1 = Path.Combine(AppContext.BaseDirectory, "SeedData", fileName);
        var candidate2 = Path.Combine(AppContext.BaseDirectory, fileName);
        var candidate3 = Path.Combine(env.ContentRootPath, "src", "ECommerce.Infrastructure", "Persistence", "SeedData", fileName);
        var path = new[] { candidate1, candidate2, candidate3 }.FirstOrDefault(File.Exists);

        if (path is not null)
        {
            var json = await File.ReadAllTextAsync(path);
            var root = JsonSerializer.Deserialize<RootSeed>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (root?.countries is { Count: > 0 })
            {
                foreach (var ctry in root.countries)
                {
                    var country = await context.Countries
                        .FirstOrDefaultAsync(c => c.NameEn == ctry.nameEn && c.NameAr == ctry.nameAr);
                    if (country is null)
                    {
                        country = new Country { NameEn = ctry.nameEn, NameAr = ctry.nameAr };
                        context.Countries.Add(country);
                        await context.SaveChangesAsync();
                    }

                    // Cities
                    foreach (var ct in ctry.cities)
                    {
                        var city = await context.Cities
                            .FirstOrDefaultAsync(ci => ci.NameEn == ct.nameEn && ci.CountryId == country.Id);

                        if (city is null)
                        {
                            city = new City
                            {
                                NameEn = ct.nameEn,
                                NameAr = ct.nameAr,
                                CountryId = country.Id
                            };
                            context.Cities.Add(city);
                            await context.SaveChangesAsync();
                        }

                        // Create a ShippingZone per city (CountryId + CityId)
                        var zoneExists = await context.ShippingZones.AnyAsync(z => z.CountryId == country.Id && z.CityId == city.Id);
                        if (!zoneExists)
                        {
                            context.ShippingZones.Add(new ShippingZone
                            {
                                CountryId = country.Id,
                                CityId = city.Id
                            });
                            await context.SaveChangesAsync();
                        }
                    }
                }
            }
        }

        // Example: Free Shipping for Cairo zone
        var egypt = await context.Countries.FirstOrDefaultAsync(c => c.NameEn == "Egypt");
        var cairo = egypt is null ? null : await context.Cities.FirstOrDefaultAsync(ci => ci.CountryId == egypt.Id && ci.NameEn == "Cairo");
        if (egypt is not null && cairo is not null)
        {
            var cairoZone = await context.ShippingZones
                .FirstOrDefaultAsync(z => z.CountryId == egypt.Id && z.CityId == cairo.Id);

            if (cairoZone is not null)
            {
                var freeCairo = await context.ShippingMethods
                    .Include(m => m.Zones)
                    .FirstOrDefaultAsync(m => m.Cost == 0 && m.CostType == ShippingCostType.Flat && m.FreeShippingThreshold == 0);

                if (freeCairo is null)
                {
                    freeCairo = new ShippingMethod
                    {
                        Cost = 0,
                        CostType = ShippingCostType.Flat,
                        EstimatedTime = "1-3 days",
                        IsDefault = true,
                        FreeShippingThreshold = 0
                    };
                    freeCairo.Zones.Add(cairoZone);
                    context.ShippingMethods.Add(freeCairo);
                    await context.SaveChangesAsync();
                }
                else if (!freeCairo.Zones.Any(z => z.Id == cairoZone.Id))
                {
                    freeCairo.Zones.Add(cairoZone);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
