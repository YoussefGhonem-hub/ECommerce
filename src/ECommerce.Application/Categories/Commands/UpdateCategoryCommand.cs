using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Categories.Commands;

public record UpdateCategoryCommand(Guid Id, string NameEn, string NameAr, Guid? ParentId, bool IsFeatured) : IRequest<Result<bool>>;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;
    public UpdateCategoryHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<bool>> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (category is null) return Result<bool>.Failure("Category.NotFound");

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.NameEn))
            errors[nameof(request.NameEn)] = ["English name required"];
        if (string.IsNullOrWhiteSpace(request.NameAr))
            errors[nameof(request.NameAr)] = ["Arabic name required"];

        if (await _context.Categories.AnyAsync(c => c.Id != request.Id && c.NameEn == request.NameEn, ct))
            errors[nameof(request.NameEn)] = (errors.TryGetValue(nameof(request.NameEn), out var e)
                ? e.Concat(["English name already used"]).ToArray()
                : ["English name already used"]);

        if (await _context.Categories.AnyAsync(c => c.Id != request.Id && c.NameAr == request.NameAr, ct))
            errors[nameof(request.NameAr)] = (errors.TryGetValue(nameof(request.NameAr), out var a)
                ? a.Concat(["Arabic name already used"]).ToArray()
                : ["Arabic name already used"]);

        if (request.ParentId == request.Id)
            errors[nameof(request.ParentId)] = ["Category cannot be its own parent"];

        if (request.ParentId.HasValue &&
            !await _context.Categories.AnyAsync(c => c.Id == request.ParentId.Value, ct))
            errors[nameof(request.ParentId)] = (errors.TryGetValue(nameof(request.ParentId), out var p)
                ? p.Concat(["Parent category not found"]).ToArray()
                : ["Parent category not found"]);

        if (errors.Count > 0) return Result<bool>.Validation(errors);

        category.NameEn = request.NameEn;
        category.NameAr = request.NameAr;
        category.NameEn = request.NameEn;
        category.ParentId = request.ParentId;
        category.IsFeatured = request.IsFeatured;
        await _context.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
