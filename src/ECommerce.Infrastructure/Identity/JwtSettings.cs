namespace ECommerce.Infrastructure.Identity;

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int DurationInMinutes { get; set; } = 60;

    // NEW: refresh token lifetime (days)
    public int RefreshTokenTtlDays { get; set; } = 30;
}
