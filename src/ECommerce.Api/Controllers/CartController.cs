using ECommerce.Application.Carts.Commands;
using ECommerce.Application.Carts.Queries.GetCartQuery;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await _mediator.Send(new GetCartQuery());
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add(AddToCartCommand addToCartCommand)
    {
        var result = await _mediator.Send(addToCartCommand);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    [HttpDelete("{cartItemId}")]
    public async Task<IActionResult> RemoveCartItem(Guid cartItemId)
    {
        var result = await _mediator.Send(new RemoveCartItemCommand(cartItemId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
