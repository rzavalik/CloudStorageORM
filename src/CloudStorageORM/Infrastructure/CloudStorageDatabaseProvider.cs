using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace CloudStorageORM.Infrastructure;

public class CloudStorageDatabaseProvider : IDatabaseProvider
{
    public string Name => "CloudStorageORM.Provider";

    public bool IsConfigured(IDbContextOptions options)
         => true;

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