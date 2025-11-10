using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Extensions;

public static class StringExtensions
{
    public static void EnsureMinimumStringLength(this ModelBuilder modelBuilder, int minLength = 500)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Iterate over all string properties
            foreach (var property in entityType.GetProperties()
                .Where(p => p.ClrType == typeof(string)))
            {
                // Ensure the column length is at least minLength
                var maxLength = property.GetMaxLength();

                if (maxLength == null || maxLength < minLength)
                {
                    property.SetMaxLength(minLength);
                }
            }
        }
    }
}
