namespace CloudStorageORM.Infrastructure
{
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.DependencyInjection;

    public class CloudStorageDatabaseProvider : IDatabaseProvider
    {
        public string Name => "CloudStorageORM.Provider";

        public bool IsConfigured(IDbContextOptions options)
             => true;

        public IDatabase Create(IDatabaseFacadeDependencies dependencies)
        {
            var databaseCreator = dependencies.DatabaseCreator;
            var executionStrategyFactory = dependencies.ExecutionStrategyFactory;
            var concurrencyDetector = dependencies.ConcurrencyDetector;

            var serviceProvider = ((IInfrastructure<IServiceProvider>)dependencies).Instance;

            var storageProvider = serviceProvider.GetRequiredService<IStorageProvider>();
            var cloudOptions = serviceProvider.GetRequiredService<CloudStorageOptions>();
            var model = serviceProvider.GetRequiredService<IModel>();
            var currentDbContext = serviceProvider.GetRequiredService<ICurrentDbContext>();

            return new CloudStorageDatabase(
                model,
                databaseCreator,
                executionStrategyFactory,
                storageProvider,
                cloudOptions,
                currentDbContext);
        }
    }
}