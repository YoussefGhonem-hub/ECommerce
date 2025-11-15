using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Lookups.Queries.GetCities;

public record GetCitiesQuery(Guid? CountryId) : IRequest<Result<List<CityLookupDto>>>;

public class GetCitiesQueryHandler : IRequestHandler<GetCitiesQuery, Result<List<CityLookupDto>>>
{
    private readonly ApplicationDbContext _context;

    public GetCitiesQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<CityLookupDto>>> Handle(GetCitiesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Cities.AsNoTracking();

        if (request.CountryId.HasValue && request.CountryId.Value != Guid.Empty)
            query = query.Where(c => c.CountryId == request.CountryId.Value);

        var items = await query
            .OrderBy(c => c.NameEn)
            .Select(c => new CityLookupDto
            {
                Id = c.Id,
                CountryId = c.CountryId,
                NameEn = c.NameEn,
                NameAr = c.NameAr
            })
            .ToListAsync(cancellationToken);

        return Result<List<CityLookupDto>>.Success(items);
    }
}