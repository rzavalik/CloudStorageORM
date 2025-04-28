namespace CloudStorageORM.Extensions
{
    using CloudStorageORM.Enums;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using CloudStorageORM.StorageProviders;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;

    public static class CloudStorageOrmExtensions
    {
        public static DbContextOptionsBuilder<TContext> UseCloudStorageORM<TContext>(
            this DbContextOptionsBuilder<TContext> builder,
            Action<CloudStorageOptions> configureOptions)
            where TContext : DbContext
        {
            var options = new CloudStorageOptions();
            configureOptions.Invoke(options);

            IStorageProvider storageProvider = options.Provider switch
            {
                CloudProvider.Azure => new AzureBlobStorageProvider(options),
                _ => throw new NotSupportedException($"Cloud provider {options.Provider} is not supported yet.")
            };

            builder.WithOptionExtension(new CloudStorageOrmOptionsExtension(storageProvider, options));
            return builder;
        }

        public static DbContextOptionsBuilder UseCloudStorageORM(
            this DbContextOptionsBuilder builder,
            Action<CloudStorageOptions> configureOptions)
        {
            var options = new CloudStorageOptions();
            configureOptions.Invoke(options);

            IStorageProvider storageProvider = options.Provider switch
            {
                CloudProvider.Azure => new AzureBlobStorageProvider(options),
                _ => throw new NotSupportedException($"Cloud provider {options.Provider} is not supported yet.")
            };

            builder.WithOptionExtension(new CloudStorageOrmOptionsExtension(storageProvider, options));
            return builder;
        }

        private static DbContextOptionsBuilder WithOptionExtension(
            this DbContextOptionsBuilder builder,
            IDbContextOptionsExtension extension)
        {
            var infrastructure = (IDbContextOptionsBuilderInfrastructure)builder;
            infrastructure.AddOrUpdateExtension(extension);
            return builder;
        }
    }
}
