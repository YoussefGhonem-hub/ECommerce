using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace ECommerce.Infrastructure.Identity;

public interface IRefreshTokenService
{
    Task<(string Token, DateTime ExpiresAtUtc)> CreateAsync(ApplicationUser user, string? ip, CancellationToken ct);
    Task<(ApplicationUser? User, RefreshToken? Token)> GetActiveAsync(string refreshToken, CancellationToken ct);
    Task<(string Plain, DateTime ExpiresAtUtc)> RotateAsync(RefreshToken current, ApplicationUser user, string? ip, CancellationToken ct);
    Task RevokeAsync(RefreshToken token, string reason, string? ip, CancellationToken ct);
    string Hash(string token);
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _db;
    private readonly JwtSettings _settings;

    public RefreshTokenService(ApplicationDbContext db, IOptions<JwtSettings> settings)
    {
        _db = db;
        _settings = settings.Value;
    }

    public async Task<(string Token, DateTime ExpiresAtUtc)> CreateAsync(ApplicationUser user, string? ip, CancellationToken ct)
    {
        var plain = GenerateSecureToken();
        var hash = Hash(plain);
        var expires = DateTime.UtcNow.AddDays(_settings.RefreshTokenTtlDays);

        var entity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAtUtc = expires,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = ip
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(ct);
        return (plain, expires);
    }

    public async Task<(ApplicationUser? User, RefreshToken? Token)> GetActiveAsync(string refreshToken, CancellationToken ct)
    {
        var hash = Hash(refreshToken);
        var token = await _db.RefreshTokens
            .AsTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token == null || !token.IsActive) return (null, null);
        return (token.User, token);
    }

    // NEW: single atomic rotation (revokes current, creates new, returns plain + expiry)
    public async Task<(string Plain, DateTime ExpiresAtUtc)> RotateAsync(RefreshToken current, ApplicationUser user, string? ip, CancellationToken ct)
    {
        // Revoke current
        current.RevokedAtUtc = DateTime.UtcNow;
        current.RevokedByIp = ip;

        // Generate new
        var plain = GenerateSecureToken();
        var hash = Hash(plain);
        var expires = DateTime.UtcNow.AddDays(_settings.RefreshTokenTtlDays);

        var replacement = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAtUtc = expires,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = ip
        };

        // Link
        current.ReplacedByTokenHash = replacement.TokenHash;

        _db.RefreshTokens.Add(replacement);

        // Extremely unlikely collision safeguard
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_RefreshTokens_TokenHash") == true)
        {
            // regenerate once
            plain = GenerateSecureToken();
            hash = Hash(plain);
            expires = DateTime.UtcNow.AddDays(_settings.RefreshTokenTtlDays);
            replacement.TokenHash = hash;
            replacement.ExpiresAtUtc = expires;
            current.ReplacedByTokenHash = hash;
            await _db.SaveChangesAsync(ct);
        }

        return (plain, expires);
    }

    public async Task RevokeAsync(RefreshToken token, string reason, string? ip, CancellationToken ct)
    {
        token.RevokedAtUtc = DateTime.UtcNow;
        token.RevokedByIp = ip;
        token.ReasonRevoked = reason;
        await _db.SaveChangesAsync(ct);
    }

    public string Hash(string token)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string GenerateSecureToken()
    {
        Span<byte> buffer = stackalloc byte[64];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer);
    }
}