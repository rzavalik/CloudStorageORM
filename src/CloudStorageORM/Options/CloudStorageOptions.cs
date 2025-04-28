namespace CloudStorageORM.Options
{
    public class CloudStorageOptions
    {
        public Enums.CloudProvider Provider { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
    }
}
