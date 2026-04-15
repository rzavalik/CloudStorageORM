using System.Collections;
using System.Linq.Expressions;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// Queryable wrapper used to expose CloudStorageORM query results through LINQ.
/// </summary>
/// <typeparam name="T">Element type produced by the query.</typeparam>
public class CloudStorageQueryable<T> : IQueryable<T>, IAsyncEnumerable<T>
{
    /// <summary>
    /// Creates a new queryable with a root constant expression.
    /// </summary>
    /// <param name="provider">Query provider that executes this queryable.</param>
    public CloudStorageQueryable(CloudStorageQueryProvider provider)
    {
        Provider = provider;
        Expression = Expression.Constant(this);
    }

    /// <summary>
    /// Creates a new queryable with an explicit expression tree.
    /// </summary>
    /// <param name="provider">Query provider that executes this queryable.</param>
    /// <param name="expression">Expression tree representing the query pipeline.</param>
    public CloudStorageQueryable(CloudStorageQueryProvider provider, Expression expression)
    {
        Provider = provider;
        Expression = expression;
    }

    public Type ElementType => typeof(T);
    public Expression Expression { get; }
    public IQueryProvider Provider { get; }

    /// <inheritdoc />
    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var items = ((CloudStorageQueryProvider)Provider).Execute<IEnumerable<T>>(Expression);

        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        var items = ((CloudStorageQueryProvider)Provider).Execute<IEnumerable<T>>(Expression);
        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}