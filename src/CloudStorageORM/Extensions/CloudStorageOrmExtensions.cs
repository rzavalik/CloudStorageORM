using CloudStorageORM.Infrastructure;
using CloudStorageORM.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CloudStorageORM.Extensions;

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
        Action<CloudStorageOptions>? configureOptions)
    {
        var options = new CloudStorageOptions();
        configureOptions?.Invoke(options);

        var extension = builder.Options.FindExtension<CloudStorageOrmOptionsExtension>()
                        ?? new CloudStorageOrmOptionsExtension(options);

        ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);

        return builder;
    }
}