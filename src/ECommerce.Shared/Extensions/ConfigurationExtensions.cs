using Microsoft.Extensions.Configuration;

namespace ECommerce.Shared.Extensions;

public static class ConfigurationExtensions
{
    public static T? GetData<T>(this IConfiguration configuration, string key)
    {
        var value = configuration.GetSection(key).Get<T>();
        //if (value is null)
        //{
        //    throw new Exception($"'{key}' Section is missing");
        //}
        return value;
    }
}
