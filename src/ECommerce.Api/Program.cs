using ECommerce.Domain.Entities;
using ECommerce.Infrastructure;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddFluentValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddValidatorsFromAssembly(typeof(ECommerce.Application.Common.Result<>).Assembly);
ECommerce.Application.Common.Mappings.MappingConfig.Register();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ECommerce.Application.Common.Behaviors.ValidationBehavior<,>));

builder.Services.AddInfrastructure(builder.Configuration);

var jwtSection = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddCors(o => o.AddPolicy("default", p =>
{
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
}));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ECommerce.Infrastructure.Persistence.ApplicationDbContext>();
    db.Database.EnsureCreated();
    var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<ApplicationRole>>();
    await ECommerce.Infrastructure.Persistence.AppDbContextSeed.SeedAsync(db, userManager, roleManager);
}


// apply migrations & seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ECommerce.Infrastructure.Persistence.ApplicationDbContext>();
    db.Database.Migrate();
    await ECommerce.Infrastructure.Persistence.ApplicationDbContextSeed.SeedAsync(scope.ServiceProvider);
}


app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "ECommerce API v1"); c.RoutePrefix = string.Empty; });

app.UseStaticFiles();

app.UseMiddleware<ECommerce.Api.Middleware.ExceptionMiddleware>();

app.UseCors("default");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
