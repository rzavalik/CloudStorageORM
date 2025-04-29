namespace CloudStorageORM.Infrastructure
{
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;
    using CloudStorageORM.Options;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Azure.StorageProviders;
    using CloudStorageORM.Enums;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.EntityFrameworkCore.Query;
    using global::Azure.Storage.Blobs;

    public class CloudStorageOrmOptionsExtension : IDbContextOptionsExtension
    {
        private DbContextOptionsExtensionInfo _info;
        private readonly CloudStorageOptions _options;

        public CloudStorageOrmOptionsExtension(CloudStorageOptions options)
        {
            _options = options;
            _info = new ExtensionInfo(this);
        }

        public CloudStorageOptions Options => _options;
        public DbContextOptionsExtensionInfo Info => _info;

        public void ApplyServices(IServiceCollection services)
        {
            var builder = new EntityFrameworkServicesBuilder(services)
                .TryAddCoreServices();

            services.AddSingleton(_options);

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
                var options = provider.GetRequiredService<CloudStorageOptions>();

                if (string.IsNullOrEmpty(options.ConnectionString))
                {
                    throw new InvalidOperationException("CloudStorageOptions.ConnectionString must be provided.");
                }

                return new BlobServiceClient(options.ConnectionString);
            });

            services.TryAddScoped<IDatabase, CloudStorageDatabase>();
            services.TryAddScoped<LoggingDefinitions, CloudStorageLoggingDefinitions>(); 
            services.TryAddScoped<IQueryContextFactory, CloudStorageQueryContextFactory>();
            services.AddSingleton<ITypeMappingSource, CloudStorageTypeMappingSource>();
            services.TryAddSingleton<IDatabaseProvider, CloudStorageDatabaseProvider>();
            services.TryAddSingleton<IDbContextTransactionManager, CloudStorageTransactionManager>();
            services.TryAddSingleton<IDatabaseCreator, CloudStorageDatabaseCreator>();
            services.TryAddSingleton<IModelSource, ModelSource>();
            services.TryAddSingleton<IModelRuntimeInitializer, ModelRuntimeInitializer>();
            services.TryAddSingleton<IDbSetInitializer, DbSetInitializer>();
            services.TryAddSingleton<ISingletonOptionsInitializer, CloudStorageSingletonOptionsInitializer>();
        }

        public void Validate(IDbContextOptions options) { }

        private class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            public ExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension) { }

            public override bool IsDatabaseProvider => true;
            public override string LogFragment => "using CloudStorageORM";
            public override int GetServiceProviderHashCode() => 0;
            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) { }
            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;
        }
    }
}