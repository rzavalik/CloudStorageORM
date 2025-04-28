namespace CloudStorageORM.Options
{
    using CloudStorageORM.Extensions;
    using CloudStorageORM.Interfaces.StorageProviders;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;

    public class CloudStorageOrmOptionsExtension : IDbContextOptionsExtension
    {
        private DbContextOptionsExtensionInfo? _info;

        public IStorageProvider StorageProvider { get; }
        public CloudStorageOptions Options { get; }

        public CloudStorageOrmOptionsExtension(IStorageProvider storageProvider, CloudStorageOptions options)
        {
            StorageProvider = storageProvider;
            Options = options;
        }

        public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

        public void ApplyServices(IServiceCollection services)
        {
            services.AddEntityFrameworkCloudStorageORM();
        }

        public void Validate(IDbContextOptions options) { }

        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension) { }

            private new CloudStorageOrmOptionsExtension Extension => (CloudStorageOrmOptionsExtension)base.Extension;

            public override bool IsDatabaseProvider => true;

            public override string LogFragment => "using CloudStorageORM ";

            public override int GetServiceProviderHashCode() => Extension.StorageProvider.GetHashCode();

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                debugInfo["CloudStorageORM:Provider"] = Extension.StorageProvider.GetType().Name;
            }

            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            {
                if (other is not ExtensionInfo otherExtension)
                {
                    return false;
                }

                return Equals(Extension.StorageProvider, otherExtension.Extension.StorageProvider) &&
                       Equals(Extension.Options, otherExtension.Extension.Options);
            }
        }
    }
}
