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
    private readonly IModel _model;
    private readonly IChangeDetector _changeDetector;
    private readonly IDbSetSource _setSource;
    private readonly IEntityGraphAttacher _entityGraphAttacher;
    private readonly IAsyncQueryProvider _queryProvider;
    private readonly IStateManager _stateManager;
    private readonly IExceptionDetector _exceptionDetector;
    private readonly IDiagnosticsLogger<DbLoggerCategory.Update> _updateLogger;
    private readonly IDiagnosticsLogger<DbLoggerCategory.Infrastructure> _infrastructureLogger;
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
        IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger,
        IDiagnosticsLogger<DbLoggerCategory.Infrastructure> infrastructureLogger)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        if (currentContext == null)
        {
            throw new ArgumentNullException(nameof(currentContext));
        }

        _changeDetector = changeDetector ?? throw new ArgumentNullException(nameof(changeDetector));
        _setSource = setSource ?? throw new ArgumentNullException(nameof(setSource));
        if (entityFinderSource == null)
        {
            throw new ArgumentNullException(nameof(entityFinderSource));
        }

        _entityGraphAttacher = entityGraphAttacher ?? throw new ArgumentNullException(nameof(entityGraphAttacher));
        _queryProvider = queryProvider ?? throw new ArgumentNullException(nameof(queryProvider));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _exceptionDetector = exceptionDetector ?? throw new ArgumentNullException(nameof(exceptionDetector));
        _updateLogger = updateLogger ?? throw new ArgumentNullException(nameof(updateLogger));
        _infrastructureLogger = infrastructureLogger ?? throw new ArgumentNullException(nameof(infrastructureLogger));

        _entityFinderFactory = new EntityFinderFactory(
            entityFinderSource,
            stateManager,
            setSource,
            currentContext.Context
        );
    }

    public IDbContextTransactionManager TransactionManager => new CloudStorageTransactionManager();
    public IModel Model => _model;
    public IModel DesignTimeModel => _model; // Returning the same model for design-time usage
    public DbContextOptions ContextOptions => throw new NotImplementedException();
    public IServiceProvider InternalServiceProvider => throw new NotImplementedException();

    public IDbSetSource SetSource => _setSource;

    public IEntityFinderFactory EntityFinderFactory => _entityFinderFactory;

    public IAsyncQueryProvider QueryProvider => _queryProvider;

    public IStateManager StateManager => _stateManager;

    public IChangeDetector ChangeDetector => _changeDetector;

    public IEntityGraphAttacher EntityGraphAttacher => _entityGraphAttacher;

    public IExceptionDetector ExceptionDetector => _exceptionDetector;

    public IDiagnosticsLogger<DbLoggerCategory.Update> UpdateLogger => _updateLogger;

    public IDiagnosticsLogger<DbLoggerCategory.Infrastructure> InfrastructureLogger => _infrastructureLogger;
}