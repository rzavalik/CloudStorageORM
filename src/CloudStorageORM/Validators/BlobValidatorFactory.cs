namespace CloudStorageORM.Validators
{
    using Enums;
    using Interfaces.Validators;
    using Providers.Azure.Validators;

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