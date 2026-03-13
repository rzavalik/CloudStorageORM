using Azure.Storage.Blobs;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Options;
using CloudStorageORM.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace CloudStorageORM.Extensions;

public static class CloudStorageOrmServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkCloudStorageOrm(
        this IServiceCollection services,
        CloudStorageOptions storageOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(storageOptions);

        services.AddSingleton(storageOptions);
        services.AddSingleton(_ => string.IsNullOrEmpty(storageOptions.ConnectionString)
            ? throw new InvalidOperationException("CloudStorageOptions.ConnectionString must be provided.")
            : new BlobServiceClient(storageOptions.ConnectionString));
        services.AddSingleton<IStorageProvider>(_ => ProviderFactory.GetStorageProvider(storageOptions));

        return services;
    }
}
