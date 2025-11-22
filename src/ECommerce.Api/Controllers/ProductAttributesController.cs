using ECommerce.Application.ProductAttributes.Queries.GetUserOrderedAttributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductAttributesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProductAttributesController(IMediator mediator) => _mediator = mediator;


    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAdminAttributes(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAdminAttributesQuery(), ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }
}