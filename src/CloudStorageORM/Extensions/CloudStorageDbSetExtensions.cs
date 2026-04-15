using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CloudStorageORM.Extensions;

/// <summary>
/// Extensions for bulk-like operations on DbSet instances in CloudStorageORM.
/// </summary>
public static class CloudStorageDbSetExtensions
{
    /// <summary>
    /// Removes all entities from a set.
    /// </summary>
    /// <typeparam name="TEntity">Entity type represented by the set.</typeparam>
    /// <param name="dbSet">The set to clear.</param>
    /// <param name="context">The DbContext that owns the set.</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    /// <returns>The number of deleted entities or objects.</returns>
    /// <remarks>
    /// For non-CloudStorageORM contexts this method deletes via <c>RemoveRange</c> followed by <c>SaveChangesAsync</c>.
    /// For CloudStorageORM contexts this method deletes provider objects under the entity prefix and detaches tracked entries.
    /// </remarks>
    /// <example>
    /// <code>
    /// var removed = await context.Set&lt;User&gt;().ClearAsync(context);
    /// </code>
    /// </example>
    public static async Task<int> ClearAsync<TEntity>(this DbSet<TEntity> dbSet, DbContext context,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(dbSet);
        ArgumentNullException.ThrowIfNull(context);

        // Standard EF providers clear via RemoveRange + SaveChanges.
        if (context is not Contexts.CloudStorageDbContext)
        {
            var entities = await dbSet.ToListAsync(cancellationToken);
            if (entities.Count == 0)
            {
                return 0;
            }

            dbSet.RemoveRange(entities);
            return await context.SaveChangesAsync(cancellationToken);
        }

        // CloudStorageORM providers clear by deleting all objects under the entity folder.
        var storageProvider = context.GetService<IStorageProvider>();
        var pathResolver = context.GetService<IBlobPathResolver>();
        var prefix = pathResolver.GetBlobName(typeof(TEntity));
        var keys = await storageProvider.ListAsync(prefix);

        foreach (var key in keys)
        {
            await storageProvider.DeleteAsync(key);
        }

        foreach (var entry in context.ChangeTracker.Entries<TEntity>().ToList())
        {
            entry.State = EntityState.Detached;
        }

        return keys.Count;
    }
}