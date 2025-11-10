using ECommerce.Application.Carts.Commands;
using ECommerce.Application.Carts.Queries.GetCartQuery;
using ECommerce.Shared.CurrentUser;
using ECommerce.Shared.Extensions;
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
    public async Task<IActionResult> Get([FromQuery] string? guestId)
    {
        var result = await _mediator.Send(new GetCartQuery(CurrentUser.UserId.ToGuid(), guestId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add(Guid productId, int quantity = 1, [FromQuery] string? guestId = null)
    {
        var result = await _mediator.Send(new AddToCartCommand(CurrentUser.UserId.ToGuid(), guestId, productId, quantity));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
