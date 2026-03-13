namespace CloudStorageORM.Providers
{
    using Azure.StorageProviders;
    using Enums;
    using Interfaces.StorageProviders;
    using Options;

    public static class ProviderFactory
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
