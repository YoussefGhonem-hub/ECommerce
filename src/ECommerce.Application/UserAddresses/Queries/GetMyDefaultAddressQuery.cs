using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.UserAddresses.Queries;

public record GetMyDefaultAddressQuery() : IRequest<Result<MyAddressDto?>>;

public class GetMyAddressesDefaultHandler : IRequestHandler<GetMyDefaultAddressQuery, Result<MyAddressDto?>>
{
    private readonly ApplicationDbContext _context;

    public GetMyAddressesDefaultHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MyAddressDto?>> Handle(GetMyDefaultAddressQuery request, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.Id;

        var dto = await _context.UserAddresses
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.IsDefault)
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
            .FirstOrDefaultAsync(cancellationToken);

        // Return Success with null if no default address is set.
        return Result<MyAddressDto?>.Success(dto);
    }
}