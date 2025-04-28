namespace SampleApp.Configuration
{
    using Microsoft.EntityFrameworkCore;
    using CloudStorageORM.Options;
    using CloudStorageORM.Extensions;

    public class CloudStorageOrmConfiguration : IStorageConfigurationProvider
    {
        private readonly CloudStorageOptions _options;

        public CloudStorageOrmConfiguration(CloudStorageOptions options)
        {
            _options = options;
        }

        public void Configure(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseCloudStorageORM(options =>
            {
                options.Provider = _options.Provider;
                options.ConnectionString = _options.ConnectionString;
                options.ContainerName = _options.ContainerName;
            });
        }
    }
}