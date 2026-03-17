using Azure.Storage.Blobs;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Options;
using CloudStorageORM.Providers;
using CloudStorageORM.Validators;
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