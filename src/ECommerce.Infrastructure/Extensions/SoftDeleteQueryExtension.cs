using ECommerce.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using System.Reflection;

namespace ECommerce.Infrastructure.Extensions;

public static class SoftDeleteQueryExtension
{
    public static void GetOnlyNotDeletedEntities(this ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                entityType.AddSoftDeleteQueryFilter();
            }
        }
    }
    public static void AddSoftDeleteQueryFilter3(this IMutableEntityType entityType)
    {
        // Guard: only apply if the type derives from BaseAuditableEntity
        if (!typeof(BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
            return;

        // Build lambda expression e => e.IsDeleted for the concrete type
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var prop = Expression.Property(parameter, nameof(BaseAuditableEntity.IsDeleted));
        var body = Expression.Equal(prop, Expression.Constant(true)); // only deleted
        var lambda = Expression.Lambda(body, parameter);

        entityType.SetQueryFilter(lambda);
    }
    private static void AddSoftDeleteQueryFilter(this IMutableEntityType entityData)
    {
        var methodToCall = typeof(SoftDeleteQueryExtension).GetMethod(nameof(GetSoftDeleteFilter),
            BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(entityData.ClrType);

        var filter = methodToCall?.Invoke(null, []);

        if (filter == null) return;
        entityData.SetQueryFilter((LambdaExpression)filter);

        entityData.AddIndex(entityData.FindProperty(nameof(BaseAuditableEntity.IsDeleted)) ??
                            throw new InvalidOperationException());
    }

    private static LambdaExpression GetSoftDeleteFilter<TEntity>()
        where TEntity : BaseAuditableEntity
    {
        Expression<Func<TEntity, bool>> filter = x => !x.IsDeleted;
        return filter;
    }
}
