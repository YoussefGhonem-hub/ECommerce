using ECommerce.Application.UserAddresses.Commands;
using ECommerce.Application.UserAddresses.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AddressesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AddressesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string CurrentUserId => User.FindFirst("uid")!.Value;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var list = await _mediator.Send(new GetMyAddressesQuery(CurrentUserId));
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserAddressCommand request)
    {
        var result = await _mediator.Send(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateUserAddressCommand request)
    {
        var result = await _mediator.Send(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteUserAddressCommand(id, CurrentUserId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}

