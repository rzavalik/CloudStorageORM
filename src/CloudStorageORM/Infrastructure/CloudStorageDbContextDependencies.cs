namespace CloudStorageORM.Infrastructure
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.EntityFrameworkCore.Metadata;
    using System;
    using Microsoft.EntityFrameworkCore.Query;
    using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.EntityFrameworkCore.Infrastructure;

    public class CloudStorageDbContextDependencies : IDbContextDependencies
    {
        private readonly IModel _model;
        private IChangeDetector _changeDetector;
        private IDbSetSource _setSource;
        private IEntityGraphAttacher _entityGraphAttacher;
        private IAsyncQueryProvider _queryProvider;
        private IStateManager _stateManager;
        private IExceptionDetector _exceptionDetector;
        private IDiagnosticsLogger<DbLoggerCategory.Update> _updateLogger;
        private IDiagnosticsLogger<DbLoggerCategory.Infrastructure> _infrastructureLogger;
        private EntityFinderFactory _entityFinderFactory;

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
            if (currentContext == null) throw new ArgumentNullException(nameof(currentContext));
            _changeDetector = changeDetector ?? throw new ArgumentNullException(nameof(changeDetector));
            _setSource = setSource ?? throw new ArgumentNullException(nameof(setSource));
            if (entityFinderSource == null) throw new ArgumentNullException(nameof(entityFinderSource));
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
        public IModel DesignTimeModel => _model;  // Returning the same model for design-time usage
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
}
