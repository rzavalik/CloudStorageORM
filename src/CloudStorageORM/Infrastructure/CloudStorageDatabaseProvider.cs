namespace CloudStorageORM.Infrastructure
{
    using global::Azure.Storage.Blobs;
    using CloudStorageORM.Options;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.DependencyInjection;

    public class CloudStorageDatabaseProvider : IDatabaseProvider
    {
        public string Name => "CloudStorageORM.Provider";

        public bool IsConfigured(IDbContextOptions options)
             => options.FindExtension<CloudStorageOrmOptionsExtension>() != null;

        public IDatabase Create(IDatabaseFacadeDependencies dependencies)
        {
            var databaseCreator = dependencies.DatabaseCreator;
            var executionStrategyFactory = dependencies.ExecutionStrategyFactory;
            var concurrencyDetector = dependencies.ConcurrencyDetector;

            var serviceProvider = ((IInfrastructure<IServiceProvider>)dependencies).Instance;

            var blobServiceClient = serviceProvider.GetRequiredService<BlobServiceClient>();
            var cloudOptions = serviceProvider.GetRequiredService<CloudStorageOptions>();
            var model = serviceProvider.GetRequiredService<IModel>();

            return new CloudStorageDatabase(
                model, 
                databaseCreator, 
                executionStrategyFactory, 
                blobServiceClient, 
                cloudOptions);
        }
    }
}