using ECommerce.Shared.Extensions;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ECommerce.Shared.CurrentUser;

public static class CurrentUser
{
    // Initialize this once at startup with the registered IHttpContextAccessor.
    public static void Initialize(IHttpContextAccessor accessor) => HttpContextAccessor = accessor;
    public static IHttpContextAccessor? HttpContextAccessor { get; set; }

    // Claim keys
    private const string SubClaim = "sub";
    private const string NameIdClaim = ClaimTypes.NameIdentifier;
    private const string PreferredUsernameClaim = "preferred_username";
    private const string EmailClaim = "email";
    private const string NameClaim = "name";
    private const string RolesArrayClaim = "roles";
    private const string RoleClaim = ClaimTypes.Role;
    private const string AudienceClaim = "aud";

    // Read guest id from request header "X-Guest-UserId" and cache per-request
    public static string? GuestId
    {
        get
        {
            var http = HttpContextAccessor?.HttpContext;
            if (http is null) return null;

            const string cacheKey = "__CurrentUserGuestId__";
            if (http.Items.TryGetValue(cacheKey, out var cached) && cached is string s)
                return s;

            var header = http.Request?.Headers["X-Guest-UserId"].FirstOrDefault();
            var guestId = string.IsNullOrWhiteSpace(header) ? null : header!.Trim();

            if (!string.IsNullOrEmpty(guestId))
                http.Items[cacheKey] = guestId;

            return guestId;
        }
        set
        {
            var http = HttpContextAccessor?.HttpContext;
            if (http is null) return;

            const string cacheKey = "__CurrentUserGuestId__";
            if (string.IsNullOrWhiteSpace(value))
            {
                if (http.Items.ContainsKey(cacheKey))
                    http.Items.Remove(cacheKey);
            }
            else
            {
                http.Items[cacheKey] = value.Trim();
            }
        }
    }

    public static Guid? Id
    {
        get
        {
            // Prefer "sub"; fall back to NameIdentifier or custom "Id"
            var raw = GetClaimValue(SubClaim)
                   ?? GetClaimValue(NameIdClaim)
                   ?? GetClaimValue("Id");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public static string UserId => GetClaimValue(SubClaim) ?? GetClaimValue(NameIdClaim) ?? GetClaimValue("Id") ?? string.Empty;
    public static string UserName => GetClaimValue(PreferredUsernameClaim) ?? GetClaimValue(ClaimTypes.Name) ?? string.Empty;
    public static string Email => GetClaimValue(EmailClaim) ?? GetClaimValue(ClaimTypes.Email) ?? string.Empty;
    public static string Name => GetClaimValue(NameClaim) ?? UserName;
    public static IReadOnlyList<string> Roles => GetRoles();
    public static IReadOnlyList<string> Permissions => GetPermissions();
    public static IReadOnlyList<string> Audiences => GetAudiences();
    public static bool IsAuthenticated => HttpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public static string? GetClaimValue(string key)
    {
        var principal = HttpContextAccessor?.HttpContext?.User;
        if (principal?.Identity is { IsAuthenticated: true })
        {
            var value = principal.Claims.FirstOrDefault(c => c.Type == key)?.Value;
            if (!string.IsNullOrEmpty(value)) return value;
        }

        // Fallback to raw JWT if claim not present on principal
        var jwt = GetRawToken();
        return jwt?.Claims.FirstOrDefault(c => c.Type == key)?.Value;
    }

    public static JwtSecurityToken? GetRawToken()
    {
        var http = HttpContextAccessor?.HttpContext;
        if (http is null) return null;

        const string cacheKey = "__CurrentUserRawJwt__";
        if (http.Items.TryGetValue(cacheKey, out var cached) && cached is JwtSecurityToken jt)
            return jt;

        var header = http.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header)) return null;

        var bearer = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header[7..].Trim()
            : header.Trim();

        if (string.IsNullOrEmpty(bearer)) return null;

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(bearer)) return null;

        var token = handler.ReadToken(bearer) as JwtSecurityToken;
        if (token != null) http.Items[cacheKey] = token;
        return token;
    }

    private static List<string> GetRoles()
    {
        if (!IsAuthenticated) return new();

        var principal = HttpContextAccessor!.HttpContext!.User;
        var roles = new List<string>();

        // Standard role claims (added by TokenService)
        roles.AddRange(principal.Claims.Where(c => c.Type == RoleClaim).Select(c => c.Value));

        // Optional roles array claim (added by TokenService)
        var rolesJson = principal.Claims.FirstOrDefault(c => c.Type == RolesArrayClaim)?.Value;
        if (!string.IsNullOrEmpty(rolesJson))
        {
            var arr = rolesJson.Deserialized<List<string>>();
            if (arr != null) roles.AddRange(arr);
        }

        // Fallback to raw token for any additional roles (if principal missing some)
        var jwt = GetRawToken();
        if (jwt != null)
        {
            roles.AddRange(jwt.Claims.Where(c => c.Type == RoleClaim).Select(c => c.Value));

            var jwtRolesJson = jwt.Claims.FirstOrDefault(c => c.Type == RolesArrayClaim)?.Value;
            if (!string.IsNullOrEmpty(jwtRolesJson))
            {
                var arr = jwtRolesJson.Deserialized<List<string>>();
                if (arr != null) roles.AddRange(arr);
            }
        }

        return roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r)
            .ToList();
    }

    private static List<string> GetPermissions()
    {
        if (!IsAuthenticated) return new();
        var principal = HttpContextAccessor!.HttpContext!.User;

        // Customize if you also emit "permissions" in your JWT
        var permissions = principal.Claims
            .Where(c => c.Type == "permissions")
            .Select(c => c.Value)
            .ToList();

        return permissions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p)
            .ToList();
    }

    private static List<string> GetAudiences()
    {
        if (!IsAuthenticated) return new();
        var principal = HttpContextAccessor!.HttpContext!.User;

        var auds = principal.Claims.Where(c => c.Type == AudienceClaim).Select(c => c.Value).ToList();
        if (auds.Count == 0)
        {
            var jwt = GetRawToken();
            if (jwt != null)
                auds = jwt.Claims.Where(c => c.Type == AudienceClaim).Select(c => c.Value).ToList();
        }

        if (auds.Count == 0) return new();

        // aud may be a single string or a JSON array
        var result = new List<string>();
        foreach (var value in auds)
        {
            var list = value.Deserialized<List<string>>();
            if (list is { Count: > 0 }) result.AddRange(list);
            else result.Add(value);
        }

        return result
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(a => a)
            .ToList();
    }

    // Manual token parsing helper
    public static string GetClaimValue(string token, string key)
    {
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token)) return string.Empty;

        var jwt = handler.ReadToken(token) as JwtSecurityToken;
        return jwt?.Claims.FirstOrDefault(c => c.Type == key)?.Value ?? string.Empty;
    }
}