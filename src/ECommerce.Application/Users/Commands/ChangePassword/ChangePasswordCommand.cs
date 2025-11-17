using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Application.Users.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
) : IRequest<Result<bool>>;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<bool>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ChangePasswordCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (CurrentUser.Id == Guid.Empty)
            return Result<bool>.Failure("Not authenticated.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
            return Result<bool>.Failure("New password and confirmation are required.");

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            return Result<bool>.Failure("New password and confirmation do not match.");

        var user = await _userManager.FindByIdAsync(CurrentUser.Id.ToString());
        if (user is null)
            return Result<bool>.Failure("User not found.");

        IdentityResult result;

        // If the user has no password yet (e.g., external login), allow setting without current password
        if (user.PasswordHash is null)
        {
            result = await _userManager.AddPasswordAsync(user, request.NewPassword);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return Result<bool>.Failure("Current password is required.");

            result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        }

        if (!result.Succeeded)
            return Result<bool>.Failure(result.Errors.Select(e => e.Description).ToArray());

        return Result<bool>.Success(true);
    }
}