using CloudStorageORM.Enums;
using CloudStorageORM.Observability;

namespace CloudStorageORM.Options;

/// <summary>
/// Root configuration model used by <c>UseCloudStorageOrm(...)</c> and DI registration.
/// </summary>
public class CloudStorageOptions
{
    /// <summary>
    /// Gets or sets the selected cloud provider.
    /// </summary>
    public CloudProvider Provider { get; set; }

    /// <summary>
    /// Gets or sets the container or bucket name used for object storage.
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets retry settings for shared provider I/O boundaries.
    /// </summary>
    public CloudStorageRetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Gets or sets Azure-specific options.
    /// </summary>
    public CloudStorageAzureOptions Azure { get; set; } = new();

    /// <summary>
    /// Gets or sets AWS-specific options.
    /// </summary>
    public CloudStorageAwsOptions Aws { get; set; } = new();

    /// <summary>
    /// Gets or sets observability options.
    /// </summary>
    public CloudStorageOrmObservabilityOptions Observability { get; set; } = new();
}