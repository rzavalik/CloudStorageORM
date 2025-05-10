namespace CloudStorageORM.Interfaces.DbSets
{
    using System.Linq.Expressions;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore.Metadata;

    public interface ICloudStorageDbSet<TEntity> where TEntity : class
    {
        ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        EntityEntry<TEntity> Add(TEntity entity);
        void AddRange(params TEntity[] entities);
        void AddRange(IEnumerable<TEntity> entities);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        Task AddRangeAsync(params TEntity[] entities);

        EntityEntry<TEntity> Attach(TEntity entity);
        void AttachRange(params TEntity[] entities);
        void AttachRange(IEnumerable<TEntity> entities);

        ValueTask<TEntity?> FindAsync(params object?[]? keyValues);
        ValueTask<TEntity?> FindAsync(object[] keyValues, CancellationToken cancellationToken = default);
        TEntity? Find(params object[] keyValues);
        Task<TEntity?> FindByIdAsync<T>(T id, CancellationToken cancellationToken = default);

        EntityEntry<TEntity> Remove(TEntity entity);
        void RemoveRange(params TEntity[] entities);
        void RemoveRange(IEnumerable<TEntity> entities);

        EntityEntry<TEntity> Update(TEntity entity);
        void UpdateRange(params TEntity[] entities);
        void UpdateRange(IEnumerable<TEntity> entities);

        List<TEntity> ToList();
        Task<List<TEntity>> ToListAsync(CancellationToken cancellationToken = default);

        IAsyncEnumerable<TEntity> AsAsyncEnumerable();
        IQueryable<TEntity> AsQueryable();
        EntityEntry<TEntity> Entry(TEntity entity);

        IEntityType EntityType { get; }
        Expression Expression { get; }
        IQueryProvider Provider { get; }
        System.Type ElementType { get; }
    }
}