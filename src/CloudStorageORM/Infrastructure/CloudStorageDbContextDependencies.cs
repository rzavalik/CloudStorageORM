using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// Provides EF Core DbContext dependency implementations for CloudStorageORM.
/// </summary>
public class CloudStorageDbContextDependencies : IDbContextDependencies
{
    private readonly EntityFinderFactory _entityFinderFactory;

    /// <summary>
    /// Initializes a new dependency container used by CloudStorageORM DbContext services.
    /// </summary>
    /// <param name="model">Resolved EF model.</param>
    /// <param name="currentContext">Current DbContext accessor.</param>
    /// <param name="changeDetector">EF change detector.</param>
    /// <param name="setSource">EF DbSet source.</param>
    /// <param name="entityFinderSource">EF entity finder source.</param>
    /// <param name="entityGraphAttacher">EF entity graph attacher.</param>
    /// <param name="queryProvider">Async query provider.</param>
    /// <param name="stateManager">EF state manager.</param>
    /// <param name="exceptionDetector">EF exception detector.</param>
    /// <param name="transactionManager">Transaction manager used by the DbContext infrastructure.</param>
    /// <param name="updateLogger">Update diagnostics logger.</param>
    /// <param name="infrastructureLogger">Infrastructure diagnostics logger.</param>
    /// <example>
    /// <code>
    /// var dependencies = new CloudStorageDbContextDependencies(...);
    /// </code>
    /// </example>
    public CloudStorageDbContextDependencies(
        IModel model,
        ICurrentDbContext currentContext,
        IChangeDetector changeDetector,
        IDbSetSource setSource,
        IEntityFinderSource entityFinderSource,
        IEntityGraphAttacher entityGraphAttacher,
        IAsyncQueryProvider queryProvider,
        IStateManager stateManager,
        IExceptionDetector exceptionDetector,
        IDbContextTransactionManager transactionManager,
        IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger,
        IDiagnosticsLogger<DbLoggerCategory.Infrastructure> infrastructureLogger)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        if (currentContext == null)
        {
            throw new ArgumentNullException(nameof(currentContext));
        }

        ChangeDetector = changeDetector ?? throw new ArgumentNullException(nameof(changeDetector));
        SetSource = setSource ?? throw new ArgumentNullException(nameof(setSource));
        if (entityFinderSource == null)
        {
            throw new ArgumentNullException(nameof(entityFinderSource));
        }

        EntityGraphAttacher = entityGraphAttacher ?? throw new ArgumentNullException(nameof(entityGraphAttacher));
        QueryProvider = queryProvider ?? throw new ArgumentNullException(nameof(queryProvider));
        StateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        ExceptionDetector = exceptionDetector ?? throw new ArgumentNullException(nameof(exceptionDetector));
        TransactionManager = transactionManager ?? throw new ArgumentNullException(nameof(transactionManager));
        UpdateLogger = updateLogger ?? throw new ArgumentNullException(nameof(updateLogger));
        InfrastructureLogger = infrastructureLogger ?? throw new ArgumentNullException(nameof(infrastructureLogger));

        _entityFinderFactory = new EntityFinderFactory(
            entityFinderSource,
            stateManager,
            setSource,
            currentContext.Context
        );
    }

    public IDbContextTransactionManager TransactionManager { get; }

    public IModel Model { get; }

    public IModel DesignTimeModel => Model; // Returning the same model for design-time usage
    public DbContextOptions ContextOptions => throw new NotImplementedException();
    public IServiceProvider InternalServiceProvider => throw new NotImplementedException();

    public IDbSetSource SetSource { get; }

    public IEntityFinderFactory EntityFinderFactory => _entityFinderFactory;

    public IAsyncQueryProvider QueryProvider { get; }

    public IStateManager StateManager { get; }

    public IChangeDetector ChangeDetector { get; }

    public IEntityGraphAttacher EntityGraphAttacher { get; }

    public IExceptionDetector ExceptionDetector { get; }

    public IDiagnosticsLogger<DbLoggerCategory.Update> UpdateLogger { get; }

    public IDiagnosticsLogger<DbLoggerCategory.Infrastructure> InfrastructureLogger { get; }
}