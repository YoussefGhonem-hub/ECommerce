using ECommerce.Application.Orders.Commands.CheckoutCommand;
using ECommerce.Application.Orders.Queries.GetMyOrders;
using MediatR;
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
    public async Task<IActionResult> MyOrders([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetMyOrdersQuery(pageNumber, pageSize));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    [HttpPost("checkout")]
    public async Task<IActionResult> CheckoutCommand(CheckoutCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
