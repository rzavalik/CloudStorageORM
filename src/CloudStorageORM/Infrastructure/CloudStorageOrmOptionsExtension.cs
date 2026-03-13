namespace CloudStorageORM.Infrastructure
{
    using Azure.Storage.Blobs;
    using Extensions;
    using Interfaces.Infrastructure;
    using Interfaces.StorageProviders;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.EntityFrameworkCore.Query;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Options;
    using Providers;

    public class CloudStorageOrmOptionsExtension : IDbContextOptionsExtension
    {
        public CloudStorageOrmOptionsExtension(CloudStorageOptions options)
        {
            Options = options;
            Info = new CloudStorageOrmOptionsExtensionInfo(this);
        }

        public CloudStorageOptions Options { get; }

        public DbContextOptionsExtensionInfo Info { get; }

        public void ApplyServices(IServiceCollection services)
        {
            services.AddSingleton(Options);

            var builder = new EntityFrameworkServicesBuilder(services)
                .TryAddCoreServices();

            services.AddSingleton<IStorageProvider>(_ => ProviderFactory.GetStorageProvider(Options));

            services.TryAddSingleton<BlobServiceClient>(_ => string.IsNullOrEmpty(Options.ConnectionString)
                ? throw new InvalidOperationException("CloudStorageOptions.ConnectionString must be provided.")
                : new BlobServiceClient(Options.ConnectionString));

            services.AddScoped<IDatabase, CloudStorageDatabase>();
            services.AddScoped<LoggingDefinitions, CloudStorageLoggingDefinitions>();
            services.AddScoped<IQueryContextFactory, CloudStorageQueryContextFactory>();

            services.AddSingleton<IBlobPathResolver, BlobPathResolver>();
            services.AddSingleton<ITypeMappingSource, CloudStorageTypeMappingSource>();
            services.AddSingleton<IDbContextTransactionManager, CloudStorageTransactionManager>();
            services.AddSingleton<IDatabaseCreator, CloudStorageDatabaseCreator>();
            services.AddSingleton<IModelSource, ModelSource>();
            services.AddSingleton<IModelRuntimeInitializer, ModelRuntimeInitializer>();
            services.AddSingleton<IDbSetInitializer, DbSetInitializer>();
            services.AddSingleton<ISingletonOptionsInitializer, CloudStorageSingletonOptionsInitializer>();
            services.AddSingleton<IDatabaseProvider, CloudStorageDatabaseProvider>();

            services.AddEntityFrameworkCloudStorageOrm(Options);
        }

        public void Validate(IDbContextOptions options)
        {
        }
    }
}