using ECommerce.Application.Common;
using ECommerce.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Application.Users.Commands.Tokens;

public sealed record RevokeRefreshTokenCommand(string RefreshToken, string? Reason = null) : IRequest<Result<bool>>;

public sealed class RevokeRefreshTokenCommandHandler : IRequestHandler<RevokeRefreshTokenCommand, Result<bool>>
{
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IHttpContextAccessor _http;

    public RevokeRefreshTokenCommandHandler(IRefreshTokenService refreshTokens, IHttpContextAccessor http)
    {
        _refreshTokens = refreshTokens;
        _http = http;
    }

    public async Task<Result<bool>> Handle(RevokeRefreshTokenCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result<bool>.Failure("Refresh token is required.");

        var (user, token) = await _refreshTokens.GetActiveAsync(request.RefreshToken, ct);
        if (token is null)
            return Result<bool>.Failure("Invalid or already revoked refresh token.");

        var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
        await _refreshTokens.RevokeAsync(token, request.Reason ?? "User initiated revocation.", ip, ct);
        return Result<bool>.Success(true);
    }
}