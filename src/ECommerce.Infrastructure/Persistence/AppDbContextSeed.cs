using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

        // CHANGED: pass userManager to seed coupons with Admin user id
        await SeedCouponsAsync(context, userManager);

        await SeedCountriesAndCitiesAsync(context); // NEW
        await SeedFreeShippingMethodAsync(context, threshold: 1000m, baseCost: 50m);
        //await SeedFreeShippingMethodForCitiesAsync(context, threshold: 1000m, baseCost: 50m, citiesEn: new[] { "Cairo" });
    }
    private static async Task SeedCouponsAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        if (await context.Coupons.AnyAsync())
            return;

        // Try to get Admin by the seeded email; fallback to first user in Admin role
        var admin = await userManager.FindByEmailAsync("admin@shop.com");
        if (admin is null)
        {
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            admin = admins.FirstOrDefault();
        }

        var adminId = (Guid?)admin?.Id;

        var now = DateTimeOffset.UtcNow;
        var longEnd = now.AddMonths(12);
        var midEnd = now.AddMonths(6);
        var shortEnd = now.AddMonths(1);

        var coupons = new List<Coupon>
        {
            new Coupon
            {
                Code = "WELCOME50",
                FixedAmount = 50m,
                Percentage = null,
                FreeShipping = false,
                StartDate = now.AddDays(-7),
                EndDate = longEnd,
                UsageLimit = 1000,
                TimesUsed = 0,
                PerUserLimit = 1,
                IsActive = true,
                UserId = adminId
            },
            new Coupon
            {
                Code = "SAVE10",
                FixedAmount = null,
                Percentage = 10m,
                FreeShipping = false,
                StartDate = now.AddDays(-7),
                EndDate = midEnd,
                UsageLimit = 2000,
                TimesUsed = 0,
                PerUserLimit = 3,
                IsActive = true,
                UserId = adminId
            },
            new Coupon
            {
                Code = "FREESHIP",
                FixedAmount = null,
                Percentage = null,
                FreeShipping = true,
                StartDate = now.AddDays(-7),
                EndDate = longEnd,
                UsageLimit = null,     // unlimited
                TimesUsed = 0,
                PerUserLimit = null,   // unlimited per user
                IsActive = true,
                UserId = adminId
            },
            new Coupon
            {
                Code = "BF25FS",
                FixedAmount = null,
                Percentage = 25m,
                FreeShipping = true,
                StartDate = now.AddDays(-3),
                EndDate = shortEnd,
                UsageLimit = 500,
                TimesUsed = 0,
                PerUserLimit = 2,
                IsActive = true,
                UserId = adminId
            },
            new Coupon
            {
                Code = "VIP100",
                FixedAmount = 100m,
                Percentage = null,
                FreeShipping = false,
                StartDate = now.AddDays(-1),
                EndDate = longEnd,
                UsageLimit = null,     // unlimited global
                TimesUsed = 0,
                PerUserLimit = 5,
                IsActive = true,
                UserId = adminId
            }
        };

        context.Coupons.AddRange(coupons);
        await context.SaveChangesAsync();
    }
    private static async Task SeedFreeShippingMethodAsync(ApplicationDbContext context, decimal threshold, decimal baseCost)
    {
        // Check if such a method already exists
        var method = await context.ShippingMethods
            .Include(m => m.Zones)
            .FirstOrDefaultAsync(m =>
                m.FreeShippingThreshold == threshold &&
                m.CostType == ShippingCostType.Flat &&
                m.Cost == baseCost);

        if (method is null)
        {
            method = new ShippingMethod
            {
                Cost = baseCost,
                CostType = ShippingCostType.Flat,
                EstimatedTime = "1-3 days",
                IsDefault = true,              // make it the default pick
                FreeShippingThreshold = threshold
            };

            // Attach to all zones
            var zones = await context.ShippingZones.AsNoTracking().ToListAsync();
            foreach (var z in zones)
                method.Zones.Add(z);

            context.ShippingMethods.Add(method);
            await context.SaveChangesAsync();
            return;
        }

        // Ensure attached to all zones (idempotent)
        var allZones = await context.ShippingZones.AsNoTracking().Select(z => z.Id).ToListAsync();
        var attached = method.Zones.Select(z => z.Id).ToHashSet();
        var missingZoneIds = allZones.Where(id => !attached.Contains(id)).ToList();
        if (missingZoneIds.Count > 0)
        {
            var missingZones = await context.ShippingZones.Where(z => missingZoneIds.Contains(z.Id)).ToListAsync();
            foreach (var z in missingZones)
                method.Zones.Add(z);

            await context.SaveChangesAsync();
        }
    }

    // Variant: seed the free-shipping method for specific cities only (by English city name)
    private static async Task SeedFreeShippingMethodForCitiesAsync(ApplicationDbContext context, decimal threshold, decimal baseCost, IEnumerable<string> citiesEn)
    {
        var cityNames = citiesEn.Select(n => n.Trim()).Where(n => n.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (cityNames.Count == 0) return;

        var targetZones = await context.ShippingZones
            .Include(z => z.City)
            .Where(z => z.CityId != null && z.City != null && cityNames.Contains(z.City.NameEn))
            .ToListAsync();

        if (targetZones.Count == 0) return;

        var method = await context.ShippingMethods
            .Include(m => m.Zones)
            .FirstOrDefaultAsync(m =>
                m.FreeShippingThreshold == threshold &&
                m.CostType == ShippingCostType.Flat &&
                m.Cost == baseCost);

        if (method is null)
        {
            method = new ShippingMethod
            {
                Cost = baseCost,
                CostType = ShippingCostType.Flat,
                EstimatedTime = "1-3 days",
                IsDefault = true,
                FreeShippingThreshold = threshold
            };
            context.ShippingMethods.Add(method);
        }

        // Attach to the specific zones only
        var attachedIds = method.Zones.Select(z => z.Id).ToHashSet();
        foreach (var z in targetZones)
            if (!attachedIds.Contains(z.Id))
                method.Zones.Add(z);

        await context.SaveChangesAsync();
    }

    private static async Task SeedCountriesAndCitiesAsync(ApplicationDbContext context)
    {
        // Skip if already seeded
        if (await context.Countries.AnyAsync())
            return;

        var fileName = "eg.locations.json";
        // Assume file copied to output directory (Content + Copy if newer)
        var potentialPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "SeedData", fileName),
            Path.Combine(AppContext.BaseDirectory, fileName),
            // Fallback to source relative (dev-time)
            Path.Combine(Directory.GetCurrentDirectory(), "src", "ECommerce.Infrastructure", "Persistence", "SeedData", fileName)
        };

        var path = potentialPaths.FirstOrDefault(File.Exists);
        if (path is null)
            return; // File not found; silently skip

        var json = await File.ReadAllTextAsync(path);
        var root = JsonSerializer.Deserialize<RootSeed>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (root?.Countries is null || root.Countries.Count == 0)
            return;

        foreach (var countrySeed in root.Countries)
        {
            // Create country
            var country = new Country
            {
                NameEn = countrySeed.NameEn,
                NameAr = countrySeed.NameAr
            };
            context.Countries.Add(country);
            await context.SaveChangesAsync();

            // Cities
            foreach (var citySeed in countrySeed.Cities)
            {
                var city = new City
                {
                    NameEn = citySeed.NameEn,
                    NameAr = citySeed.NameAr,
                    CountryId = country.Id
                };
                context.Cities.Add(city);
                await context.SaveChangesAsync();

                // ShippingZone per city (Country + City)
                var zone = new ShippingZone
                {
                    CountryId = country.Id,
                    CityId = city.Id
                };
                context.ShippingZones.Add(zone);
                await context.SaveChangesAsync();
            }
        }

        // Optional: create a free shipping method for Cairo
        var egypt = await context.Countries.FirstOrDefaultAsync(c => c.NameEn == "Egypt");
        if (egypt != null)
        {
            var cairo = await context.Cities.FirstOrDefaultAsync(c => c.CountryId == egypt.Id && c.NameEn == "Cairo");
            if (cairo != null)
            {
                var cairoZone = await context.ShippingZones.FirstAsync(z => z.CountryId == egypt.Id && z.CityId == cairo.Id);
                var freeMethodExists = await context.ShippingMethods
                    .Include(m => m.Zones)
                    .AnyAsync(m => m.Cost == 0 && m.FreeShippingThreshold == 0 && m.Zones.Any(z => z.Id == cairoZone.Id));

                if (!freeMethodExists)
                {
                    var freeMethod = new ShippingMethod
                    {
                        Cost = 0,
                        CostType = ShippingCostType.Flat,
                        EstimatedTime = "1-3 days",
                        IsDefault = true,
                        FreeShippingThreshold = 0
                    };
                    freeMethod.Zones.Add(cairoZone);
                    context.ShippingMethods.Add(freeMethod);
                    await context.SaveChangesAsync();
                }
            }
        }
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

    #region  JSON DTOs
    private sealed class RootSeed
    {
        public List<CountrySeed> Countries { get; set; } = new();
    }

    private sealed class CountrySeed
    {
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public List<CitySeed> Cities { get; set; } = new();
    }

    private sealed class CitySeed
    {
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
    }
    #endregion
}

