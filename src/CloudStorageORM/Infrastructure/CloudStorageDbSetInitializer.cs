using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// No-op DbSet initializer for CloudStorageORM contexts.
/// </summary>
public class CloudStorageDbSetInitializer : IDbSetInitializer
{
    /// <inheritdoc />
    public void InitializeSets(DbContext context)
    {
    }
}