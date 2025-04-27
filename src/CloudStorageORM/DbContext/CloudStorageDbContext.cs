namespace CloudStorageORM.DbContext
{
    using CloudStorageORM.Interfaces.StorageProviders;
    using CloudStorageORM.Options;
    using CloudStorageORM.Repositories;
    using System.Threading.Tasks;

    public class CloudStorageDbContext
    {
        private readonly CloudStorageOptions _options;
        private readonly IStorageProvider _storageProvider;

        public CloudStorageDbContext(CloudStorageOptions options, IStorageProvider storageProvider)
        {
            _options = options;
            _storageProvider = storageProvider;
        }

        public CloudStorageRepository<TEntity> Set<TEntity>() where TEntity : class
        {
            return new CloudStorageRepository<TEntity>(_storageProvider);
        }

        public Task<int> SaveChangesAsync()
        {
            return Task.FromResult(0);
        }
    }
}