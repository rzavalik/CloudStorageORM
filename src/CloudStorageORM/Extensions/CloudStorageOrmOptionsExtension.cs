namespace CloudStorageORM.Infrastructure
{
    using CloudStorageORM.Azure.StorageProviders;
    using CloudStorageORM.Enums;
    using CloudStorageORM.Extensions;
    using CloudStorageORM.Interfaces.Infrastructure;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using global::Azure.Storage.Blobs;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.EntityFrameworkCore.Query;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public class CloudStorageOrmOptionsExtension : IDbContextOptionsExtension
    {
        private DbContextOptionsExtensionInfo _info;
        private readonly CloudStorageOptions _options;

        public CloudStorageOrmOptionsExtension(CloudStorageOptions options)
        {
            _options = options;
            _info = new CloudStorageOrmOptionsExtensionInfo(this);
        }

        public CloudStorageOptions Options => _options;
        public DbContextOptionsExtensionInfo Info => _info;

        public void ApplyServices(IServiceCollection services)
        {
            services.AddSingleton(_options);

            var builder = new EntityFrameworkServicesBuilder(services)
                .TryAddCoreServices();

            services.AddSingleton<IStorageProvider>(provider =>
            {
                return _options.Provider switch
                {
                    CloudProvider.Azure => new AzureBlobStorageProvider(_options),
                    _ => throw new NotSupportedException($"Provider {_options.Provider} not supported.")
                };
            });

            services.TryAddSingleton<BlobServiceClient>(provider =>
            {
                if (string.IsNullOrEmpty(_options.ConnectionString))
                {
                    throw new InvalidOperationException("CloudStorageOptions.ConnectionString must be provided.");
                }

                return new BlobServiceClient(_options.ConnectionString);
            });

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

            services.AddEntityFrameworkCloudStorageORM(_options);
        }

        public void Validate(IDbContextOptions options)
        {
        }
    }
}