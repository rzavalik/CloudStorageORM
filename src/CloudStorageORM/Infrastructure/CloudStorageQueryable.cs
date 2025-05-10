namespace CloudStorageORM.Infrastructure
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using Microsoft.EntityFrameworkCore.Query;

    public class CloudStorageQueryable<T> : IQueryable<T>, IAsyncEnumerable<T>
    {
        public CloudStorageQueryable(CloudStorageQueryProvider provider)
        {
            Provider = provider;
            Expression = Expression.Constant(this, typeof(IQueryable<T>));
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
            var asyncProvider = (IAsyncQueryProvider)Provider;

            var resultTask = asyncProvider.ExecuteAsync<Task<IEnumerable<T>>>(Expression, cancellationToken);

            var result = await resultTask;

            foreach (var item in result)
            {
                yield return item;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            var result = Provider.Execute<IEnumerable<T>>(Expression);
            return result.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
