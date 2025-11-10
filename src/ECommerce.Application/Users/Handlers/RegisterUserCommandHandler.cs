using ECommerce.Application.Common;
using ECommerce.Application.Users.Commands;
using ECommerce.Application.Users.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Application.Users.Handlers;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<AuthResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public RegisterUserCommandHandler(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existingByEmail = await _userManager.FindByEmailAsync(request.Request.Email);
        if (existingByEmail is not null)
            return Result<AuthResponse>.Failure("Email already exists");

        var user = new ApplicationUser
        {
            FullName = request.Request.FullName,
            Email = request.Request.Email,
            PhoneNumber = request.Request.PhoneNumber,
            UserName = request.Request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Request.Password);
        if (!result.Succeeded)
            return Result<AuthResponse>.Failure(result.Errors.Select(e => e.Description).ToArray());

        await _userManager.AddToRoleAsync(user, "Customer");

        var token = _tokenService.GenerateToken(user, new List<string> { "Customer" });
        return Result<AuthResponse>.Success(new AuthResponse(token, DateTime.UtcNow.AddHours(1), CurrentUser.UserId, user.Email!));
    }
}
