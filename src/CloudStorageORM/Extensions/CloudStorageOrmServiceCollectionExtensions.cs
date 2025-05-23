﻿namespace CloudStorageORM.Extensions
{
    using CloudStorageORM.Infrastructure;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using CloudStorageORM.Providers;
    using global::Azure.Storage.Blobs;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
    using Microsoft.EntityFrameworkCore.Query;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.EntityFrameworkCore.Storage.Internal;
    using Microsoft.Extensions.DependencyInjection;

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
                .TryAdd<ISingletonOptionsInitializer, CloudStorageSingletonOptionsInitializer>()
                .TryAdd<IProviderConventionSetBuilder, RelationalConventionSetBuilder>()
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
                return ProviderFactory.GetStorageProvider(storageOptions);
            });

            return services;
        }
    }
}
