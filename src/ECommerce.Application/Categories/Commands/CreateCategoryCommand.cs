using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Categories.Commands;

public record CreateCategoryCommand(string NameEn, string NameAr, Guid? ParentId, bool IsFeatured) : IRequest<Result<Guid>>;

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;
    public CreateCategoryHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.NameEn))
            errors[nameof(request.NameEn)] = ["English name required"];
        if (string.IsNullOrWhiteSpace(request.NameAr))
            errors[nameof(request.NameAr)] = ["Arabic name required"];

        if (await _context.Categories.AnyAsync(c => c.NameEn == request.NameEn, ct))
            errors[nameof(request.NameEn)] = (errors.TryGetValue(nameof(request.NameEn), out var e)
                ? e.Concat(["English name already exists"]).ToArray()
                : ["English name already exists"]);

        if (await _context.Categories.AnyAsync(c => c.NameAr == request.NameAr, ct))
            errors[nameof(request.NameAr)] = (errors.TryGetValue(nameof(request.NameAr), out var a)
                ? a.Concat(["Arabic name already exists"]).ToArray()
                : ["Arabic name already exists"]);

        if (request.ParentId.HasValue &&
            !await _context.Categories.AnyAsync(c => c.Id == request.ParentId.Value, ct))
            errors[nameof(request.ParentId)] = ["Parent category not found"];

        if (errors.Count > 0) return Result<Guid>.Validation(errors);

        var entity = new Category
        {
            NameEn = request.NameEn,
            NameAr = request.NameAr,
            ParentId = request.ParentId,
            IsFeatured = request.IsFeatured
        };

        _context.Categories.Add(entity);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(entity.Id);
    }
}