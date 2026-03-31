using CloudStorageORM.Enums;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Options;
using CloudStorageORM.Providers.Aws.StorageProviders;
using CloudStorageORM.Providers.Azure.StorageProviders;

namespace CloudStorageORM.Providers;

public static class ProviderFactory
{
    public static IStorageProvider GetStorageProvider(CloudStorageOptions options)
    {
        return options.Provider switch
        {
            CloudProvider.Azure => new AzureBlobStorageProvider(options),
            CloudProvider.Aws => new AwsS3StorageProvider(options),
            _ => throw new NotSupportedException($"Provider {options.Provider} not supported.")
        };
    }
}