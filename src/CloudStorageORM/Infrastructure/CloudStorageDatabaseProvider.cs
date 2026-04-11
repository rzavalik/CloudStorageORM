using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// EF Core database-provider descriptor for CloudStorageORM.
/// </summary>
public class CloudStorageDatabaseProvider : IDatabaseProvider
{
    /// <inheritdoc />
    public string Name => "CloudStorageORM.Provider";

    /// <inheritdoc />
    public bool IsConfigured(IDbContextOptions options)
         => true;

    /// <summary>
    /// Creates the provider-specific <see cref="IDatabase" /> instance from EF Core facade dependencies.
    /// </summary>
    /// <param name="dependencies">Resolved EF Core database-facade dependencies.</param>
    /// <returns>A configured <see cref="CloudStorageDatabase" /> instance.</returns>
    /// <example>
    /// <code>
    /// var database = CloudStorageDatabaseProvider.Create(dependencies);
    /// </code>
    /// </example>
    public static IDatabase Create(IDatabaseFacadeDependencies dependencies)
    {
        var databaseCreator = dependencies.DatabaseCreator;
        var executionStrategyFactory = dependencies.ExecutionStrategyFactory;
        //var concurrencyDetector = dependencies.ConcurrencyDetector;

        // ReSharper disable once SuspiciousTypeConversion.Global
        var serviceProvider = ((IInfrastructure<IServiceProvider>)dependencies).Instance;

        var storageProvider = serviceProvider.GetRequiredService<IStorageProvider>();
        //var cloudOptions = serviceProvider?.GetRequiredService<CloudStorageOptions>();
        var model = serviceProvider.GetRequiredService<IModel>();
        var currentDbContext = serviceProvider.GetRequiredService<ICurrentDbContext>();
        var blobPathResolver = serviceProvider.GetRequiredService<IBlobPathResolver>();
        var transactionManager = serviceProvider.GetRequiredService<IDbContextTransactionManager>();

        return new CloudStorageDatabase(
            model,
            databaseCreator,
            executionStrategyFactory,
            storageProvider,
            currentDbContext,
            blobPathResolver,
            transactionManager);
    }
}