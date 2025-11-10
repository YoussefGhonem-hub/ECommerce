using ECommerce.Application.Common;
using ECommerce.Application.Users.Commands;
using ECommerce.Application.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<Result<AuthResponse>>> Register(RegisterRequest request)
    {
        var result = await _mediator.Send(new RegisterUserCommand(request));
        if (!result.Succeeded) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<Result<AuthResponse>>> Login(LoginRequest request)
    {
        var result = await _mediator.Send(new LoginUserCommand(request));
        if (!result.Succeeded) return Unauthorized(result);
        return Ok(result);
    }
}
