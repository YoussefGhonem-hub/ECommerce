using ECommerce.Application.Lookups.Queries.GetCities;
using ECommerce.Application.Lookups.Queries.GetCountries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LookupController : ControllerBase
{
    private readonly IMediator _mediator;

    public LookupController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/lookup/countries
    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries()
    {
        var result = await _mediator.Send(new GetCountriesQuery());
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    // GET: api/lookup/cities?countryId=...
    [HttpGet("cities")]
    public async Task<IActionResult> GetCities([FromQuery] Guid? countryId)
    {
        var result = await _mediator.Send(new GetCitiesQuery(countryId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    // GET: api/lookup/countries/{countryId}/cities
    [HttpGet("countries/{countryId:guid}/cities")]
    public async Task<IActionResult> GetCitiesByCountry(Guid countryId)
    {
        var result = await _mediator.Send(new GetCitiesQuery(countryId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}