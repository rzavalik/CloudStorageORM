using CloudStorageORM.Infrastructure;
using CloudStorageORM.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CloudStorageORM.Extensions;

/// <summary>
/// Extension methods for configuring CloudStorageORM on EF Core option builders.
/// </summary>
public static class CloudStorageOrmExtensions
{
    /// <summary>
    /// Configures CloudStorageORM for a typed DbContext options builder.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type being configured.</typeparam>
    /// <param name="builder">The typed options builder.</param>
    /// <param name="configureOptions">Action used to configure <see cref="CloudStorageOptions" />.</param>
    /// <returns>The same typed builder instance for chaining.</returns>
    /// <example>
    /// <code>
    /// optionsBuilder.UseCloudStorageOrm&lt;MyDbContext&gt;(o =&gt;
    /// {
    ///     o.Provider = CloudStorageORM.Enums.CloudProvider.Azure;
    ///     o.ContainerName = "app-data";
    ///     o.Azure.ConnectionString = "UseDevelopmentStorage=true";
    /// });
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<TContext> UseCloudStorageOrm<TContext>(
        this DbContextOptionsBuilder<TContext> builder,
        Action<CloudStorageOptions> configureOptions)
        where TContext : DbContext
    {
        return (DbContextOptionsBuilder<TContext>)((DbContextOptionsBuilder)builder).UseCloudStorageOrm(
            configureOptions);
    }

    /// <summary>
    /// Configures CloudStorageORM for an untyped DbContext options builder.
    /// </summary>
    /// <param name="builder">The options builder.</param>
    /// <param name="configureOptions">Optional action used to configure <see cref="CloudStorageOptions" />.</param>
    /// <returns>The same builder instance for chaining.</returns>
    /// <example>
    /// <code>
    /// optionsBuilder.UseCloudStorageOrm(o =&gt;
    /// {
    ///     o.Provider = CloudStorageORM.Enums.CloudProvider.Aws;
    ///     o.ContainerName = "app-data";
    ///     o.Aws.Region = "us-east-1";
    ///     o.Aws.AccessKeyId = "test";
    ///     o.Aws.SecretAccessKey = "test";
    /// });
    /// </code>
    /// </example>
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