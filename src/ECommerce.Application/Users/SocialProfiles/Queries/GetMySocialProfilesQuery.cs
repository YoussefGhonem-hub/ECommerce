using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Application.Users.SocialProfiles.Queries;

public sealed record GetMySocialProfilesQuery() : IRequest<Result<SocialProfilesDto>>;

public sealed class GetMySocialProfilesQueryHandler : IRequestHandler<GetMySocialProfilesQuery, Result<SocialProfilesDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetMySocialProfilesQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<SocialProfilesDto>> Handle(GetMySocialProfilesQuery request, CancellationToken cancellationToken)
    {
        if (CurrentUser.Id == Guid.Empty)
            return Result<SocialProfilesDto>.Failure("Not authenticated.");

        var user = await _userManager.FindByIdAsync(CurrentUser.Id.ToString());
        if (user is null)
            return Result<SocialProfilesDto>.Failure("User not found.");

        var s = user.SocialProfiles ?? new();
        var dto = new SocialProfilesDto
        {
            FacebookUrl = s.FacebookUrl,
            InstagramUrl = s.InstagramUrl,
            YouTubeUrl = s.YouTubeUrl,
            TikTokUrl = s.TikTokUrl,
            WebsiteUrl = s.WebsiteUrl,
            TelegramUrl = s.TelegramUrl,
            WhatsAppUrl = s.WhatsAppUrl
        };

        return Result<SocialProfilesDto>.Success(dto);
    }
}