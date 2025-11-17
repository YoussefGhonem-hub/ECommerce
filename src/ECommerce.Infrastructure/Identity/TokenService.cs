using ECommerce.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ECommerce.Infrastructure.Identity;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user, IList<string> roles);
    (string AccessToken, DateTime ExpiresAtUtc) GenerateAccessToken(ApplicationUser user, IList<string> roles);
}

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public (string AccessToken, DateTime ExpiresAtUtc) GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_settings.DurationInMinutes);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("Id", user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName ?? string.Empty),
            new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
            new Claim("preferred_username", user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        foreach (var role in roles ?? Array.Empty<string>())
            claims.Add(new Claim(ClaimTypes.Role, role));

        claims.Add(new Claim("roles", JsonSerializer.Serialize(roles ?? Array.Empty<string>())));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public string GenerateToken(ApplicationUser user, IList<string> roles)
        => GenerateAccessToken(user, roles).AccessToken;
}