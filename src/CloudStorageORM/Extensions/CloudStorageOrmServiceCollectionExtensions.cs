namespace CloudStorageORM.Extensions
{
    using global::Azure.Storage.Blobs;
    using CloudStorageORM.Enums;
    using CloudStorageORM.Infrastructure;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
    using Microsoft.EntityFrameworkCore.Query;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.EntityFrameworkCore.Storage.Internal;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using CloudStorageORM.Azure.StorageProviders;

    public static class CloudStorageOrmServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkCloudStorageORM(
            this IServiceCollection services,
            CloudStorageOptions storageOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (storageOptions == null) throw new ArgumentNullException(nameof(storageOptions));

            var builder = new EntityFrameworkServicesBuilder(services)
                .TryAddCoreServices()
                .TryAdd<IDatabaseProvider, CloudStorageDatabaseProvider>()
                .TryAdd<IDatabase, CloudStorageDatabase>()
                .TryAdd<IDatabaseCreator, CloudStorageDatabaseCreator>()
                .TryAdd<IDbSetInitializer, DbSetInitializer>()
                .TryAdd<ITypeMappingSource, CloudStorageTypeMappingSource>()
                .TryAdd<IQueryContextFactory, CloudStorageQueryContextFactory>()
                .TryAdd<IExecutionStrategyFactory, ExecutionStrategyFactory>()
                .TryAdd<IModelSource, ModelSource>()
                .TryAdd<LoggingDefinitions, CloudStorageLoggingDefinitions>();

            services.AddSingleton(storageOptions);
            services.AddSingleton(provider =>
            {
                if (string.IsNullOrEmpty(storageOptions.ConnectionString))
                {
                    throw new InvalidOperationException("CloudStorageOptions.ConnectionString must be provided.");
                }

                return new BlobServiceClient(storageOptions.ConnectionString);
            });
            services.AddSingleton<IStorageProvider>(provider =>
            {
                return storageOptions.Provider switch
                {
                    CloudProvider.Azure => new AzureBlobStorageProvider(storageOptions),
                    _ => throw new NotSupportedException($"Cloud provider {storageOptions.Provider} is not supported yet.")
                };
            });
            services.AddSingleton<ISingletonOptionsInitializer, CloudStorageSingletonOptionsInitializer>();
            services.TryAddSingleton<IProviderConventionSetBuilder, RelationalConventionSetBuilder>();

            return services;
        }
    }
}
