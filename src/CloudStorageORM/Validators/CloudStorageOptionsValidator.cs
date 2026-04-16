using CloudStorageORM.Enums;
using CloudStorageORM.Options;

namespace CloudStorageORM.Validators;

/// <summary>
/// Validates CloudStorageORM configuration options before provider initialization.
/// </summary>
public static class CloudStorageOptionsValidator
{
    /// <summary>
    /// Validates common and provider-specific option fields.
    /// </summary>
    /// <param name="options">Options instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing for the selected provider.</exception>
    /// <exception cref="NotSupportedException">Thrown when the provider is not supported.</exception>
    /// <example>
    /// <code>
    /// CloudStorageOptionsValidator.Validate(new CloudStorageORM.Options.CloudStorageOptions
    /// {
    ///     Provider = CloudStorageORM.Enums.CloudProvider.Aws,
    ///     ContainerName = "app-data",
    ///     Aws = { Region = "us-east-1", AccessKeyId = "test", SecretAccessKey = "test" }
    /// });
    /// </code>
    /// </example>
    public static void Validate(CloudStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ContainerName))
        {
            throw new InvalidOperationException("CloudStorageOptions.ContainerName must be provided.");
        }

        ValidateRetryOptions(options);

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

    private static void ValidateRetryOptions(CloudStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.Retry);

        if (options.Retry.MaxRetries < 0)
        {
            throw new InvalidOperationException(
                "CloudStorageOptions.Retry.MaxRetries must be greater than or equal to zero.");
        }

        if (options.Retry.BaseDelay < TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "CloudStorageOptions.Retry.BaseDelay must be greater than or equal to zero.");
        }

        if (options.Retry.MaxDelay < TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "CloudStorageOptions.Retry.MaxDelay must be greater than or equal to zero.");
        }

        if (options.Retry.MaxDelay < options.Retry.BaseDelay)
        {
            throw new InvalidOperationException(
                "CloudStorageOptions.Retry.MaxDelay must be greater than or equal to BaseDelay.");
        }

        if (options.Retry.JitterFactor is < 0d or > 1d)
        {
            throw new InvalidOperationException("CloudStorageOptions.Retry.JitterFactor must be between 0 and 1.");
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