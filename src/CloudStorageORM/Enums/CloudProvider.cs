namespace CloudStorageORM.Enums;

/// <summary>
/// Supported cloud object-storage providers.
/// </summary>
public enum CloudProvider
{
    /// <summary>
    /// Azure Blob Storage.
    /// </summary>
    Azure,

    /// <summary>
    /// AWS S3.
    /// </summary>
    Aws,

    /// <summary>
    /// Google Cloud Storage (planned, not yet implemented).
    /// </summary>
    Gcp
}