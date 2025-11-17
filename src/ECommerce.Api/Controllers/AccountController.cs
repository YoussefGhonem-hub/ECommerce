using ECommerce.Application.Users.Commands.ChangePassword;
using ECommerce.Application.Users.Commands.UpdateAccountSettings;
using ECommerce.Application.Users.Queries.GetMyProfile;
using ECommerce.Application.Users.SocialProfiles.Commands;
using ECommerce.Application.Users.SocialProfiles.Queries;
using ECommerce.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(IMediator mediator, UserManager<ApplicationUser> userManager)
    {
        _mediator = mediator;
        _userManager = userManager;
    }

    // GET api/account/me
    [HttpGet("profile")]
    [AllowAnonymous]
    public async Task<IActionResult> Me([FromQuery] GetMyProfileQuery query)
    {
        var result = await _mediator.Send(query);
        return result.Succeeded ? Ok(result) : Unauthorized(result);
    }

    // PUT api/account/settings (multipart/form-data)
    [HttpPost("settings")]
    [RequestSizeLimit(10_000_000)] // ~10MB
    public async Task<IActionResult> UpdateSettings([FromForm] UpdateAccountSettingsCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    // PUT api/account/password
    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    // GET api/account/social-profiles
    [HttpGet("social-profiles")]
    public async Task<IActionResult> GetMySocialProfiles([FromQuery] GetMySocialProfilesQuery query)
    {
        var result = await _mediator.Send(query);
        return result.Succeeded ? Ok(result) : Unauthorized(result);
    }

    // PUT api/account/social-profiles
    [HttpPost("social-profiles")]
    public async Task<IActionResult> UpdateMySocialProfiles([FromBody] UpdateMySocialProfilesCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}