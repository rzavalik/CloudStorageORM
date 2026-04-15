using CloudStorageORM.Enums;
using CloudStorageORM.Interfaces.Validators;
using CloudStorageORM.Providers.Aws.Validators;
using CloudStorageORM.Providers.Azure.Validators;

namespace CloudStorageORM.Validators;

/// <summary>
/// Creates provider-specific blob-name validators.
/// </summary>
public static class BlobValidatorFactory
{
    /// <summary>
    /// Creates a blob validator for the given cloud provider.
    /// </summary>
    /// <param name="provider">Cloud provider that determines validation rules.</param>
    /// <returns>A provider-specific implementation of <see cref="IBlobValidator" />.</returns>
    /// <exception cref="NotSupportedException">Thrown when the provider is not supported.</exception>
    /// <example>
    /// <code>
    /// var validator = BlobValidatorFactory.Create(CloudStorageORM.Enums.CloudProvider.Azure);
    /// var isValid = validator.IsBlobNameValid("users/42.json");
    /// </code>
    /// </example>
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