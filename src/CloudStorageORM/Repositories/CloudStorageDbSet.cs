namespace CloudStorageORM.Repositories
{
    using System.Collections;
    using System.Linq.Expressions;
    using Interfaces.StorageProviders;

    public class CloudStorageDbSet<TEntity>(IStorageProvider storageProvider)
        : IQueryable<TEntity>, IAsyncEnumerable<TEntity>
        where TEntity : class
    {
        private List<TEntity>? _cache;

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

        public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var list = await ToListAsync(cancellationToken);
            return list.AsQueryable().FirstOrDefault(predicate);
        }

        public Type ElementType => typeof(TEntity);

        public Expression Expression => Expression.Constant(this);

        public IQueryProvider Provider => Enumerable.Empty<TEntity>().AsQueryable().Provider;


        public async IAsyncEnumerator<TEntity> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var list = await ToListAsync(cancellationToken);
            foreach (var item in list)
            {
                yield return item;
            }
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return _cache?.GetEnumerator() ?? Enumerable.Empty<TEntity>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}