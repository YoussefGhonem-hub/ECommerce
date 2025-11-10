using System.Security.Cryptography;
using System.Text;

namespace ECommerce.Shared.Extensions;
public static class Utils
{
    public static string GeneratePassword(int length = 10)
    {

        const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "1234567890";
        const string specials = "@#$%^&*_?";
        string allChars = letters + digits;

        var password = new StringBuilder();
        var rng = RandomNumberGenerator.Create();

        // Ensure one of each required type
        password.Append(GetRandomChar(letters, rng));     // 1 letter
        password.Append(GetRandomChar(digits, rng));      // 1 digit
        password.Append(GetRandomChar(specials, rng));    // 1 special

        // Fill the rest with letters + digits
        for (int i = 3; i < length; i++)
        {
            password.Append(GetRandomChar(allChars, rng));
        }

        return password.ToString();
    }

    private static char GetRandomChar(string chars, RandomNumberGenerator rng)
    {
        var buffer = new byte[1];
        do
        {
            rng.GetBytes(buffer);
        } while (buffer[0] >= byte.MaxValue - byte.MaxValue % chars.Length);

        return chars[buffer[0] % chars.Length];
    }
}