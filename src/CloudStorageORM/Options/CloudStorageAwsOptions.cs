namespace CloudStorageORM.Options;

/// <summary>
/// AWS S3-specific configuration.
/// </summary>
public class CloudStorageAwsOptions
{
    /// <summary>
    /// Gets or sets the AWS access key identifier.
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS secret access key.
    /// </summary>
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS region name.
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the S3 service URL, typically used for LocalStack or other S3-compatible endpoints.
    /// </summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether path-style access should be used.
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;
}