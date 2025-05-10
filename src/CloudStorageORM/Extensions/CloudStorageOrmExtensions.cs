namespace CloudStorageORM.Extensions
{
    using CloudStorageORM.Infrastructure;
    using CloudStorageORM.Options;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;

    public static class CloudStorageOrmExtensions
    {
        public static DbContextOptionsBuilder<TContext> UseCloudStorageORM<TContext>(
            this DbContextOptionsBuilder<TContext> builder,
            Action<CloudStorageOptions> configureOptions)
            where TContext : DbContext
        {
            return (DbContextOptionsBuilder<TContext>)UseCloudStorageORM((DbContextOptionsBuilder)builder, configureOptions);
        }

        public static DbContextOptionsBuilder UseCloudStorageORM(
            this DbContextOptionsBuilder builder,
            Action<CloudStorageOptions> configureOptions)
        {
            var options = new CloudStorageOptions();
            configureOptions?.Invoke(options);

            var extension = builder
                .Options
                .FindExtension<CloudStorageOrmOptionsExtension>()
                ?? new CloudStorageOrmOptionsExtension(options);

            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);

            return builder;
        }
    }
}