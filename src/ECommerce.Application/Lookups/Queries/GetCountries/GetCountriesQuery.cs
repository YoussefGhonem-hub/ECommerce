using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Lookups.Queries.GetCountries;

public record GetCountriesQuery() : IRequest<Result<List<CountryLookupDto>>>;

public class GetCountriesQueryHandler : IRequestHandler<GetCountriesQuery, Result<List<CountryLookupDto>>>
{
    private readonly ApplicationDbContext _context;

    public GetCountriesQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<CountryLookupDto>>> Handle(GetCountriesQuery request, CancellationToken cancellationToken)
    {
        var items = await _context.Countries
            .AsNoTracking()
            .OrderBy(c => c.NameEn)
            .Select(c => new CountryLookupDto
            {
                Id = c.Id,
                NameEn = c.NameEn,
                NameAr = c.NameAr
            })
            .ToListAsync(cancellationToken);

        return Result<List<CountryLookupDto>>.Success(items);
    }
}