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

/// <summary>
/// EF Core options extension that wires CloudStorageORM services and settings.
/// </summary>
public class CloudStorageOrmOptionsExtension : IDbContextOptionsExtension
{
    /// <summary>
    /// Creates a new CloudStorageORM options extension.
    /// </summary>
    /// <param name="options">Cloud storage options used to configure provider services.</param>
    /// <example>
    /// <code>
    /// var extension = new CloudStorageOrmOptionsExtension(options);
    /// </code>
    /// </example>
    public CloudStorageOrmOptionsExtension(CloudStorageOptions options)
    {
        Options = options;
        Info = new CloudStorageOrmOptionsExtensionInfo(this);
    }

    public CloudStorageOptions Options { get; }

    /// <inheritdoc />
    public DbContextOptionsExtensionInfo Info { get; }

    /// <inheritdoc />
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
        services.AddScoped<IDbContextTransactionManager, CloudStorageTransactionManager>();
        services.AddSingleton<IDatabaseCreator, CloudStorageDatabaseCreator>();
        services.AddSingleton<IModelSource, ModelSource>();
        services.AddSingleton<IModelRuntimeInitializer, ModelRuntimeInitializer>();
        services.AddSingleton<IDbSetInitializer, DbSetInitializer>();
        services.AddSingleton<ISingletonOptionsInitializer, CloudStorageSingletonOptionsInitializer>();
        services.AddSingleton<IDatabaseProvider, CloudStorageDatabaseProvider>();

        services.AddEntityFrameworkCloudStorageOrm(Options);
    }

    /// <inheritdoc />
    public void Validate(IDbContextOptions options)
    {
        CloudStorageOptionsValidator.Validate(Options);
    }
}