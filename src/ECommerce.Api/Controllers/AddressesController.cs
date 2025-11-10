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
    public async Task<IActionResult> Create(CreateAddressRequest request)
    {
        var result = await _mediator.Send(new CreateUserAddressCommand(CurrentUserId, request.Country, request.City, request.Street, request.PostalCode, request.IsDefault));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateAddressRequest request)
    {
        var result = await _mediator.Send(new UpdateUserAddressCommand(id, CurrentUserId, request.Country, request.City, request.Street, request.PostalCode, request.IsDefault));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteUserAddressCommand(id, CurrentUserId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}

public record CreateAddressRequest(string Country, string City, string Street, string? PostalCode, bool IsDefault);
public record UpdateAddressRequest(string Country, string City, string Street, string? PostalCode, bool IsDefault);
