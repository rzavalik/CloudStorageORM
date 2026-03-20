using Azure.Storage.Blobs;
using CloudStorageORM.Enums;
using CloudStorageORM.Extensions;
using CloudStorageORM.Interfaces.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Options;
using CloudStorageORM.Providers;
using CloudStorageORM.Validators;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CloudStorageORM.Infrastructure;

public class CloudStorageOrmOptionsExtension : IDbContextOptionsExtension
{
    public CloudStorageOrmOptionsExtension(CloudStorageOptions options)
    {
        Options = options;
        Info = new CloudStorageOrmOptionsExtensionInfo(this);
    }

    public CloudStorageOptions Options { get; }

    public DbContextOptionsExtensionInfo Info { get; }

    public void ApplyServices(IServiceCollection services)
    {
        CloudStorageOptionsValidator.Validate(Options);

        services.AddSingleton(Options);

        new EntityFrameworkServicesBuilder(services).TryAddCoreServices();

        services.AddSingleton<IStorageProvider>(_ => ProviderFactory.GetStorageProvider(Options));

        if (Options.Provider == CloudProvider.Azure)
        {
            services.TryAddSingleton(_ => new BlobServiceClient(Options.Azure.ConnectionString));
        }

        services.AddScoped<IDatabase, CloudStorageDatabase>();
        services.AddScoped<LoggingDefinitions, CloudStorageLoggingDefinitions>();
        services.AddScoped<IQueryContextFactory, CloudStorageQueryContextFactory>();

        services.AddSingleton<IBlobPathResolver, BlobPathResolver>();
        services.AddSingleton<ITypeMappingSource, CloudStorageTypeMappingSource>();
        services.AddSingleton<IDbContextTransactionManager, CloudStorageTransactionManager>();
        services.AddSingleton<IDatabaseCreator, CloudStorageDatabaseCreator>();
        services.AddSingleton<IModelSource, ModelSource>();
        services.AddSingleton<IModelRuntimeInitializer, ModelRuntimeInitializer>();
        services.AddSingleton<IDbSetInitializer, DbSetInitializer>();
        services.AddSingleton<ISingletonOptionsInitializer, CloudStorageSingletonOptionsInitializer>();
        services.AddSingleton<IDatabaseProvider, CloudStorageDatabaseProvider>();

        services.AddEntityFrameworkCloudStorageOrm(Options);
    }

    public void Validate(IDbContextOptions options)
    {
        CloudStorageOptionsValidator.Validate(Options);
    }
}