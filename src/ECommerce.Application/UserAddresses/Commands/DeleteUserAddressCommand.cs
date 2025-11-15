using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.UserAddresses.Commands;

public record DeleteUserAddressCommand(Guid Id) : IRequest<Result<bool>>;
public class DeleteUserAddressHandler : IRequestHandler<DeleteUserAddressCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public DeleteUserAddressHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteUserAddressCommand request, CancellationToken cancellationToken)
    {
        var address = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == CurrentUser.Id, cancellationToken);
        if (address is null) return Result<bool>.Failure("Not found");
        _context.UserAddresses.Remove(address);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
