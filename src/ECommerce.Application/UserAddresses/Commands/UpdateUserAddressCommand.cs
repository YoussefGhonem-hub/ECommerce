using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.UserAddresses.Commands;

public record UpdateUserAddressCommand(Guid Id, string UserId, string Country, string City, string Street, string? PostalCode, bool IsDefault) : IRequest<Result<bool>>;
public class UpdateUserAddressHandler : IRequestHandler<UpdateUserAddressCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public UpdateUserAddressHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateUserAddressCommand request, CancellationToken cancellationToken)
    {
        var address = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == CurrentUser.Id, cancellationToken);
        if (address is null) return Result<bool>.Failure("Not found");

        if (request.IsDefault)
        {
            var existing = await _context.UserAddresses.Where(a => a.UserId == CurrentUser.Id).ToListAsync(cancellationToken);
            foreach (var a in existing) a.IsDefault = false;
        }

        address.Country = request.Country;
        address.City = request.City;
        address.Street = request.Street;
        address.IsDefault = request.IsDefault;

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
