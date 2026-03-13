using System.Collections;
using System.Linq.Expressions;

namespace CloudStorageORM.Infrastructure;

public class CloudStorageQueryable<T> : IQueryable<T>, IAsyncEnumerable<T>
{
    public CloudStorageQueryable(CloudStorageQueryProvider provider)
    {
        Provider = provider;
        Expression = Expression.Constant(this);
    }

    public CloudStorageQueryable(CloudStorageQueryProvider provider, Expression expression)
    {
        Provider = provider;
        Expression = expression;
    }

    public Type ElementType => typeof(T);
    public Expression Expression { get; }
    public IQueryProvider Provider { get; }

    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var items = ((CloudStorageQueryProvider)Provider).Execute<IEnumerable<T>>(Expression);

        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }

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
