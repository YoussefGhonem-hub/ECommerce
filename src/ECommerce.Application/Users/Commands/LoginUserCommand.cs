using ECommerce.Application.Common;
using ECommerce.Application.Users.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Application.Users.Commands;

public record LoginUserCommand(LoginRequest Request) : IRequest<Result<AuthResponse>>;
public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<AuthResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;

    public LoginUserCommandHandler(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        ApplicationUser? user = null;

        if (req.UserNameOrEmail.Contains("@"))
            user = await _userManager.FindByEmailAsync(req.UserNameOrEmail);
        else
            user = await _userManager.FindByNameAsync(req.UserNameOrEmail);

        if (user is null)
            return Result<AuthResponse>.Failure("Invalid credentials");

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, req.Password, false);
        if (!signInResult.Succeeded)
            return Result<AuthResponse>.Failure("Invalid credentials");

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);
        return Result<AuthResponse>.Success(new AuthResponse(token, DateTime.UtcNow.AddHours(1), user.Id.ToString(), user.Email!));
    }
}
