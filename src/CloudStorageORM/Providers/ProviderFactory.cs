namespace CloudStorageORM.Providers
{
    using CloudStorageORM.Enums;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using CloudStorageORM.Providers.Azure.StorageProviders;

    internal static class ProviderFactory
    {
        public static IStorageProvider GetStorageProvider(CloudStorageOptions options)
        {
            return options.Provider switch
            {
                CloudProvider.Azure => new AzureBlobStorageProvider(options),
                _ => throw new NotSupportedException($"Provider {options.Provider} not supported.")
            };
        }
    }
}
