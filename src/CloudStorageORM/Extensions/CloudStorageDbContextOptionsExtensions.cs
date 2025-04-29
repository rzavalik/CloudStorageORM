namespace CloudStorageORM.Extensions
{
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using CloudStorageORM.Infrastructure;
    using global::Azure.Storage.Blobs;
    using CloudStorageORM.Options;
    using Microsoft.EntityFrameworkCore.Internal;

    internal class CloudStorageOptionsExtension : IDbContextOptionsExtension
    {
        private Action<CloudStorageOptions>? _configureOptions;

        public Action<CloudStorageOptions>? ConfigureOptions
        {
            get => _configureOptions;
            set => _configureOptions = value;
        }

        public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

        public void ApplyServices(IServiceCollection services)
        {
            services.TryAddSingleton<CloudStorageOptions>();

            services.TryAddSingleton<ITypeMappingSource, CloudStorageTypeMappingSource>();
            services.TryAddSingleton<ISingletonOptionsInitializer, CloudStorageSingletonOptionsInitializer>();

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
        }

        public void Validate(IDbContextOptions options)
        {
            // Optional: Add validation if needed
        }

        private class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension) { }

            public override bool IsDatabaseProvider => true;
            public override string LogFragment => "using CloudStorageORM ";
            public override int GetServiceProviderHashCode() => 0;

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                debugInfo["CloudStorageORM"] = "1";
            }

            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => true;
        }
    }
}
