namespace CloudStorageORM.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using CloudStorageORM.Infrastructure;
    using CloudStorageORM.Interfaces.DbSets;
    using CloudStorageORM.Interfaces.Infrastructure;
    using CloudStorageORM.Interfaces.StorageProviders;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Metadata;

    public class CloudStorageDbSet<TEntity> :
        DbSet<TEntity>,
        ICloudStorageDbSet<TEntity>,
        IQueryable<TEntity>,
        IAsyncEnumerable<TEntity>
        where TEntity : class
    {
        private readonly DbContext _context;
        private readonly IStorageProvider _storageProvider;
        private readonly IBlobPathResolver _blobPathResolver;
        private readonly CloudStorageQueryProvider _queryProvider;
        private readonly Expression _expression;
        private List<TEntity>? _cache;

        public CloudStorageDbSet(
            IStorageProvider storageProvider,
            IBlobPathResolver blobPathResolver,
            ICurrentDbContext currentDbContext,
            CloudStorageDatabase database)
        {
            _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
            _blobPathResolver = blobPathResolver ?? throw new ArgumentNullException(nameof(blobPathResolver));
            _context = currentDbContext.Context ?? throw new ArgumentNullException(nameof(currentDbContext));
            _queryProvider = new CloudStorageQueryProvider(database, _blobPathResolver);
            _expression = Expression.Constant(new CloudStorageQueryable<TEntity>(_queryProvider));
        }

        public IQueryProvider Provider => _queryProvider;

        public Expression Expression => _expression;

        public Type ElementType => typeof(TEntity);

        public async IAsyncEnumerator<TEntity> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            var list = await ToListAsync(cancellationToken);
            foreach (var item in list)
            {
                yield return item;
            }
        }

        public override IEntityType EntityType => throw new NotImplementedException("This is not used in CloudStorageORM directly.");

        public override IAsyncEnumerable<TEntity> AsAsyncEnumerable()
        {
            return ToAsyncEnumerable();
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return (_cache ?? ToListAsync().GetAwaiter().GetResult()).GetEnumerator();
        }

        private async IAsyncEnumerable<TEntity> ToAsyncEnumerable(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var list = await ToListAsync(cancellationToken);
            foreach (var item in list)
            {
                yield return item;
            }
        }

        private object GetEntityKey(object entity)
        {
            var keyProperty = _context
                .Model
                .FindEntityType(typeof(TEntity))?
                .FindPrimaryKey()?
                .Properties
                .FirstOrDefault();

            if (keyProperty == null)
            {
                throw new InvalidOperationException($"Entity {typeof(TEntity).Name} must have an 'Id' property.");
            }

            var key = keyProperty.PropertyInfo?.GetValue(entity);
            if (key == null)
            {
                throw new InvalidOperationException($"Entity {typeof(TEntity).Name} has null 'Id' value.");
            }

            return key;
        }

        #region Entity Operations

        public override EntityEntry<TEntity> Add(TEntity entity)
        {
            return AddAsync(entity).AsTask().GetAwaiter().GetResult();
        }

        public override ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var path = _blobPathResolver.GetPath(typeof(TEntity), GetEntityKey(entity));
            _storageProvider.SaveAsync(path, entity).GetAwaiter().GetResult();

            _cache?.Add(entity);

            return new ValueTask<EntityEntry<TEntity>>(_context.Entry(entity));
        }

        public async Task<List<TEntity>> ToListAsync(CancellationToken cancellationToken = default)
        {
            if (false && _cache != null)
            {
                return [.. _cache];
            }

            var path = _blobPathResolver.GetPath(typeof(TEntity));
            var keys = await _storageProvider.ListAsync(path);
            var list = new List<TEntity>();

            foreach (var key in keys)
            {
                var entity = await _storageProvider.ReadAsync<TEntity>(key);
                if (entity != null)
                {
                    list.Add(entity);
                }
            }

            _cache = list;
            return list;
        }

        public List<TEntity> ToList()
        {
            return ToListAsync().GetAwaiter().GetResult();
        }

        public override void AddRange(params TEntity[] entities)
        {
            foreach (var entity in entities)
            {
                Add(entity);
            }
        }

        public override void AddRange(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                Add(entity);
            }
        }

        public override EntityEntry<TEntity> Attach(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var entry = _context.Entry(entity);
            entry.State = EntityState.Unchanged;

            _cache ??= new List<TEntity>();
            if (!_cache.Contains(entity))
            {
                _cache.Add(entity);
            }

            return entry;
        }

        public override void AttachRange(params TEntity[] entities)
        {
            foreach (var entity in entities)
            {
                Attach(entity);
            }
        }

        public override void AttachRange(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                Attach(entity);
            }
        }

        public override ValueTask<TEntity?> FindAsync(params object?[]? keyValues)
        {
            if (keyValues is not [var id])
            {
                throw new NotSupportedException("Only single-key lookups are supported.");
            }

            var result = FindByIdAsync(id).GetAwaiter().GetResult();
            return new ValueTask<TEntity?>(result);
        }

        public override async ValueTask<TEntity?> FindAsync(object[] keyValues, CancellationToken cancellationToken = default)
        {
            if (keyValues is not [var id])
            {
                throw new NotSupportedException("Only single-key lookups are supported.");
            }

            return await FindByIdAsync(id, cancellationToken);
        }

        public override TEntity? Find(params object[] keyValues)
        {
            if (keyValues is not [var id])
            {
                throw new NotSupportedException("Only single-key lookups are supported.");
            }

            return FindByIdAsync(id).GetAwaiter().GetResult();
        }

        public async Task<TEntity?> FindByIdAsync<T>(T id, CancellationToken cancellationToken = default)
        {
            var trackedEntity = _context.ChangeTracker
                .Entries<TEntity>()
                .FirstOrDefault(e =>
                    e.State != EntityState.Detached &&
                    Equals(GetEntityKey(e.Entity), id))
                ?.Entity;

            if (trackedEntity != null)
            {
                return trackedEntity;
            }

            var path = _blobPathResolver.GetPath(typeof(TEntity), id);
            var entity = await _storageProvider.ReadAsync<TEntity>(path);

            if (entity != null)
            {
                _cache ??= new List<TEntity>();
                if (!_cache.Contains(entity))
                {
                    _cache.Add(entity);
                }

                _context.Attach(entity);
            }

            return entity;
        }

        public override EntityEntry<TEntity> Remove(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var entry = _context.Entry(entity);
            entry.State = EntityState.Deleted;

            var path = _blobPathResolver.GetPath(typeof(TEntity), GetEntityKey(entity));
            _ = _storageProvider.DeleteAsync(path);

            _cache?.Remove(entity);

            return entry;
        }

        public override void RemoveRange(params TEntity[] entities)
        {
            RemoveRange((IEnumerable<TEntity>)entities);
        }

        public override void RemoveRange(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                Remove(entity);
            }
        }

        public override EntityEntry<TEntity> Update(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var entry = _context.Entry(entity);
            entry.State = EntityState.Modified;

            var path = _blobPathResolver.GetPath(typeof(TEntity), GetEntityKey(entity));
            _ = _storageProvider.SaveAsync(path, entity);

            if (_cache != null)
            {
                var existing = _cache
                    .FirstOrDefault(e => Equals(GetEntityKey(e), GetEntityKey(entity)));

                if (existing != null)
                {
                    _cache.Remove(existing);
                }

                _cache.Add(entity);
            }

            return entry;
        }

        public override void UpdateRange(params TEntity[] entities)
        {
            UpdateRange((IEnumerable<TEntity>)entities);
        }

        public override void UpdateRange(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                Update(entity);
            }
        }

        public override async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
            {
                await AddAsync(entity, cancellationToken);
            }
        }
        public override Task AddRangeAsync(params TEntity[] entities)
        {
            return AddRangeAsync((IEnumerable<TEntity>)entities);
        }

        public override IQueryable<TEntity> AsQueryable()
        {
            return this;
        }

        public override EntityEntry<TEntity> Entry(TEntity entity)
        {
            return _context.Entry(entity);
        }

        #endregion
    }
}