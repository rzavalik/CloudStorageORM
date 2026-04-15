using Azure.Storage.Blobs;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Options;
using CloudStorageORM.Providers;
using CloudStorageORM.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace CloudStorageORM.Extensions;

/// <summary>
/// Service collection extensions for registering CloudStorageORM services.
/// </summary>
public static class CloudStorageOrmServiceCollectionExtensions
{
    /// <summary>
    /// Registers CloudStorageORM services and the configured storage provider in dependency injection.
    /// </summary>
    /// <param name="services">Service collection receiving CloudStorageORM registrations.</param>
    /// <param name="storageOptions">Configured CloudStorageORM options to validate and register.</param>
    /// <returns>The same <see cref="IServiceCollection" /> instance for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEntityFrameworkCloudStorageOrm(new CloudStorageOptions
    /// {
    ///     Provider = CloudStorageORM.Enums.CloudProvider.Azure,
    ///     ContainerName = "app-data",
    ///     Azure = { ConnectionString = "UseDevelopmentStorage=true" }
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEntityFrameworkCloudStorageOrm(
        this IServiceCollection services,
        CloudStorageOptions storageOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(storageOptions);

        CloudStorageOptionsValidator.Validate(storageOptions);

        services.AddSingleton(storageOptions);

        if (storageOptions.Provider == Enums.CloudProvider.Azure)
        {
            services.AddSingleton(_ => new BlobServiceClient(storageOptions.Azure.ConnectionString));
        }

        services.AddSingleton<IStorageProvider>(_ => ProviderFactory.GetStorageProvider(storageOptions));

        return services;
    }
}