using CloudStorageORM.Enums;
using CloudStorageORM.Options;

namespace CloudStorageORM.Validators;

public static class CloudStorageOptionsValidator
{
    public static void Validate(CloudStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ContainerName))
        {
            throw new InvalidOperationException("CloudStorageOptions.ContainerName must be provided.");
        }

        switch (options.Provider)
        {
            case CloudProvider.Azure:
                ValidateAzureOptions(options);
                break;
            case CloudProvider.Aws:
                ValidateAwsOptions(options);
                break;
            default:
                throw new NotSupportedException($"Provider {options.Provider} not supported.");
        }
    }

    private static void ValidateAzureOptions(CloudStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Azure.ConnectionString))
        {
            throw new InvalidOperationException("CloudStorageOptions.Azure.ConnectionString must be provided.");
        }
    }

    private static void ValidateAwsOptions(CloudStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Aws.AccessKeyId))
        {
            throw new InvalidOperationException("CloudStorageOptions.Aws.AccessKeyId must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.Aws.SecretAccessKey))
        {
            throw new InvalidOperationException("CloudStorageOptions.Aws.SecretAccessKey must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.Aws.Region))
        {
            throw new InvalidOperationException("CloudStorageOptions.Aws.Region must be provided.");
        }
    }
}