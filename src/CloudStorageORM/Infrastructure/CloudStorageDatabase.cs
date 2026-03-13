using System.Linq.Expressions;
using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace CloudStorageORM.Infrastructure;

public class CloudStorageDatabase(
    IModel model,
    IDatabaseCreator databaseCreator,
    IExecutionStrategyFactory executionStrategyFactory,
    IStorageProvider storageProvider,
    ICurrentDbContext currentDbContext,
    IBlobPathResolver blobPathResolver)
    : IDatabase
{
    private readonly DbContext _context =
        currentDbContext.Context ?? throw new ArgumentNullException(nameof(currentDbContext));

    private readonly IStorageProvider _storageProvider =
        storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));

    private readonly IBlobPathResolver _blobPathResolver =
        blobPathResolver ?? throw new ArgumentNullException(nameof(blobPathResolver));

    public IModel Model { get; } = model ?? throw new ArgumentNullException(nameof(model));

    private IDatabaseCreator Creator { get; } =
        databaseCreator ?? throw new ArgumentNullException(nameof(databaseCreator));

    public IExecutionStrategyFactory ExecutionStrategyFactory { get; } = executionStrategyFactory ??
                                                                         throw new ArgumentNullException(
                                                                             nameof(executionStrategyFactory));

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

    public async Task<int> SaveChangesAsync(IList<IUpdateEntry> entries,
        CancellationToken cancellationToken = default)
    {
        return await ProcessChangesAsync(entries);
    }

    private async Task<int> ProcessChangesAsync(IList<IUpdateEntry> entries)
    {
        var changes = 0;

        foreach (var entry in entries)
        {
            var entity = ((InternalEntityEntry)entry).Entity;
            var path = _blobPathResolver.GetPath(entry);

            // Verifica se há um outro objeto já rastreado com a mesma chave
            var entityType = entry.EntityType.ClrType;
            var key = entry.EntityType.FindPrimaryKey();
            var keyProps = key?.Properties;

            if (keyProps != null)
            {
                var existingTracked = entry.Context.ChangeTracker.Entries()
                    .FirstOrDefault(e =>
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
            var tracked = entry.Context.ChangeTracker
                .Entries()
                .Any(e =>
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

    private static bool KeysMatch(EntityEntry trackedEntry, IUpdateEntry newEntry)
    {
        var keyProps = newEntry.EntityType.FindPrimaryKey()?.Properties;
        if (keyProps == null)
        {
            return false;
        }

        var entity = ((InternalEntityEntry)newEntry).Entity;

        foreach (var keyProp in keyProps)
        {
            var trackedValue = keyProp.PropertyInfo?.GetValue(trackedEntry.Entity);
            var newValue = keyProp.PropertyInfo?.GetValue(entity);

            if (!Equals(trackedValue, newValue))
            {
                return false;
            }
        }

        return true;
    }

    Func<QueryContext, TResult> IDatabase.CompileQuery<TResult>(Expression query, bool async)
    {
        var provider = new CloudStorageQueryProvider(this, _blobPathResolver);

        return queryContext =>
        {
            var executableQuery = ReplaceQueryParameters(query, queryContext);

            var resultType = typeof(TResult);
            if (!IsSequenceResult(resultType))
            {
                return provider.Execute<TResult>(executableQuery);
            }

            var entityType = resultType.GetGenericArguments()[0];
            var queryableType = typeof(CloudStorageQueryable<>).MakeGenericType(entityType);
            var queryable = (IQueryable)Activator.CreateInstance(queryableType, provider, executableQuery)!;
            return (TResult)queryable;
        };
    }

    private static Expression ReplaceQueryParameters(Expression query, QueryContext? queryContext)
    {
        var parameterValues = queryContext?.ParameterValues ?? new Dictionary<string, object?>();
        return new QueryParameterReplacingVisitor(parameterValues).Visit(query);
    }

    private static bool IsSequenceResult(Type resultType)
    {
        if (!resultType.IsGenericType)
        {
            return false;
        }

        var resultTypeDefinition = resultType.GetGenericTypeDefinition();
        return resultTypeDefinition == typeof(IQueryable<>)
               || resultTypeDefinition == typeof(IEnumerable<>)
               || resultTypeDefinition == typeof(IOrderedQueryable<>)
               || resultTypeDefinition == typeof(IOrderedEnumerable<>)
               || resultTypeDefinition == typeof(IAsyncEnumerable<>);
    }

    private sealed class QueryParameterReplacingVisitor(IReadOnlyDictionary<string, object?> parameterValues)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node.Name is null || !parameterValues.TryGetValue(node.Name, out var value))
            {
                return base.VisitParameter(node);
            }

            if (value is null)
            {
                return node.Type.IsValueType && Nullable.GetUnderlyingType(node.Type) is null
                    ? Expression.Default(node.Type)
                    : Expression.Constant(null, node.Type);
            }

            return Expression.Constant(value, node.Type);
        }
    }

    private async Task<IList<TEntity>> InternalListAsync<TEntity>(string path, DbContext context)
        where TEntity : class
    {
        var files = await _storageProvider.ListAsync(path);
        var results = new List<TEntity>();

        foreach (var file in files)
        {
            var entity = await _storageProvider.ReadAsync<TEntity>(file);

            AttachOrReplaceTrackedEntity(context, entity);

            results.Add(entity);
        }

        return results;
    }

    public async Task<IList<TEntity>> LoadEntitiesAsync<TEntity>(DbContext context)
        where TEntity : class
    {
        return await InternalListAsync<TEntity>(
            _blobPathResolver.GetBlobName(typeof(TEntity)),
            context);
    }

    public async Task<TEntity?> TryLoadByPrimaryKeyAsync<TEntity>(object keyValue, DbContext? context = null)
        where TEntity : class
    {
        var path = _blobPathResolver.GetPath(typeof(TEntity), keyValue);
        var entity = await _storageProvider.ReadAsync<TEntity>(path);

        AttachOrReplaceTrackedEntity(context ?? _context, entity);
        return entity;
    }

    public async Task<IList<TEntity>> ToListAsync<TEntity>(string containerName)
        where TEntity : class
    {
        var files = await _storageProvider.ListAsync(containerName);
        var results = new List<TEntity>();

        var entityType = _context.Model.FindEntityType(typeof(TEntity));
        var keyProperties = entityType?.FindPrimaryKey()?.Properties;

        foreach (var file in files)
        {
            var entity = await _storageProvider.ReadAsync<TEntity?>(file);

            if (entity is null)
            {
                continue;
            }

            AttachOrReplaceTrackedEntity(_context, entity, keyProperties);

            results.Add(entity);
        }

        return results;
    }

    public Expression<Func<QueryContext, TResult>> CompileQueryExpression<TResult>(Expression query, bool async,
        IReadOnlySet<string> nonNullableReferenceTypeParameters)
    {
        throw new NotImplementedException();
    }

    private void AttachOrReplaceTrackedEntity<TEntity>(DbContext? context, TEntity? entity,
        IReadOnlyList<IProperty>? keyProperties = null)
        where TEntity : class
    {
        if (context is null || entity is null)
        {
            return;
        }

        keyProperties ??= Model.FindEntityType(typeof(TEntity))?.FindPrimaryKey()?.Properties;
        var existingTracked = context.ChangeTracker.Entries()
            .FirstOrDefault(e =>
                keyProperties != null &&
                keyProperties.All(p =>
                    Equals(
                        p.PropertyInfo?.GetValue(e.Entity),
                        p.PropertyInfo?.GetValue(entity))));

        if (existingTracked != null)
        {
            context.Entry(existingTracked.Entity).State = EntityState.Detached;
        }

        context.Attach(entity);
    }
}