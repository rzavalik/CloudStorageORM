using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using CloudStorageORM.Abstractions;
using CloudStorageORM.Constants;
using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Observability;
using CloudStorageORM.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.Logging;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// IDatabase implementation that persists EF entity changes to object storage.
/// </summary>
/// <param name="model">The EF model being persisted.</param>
/// <param name="databaseCreator">Provider database creator.</param>
/// <param name="executionStrategyFactory">EF execution strategy factory.</param>
/// <param name="storageProvider">Cloud storage provider implementation.</param>
/// <param name="currentDbContext">Accessor for the current DbContext instance.</param>
/// <param name="blobPathResolver">Resolves entity blob paths.</param>
/// <param name="transactionManager">Current transaction manager.</param>
/// <param name="cloudStorageOptions">CloudStorageORM options controlling retries and observability.</param>
/// <param name="loggerFactory">Factory used for category-specific loggers.</param>
/// <param name="logger">Logger for database-level events.</param>
public class CloudStorageDatabase(
    IModel model,
    IDatabaseCreator databaseCreator,
    IExecutionStrategyFactory executionStrategyFactory,
    IStorageProvider storageProvider,
    ICurrentDbContext currentDbContext,
    IBlobPathResolver blobPathResolver,
    IDbContextTransactionManager transactionManager,
    CloudStorageOptions? cloudStorageOptions = null,
    ILoggerFactory? loggerFactory = null,
    ILogger<CloudStorageDatabase>? logger = null)
    : IDatabase
{
    private readonly DbContext _context =
        currentDbContext.Context ?? throw new ArgumentNullException(nameof(currentDbContext));

    private readonly IStorageProvider _storageProvider =
        storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));

    private readonly IBlobPathResolver _blobPathResolver =
        blobPathResolver ?? throw new ArgumentNullException(nameof(blobPathResolver));

    private readonly IDbContextTransactionManager _transactionManager =
        transactionManager ?? throw new ArgumentNullException(nameof(transactionManager));

    private readonly ILogger<CloudStorageDatabase>? _logger = logger;

    private readonly ILogger<CloudStorageQueryProvider>? _queryLogger =
        loggerFactory?.CreateLogger<CloudStorageQueryProvider>();

    private readonly bool _enableLogging = cloudStorageOptions?.Observability.EnableLogging ?? true;
    private readonly bool _enableTracing = cloudStorageOptions?.Observability.EnableTracing ?? true;

    private readonly CloudStorageRetryExecutor _retryExecutor =
        new(cloudStorageOptions?.Retry ?? new CloudStorageRetryOptions());

    /// <summary>
    /// Gets the EF model used by the database implementation.
    /// </summary>
    public IModel Model { get; } = model ?? throw new ArgumentNullException(nameof(model));

    private IDatabaseCreator Creator { get; } =
        databaseCreator ?? throw new ArgumentNullException(nameof(databaseCreator));

    /// <summary>
    /// Gets the execution strategy factory used for EF infrastructure integration.
    /// </summary>
    public IExecutionStrategyFactory ExecutionStrategyFactory { get; } = executionStrategyFactory ??
                                                                         throw new ArgumentNullException(
                                                                             nameof(executionStrategyFactory));

    /// <summary>
    /// Ensures the backing store is created for the current provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task that completes when creation has been verified or performed.</returns>
    public Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
        => Creator.EnsureCreatedAsync(cancellationToken);

    /// <summary>
    /// Ensures the backing store is deleted for the current provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task that completes when deletion has been verified or performed.</returns>
    public Task EnsureDeletedAsync(CancellationToken cancellationToken = default)
        => Creator.EnsureDeletedAsync(cancellationToken);

    /// <summary>
    /// Persists pending update entries to object storage synchronously.
    /// </summary>
    /// <param name="entries">Update entries representing tracked entity changes.</param>
    /// <returns>The number of persisted changes.</returns>
    public int SaveChanges(IList<IUpdateEntry> entries)
    {
        var request = ProcessChangesAsync(entries, CancellationToken.None);
        request.Wait();
        return request.Result;
    }

    /// <summary>
    /// Persists pending update entries to object storage asynchronously.
    /// </summary>
    /// <param name="entries">Update entries representing tracked entity changes.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The number of persisted changes.</returns>
    public async Task<int> SaveChangesAsync(IList<IUpdateEntry> entries,
        CancellationToken cancellationToken = default)
    {
        return await ProcessChangesAsync(entries, cancellationToken);
    }

    /// <summary>
    /// Compiles a query expression into an executable delegate.
    /// </summary>
    /// <typeparam name="TResult">The query result type.</typeparam>
    /// <param name="query">The query expression to compile.</param>
    /// <param name="async">Whether the query should be compiled for asynchronous execution.</param>
    /// <returns>A compiled query delegate.</returns>
    public Func<QueryContext, TResult> CompileQuery<TResult>(Expression query, bool async)
    {
        throw new NotImplementedException();
    }

    private async Task<int> ProcessChangesAsync(IList<IUpdateEntry> entries, CancellationToken cancellationToken)
    {
        using var activity = _enableTracing ? CloudStorageOrmActivitySource.StartActivity("SaveChanges") : null;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (_enableLogging)
            {
                _logger?.LogSaveChangesStarting(entries.Count);
            }

            var changes = 0;

            foreach (var entry in entries)
            {
                var path = _blobPathResolver.GetPath(entry);
                var entity = ((InternalEntityEntry)entry).Entity;

                // Check whether another entity with the same key is already tracked.
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
                        // Detach the previous instance so the new one can be used.
                        entry.Context.Entry(existingTracked.Entity).State = EntityState.Detached;
                    }
                }

                // Ensure the entity is still tracked after resolving key conflicts.
                var tracked = entry.Context.ChangeTracker
                    .Entries()
                    .Any(e =>
                        e.Entity.GetType() == entity.GetType() &&
                        KeysMatch(e, entry));

                if (!tracked)
                {
                    continue;
                }

                switch (entry.EntityState)
                {
                    case EntityState.Added:
                    {
                        var concurrencyEnabled = TryGetConcurrencyProperty(entry.EntityType, out _);
                        var newETag = await ExecuteOrStageSaveAsync(path, entity, null, concurrencyEnabled,
                            cancellationToken);
                        ApplySavedETag(entry, newETag);
                        if (_enableLogging)
                        {
                            _logger?.LogEntitySaved(entry.EntityType.ClrType.Name,
                                Path.GetFileNameWithoutExtension(path),
                                "added");
                        }

                        changes++;
                        break;
                    }

                    case EntityState.Modified:
                    {
                        var concurrencyEnabled = TryGetConcurrencyProperty(entry.EntityType, out _);
                        var originalETag = GetOriginalETag(entry);
                        if (concurrencyEnabled && string.IsNullOrWhiteSpace(originalETag))
                        {
                            throw new DbUpdateConcurrencyException(
                                "ETag concurrency is enabled, but no original ETag value is available for update.");
                        }

                        var newETag = await ExecuteOrStageSaveAsync(path, entity, originalETag, concurrencyEnabled,
                            cancellationToken);
                        ApplySavedETag(entry, newETag);
                        if (_enableLogging)
                        {
                            _logger?.LogEntitySaved(entry.EntityType.ClrType.Name,
                                Path.GetFileNameWithoutExtension(path),
                                "updated");
                        }

                        changes++;
                        break;
                    }

                    case EntityState.Deleted:
                    {
                        var concurrencyEnabled = TryGetConcurrencyProperty(entry.EntityType, out _);
                        var originalETag = GetOriginalETag(entry);
                        if (concurrencyEnabled && string.IsNullOrWhiteSpace(originalETag))
                        {
                            throw new DbUpdateConcurrencyException(
                                "ETag concurrency is enabled, but no original ETag value is available for delete.");
                        }

                        await ExecuteOrStageDeleteAsync(path, originalETag, concurrencyEnabled, cancellationToken);
                        if (_enableLogging)
                        {
                            _logger?.LogEntitySaved(entry.EntityType.ClrType.Name,
                                Path.GetFileNameWithoutExtension(path),
                                "deleted");
                        }

                        changes++;
                        break;
                    }
                }
            }

            stopwatch.Stop();
            if (_enableLogging)
            {
                _logger?.LogSaveChangesCompleted(changes, stopwatch.ElapsedMilliseconds);
            }

            activity?.SetTag("changes.count", changes);
            return changes;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            if (_enableLogging)
            {
                _logger?.LogSaveChangesFailed(ex, stopwatch.ElapsedMilliseconds);
            }

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private async Task<string?> ExecuteOrStageSaveAsync(
        string path,
        object entity,
        string? ifMatchETag,
        bool useConditionalRequest,
        CancellationToken cancellationToken)
    {
        if (_transactionManager is CloudStorageTransactionManager { HasActiveTransaction: true } manager)
        {
            if (manager.IsDurableJournalEnabled)
            {
                var stagedIfMatchETag = useConditionalRequest ? ifMatchETag : null;
                await manager.StageSaveOperationAsync(path, entity, stagedIfMatchETag, cancellationToken);
            }
            else
            {
                manager.EnqueueOperation(_ => _storageProvider.SaveAsync(path, entity));
            }

            return null;
        }

        try
        {
            if (_enableLogging)
            {
                _logger?.LogBlobUploadStarting(path, 0);
            }

            if (useConditionalRequest)
            {
                var savedEtag = await ExecuteWithRetryAsync(_ => _storageProvider.SaveAsync(path, entity, ifMatchETag),
                    cancellationToken);
                if (_enableLogging)
                {
                    _logger?.LogBlobUploadCompleted(path, 0);
                }

                return savedEtag;
            }

            await ExecuteWithRetryAsync(_ => _storageProvider.SaveAsync(path, entity), cancellationToken);
            if (_enableLogging)
            {
                _logger?.LogBlobUploadCompleted(path, 0);
            }

            return null;
        }
        catch (StoragePreconditionFailedException ex)
        {
            if (_enableLogging)
            {
                _logger?.LogConcurrencyConflict(entity.GetType().Name, Path.GetFileNameWithoutExtension(path), path);
            }

            throw CreateConcurrencyException(path, ex);
        }
    }

    private async Task ExecuteOrStageDeleteAsync(
        string path,
        string? ifMatchETag,
        bool useConditionalRequest,
        CancellationToken cancellationToken)
    {
        if (_transactionManager is CloudStorageTransactionManager { HasActiveTransaction: true } manager)
        {
            if (manager.IsDurableJournalEnabled)
            {
                var stagedIfMatchETag = useConditionalRequest ? ifMatchETag : null;
                await manager.StageDeleteOperationAsync(path, stagedIfMatchETag, cancellationToken);
            }
            else
            {
                manager.EnqueueOperation(_ => _storageProvider.DeleteAsync(path));
            }

            return;
        }

        try
        {
            if (_enableLogging)
            {
                _logger?.LogBlobDeletionStarting(path);
            }

            if (!useConditionalRequest)
            {
                await ExecuteWithRetryAsync(_ => _storageProvider.DeleteAsync(path), cancellationToken);
                if (_enableLogging)
                {
                    _logger?.LogBlobDeletionCompleted(path, 0);
                }

                return;
            }

            await ExecuteWithRetryAsync(_ => _storageProvider.DeleteAsync(path, ifMatchETag), cancellationToken);
            if (_enableLogging)
            {
                _logger?.LogBlobDeletionCompleted(path, 0);
            }
        }
        catch (StoragePreconditionFailedException ex)
        {
            if (_enableLogging)
            {
                _logger?.LogConcurrencyConflict("Entity", Path.GetFileNameWithoutExtension(path), path);
            }

            throw CreateConcurrencyException(path, ex);
        }
    }

    private static bool KeysMatch(EntityEntry trackedEntry, IUpdateEntry newEntry)
    {
        var keyProps = newEntry.EntityType.FindPrimaryKey()?.Properties;
        if (keyProps == null)
        {
            return false;
        }

        var entity = ((InternalEntityEntry)newEntry).Entity;

        return !(
            from keyProp in keyProps
            let trackedValue = keyProp.PropertyInfo?.GetValue(trackedEntry.Entity)
            let newValue = keyProp.PropertyInfo?.GetValue(entity)
            where !Equals(trackedValue, newValue)
            select trackedValue
        ).Any();
    }

    internal Task ExecuteWithRetryAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        return _retryExecutor.ExecuteAsync(operation, cancellationToken);
    }

    internal Task<TResult> ExecuteWithRetryAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        return _retryExecutor.ExecuteAsync(operation, cancellationToken);
    }

    Func<QueryContext, TResult> IDatabase.CompileQuery<TResult>(Expression query, bool async)
    {
        var provider = new CloudStorageQueryProvider(
            this,
            _blobPathResolver,
            _queryLogger,
            _enableLogging,
            _enableTracing);

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
        var parameterValues = TryGetQueryParameterValues(queryContext) ?? new Dictionary<string, object?>();
        return new QueryParameterReplacingVisitor(parameterValues).Visit(query);
    }

    private static IReadOnlyDictionary<string, object?>? TryGetQueryParameterValues(QueryContext? queryContext)
    {
        var property = queryContext?.GetType().GetProperty("ParameterValues");
        return property?.GetValue(queryContext) as IReadOnlyDictionary<string, object?>;
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
        var files = await ExecuteWithRetryAsync(_ => _storageProvider.ListAsync(path));
        var results = new List<TEntity>();

        foreach (var file in files)
        {
            if (_enableLogging)
            {
                _logger?.LogBlobDownloadStarting(file);
            }

            var storageObject = await ExecuteWithRetryAsync(_ => _storageProvider.ReadWithMetadataAsync<TEntity>(file));
            if (_enableLogging)
            {
                _logger?.LogBlobDownloadCompleted(file, 0, 0);
            }

            if (!storageObject.Exists || storageObject.Value is null)
            {
                continue;
            }

            AttachOrReplaceTrackedEntity(context, storageObject.Value, storageObject.ETag);

            results.Add(storageObject.Value);
        }

        return results;
    }

    /// <summary>
    /// Loads all entities of a given type and attaches them to the provided DbContext.
    /// </summary>
    /// <typeparam name="TEntity">Entity type to load.</typeparam>
    /// <param name="context">DbContext used for tracking loaded entities.</param>
    /// <returns>All existing entities for the requested type.</returns>
    /// <example>
    /// <code>
    /// var users = await database.LoadEntitiesAsync&lt;User&gt;(context);
    /// </code>
    /// </example>
    public async Task<IList<TEntity>> LoadEntitiesAsync<TEntity>(DbContext context)
        where TEntity : class
    {
        return await InternalListAsync<TEntity>(
            _blobPathResolver.GetBlobName(typeof(TEntity)),
            context);
    }

    /// <summary>
    /// Loads a paged slice of entities for the requested type using provider-native key listing.
    /// </summary>
    /// <typeparam name="TEntity">Entity type to load.</typeparam>
    /// <param name="skip">Number of matching entities to skip.</param>
    /// <param name="take">Maximum number of matching entities to load.</param>
    /// <param name="context">Optional context used for tracking; defaults to the current context.</param>
    /// <returns>A page of entities after applying skip/take.</returns>
    public async Task<IList<TEntity>> LoadPageAsync<TEntity>(int skip, int take, DbContext? context = null)
        where TEntity : class
    {
        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skip), skip, "Skip must be greater than or equal to zero.");
        }

        switch (take)
        {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(take), take,
                    "Take must be greater than or equal to zero.");

            case 0:
                return [];
        }

        var targetContext = context ?? _context;
        var blobName = _blobPathResolver.GetBlobName(typeof(TEntity));
        var pagePaths = await SelectPathsPageAsync(blobName, _ => true, skip, take);
        return await LoadEntitiesByPathsAsync<TEntity>(targetContext, pagePaths);
    }

    /// <summary>
    /// Loads a single entity by primary-key value from object storage and attaches it to the context.
    /// </summary>
    /// <typeparam name="TEntity">Entity type to load.</typeparam>
    /// <param name="keyValue">Primary-key value used to resolve the blob path.</param>
    /// <param name="context">Optional context used for tracking; defaults to the current context.</param>
    /// <returns>The loaded entity when found; otherwise <see langword="null" />.</returns>
    /// <example>
    /// <code>
    /// var user = await database.TryLoadByPrimaryKeyAsync&lt;User&gt;(42);
    /// </code>
    /// </example>
    public async Task<TEntity?> TryLoadByPrimaryKeyAsync<TEntity>(object keyValue, DbContext? context = null)
        where TEntity : class
    {
        var path = _blobPathResolver.GetPath(typeof(TEntity), keyValue);
        if (_enableLogging)
        {
            _logger?.LogBlobDownloadStarting(path);
        }

        var storageObject = await ExecuteWithRetryAsync(_ => _storageProvider.ReadWithMetadataAsync<TEntity>(path));
        if (_enableLogging)
        {
            _logger?.LogBlobDownloadCompleted(path, 0, 0);
        }

        var entity = storageObject.Value;

        AttachOrReplaceTrackedEntity(context ?? _context, entity, storageObject.ETag);
        return entity;
    }

    /// <summary>
    /// Loads entities whose primary keys are within the provided range and attaches them to the context.
    /// </summary>
    /// <typeparam name="TEntity">Entity type to load.</typeparam>
    /// <param name="lowerBound">Optional lower bound key value.</param>
    /// <param name="lowerInclusive"><see langword="true" /> to include the lower bound.</param>
    /// <param name="upperBound">Optional upper bound key value.</param>
    /// <param name="upperInclusive"><see langword="true" /> to include the upper bound.</param>
    /// <param name="context">Optional context used for tracking; defaults to the current context.</param>
    /// <returns>Entities whose storage keys satisfy the specified range.</returns>
    /// <example>
    /// <code>
    /// var users = await database.LoadByPrimaryKeyRangeAsync&lt;User&gt;(100, true, 200, false);
    /// </code>
    /// </example>
    public async Task<IList<TEntity>> LoadByPrimaryKeyRangeAsync<TEntity>(
        object? lowerBound,
        bool lowerInclusive,
        object? upperBound,
        bool upperInclusive,
        DbContext? context = null)
        where TEntity : class
    {
        var targetContext = context ?? _context;
        var blobName = _blobPathResolver.GetBlobName(typeof(TEntity));
        var files = await ExecuteWithRetryAsync(_ => _storageProvider.ListAsync(blobName));
        var results = new List<TEntity>();

        var entityType = Model.FindEntityType(typeof(TEntity));
        var keyProperties = entityType?.FindPrimaryKey()?.Properties;
        var keyProperty = keyProperties?.FirstOrDefault();

        if (keyProperty is null)
        {
            return results;
        }

        var keyType = Nullable.GetUnderlyingType(keyProperty.ClrType) ?? keyProperty.ClrType;
        var typedLower = lowerBound is null ? null : ConvertToKeyType(lowerBound, keyType);
        var typedUpper = upperBound is null ? null : ConvertToKeyType(upperBound, keyType);

        foreach (var file in files)
        {
            var keyText = Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrWhiteSpace(keyText)
                || !TryParseStorageKey(keyText, keyType, out var keyValue)
                || !IsWithinRange(keyValue, typedLower, lowerInclusive, typedUpper, upperInclusive))
            {
                continue;
            }

            if (_enableLogging)
            {
                _logger?.LogBlobDownloadStarting(file);
            }

            var storageObject =
                await ExecuteWithRetryAsync(_ => _storageProvider.ReadWithMetadataAsync<TEntity?>(file));
            if (_enableLogging)
            {
                _logger?.LogBlobDownloadCompleted(file, 0, 0);
            }

            if (!storageObject.Exists || storageObject.Value is null)
            {
                continue;
            }

            AttachOrReplaceTrackedEntity(targetContext, storageObject.Value, storageObject.ETag, keyProperties);
            results.Add(storageObject.Value);
        }

        return results;
    }

    /// <summary>
    /// Loads a paged slice of entities whose primary keys are in the provided range.
    /// </summary>
    /// <typeparam name="TEntity">Entity type to load.</typeparam>
    /// <param name="lowerBound">Optional lower bound key value.</param>
    /// <param name="lowerInclusive"><see langword="true" /> to include the lower bound.</param>
    /// <param name="upperBound">Optional upper bound key value.</param>
    /// <param name="upperInclusive"><see langword="true" /> to include the upper bound.</param>
    /// <param name="skip">Number of matching entities to skip.</param>
    /// <param name="take">Maximum number of matching entities to load.</param>
    /// <param name="context">Optional context used for tracking; defaults to the current context.</param>
    /// <returns>A page of entities whose keys satisfy the requested range after applying skip/take.</returns>
    public async Task<IList<TEntity>> LoadByPrimaryKeyRangePageAsync<TEntity>(
        object? lowerBound,
        bool lowerInclusive,
        object? upperBound,
        bool upperInclusive,
        int skip,
        int take,
        DbContext? context = null)
        where TEntity : class
    {
        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skip), skip, "Skip must be greater than or equal to zero.");
        }

        if (take < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(take), take, "Take must be greater than or equal to zero.");
        }

        if (take == 0)
        {
            return [];
        }

        var targetContext = context ?? _context;
        var blobName = _blobPathResolver.GetBlobName(typeof(TEntity));
        var entityType = Model.FindEntityType(typeof(TEntity));
        var keyProperties = entityType?.FindPrimaryKey()?.Properties;
        var keyProperty = keyProperties?.FirstOrDefault();

        if (keyProperty is null)
        {
            return [];
        }

        var keyType = Nullable.GetUnderlyingType(keyProperty.ClrType) ?? keyProperty.ClrType;
        var typedLower = lowerBound is null ? null : ConvertToKeyType(lowerBound, keyType);
        var typedUpper = upperBound is null ? null : ConvertToKeyType(upperBound, keyType);

        var pagePaths = await SelectPathsPageAsync(
            blobName,
            path =>
            {
                var keyText = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(keyText)
                    || !TryParseStorageKey(keyText, keyType, out var keyValue))
                {
                    return false;
                }

                return IsWithinRange(keyValue, typedLower, lowerInclusive, typedUpper, upperInclusive);
            },
            skip,
            take);

        return await LoadEntitiesByPathsAsync<TEntity>(targetContext, pagePaths, keyProperties);
    }

    /// <summary>
    /// Loads all entities for the specified blob prefix and attaches them to the current context.
    /// </summary>
    /// <typeparam name="TEntity">Entity type to load.</typeparam>
    /// <param name="containerName">Blob prefix or folder name to enumerate.</param>
    /// <returns>A list containing all deserialized entities found under the prefix.</returns>
    /// <example>
    /// <code>
    /// var users = await database.ToListAsync&lt;User&gt;("users");
    /// </code>
    /// </example>
    public async Task<IList<TEntity>> ToListAsync<TEntity>(string containerName)
        where TEntity : class
    {
        var files = await ExecuteWithRetryAsync(_ => _storageProvider.ListAsync(containerName));
        var results = new List<TEntity>();

        var entityType = _context.Model.FindEntityType(typeof(TEntity));
        var keyProperties = entityType?.FindPrimaryKey()?.Properties;

        foreach (var file in files)
        {
            if (_enableLogging)
            {
                _logger?.LogBlobDownloadStarting(file);
            }

            var storageObject =
                await ExecuteWithRetryAsync(_ => _storageProvider.ReadWithMetadataAsync<TEntity?>(file));
            if (_enableLogging)
            {
                _logger?.LogBlobDownloadCompleted(file, 0, 0);
            }

            if (!storageObject.Exists || storageObject.Value is null)
            {
                continue;
            }

            AttachOrReplaceTrackedEntity(_context, storageObject.Value, storageObject.ETag, keyProperties);

            results.Add(storageObject.Value);
        }

        return results;
    }

    /// <summary>
    /// Compiles a query expression into an executable delegate.
    /// </summary>
    /// <typeparam name="TResult">Compiled query result type.</typeparam>
    /// <param name="query">Query expression to compile.</param>
    /// <param name="async">Whether query compilation targets async execution.</param>
    /// <param name="nonNullableReferenceTypeParameters">Set of non-nullable parameter names.</param>
    /// <returns>A compiled query delegate.</returns>
    /// <exception cref="NotImplementedException">Always thrown in the current implementation.</exception>
    public Expression<Func<QueryContext, TResult>> CompileQueryExpression<TResult>(Expression query, bool async,
        IReadOnlySet<string> nonNullableReferenceTypeParameters)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Compiles a query expression into an executable delegate.
    /// </summary>
    /// <typeparam name="TResult">Compiled query result type.</typeparam>
    /// <param name="query">Query expression to compile.</param>
    /// <param name="async">Whether query compilation targets async execution.</param>
    /// <returns>A compiled query delegate.</returns>
    /// <exception cref="NotImplementedException">Always thrown in the current implementation.</exception>
    public Expression<Func<QueryContext, TResult>> CompileQueryExpression<TResult>(Expression query, bool async)
    {
        throw new NotImplementedException();
    }

    private void AttachOrReplaceTrackedEntity<TEntity>(DbContext? context, TEntity? entity, string? eTag,
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
        ApplyTrackedETag(context, entity, eTag);
    }

    private static string? GetOriginalETag(IUpdateEntry entry)
    {
        if (!TryGetConcurrencyProperty(entry.EntityType, out var property))
        {
            return null;
        }

        var internalEntry = (InternalEntityEntry)entry;
        var originalValue = internalEntry.GetOriginalValue(property!);
        return originalValue?.ToString();
    }

    private static void ApplySavedETag(IUpdateEntry entry, string? eTag)
    {
        if (string.IsNullOrWhiteSpace(eTag) || !TryGetConcurrencyProperty(entry.EntityType, out var property))
        {
            return;
        }

        var entity = ((InternalEntityEntry)entry).Entity;
        var dbEntry = entry.Context.Entry(entity);
        var propertyEntry = dbEntry.Property(property!.Name);
        propertyEntry.CurrentValue = eTag;
        propertyEntry.OriginalValue = eTag;
        propertyEntry.IsModified = false;

        if (entity is IETag eTagEntity)
        {
            eTagEntity.ETag = eTag;
        }
    }

    private static void ApplyTrackedETag<TEntity>(DbContext context, TEntity entity, string? eTag)
        where TEntity : class
    {
        if (!TryGetConcurrencyProperty(context.Model.FindEntityType(typeof(TEntity)), out var property))
        {
            return;
        }

        var propertyEntry = context.Entry(entity).Property(property!.Name);
        propertyEntry.CurrentValue = eTag;
        propertyEntry.OriginalValue = eTag;
        propertyEntry.IsModified = false;

        if (entity is IETag eTagEntity)
        {
            eTagEntity.ETag = eTag;
        }
    }

    private static bool TryGetConcurrencyProperty(IReadOnlyTypeBase? entityType, out IProperty? property)
    {
        property = null!;
        if (entityType is not IReadOnlyEntityType readOnlyEntityType)
        {
            return false;
        }

        var enabled =
            readOnlyEntityType.FindAnnotation(AnnotationsConstants.ETagConcurrencyEnabledAnnotation)?.Value as bool?;
        if (enabled != true)
        {
            return false;
        }

        var propertyName = readOnlyEntityType.FindAnnotation(AnnotationsConstants.ETagConcurrencyPropertyNameAnnotation)
            ?.Value as string;
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return false;
        }

        property = (IProperty?)readOnlyEntityType.FindProperty(propertyName);
        return property is not null;
    }

    private static DbUpdateConcurrencyException CreateConcurrencyException(string path, Exception innerException)
    {
        return new DbUpdateConcurrencyException(
            $"The operation expected the object ETag to match for '{path}', but it was updated by another process.",
            innerException);
    }

    private static object ConvertToKeyType(object value, Type keyType)
    {
        if (keyType.IsInstanceOfType(value))
        {
            return value;
        }

        if (keyType == typeof(Guid))
        {
            return value is Guid guid ? guid : Guid.Parse(value.ToString() ?? string.Empty);
        }

        if (keyType.IsEnum)
        {
            return Enum.Parse(keyType, value.ToString() ?? string.Empty, ignoreCase: true);
        }

        if (keyType == typeof(string))
        {
            return value.ToString() ?? string.Empty;
        }

        return Convert.ChangeType(value, keyType, CultureInfo.InvariantCulture)
               ?? throw new InvalidOperationException($"Could not convert key value to '{keyType.Name}'.");
    }

    private static bool TryParseStorageKey(string rawValue, Type keyType, out object parsed)
    {
        try
        {
            parsed = ConvertToKeyType(rawValue, keyType);
            return true;
        }
        catch
        {
            parsed = null!;
            return false;
        }
    }

    private static bool IsWithinRange(object keyValue, object? lowerBound, bool lowerInclusive, object? upperBound,
        bool upperInclusive)
    {
        if (lowerBound is not null)
        {
            var cmp = CompareKeyValues(keyValue, lowerBound);
            if (lowerInclusive ? cmp < 0 : cmp <= 0)
            {
                return false;
            }
        }

        if (upperBound is null)
        {
            return true;
        }

        {
            var cmp = CompareKeyValues(keyValue, upperBound);
            if (upperInclusive ? cmp > 0 : cmp >= 0)
            {
                return false;
            }
        }

        return true;
    }

    private static int CompareKeyValues(object left, object right)
    {
        return left is not IComparable comparable
            ? throw new InvalidOperationException("Primary key type must implement IComparable for range filtering.")
            : comparable.CompareTo(right);
    }

    private async Task<List<string>> SelectPathsPageAsync(
        string prefix,
        Func<string, bool> include,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 1000;
        var results = new List<string>(Math.Max(4, take));
        var remainingSkip = skip;
        string? continuationToken = null;

        while (results.Count < take)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = await ExecuteWithRetryAsync(token =>
                _storageProvider.ListPageAsync(prefix, pageSize, continuationToken, token), cancellationToken);

            if (page.Keys.Count == 0 && !page.HasMore)
            {
                break;
            }

            foreach (var key in page.Keys)
            {
                if (!include(key))
                {
                    continue;
                }

                if (remainingSkip > 0)
                {
                    remainingSkip--;
                    continue;
                }

                results.Add(key);
                if (results.Count >= take)
                {
                    break;
                }
            }

            if (!page.HasMore || string.IsNullOrWhiteSpace(page.ContinuationToken))
            {
                break;
            }

            continuationToken = page.ContinuationToken;
        }

        return results;
    }

    private async Task<IList<TEntity>> LoadEntitiesByPathsAsync<TEntity>(
        DbContext targetContext,
        IReadOnlyList<string> paths,
        IReadOnlyList<IProperty>? keyProperties = null)
        where TEntity : class
    {
        var results = new List<TEntity>(paths.Count);

        foreach (var path in paths)
        {
            if (_enableLogging)
            {
                _logger?.LogBlobDownloadStarting(path);
            }

            var storageObject =
                await ExecuteWithRetryAsync(_ => _storageProvider.ReadWithMetadataAsync<TEntity?>(path));
            if (_enableLogging)
            {
                _logger?.LogBlobDownloadCompleted(path, 0, 0);
            }

            if (!storageObject.Exists || storageObject.Value is null)
            {
                continue;
            }

            AttachOrReplaceTrackedEntity(targetContext, storageObject.Value, storageObject.ETag, keyProperties);
            results.Add(storageObject.Value);
        }

        return results;
    }
}