using System.Collections;
using System.Linq.Expressions;
using CloudStorageORM.Interfaces.StorageProviders;

namespace CloudStorageORM.Repositories;

/// <summary>
/// Lightweight query and enumeration wrapper for entities stored through <see cref="IStorageProvider" />.
/// </summary>
/// <typeparam name="TEntity">Entity type represented by this set.</typeparam>
public class CloudStorageDbSet<TEntity>(IStorageProvider storageProvider)
    : IQueryable<TEntity>, IAsyncEnumerable<TEntity>
    where TEntity : class
{
    private List<TEntity>? _cache;

    /// <summary>
    /// Loads all entities into memory and caches the result for subsequent queries.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the asynchronous enumeration.</param>
    /// <returns>A snapshot of the stored entities.</returns>
    /// <example>
    /// <code>
    /// var users = await dbSet.ToListAsync();
    /// </code>
    /// </example>
    public async Task<List<TEntity>> ToListAsync(CancellationToken cancellationToken = default)
    {
        if (_cache != null)
        {
            return _cache.ToList();
        }

        var path = typeof(TEntity).Name;
        var keys = await storageProvider.ListAsync(path);
        var list = new List<TEntity>();

        foreach (var key in keys)
        {
            var entity = await storageProvider.ReadAsync<TEntity>(key);
            list.Add(entity);
        }

        _cache = list;
        return _cache.ToList();
    }

    /// <summary>
    /// Returns the first entity matching the supplied predicate, or <see langword="null" /> when no match exists.
    /// </summary>
    /// <param name="predicate">Predicate used to filter entities in memory.</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous enumeration.</param>
    /// <returns>The first matching entity, or <see langword="null" /> if no entity matches.</returns>
    /// <example>
    /// <code>
    /// var user = await dbSet.FirstOrDefaultAsync(x => x.Id == 42);
    /// </code>
    /// </example>
    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var list = await ToListAsync(cancellationToken);
        return list.AsQueryable().FirstOrDefault(predicate);
    }

    /// <summary>
    /// Gets the CLR element type represented by this queryable set.
    /// </summary>
    public Type ElementType => typeof(TEntity);

    /// <summary>
    /// Gets the LINQ expression associated with this queryable wrapper.
    /// </summary>
    public Expression Expression => Expression.Constant(this);

    /// <summary>
    /// Gets the LINQ provider used to satisfy queryable operations.
    /// </summary>
    public IQueryProvider Provider => Enumerable.Empty<TEntity>().AsQueryable().Provider;


    /// <summary>
    /// Returns an asynchronous enumerator over the cached entity list.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the asynchronous enumeration.</param>
    /// <returns>An asynchronous enumerator for the current entity snapshot.</returns>
    /// <example>
    /// <code>
    /// await foreach (var user in dbSet)
    /// {
    ///     Console.WriteLine(user);
    /// }
    /// </code>
    /// </example>
    public async IAsyncEnumerator<TEntity> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var list = await ToListAsync(cancellationToken);
        foreach (var item in list)
        {
            yield return item;
        }
    }

    /// <summary>
    /// Returns a synchronous enumerator over the cached entity list.
    /// </summary>
    /// <returns>An enumerator over the currently cached entities, or an empty enumerator if the set has not been materialized yet.</returns>
    public IEnumerator<TEntity> GetEnumerator()
    {
        return _cache?.GetEnumerator() ?? Enumerable.Empty<TEntity>().GetEnumerator();
    }

    /// <summary>
    /// Returns a non-generic enumerator over the cached entity list.
    /// </summary>
    /// <returns>A non-generic enumerator over the current entity snapshot.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}