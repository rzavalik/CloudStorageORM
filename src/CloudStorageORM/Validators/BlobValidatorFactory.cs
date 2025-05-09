namespace CloudStorageORM.Validators
{
    using CloudStorageORM.Enums;
    using CloudStorageORM.Interfaces.Validators;
    using CloudStorageORM.Providers.Azure.Validators;

    public static class BlobValidatorFactory
    {
        public static IBlobValidator Create(CloudProvider provider)
        {
            return provider switch
            {
                CloudProvider.Azure => new AzureBlobValidator(),
                _ => throw new NotSupportedException($"Provider {provider} not supported")
            };
        }
    }
}