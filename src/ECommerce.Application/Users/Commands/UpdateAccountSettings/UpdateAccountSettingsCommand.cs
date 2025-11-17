using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Shared.CurrentUser;
using ECommerce.Shared.Storage;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Application.Users.Commands.UpdateAccountSettings;

public record UpdateAccountSettingsCommand(
    string FullName,
    string Email,
    IFormFile? Avatar // bound from multipart/form-data
) : IRequest<Result<bool>>;

public class UpdateAccountSettingsHandler : IRequestHandler<UpdateAccountSettingsCommand, Result<bool>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileStorage _storage;

    public UpdateAccountSettingsHandler(UserManager<ApplicationUser> userManager, IFileStorage storage)
    {
        _userManager = userManager;
        _storage = storage;
    }

    public async Task<Result<bool>> Handle(UpdateAccountSettingsCommand request, CancellationToken cancellationToken)
    {
        if (CurrentUser.Id == Guid.Empty)
            return Result<bool>.Failure("Not authenticated.");

        var user = await _userManager.FindByIdAsync(CurrentUser.Id.ToString());
        if (user is null)
            return Result<bool>.Failure("User not found.");

        // Email uniqueness check
        if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _userManager.FindByEmailAsync(request.Email);
            if (existing is not null && existing.Id != user.Id)
                return Result<bool>.Failure("Email is already in use.");

            user.Email = request.Email;
            user.UserName = request.Email; // optional alignment with email
        }

        // Update full name
        user.FullName = request.FullName;

        // Avatar handling (fully in handler)
        if (request.Avatar is not null && request.Avatar.Length > 0)
        {
            if (!request.Avatar.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return Result<bool>.Failure("Avatar must be an image.");

            await using var stream = request.Avatar.OpenReadStream();
            var path = await _storage.SaveUserAvatarAsync(user.Id, stream, request.Avatar.FileName, request.Avatar.ContentType, cancellationToken);

            // optional: delete old avatar
            if (!string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.Equals(user.AvatarUrl, path, StringComparison.OrdinalIgnoreCase))
            {
                await _storage.DeleteAsync(user.AvatarUrl!, cancellationToken);
            }

            user.AvatarUrl = path;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Result<bool>.Failure(result.Errors.Select(e => e.Description).ToArray());

        return Result<bool>.Success(true);
    }
}