using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.UserAddresses.Commands;

public record CreateUserAddressCommand(Guid CountryId, Guid CityId, string Street, string FullName, string? HouseNo, string MobileNumber, bool IsDefault = false) : IRequest<Result<Guid>>;

public class CreateUserAddressHandler : IRequestHandler<CreateUserAddressCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;

    public CreateUserAddressHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateUserAddressCommand request, CancellationToken cancellationToken)
    {
        // Basic validation
        var errors = new Dictionary<string, string[]>();
        if (request.CountryId == Guid.Empty)
            errors[nameof(request.CountryId)] = new[] { "CountryId is required." };
        if (request.CityId == Guid.Empty)
            errors[nameof(request.CityId)] = new[] { "CityId is required." };
        if (string.IsNullOrWhiteSpace(request.Street))
            errors[nameof(request.Street)] = new[] { "Street is required." };
        if (string.IsNullOrWhiteSpace(request.FullName))
            errors[nameof(request.FullName)] = new[] { "FullName is required." };
        if (string.IsNullOrWhiteSpace(request.MobileNumber))
            errors[nameof(request.MobileNumber)] = new[] { "MobileNumber is required." };

        if (errors.Count > 0)
            return Result<Guid>.Validation(errors);

        // Ensure City exists and belongs to Country
        var city = await _context.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CityId, cancellationToken);
        if (city is null)
            return Result<Guid>.Failure("City not found.");
        if (city.CountryId != request.CountryId)
            return Result<Guid>.Failure("Invalid CityId/CountryId combination.");

        var address = new UserAddress
        {
            UserId = CurrentUser.Id,
            FullName = request.FullName,
            HouseNo = request.HouseNo,
            MobileNumber = request.MobileNumber,
            CityId = request.CityId,
            Street = request.Street,
            IsDefault = request.IsDefault
        };

        // If marking as default, unset any previous defaults for this user
        if (address.IsDefault)
        {
            var previousDefaults = await _context.UserAddresses
                .Where(a => a.UserId == CurrentUser.Id && a.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var a in previousDefaults)
                a.IsDefault = false;
        }

        _context.UserAddresses.Add(address);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(address.Id);
        }
        catch (DbUpdateException ex)
        {
            // Friendly FK message
            return Result<Guid>.Failure("Failed to create address. Please ensure CityId is valid.", ex.InnerException?.Message ?? ex.Message);
        }
    }
}