using ECommerce.Application.Common;
using ECommerce.Application.Users.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ECommerce.Application.Users.Commands;

public record LoginUserCommand(LoginRequest Request) : IRequest<Result<TokenPairResponse>>;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<TokenPairResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IHttpContextAccessor _http;
    private readonly JwtSettings _jwt;

    public LoginUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IRefreshTokenService refreshTokens,
        IHttpContextAccessor http,
        IOptions<JwtSettings> jwtOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _http = http;
        _jwt = jwtOptions.Value;
    }

    public async Task<Result<TokenPairResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        ApplicationUser? user = req.UserNameOrEmail.Contains("@")
            ? await _userManager.FindByEmailAsync(req.UserNameOrEmail)
            : await _userManager.FindByNameAsync(req.UserNameOrEmail);

        if (user is null)
            return Result<TokenPairResponse>.Failure("Invalid credentials");

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, req.Password, false);
        if (!signInResult.Succeeded)
            return Result<TokenPairResponse>.Failure("Invalid credentials");

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateToken(user, roles);
        var accessExp = DateTime.UtcNow.AddMinutes(_jwt.DurationInMinutes);

        var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var (refreshToken, refreshExp) = await _refreshTokens.CreateAsync(user, ip, cancellationToken);

        var pair = new TokenPairResponse(
            accessToken,
            accessExp,
            refreshToken,
            refreshExp
        );

        return Result<TokenPairResponse>.Success(pair);
    }
}
