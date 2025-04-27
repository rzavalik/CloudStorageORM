namespace CloudStorageORM.Options
{
    public class CloudStorageOptions
    {
        public Enums.CloudProvider Provider { get; set; }
        public string ConnectionString { get; set; }
        public string ContainerName { get; set; }
    }
}
