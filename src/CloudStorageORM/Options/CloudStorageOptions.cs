using CloudStorageORM.Enums;

namespace CloudStorageORM.Options;

public class CloudStorageOptions
{
    public CloudProvider Provider { get; set; }
    public string ContainerName { get; set; } = string.Empty;
    public CloudStorageAzureOptions Azure { get; set; } = new();
    public CloudStorageAwsOptions Aws { get; set; } = new();
}