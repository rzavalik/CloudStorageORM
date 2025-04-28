namespace CloudStorageORM.Infrastructure
{
    using System.Collections;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.EntityFrameworkCore.Query;
    using System.Threading;

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
            var cloudProvider = (CloudStorageQueryProvider)Provider;
            var list = await cloudProvider.LoadEntitiesAsync<T>();

            foreach (var item in list)
            {
                yield return item;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            var task = ((CloudStorageQueryProvider)Provider).LoadEntitiesAsync<T>();
            task.Wait();
            var list = task.Result;
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
