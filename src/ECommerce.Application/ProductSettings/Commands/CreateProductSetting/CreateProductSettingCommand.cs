using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.ProductSettings.Commands.CreateProductSetting;

public record CreateProductSettingCommand(
    DiscountKind Kind,
    decimal Value,
    bool AppliesToAllProducts,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    bool IsActive,
    List<Guid>? ProductIds
) : IRequest<Result<Guid>>;

public class CreateProductSettingCommandHandler : IRequestHandler<CreateProductSettingCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;

    public CreateProductSettingCommandHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateProductSettingCommand request, CancellationToken ct)
    {
        // Basic validation


        if (request.Value <= 0)
            return Result<Guid>.Validation(new() { { nameof(request.Value), new[] { "Value must be > 0." } } });

        if (request.Kind == DiscountKind.Percentage && (request.Value <= 0 || request.Value > 100))
            return Result<Guid>.Validation(new() { { nameof(request.Value), new[] { "Percentage must be between 0 and 100." } } });

        if (request.StartDate >= request.EndDate)
            return Result<Guid>.Validation(new() { { nameof(request.EndDate), new[] { "EndDate must be after StartDate." } } });

        var entity = new ProductSetting
        {

            Kind = request.Kind,
            Value = request.Value,
            AppliesToAllProducts = request.AppliesToAllProducts,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.IsActive
        };

        if (!request.AppliesToAllProducts && request.ProductIds is not null && request.ProductIds.Count > 0)
        {
            var products = await _context.Products
                .Where(p => request.ProductIds.Contains(p.Id))
                .ToListAsync(ct);

            foreach (var p in products)
                entity.Products.Add(p);
        }

        _context.ProductSettings.Add(entity);
        await _context.SaveChangesAsync(ct);
        return Result<Guid>.Success(entity.Id);
    }
}