namespace CloudStorageORM.Infrastructure
{
    using System.Collections;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.EntityFrameworkCore.Query;

    public class CloudStorageQueryable<T> : IQueryable<T>
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
