using ECommerce.Application.Orders.Commands.CheckoutCommand;
using ECommerce.Application.Orders.Queries.GetMyOrders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpGet("me")]
    public async Task<IActionResult> MyOrders([FromQuery] GetMyOrdersQuery getMyOrdersQuery)
    {
        var result = await _mediator.Send(getMyOrdersQuery);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    [HttpPost("checkout")]
    [Authorize]
    public async Task<IActionResult> CheckoutCommand(CheckoutCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
