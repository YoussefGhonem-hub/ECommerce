namespace ECommerce.Shared.Extensions;

using System.Text.Json;

public static class JsonSerializationExtensions
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialized<T>(this T obj)
    {
        return JsonSerializer.Serialize(obj, _options);
    }

    public static T? Deserialized<T>(this string? json)
    {
        if (json == null)
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(json, _options);
    }

}

