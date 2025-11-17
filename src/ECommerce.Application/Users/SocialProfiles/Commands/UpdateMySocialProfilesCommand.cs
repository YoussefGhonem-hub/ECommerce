using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Application.Users.SocialProfiles.Commands;

public sealed record UpdateMySocialProfilesCommand(
    string? FacebookUrl,
    string? InstagramUrl,
    string? YouTubeUrl,
    string? TikTokUrl,
    string? WebsiteUrl,
    string? TelegramUrl,
    string? WhatsAppUrl
) : IRequest<Result<bool>>;

public sealed class UpdateMySocialProfilesCommandHandler : IRequestHandler<UpdateMySocialProfilesCommand, Result<bool>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateMySocialProfilesCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<bool>> Handle(UpdateMySocialProfilesCommand request, CancellationToken cancellationToken)
    {
        if (CurrentUser.Id == Guid.Empty)
            return Result<bool>.Failure("Not authenticated.");

        var user = await _userManager.FindByIdAsync(CurrentUser.Id.ToString());
        if (user is null)
            return Result<bool>.Failure("User not found.");

        // basic URL validation helper
        static string? Normalize(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            return Uri.TryCreate(url.Trim(), UriKind.Absolute, out _) ? url.Trim() : throw new ArgumentException("Invalid URL");
        }

        try
        {
            user.SocialProfiles ??= new UserSocialProfiles();

            user.SocialProfiles.FacebookUrl = Normalize(request.FacebookUrl);
            user.SocialProfiles.InstagramUrl = Normalize(request.InstagramUrl);
            user.SocialProfiles.YouTubeUrl = Normalize(request.YouTubeUrl);
            user.SocialProfiles.TikTokUrl = Normalize(request.TikTokUrl);
            user.SocialProfiles.WebsiteUrl = Normalize(request.WebsiteUrl);
            user.SocialProfiles.TelegramUrl = Normalize(request.TelegramUrl);
            user.SocialProfiles.WhatsAppUrl = Normalize(request.WhatsAppUrl);
        }
        catch (ArgumentException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
            return Result<bool>.Failure(update.Errors.Select(e => e.Description).ToArray());

        return Result<bool>.Success(true);
    }
}