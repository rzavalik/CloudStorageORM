using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CloudStorageORM.Extensions;

public static class CloudStorageDbSetExtensions
{
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