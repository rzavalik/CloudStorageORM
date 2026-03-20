namespace CloudStorageORM.Options;

public class CloudStorageAwsOptions
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
    public bool ForcePathStyle { get; set; } = true;
}