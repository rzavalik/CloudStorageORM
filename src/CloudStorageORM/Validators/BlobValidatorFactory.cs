using CloudStorageORM.Enums;
using CloudStorageORM.Interfaces.Validators;
using CloudStorageORM.Providers.Aws.Validators;
using CloudStorageORM.Providers.Azure.Validators;

namespace CloudStorageORM.Validators;

public static class BlobValidatorFactory
{
    public static IBlobValidator Create(CloudProvider provider)
    {
        return provider switch
        {
            CloudProvider.Azure => new AzureBlobValidator(),
            CloudProvider.Aws => new AwsBlobValidator(),
            _ => throw new NotSupportedException($"Provider {provider} not supported")
        };
    }
}