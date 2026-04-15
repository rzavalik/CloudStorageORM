using CloudStorageORM.Enums;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Options;
using CloudStorageORM.Providers.Aws.StorageProviders;
using CloudStorageORM.Providers.Azure.StorageProviders;

namespace CloudStorageORM.Providers;

/// <summary>
/// Creates storage provider implementations from validated CloudStorageORM options.
/// </summary>
public static class ProviderFactory
{
    /// <summary>
    /// Resolves the correct <see cref="IStorageProvider" /> implementation for the configured cloud provider.
    /// </summary>
    /// <param name="options">Validated CloudStorageORM options that describe which provider to create.</param>
    /// <returns>A storage provider instance matching <paramref name="options" />.</returns>
    /// <exception cref="NotSupportedException">Thrown when the configured provider is not supported.</exception>
    /// <example>
    /// <code>
    /// var provider = ProviderFactory.GetStorageProvider(options);
    /// </code>
    /// </example>
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