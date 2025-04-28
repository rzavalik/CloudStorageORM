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
            _changeDetector = changeDetector;
            _setSource = setSource;
            _entityGraphAttacher = entityGraphAttacher;
            _queryProvider = queryProvider;
            _stateManager = stateManager;
            _exceptionDetector = exceptionDetector;
            _updateLogger = updateLogger;
            _infrastructureLogger = infrastructureLogger;
            _entityFinderFactory = new EntityFinderFactory(entityFinderSource, stateManager, setSource, currentContext.Context);
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
