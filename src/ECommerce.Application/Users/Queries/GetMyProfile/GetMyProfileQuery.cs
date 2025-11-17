using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Application.Users.Queries.GetMyProfile;

public sealed record GetMyProfileQuery() : IRequest<Result<MyProfileDto>>;

public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, Result<MyProfileDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetMyProfileQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<MyProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        if (CurrentUser.Id == Guid.Empty)
            return Result<MyProfileDto>.Failure("Not authenticated.");

        var user = await _userManager.FindByIdAsync(CurrentUser.Id.ToString());
        if (user is null)
            return Result<MyProfileDto>.Failure("User not found.");

        var dto = new MyProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            AvatarUrl = user.AvatarUrl
        };

        return Result<MyProfileDto>.Success(dto);
    }
}