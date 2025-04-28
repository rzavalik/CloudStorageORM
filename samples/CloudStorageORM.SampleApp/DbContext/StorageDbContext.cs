namespace SampleApp.DbContext
{
    using CloudStorageORM.DbContext;
    using CloudStorageORM.Enums;
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using CloudStorageORM.StorageProviders;
    using Microsoft.EntityFrameworkCore;
    using SampleApp.Models;

    public class StorageDbContext : DbContext
    {
        private readonly IStorageProvider _storageProvider;

        public DbSet<User> Users => Set<User>();

        public StorageDbContext(
            DbContextOptions<CloudStorageDbContext> options,
            CloudStorageOptions storageOptions)
            : base(options)
        {
            _storageProvider = CreateStorageProvider(storageOptions);
        }

        private IStorageProvider CreateStorageProvider(CloudStorageOptions storageOptions)
        {
            return storageOptions.Provider switch
            {
                CloudProvider.Azure => new AzureBlobStorageProvider(storageOptions),
                // Add more providers as needed (e.g., Amazon S3, Google Cloud Storage)
                _ => throw new NotSupportedException($"Cloud provider {storageOptions.Provider} is not supported yet.")
            };
        }
    }
}