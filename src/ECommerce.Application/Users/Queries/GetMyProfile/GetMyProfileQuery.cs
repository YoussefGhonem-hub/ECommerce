using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Application.Users.Queries.GetMyProfile;

public sealed record GetMyProfileQuery() : IRequest<Result<MyProfileDto>>;

public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, Result<MyProfileDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetMyProfileQueryHandler(UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<MyProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        if (CurrentUser.Id == Guid.Empty)
            return Result<MyProfileDto>.Failure("Not authenticated.");

        var user = await _userManager.FindByIdAsync(CurrentUser.Id.ToString());
        if (user is null)
            return Result<MyProfileDto>.Failure("User not found.");

        var avatarUrl = user.AvatarUrl;

        // If stored as relative (e.g., /uploads/...), prefix with base URL
        if (!string.IsNullOrWhiteSpace(avatarUrl) &&
            Uri.TryCreate(avatarUrl, UriKind.Relative, out _) &&
            _httpContextAccessor.HttpContext is { } ctx)
        {
            var req = ctx.Request;
            var baseUrl = $"{req.Scheme}://{req.Host.Value}";
            avatarUrl = $"{baseUrl}{avatarUrl}";
        }

        var dto = new MyProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            AvatarUrl = avatarUrl
        };

        return Result<MyProfileDto>.Success(dto);
    }
}