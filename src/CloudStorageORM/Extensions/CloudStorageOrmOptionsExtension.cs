namespace CloudStorageORM.Infrastructure
{
    using CloudStorageORM.Extensions;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using CloudStorageORM.Providers;
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
                return ProviderFactory.GetStorageProvider(_options);
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
            services.AddScoped<CloudStorageDatabase>();
            services.AddScoped<IModelSource, ModelSource>();
            services.AddScoped<IModelRuntimeInitializer, ModelRuntimeInitializer>();
            services.AddScoped<IDbSetInitializer, DbSetInitializer>();

            services.AddSingleton<ITypeMappingSource, CloudStorageTypeMappingSource>();
            services.AddSingleton<IDbContextTransactionManager, CloudStorageTransactionManager>();
            services.AddSingleton<IDatabaseCreator, CloudStorageDatabaseCreator>();
            services.AddSingleton<ISingletonOptionsInitializer, CloudStorageSingletonOptionsInitializer>();
            services.AddSingleton<IDatabaseProvider, CloudStorageDatabaseProvider>();

            services.Replace(ServiceDescriptor.Singleton<IDbSetSource, CloudStorageDbSetSource>());

            services.AddEntityFrameworkCloudStorageORM(_options);
        }

        public void Validate(IDbContextOptions options)
        {
        }
    }
}