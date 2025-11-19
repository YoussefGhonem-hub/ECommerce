using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ECommerce.Infrastructure.Persistence;

public static class AppDbContextSeed
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IWebHostEnvironment env)
    {
        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager);

        // Prefer the Admin account, not SuperAdmin
        var adminId = await GetAdminUserIdAsync(userManager);              // CHANGED
        var customerId = await GetFirstUserIdInRoleAsync(userManager, "Customer");

        await SeedCategoriesAsync(context);
        await SeedProductsAsync(context, adminId);
        await SeedFeaturedProductsAsync(context);
        await SeedFashionCatalogAsync(context, userManager);
        await SeedCustomerUserAddressesAsync(context, customerId);
        await SeedProductAttributesForTShirtsAsync(context, adminId);
        await SeedCouponsAsync(context, userManager, adminId);
        await SeedProductSettingsAsync(context, adminId);
        await SeedCountriesAndCitiesAsync(context, env);
        await SeedFreeShippingMethodAsync(context, threshold: 1000m, baseCost: 50m);
        await SeedFaqCategoriesAsync(context); // NEW - Seed FAQ categories before FAQs
        await SeedFaqsAsync(context, adminId); // NEW
    }

    private static async Task<Guid?> GetFirstUserIdInRoleAsync(UserManager<ApplicationUser> userManager, string role)
    {
        var users = await userManager.GetUsersInRoleAsync(role);
        return users.FirstOrDefault()?.Id;
    }

    // NEW: prefer the dedicated Admin user over any SuperAdmin (who is also in Admin)
    private static async Task<Guid?> GetAdminUserIdAsync(UserManager<ApplicationUser> userManager)
    {
        // Prefer the known seeded admin account
        var admin = await userManager.FindByEmailAsync("admin@shop.com");
        if (admin != null) return admin.Id;

        // Fallback to any user in Admin role excluding SuperAdmins
        var admins = await userManager.GetUsersInRoleAsync("Admin");
        if (admins == null || admins.Count == 0) return null;

        var superAdmins = await userManager.GetUsersInRoleAsync("SuperAdmin");
        var superAdminIds = new HashSet<Guid>(superAdmins.Select(u => u.Id));

        var pureAdmin = admins.FirstOrDefault(u => !superAdminIds.Contains(u.Id));
        return pureAdmin?.Id ?? admins.First().Id;
    }

    private static async Task SeedProductSettingsAsync(ApplicationDbContext context, Guid? adminId)
    {
        if (await context.ProductSettings.AnyAsync())
        {
            // Ensure existing settings have a UserId if missing
            var withoutUser = await context.ProductSettings.Where(ps => ps.UserId == null && adminId != null).ToListAsync();
            if (withoutUser.Count > 0)
            {
                foreach (var ps in withoutUser)
                    ps.UserId = adminId;
                await context.SaveChangesAsync();
            }
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var setting = new ProductSetting
        {
            Kind = DiscountKind.FixedAmount,
            Value = 10m,
            AppliesToAllProducts = true,
            StartDate = now.AddDays(-1),
            EndDate = now.AddMonths(6),
            IsActive = true,
            UserId = adminId // CHANGED
        };

        context.ProductSettings.Add(setting);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCouponsAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, Guid? adminId)
    {
        if (await context.Coupons.AnyAsync())
        {
            // Backfill missing UserId for existing coupons
            var missingUser = await context.Coupons.Where(c => c.UserId == null && adminId != null).ToListAsync();
            if (missingUser.Count > 0)
            {
                foreach (var c in missingUser)
                    c.UserId = adminId;
                await context.SaveChangesAsync();
            }
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var longEnd = now.AddMonths(12);
        var midEnd = now.AddMonths(6);
        var shortEnd = now.AddMonths(1);

        var coupons = new List<Coupon>
        {
            new Coupon { Code = "WELCOME50", FixedAmount = 50m, StartDate = now.AddDays(-7), EndDate = longEnd, UsageLimit = 1000, TimesUsed = 0, PerUserLimit = 1, IsActive = true, UserId = adminId },
            new Coupon { Code = "SAVE10", Percentage = 10m, StartDate = now.AddDays(-7), EndDate = midEnd, UsageLimit = 2000, TimesUsed = 0, PerUserLimit = 3, IsActive = true, UserId = adminId },
            new Coupon { Code = "FREESHIP", FreeShipping = true, StartDate = now.AddDays(-7), EndDate = longEnd, TimesUsed = 0, IsActive = true, UserId = adminId },
            new Coupon { Code = "BF25FS", Percentage = 25m, FreeShipping = true, StartDate = now.AddDays(-3), EndDate = shortEnd, UsageLimit = 500, TimesUsed = 0, PerUserLimit = 2, IsActive = true, UserId = adminId },
            new Coupon { Code = "VIP100", FixedAmount = 100m, StartDate = now.AddDays(-1), EndDate = longEnd, TimesUsed = 0, PerUserLimit = 5, IsActive = true, UserId = adminId }
        };

        context.Coupons.AddRange(coupons);
        await context.SaveChangesAsync();
    }

    private static async Task SeedFreeShippingMethodAsync(ApplicationDbContext context, decimal threshold, decimal baseCost)
    {
        // Load all zones as tracked entities (they were just created earlier in this seeding pass)
        var zones = await context.ShippingZones.ToListAsync();

        // Try to find an existing free-shipping method with same characteristics
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

            foreach (var z in zones)
            {
                // Avoid accidental duplicates
                if (!method.Zones.Any(existing => existing.Id == z.Id))
                    method.Zones.Add(z);
            }

            context.ShippingMethods.Add(method);
            await context.SaveChangesAsync();
            return;
        }

        // Ensure the method is attached to all zones (idempotent)
        foreach (var z in zones)
        {
            if (!method.Zones.Any(existing => existing.Id == z.Id))
                method.Zones.Add(z);
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedCountriesAndCitiesAsync(ApplicationDbContext context, IWebHostEnvironment env)
    {
        if (await context.Countries.AnyAsync())
            return;

        var path = Path.Combine(env.WebRootPath, "SeedData", "eg.locations.json");
        if (!File.Exists(path)) return;

        var json = await File.ReadAllTextAsync(path);
        var root = JsonSerializer.Deserialize<RootSeed>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (root?.Countries is null || root.Countries.Count == 0) return;

        foreach (var countrySeed in root.Countries)
        {
            var country = new Country { NameEn = countrySeed.NameEn, NameAr = countrySeed.NameAr };
            context.Countries.Add(country);
            await context.SaveChangesAsync();

            foreach (var citySeed in countrySeed.Cities)
            {
                var city = new City { NameEn = citySeed.NameEn, NameAr = citySeed.NameAr, CountryId = country.Id };
                context.Cities.Add(city);
                await context.SaveChangesAsync();

                var zone = new ShippingZone { CountryId = country.Id, CityId = city.Id };
                context.ShippingZones.Add(zone);
                await context.SaveChangesAsync();
            }
        }

        var egypt = await context.Countries.FirstOrDefaultAsync(c => c.NameEn == "Egypt");
        if (egypt != null)
        {
            var cairo = await context.Cities.FirstOrDefaultAsync(c => c.CountryId == egypt.Id && c.NameEn == "Cairo");
            if (cairo != null)
            {
                var cairoZone = await context.ShippingZones.FirstAsync(z => z.CountryId == egypt.Id && z.CityId == cairo.Id);
                var freeExists = await context.ShippingMethods
                    .Include(m => m.Zones)
                    .AnyAsync(m => m.Cost == 0 && m.FreeShippingThreshold == 0 && m.Zones.Any(z => z.Id == cairoZone.Id));

                if (!freeExists)
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

    // CHANGED: Seed addresses for the Customer user instead of Admin
    private static async Task SeedCustomerUserAddressesAsync(ApplicationDbContext context, Guid? customerId)
    {
        if (customerId == null) return;

        var existingCount = await context.UserAddresses.CountAsync(a => a.UserId == customerId);
        if (existingCount >= 3) return;

        var egypt = await context.Countries.FirstOrDefaultAsync(c => c.NameEn == "Egypt");
        if (egypt is null) return;

        var cairo = await context.Cities.FirstOrDefaultAsync(c => c.NameEn == "Cairo" && c.CountryId == egypt.Id)
                    ?? await context.Cities.FirstOrDefaultAsync(c => c.CountryId == egypt.Id);
        var alex = await context.Cities.FirstOrDefaultAsync(c => c.NameEn == "Alexandria" && c.CountryId == egypt.Id) ?? cairo;
        var giza = await context.Cities.FirstOrDefaultAsync(c => c.NameEn == "Giza" && c.CountryId == egypt.Id) ?? cairo;

        if (cairo is null || alex is null || giza is null) return;

        var addresses = new List<UserAddress>
        {
            new UserAddress { UserId = customerId, FullName = "Customer Home", CityId = cairo.Id, Street = "123 Nile Corniche", MobileNumber = "+201001112223", HouseNo = "Bldg 5", IsDefault = true },
            new UserAddress { UserId = customerId, FullName = "Customer Work", CityId = alex.Id, Street = "45 Port Gate Street", MobileNumber = "+201001112224", HouseNo = "Suite 12A", IsDefault = false },
            new UserAddress { UserId = customerId, FullName = "Customer Warehouse", CityId = giza.Id, Street = "Industrial Zone 7", MobileNumber = "+201001112225", HouseNo = "Warehouse 3", IsDefault = false }
        };

        context.UserAddresses.AddRange(addresses);
        await context.SaveChangesAsync();
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
            if ((await userManager.CreateAsync(superAdmin, "SuperAdmin@123")).Succeeded)
                await userManager.AddToRolesAsync(superAdmin, new[] { "SuperAdmin", "Admin" });
        }

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
            if ((await userManager.CreateAsync(admin, "Admin@123")).Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

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
            if ((await userManager.CreateAsync(customer, "Customer@123")).Succeeded)
                await userManager.AddToRoleAsync(customer, "Customer");
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
            context.FeaturedProducts.Add(new FeaturedProduct { ProductId = prod.Id, DisplayOrder = 1 });
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedProductAttributesForTShirtsAsync(ApplicationDbContext context, Guid? adminId)
    {
        var colorAttr = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Name == "Color")
                        ?? (await context.ProductAttributes.AddAsync(new ProductAttribute { Name = "Color" })).Entity;
        var sizeAttr = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Name == "Size")
                       ?? (await context.ProductAttributes.AddAsync(new ProductAttribute { Name = "Size" })).Entity;
        var materialAttr = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Name == "Material")
                           ?? (await context.ProductAttributes.AddAsync(new ProductAttribute { Name = "Material" })).Entity;

        await context.SaveChangesAsync();

        var colorValues = await EnsureAttributeValuesAsync(context, colorAttr, "White", "Black", "Red", "Blue");
        var sizeValues = await EnsureAttributeValuesAsync(context, sizeAttr, "S", "M", "L", "XL");
        var materialValues = await EnsureAttributeValuesAsync(context, materialAttr, "Cotton", "Polyester");

        var tshirt = await context.Products.FirstOrDefaultAsync(p => p.SKU == "TS-1");
        if (tshirt is null) return;

        // Ensure product has admin owner
        if (tshirt.UserId == null && adminId != null)
        {
            tshirt.UserId = adminId;
            await context.SaveChangesAsync();
        }

        var existingMappings = await context.ProductAttributeMappings
            .Where(m => m.ProductId == tshirt.Id)
            .ToListAsync();

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

    private static async Task SeedProductsAsync(ApplicationDbContext context, Guid? adminId)
    {
        var fashion = await context.Categories.FirstOrDefaultAsync(c => c.NameEn == "Fashion");
        if (fashion is null) return;

        var colorAttr = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Name == "Color")
                        ?? (await context.ProductAttributes.AddAsync(new ProductAttribute { Name = "Color" })).Entity;
        var sizeAttr = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Name == "Size")
                       ?? (await context.ProductAttributes.AddAsync(new ProductAttribute { Name = "Size" })).Entity;

        await context.SaveChangesAsync();

        var colorValues = await EnsureAttributeValuesAsync(context, colorAttr, "White", "Black", "Red", "Blue", "Green");
        var sizeValues = await EnsureAttributeValuesAsync(context, sizeAttr, "XS", "S", "M", "L", "XL");

        var existingFashion = await context.Products
            .Where(p => p.CategoryId == fashion.Id)
            .ToListAsync();

        // Backfill UserId for existing products
        if (adminId != null)
        {
            var withoutUser = existingFashion.Where(p => p.UserId == null).ToList();
            if (withoutUser.Count > 0)
            {
                foreach (var p in withoutUser)
                    p.UserId = adminId;
                await context.SaveChangesAsync();
            }
        }

        var toCreate = 20 - existingFashion.Count;
        if (toCreate <= 0) return;

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
                Color = colorValues[idx % colorValues.Count].Value,
                AllowBackorder = rnd.Next(0, 3) == 0,
                UserId = adminId // CHANGED
            };

            newProducts.Add(p);
        }

        await context.Products.AddRangeAsync(newProducts);
        await context.SaveChangesAsync();

        var mappings = new List<ProductAttributeMapping>();
        foreach (var p in newProducts)
        {
            foreach (var s in sizeValues)
            {
                mappings.Add(new ProductAttributeMapping
                {
                    ProductId = p.Id,
                    ProductAttributeId = sizeAttr.Id,
                    ProductAttributeValueId = s.Id
                });
            }

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
                        UserId = user.Id.ToString(),
                        User = user,
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

        foreach (var p in newProducts)
        {
            var avg = await context.ProductReviews
                .Where(r => r.ProductId == p.Id && r.IsApproved)
                .AverageAsync(r => (double?)r.Rating) ?? 0.0;

            p.AverageRating = Math.Round(avg, 2);
        }

        await context.SaveChangesAsync();
    }

    // NEW: Seed FAQ categories
    private static async Task SeedFaqCategoriesAsync(ApplicationDbContext context)
    {
        if (await context.FaqCategories.AnyAsync()) return;

        var categories = new List<FaqCategory>
        {
            new() { NameEn = "Delivery", NameAr = "التسليم", DisplayOrder = 1, IsActive = true },
            new() { NameEn = "Cancellation & Return My Orders", NameAr = "إلغاء وإرجاع طلباتي", DisplayOrder = 2, IsActive = true },
            new() { NameEn = "Product & Services", NameAr = "المنتج والخدمات", DisplayOrder = 3, IsActive = true }
        };

        context.FaqCategories.AddRange(categories);
        await context.SaveChangesAsync();
    }

    // Replace previous SeedFaqsAsync with this version (assumes categories exist).
    private static async Task SeedFaqsAsync(ApplicationDbContext context, Guid? adminId)
    {
        if (await context.Faqs.AnyAsync()) return;

        var delivery = await context.FaqCategories.FirstAsync(c => c.NameEn == "Delivery");
        var returns = await context.FaqCategories.FirstAsync(c => c.NameEn == "Cancellation & Return My Orders");
        var product = await context.FaqCategories.FirstAsync(c => c.NameEn == "Product & Services");

        var faqs = new List<Faq>
        {
            // Delivery
            new()
            {
                FaqCategoryId = delivery.Id,
                QuestionEn = "How long does delivery take?",
                QuestionAr = "كم يستغرق وقت التسليم؟",
                AnswerEn = "Standard delivery typically takes 1-3 days as shown in the shipping method. Some cities (e.g., Cairo) may qualify for faster or free delivery.",
                AnswerAr = "عادةً ما يستغرق التسليم القياسي من 1 إلى 3 أيام كما هو موضح في طريقة الشحن. بعض المدن (مثل القاهرة) قد تحصل على توصيل أسرع أو مجاني.",
                DisplayOrder = 1
            },
            new()
            {
                FaqCategoryId = delivery.Id,
                QuestionEn = "Do you offer free shipping?",
                QuestionAr = "هل تقدمون شحنًا مجانيًا؟",
                AnswerEn = "Yes. Orders reaching the free shipping threshold or in special zones (like Cairo) can qualify for free shipping.",
                AnswerAr = "نعم. الطلبات التي تصل إلى حد الشحن المجاني أو في المناطق الخاصة (مثل القاهرة) يمكن أن تحصل على الشحن المجاني.",
                DisplayOrder = 2
            },
            new()
            {
                FaqCategoryId = delivery.Id,
                QuestionEn = "How can I track my order?",
                QuestionAr = "كيف يمكنني تتبع طلبي؟",
                AnswerEn = "Track your order from the Orders section in your account using the order or tracking number when available.",
                AnswerAr = "تتبع طلبك من قسم الطلبات في حسابك باستخدام رقم الطلب أو رقم التتبع عند توفره.",
                DisplayOrder = 3
            },

            // Cancellation & Return
            new()
            {
                FaqCategoryId = returns.Id,
                QuestionEn = "Can I cancel my order before it ships?",
                QuestionAr = "هل يمكنني إلغاء طلبي قبل شحنه؟",
                AnswerEn = "Cancellation is allowed while status is Pending. After Processing or Shipped, cancellation may not be possible.",
                AnswerAr = "يمكن الإلغاء طالما أن حالة الطلب قيد الانتظار. بعد الانتقال للمعالجة أو الشحن قد لا يكون الإلغاء ممكنًا.",
                DisplayOrder = 1
            },
            new()
            {
                FaqCategoryId = returns.Id,
                QuestionEn = "What is your return policy?",
                QuestionAr = "ما هي سياسة الإرجاع؟",
                AnswerEn = "Returns accepted within 14 days if unused, in original packaging, and not excluded (e.g., hygiene items).",
                AnswerAr = "يُقبل الإرجاع خلال 14 يومًا إذا كان غير مستخدم وفي عبوته الأصلية وغير مستثنى (مثل المنتجات الصحية).",
                DisplayOrder = 2
            },
            new()
            {
                FaqCategoryId = returns.Id,
                QuestionEn = "How do I request a return?",
                QuestionAr = "كيف أطلب إرجاع منتج؟",
                AnswerEn = "Submit a request from the Orders page with order number and reason. Approved requests receive instructions.",
                AnswerAr = "قدّم طلب الإرجاع من صفحة الطلبات مع رقم الطلب والسبب. الطلبات المعتمدة تحصل على التعليمات.",
                DisplayOrder = 3
            },

            // Product & Services
            new()
            {
                FaqCategoryId = product.Id,
                QuestionEn = "How do I choose size and color?",
                QuestionAr = "كيف أختار المقاس واللون؟",
                AnswerEn = "Select attributes (Size, Color) on the product page before adding to cart. We map sizes (S–XL) and colors (White, Black, etc.).",
                AnswerAr = "اختر السمات (المقاس، اللون) من صفحة المنتج قبل الإضافة للسلة. نوفر مقاسات (S–XL) وألوان (أبيض، أسود...).",
                DisplayOrder = 1
            },
            new()
            {
                FaqCategoryId = product.Id,
                QuestionEn = "How are product ratings calculated?",
                QuestionAr = "كيف يتم حساب تقييمات المنتج؟",
                AnswerEn = "Average rating is computed from approved customer reviews (1–5 stars).",
                AnswerAr = "يُحسب متوسط التقييم من المراجعات المعتمدة للعملاء (1–5 نجوم).",
                DisplayOrder = 2
            },
            new()
            {
                FaqCategoryId = product.Id,
                QuestionEn = "Can coupons stack with discounts?",
                QuestionAr = "هل يمكن استخدام القسائم مع الخصومات؟",
                AnswerEn = "Coupons apply if valid; stacking depends on coupon restrictions and active product discounts.",
                AnswerAr = "تُطبق القسائم إذا كانت صالحة؛ الجمع يعتمد على قيود القسيمة والخصومات النشطة.",
                DisplayOrder = 3
            },
            new()
            {
                FaqCategoryId = product.Id,
                QuestionEn = "Are the brands authentic?",
                QuestionAr = "هل العلامات التجارية أصلية؟",
                AnswerEn = "We list verified brand products (Nike, Adidas, Levi's, etc.) from trusted sources.",
                AnswerAr = "نُدرج منتجات علامات تجارية موثوقة (Nike، Adidas، Levi's، إلخ) من مصادر موثوقة.",
                DisplayOrder = 4
            }
        };

        context.Faqs.AddRange(faqs);
        await context.SaveChangesAsync();
    }

    #region JSON DTOs
    private sealed class RootSeed { public List<CountrySeed> Countries { get; set; } = new(); }
    private sealed class CountrySeed { public string NameEn { get; set; } = string.Empty; public string NameAr { get; set; } = string.Empty; public List<CitySeed> Cities { get; set; } = new(); }
    private sealed class CitySeed { public string NameEn { get; set; } = string.Empty; public string NameAr { get; set; } = string.Empty; }
    #endregion
}