namespace ECommerce.Shared.Extensions;
public static class StringExtensions
{
    public static Guid ToGuid(this string? value)
    {
        if (Guid.TryParse(value, out var result))
        {
            return result;
        }
        return Guid.Empty;
    }

    public static string Capitalize(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return char.ToUpper(input[0]) + input[1..];
    }
}

