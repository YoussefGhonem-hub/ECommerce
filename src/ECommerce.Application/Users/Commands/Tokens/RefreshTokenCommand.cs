using ECommerce.Application.Common;
using ECommerce.Application.Users.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Application.Users.Commands.Tokens;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<TokenPairResponse>>;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<TokenPairResponse>>
{
    private readonly IRefreshTokenService _refreshTokens;
    private readonly ITokenService _tokens;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _http;

    public RefreshTokenCommandHandler(
        IRefreshTokenService refreshTokens,
        ITokenService tokens,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor http)
    {
        _refreshTokens = refreshTokens;
        _tokens = tokens;
        _userManager = userManager;
        _http = http;
    }

    public async Task<Result<TokenPairResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result<TokenPairResponse>.Failure("Refresh token is required.");

        var (user, token) = await _refreshTokens.GetActiveAsync(request.RefreshToken, ct);
        if (user is null || token is null)
            return Result<TokenPairResponse>.Failure("Invalid or expired refresh token.");

        // rotate refresh token
        var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var (newRefreshPlain, newRefreshExp) = await _refreshTokens.CreateAsync(user, ip, ct);
        var replacement = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _refreshTokens.Hash(newRefreshPlain),
            ExpiresAtUtc = newRefreshExp,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = ip
        };
        await _refreshTokens.RotateAsync(token, replacement, ip, ct);

        // issue access token
        var roles = await _userManager.GetRolesAsync(user);
        var (access, accessExp) = _tokens.GenerateAccessToken(user, roles);

        var pair = new TokenPairResponse(access, accessExp, newRefreshPlain, newRefreshExp);
        return Result<TokenPairResponse>.Success(pair);
    }
}