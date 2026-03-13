namespace CloudStorageORM.Extensions
{
    using Infrastructure;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Options;

    public static class CloudStorageOrmExtensions
    {
        public static DbContextOptionsBuilder<TContext> UseCloudStorageOrm<TContext>(
            this DbContextOptionsBuilder<TContext> builder,
            Action<CloudStorageOptions> configureOptions)
            where TContext : DbContext
        {
            return (DbContextOptionsBuilder<TContext>)((DbContextOptionsBuilder)builder).UseCloudStorageOrm(
                configureOptions);
        }

        public static DbContextOptionsBuilder UseCloudStorageOrm(
            this DbContextOptionsBuilder builder,
            Action<CloudStorageOptions> configureOptions)
        {
            var options = new CloudStorageOptions();
            configureOptions.Invoke(options);

            var extension = builder.Options.FindExtension<CloudStorageOrmOptionsExtension>()
                            ?? new CloudStorageOrmOptionsExtension(options);

            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);

            return builder;
        }
    }
}