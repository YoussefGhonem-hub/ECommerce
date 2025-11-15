using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.UserAddresses.Commands;

public record UpdateUserAddressCommand(
    Guid Id,
    Guid CountryId,
    Guid CityId,
    string Street,
    string FullName,
    string? HouseNo,
    string MobileNumber,
    bool IsDefault
) : IRequest<Result<bool>>;

public class UpdateUserAddressHandler : IRequestHandler<UpdateUserAddressCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public UpdateUserAddressHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateUserAddressCommand request, CancellationToken cancellationToken)
    {
        if (CurrentUser.Id is null)
            return Result<bool>.Failure("Unauthorized");

        var address = await _context.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == CurrentUser.Id, cancellationToken);

        if (address is null)
            return Result<bool>.Failure("Not found");

        if (string.IsNullOrWhiteSpace(request.Street))
            return Result<bool>.Failure("Street is required");

        if (string.IsNullOrWhiteSpace(request.FullName))
            return Result<bool>.Failure("Full name is required");

        if (string.IsNullOrWhiteSpace(request.MobileNumber))
            return Result<bool>.Failure("Mobile number is required");

        // Validate City exists and belongs to Country
        var city = await _context.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CityId, cancellationToken);

        if (city is null)
            return Result<bool>.Failure("Invalid city");

        if (city.CountryId != request.CountryId)
            return Result<bool>.Failure("City does not belong to the selected country");

        // If this address is the new default, unset other defaults for this user efficiently
        if (request.IsDefault)
        {
            await _context.UserAddresses
                .Where(a => a.UserId == CurrentUser.Id && a.Id != request.Id && a.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), cancellationToken);
        }

        // Apply updates (full set of properties)
        address.CityId = request.CityId;
        address.Street = request.Street.Trim();
        address.FullName = request.FullName.Trim();
        address.HouseNo = string.IsNullOrWhiteSpace(request.HouseNo) ? null : request.HouseNo.Trim();
        address.MobileNumber = request.MobileNumber.Trim();
        address.IsDefault = request.IsDefault;

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}