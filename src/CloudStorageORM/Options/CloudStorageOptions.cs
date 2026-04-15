using CloudStorageORM.Enums;
using CloudStorageORM.Observability;

namespace CloudStorageORM.Options;

public class CloudStorageOptions
{
    public CloudProvider Provider { get; set; }
    public string ContainerName { get; set; } = string.Empty;
    public CloudStorageAzureOptions Azure { get; set; } = new();
    public CloudStorageAwsOptions Aws { get; set; } = new();
    public CloudStorageOrmObservabilityOptions Observability { get; set; } = new();
}