namespace CloudStorageORM.Options;

/// <summary>
/// Azure Blob Storage-specific configuration.
/// </summary>
public class CloudStorageAzureOptions
{
    /// <summary>
    /// Gets or sets the Azure Storage connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}