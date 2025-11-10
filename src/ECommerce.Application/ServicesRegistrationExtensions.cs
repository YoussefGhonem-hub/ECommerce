using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace RIS.Application;

public static class ServicesRegistrationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        //services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ECommerce.Application.Common.Behaviors.ValidationBehavior<,>));
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(typeof(ECommerce.Application.Common.Result<>).Assembly);
        ECommerce.Application.Common.Mappings.MappingConfig.Register();

        return services;
    }
}