namespace CloudStorageORM.Validators
{
    using CloudStorageORM.Azure.Validators;
    using CloudStorageORM.Enums;
    using CloudStorageORM.Interfaces.Validators;

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