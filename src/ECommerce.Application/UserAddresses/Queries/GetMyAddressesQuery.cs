using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.UserAddresses.Queries;

public record GetMyAddressesQuery() : IRequest<Result<List<MyAddressDto>>>;

public class GetMyAddressesHandler : IRequestHandler<GetMyAddressesQuery, Result<List<MyAddressDto>>>
{
    private readonly ApplicationDbContext _context;

    public GetMyAddressesHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<MyAddressDto>>> Handle(GetMyAddressesQuery request, CancellationToken cancellationToken)
    {
        var items = await _context.UserAddresses
            .AsNoTracking()
            .Where(a => a.UserId == CurrentUser.Id)
            .OrderByDescending(a => a.IsDefault)
            .Select(a => new MyAddressDto
            {
                Id = a.Id,
                UserId = a.UserId,
                FullName = a.FullName,
                CountryId = a.City.CountryId,
                CountryNameEn = a.City.Country!.NameEn,
                CountryNameAr = a.City.Country!.NameAr,
                CityId = a.CityId,
                CityNameEn = a.City.NameEn,
                CityNameAr = a.City.NameAr,
                Street = a.Street,
                MobileNumber = a.MobileNumber,
                HouseNo = a.HouseNo,
                IsDefault = a.IsDefault
            })
            .ToListAsync(cancellationToken);

        return Result<List<MyAddressDto>>.Success(items);
    }
}
