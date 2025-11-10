using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Expose EF context via application interface (used across handlers)
        services.AddScoped<ApplicationDbContext>();

        // Identity for APIs with custom role (Guid keys) and SignInManager
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
        })
        .AddRoles<ApplicationRole>()                       // use your ApplicationRole : IdentityRole<Guid>
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager()                                // REQUIRED for SignInManager<ApplicationUser>
        .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();

        // JWT options + token service
        services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
