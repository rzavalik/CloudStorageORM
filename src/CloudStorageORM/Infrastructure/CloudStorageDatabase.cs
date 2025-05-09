namespace CloudStorageORM.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using CloudStorageORM.Interfaces.Infrastructure;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.EntityFrameworkCore.Query;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.EntityFrameworkCore.Update;

    public class CloudStorageDatabase : IDatabase
    {
        private readonly DbContext _context;
        private readonly CloudStorageOptions _options;
        private readonly IStorageProvider _storageProvider;
        private readonly IBlobPathResolver _blobPathResolver;

        public IModel Model { get; }
        public IDatabaseCreator Creator { get; }
        public IExecutionStrategyFactory ExecutionStrategyFactory { get; }

        public CloudStorageDatabase(
            IModel model,
            IDatabaseCreator databaseCreator,
            IExecutionStrategyFactory executionStrategyFactory,
            IStorageProvider storageProvider,
            CloudStorageOptions options,
            ICurrentDbContext currentDbContext,
            IBlobPathResolver blobPathResolver)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Creator = databaseCreator ?? throw new ArgumentNullException(nameof(databaseCreator));
            ExecutionStrategyFactory = executionStrategyFactory ?? throw new ArgumentNullException(nameof(executionStrategyFactory));

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
            _context = currentDbContext.Context ?? throw new ArgumentNullException(nameof(currentDbContext));
            _blobPathResolver = blobPathResolver ?? throw new ArgumentNullException(nameof(blobPathResolver));
        }

        public Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
            => Creator.EnsureCreatedAsync(cancellationToken);

        public Task EnsureDeletedAsync(CancellationToken cancellationToken = default)
            => Creator.EnsureDeletedAsync(cancellationToken);

        public int SaveChanges(IList<IUpdateEntry> entries)
        {
            var request = ProcessChangesAsync(entries);
            request.Wait();
            return request.Result;
        }

        public async Task<int> SaveChangesAsync(IList<IUpdateEntry> entries, CancellationToken cancellationToken = default)
        {
            return await ProcessChangesAsync(entries);
        }

        private async Task<int> ProcessChangesAsync(IList<IUpdateEntry> entries)
        {
            var changes = 0;

            foreach (var entry in entries)
            {
                var entity = ((Microsoft.EntityFrameworkCore.ChangeTracking.Internal.InternalEntityEntry)entry).Entity;
                var path = _blobPathResolver.GetPath(entry);

                // Verifica se há um outro objeto já rastreado com a mesma chave
                var entityType = entry.EntityType.ClrType;
                var key = entry.EntityType.FindPrimaryKey();
                var keyProps = key?.Properties;

                if (keyProps != null)
                {
                    var existingTracked = entry.Context?.ChangeTracker.Entries()
                        .FirstOrDefault(e =>
                            e.Entity != null &&
                            e.Entity.GetType() == entityType &&
                            !ReferenceEquals(e.Entity, entity) &&
                            keyProps.All(p =>
                                Equals(
                                    p.PropertyInfo?.GetValue(e.Entity),
                                    p.PropertyInfo?.GetValue(entity))));

                    if (existingTracked != null)
                    {
                        // Desanexa a instância antiga para permitir que a nova seja usada
                        entry.Context.Entry(existingTracked.Entity).State = EntityState.Detached;
                    }
                }

                // Verifica se ainda está rastreado após resolver o conflito
                var tracked = entry.Context?.ChangeTracker
                    .Entries()
                    .Any(e =>
                        e.Entity != null &&
                        e.Entity.GetType() == entity.GetType() &&
                        KeysMatch(e, entry));

                if (tracked != true)
                {
                    continue;
                }

                switch (entry.EntityState)
                {
                    case EntityState.Added:
                    case EntityState.Modified:
                        await _storageProvider.SaveAsync(path, entity);
                        changes++;
                        break;

                    case EntityState.Deleted:
                        await _storageProvider.DeleteAsync(path);
                        changes++;
                        break;
                }
            }

            return changes;
        }

        private bool KeysMatch(EntityEntry trackedEntry, IUpdateEntry newEntry)
        {
            var keyProps = newEntry.EntityType.FindPrimaryKey()?.Properties;
            if (keyProps == null)
            {
                return false;
            }

            var entity = ((Microsoft.EntityFrameworkCore.ChangeTracking.Internal.InternalEntityEntry)newEntry).Entity;

            foreach (var keyProp in keyProps)
            {
                var trackedValue = keyProp.PropertyInfo?.GetValue(trackedEntry.Entity);
                var newValue = keyProp.PropertyInfo?.GetValue(entity);

                if (!object.Equals(trackedValue, newValue))
                {
                    return false;
                }
            }

            return true;
        }

        Func<QueryContext, TResult> IDatabase.CompileQuery<TResult>(Expression query, bool async)
        {
            return queryContext =>
            {
                Type entityType;

                if (typeof(TResult).IsGenericType &&
                    typeof(TResult).GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
                {
                    entityType = typeof(TResult).GetGenericArguments()[0];
                }
                else
                {
                    entityType = typeof(TResult);
                }

                var provider = new CloudStorageQueryProvider(this, _blobPathResolver);
                var queryableType = typeof(CloudStorageQueryable<>).MakeGenericType(entityType);

                var queryable = (IQueryable)Activator.CreateInstance(queryableType, provider)!;

                return (TResult)(object)queryable;
            };
        }

        internal async Task<IList<TEntity>> InternalListAsync<TEntity>(string path, DbContext context)
            where TEntity : class
        {
            var files = await _storageProvider.ListAsync(path);
            var results = new List<TEntity>();

            foreach (var file in files)
            {
                var entity = await _storageProvider.ReadAsync<TEntity>(file);

                if (context != null)
                {
                    var key = Model.FindEntityType(typeof(TEntity))?.FindPrimaryKey();
                    if (key != null)
                    {
                        var tracked = context.ChangeTracker.Entries<TEntity>()
                            .FirstOrDefault(e =>
                                key.Properties.All(p =>
                                    Equals(
                                        EF.Property<object>(e.Entity, p.Name),
                                        EF.Property<object>(entity, p.Name))));

                        if (tracked != null)
                        {
                            context.Entry(tracked.Entity).State = EntityState.Detached;
                        }
                    }

                    context.Attach(entity);
                }

                results.Add(entity);
            }

            return results;
        }

        internal async Task<IList<TEntity>> LoadEntitiesAsync<TEntity>(DbContext context)
            where TEntity : class
        {
            return await InternalListAsync<TEntity>(
                _blobPathResolver.GetBlobName(typeof(TEntity)),
                context);
        }

        public async Task<IList<TEntity>> ToListAsync<TEntity>(string containerName)
        {
            var files = await _storageProvider.ListAsync(containerName);
            var results = new List<TEntity>();

            var entityType = _context.Model.FindEntityType(typeof(TEntity));
            var keyProperties = entityType?.FindPrimaryKey()?.Properties;

            foreach (var file in files)
            {
                var entity = await _storageProvider.ReadAsync<TEntity>(file);
                if (entity is null)
                {
                    continue;
                }

                var existingTracked = _context.ChangeTracker.Entries()
                    .FirstOrDefault(e =>
                        keyProperties != null &&
                        keyProperties.All(p =>
                            Equals(
                                p.PropertyInfo?.GetValue(e.Entity),
                                p.PropertyInfo?.GetValue(entity)
                            )
                        ));

                if (existingTracked != null)
                {
                    _context.Entry(existingTracked.Entity).State = EntityState.Detached;
                }

                _context.Attach(entity);

                results.Add(entity);
            }

            return results;
        }

        public Expression<Func<QueryContext, TResult>> CompileQueryExpression<TResult>(Expression query, bool async, IReadOnlySet<string> nonNullableReferenceTypeParameters)
        {
            throw new NotImplementedException();
        }
    }
}
