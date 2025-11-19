using ECommerce.Application.Faqs.Commands.CreateFaq;
using ECommerce.Application.Faqs.Commands.DeleteFaq;
using ECommerce.Application.Faqs.Commands.UpdateFaq;
using ECommerce.Application.Faqs.Dtos;
using ECommerce.Application.Faqs.Queries.GetFaqs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaqsController : ControllerBase
{
    private readonly IMediator _mediator;
    public FaqsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<FaqDto>>> Get()
    {
        var result = await _mediator.Send(new GetFaqsQuery());
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create(CreateFaqCommand command, CancellationToken ct = default)
    {
        var result = await _mediator.Send(command, ct);
        return result.Succeeded ? CreatedAtAction(nameof(Get), new { id = result.Data }, result.Data) : BadRequest();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(Guid id, UpdateFaqCommand command, CancellationToken ct = default)
    {
        if (id != command.Id) return BadRequest("Mismatched Id");
        var result = await _mediator.Send(command, ct);
        return result.Succeeded ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteFaqCommand(id), ct);
        return result.Succeeded ? NoContent() : NotFound();
    }
}